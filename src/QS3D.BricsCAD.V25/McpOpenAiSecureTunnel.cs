using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace QS3D.BricsCAD.V25
{
    internal enum McpTransportProvider
    {
        OpenAiSecureTunnel,
        CloudflareNamedTunnel,
        CloudflareQuickTunnel
    }

    /// <summary>
    /// Chooses exactly one user-facing MCP transport path. New installs prefer OpenAI Secure MCP
    /// Tunnel because it needs no user-owned public hostname. Existing Named Tunnel users keep
    /// their current behavior until they explicitly switch providers in Agent Center.
    /// </summary>
    internal static class McpTransportCoordinator
    {
        private static readonly object Sync = new object();
        private static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "MCP", "Transport");
        private static string ProviderPath => Path.Combine(SettingsDirectory, "provider.txt");
        private static string RegistrationPath => Path.Combine(SettingsDirectory, "chatgpt-registration.txt");

        public static McpTransportProvider SelectedProvider
        {
            get
            {
                lock (Sync) return LoadProvider();
            }
        }

        public static string SelectedProviderLabel
        {
            get
            {
                switch (SelectedProvider)
                {
                    case McpTransportProvider.CloudflareNamedTunnel: return "Cloudflare Named Tunnel";
                    case McpTransportProvider.CloudflareQuickTunnel: return "Cloudflare Quick Tunnel · test only";
                    default: return "OpenAI Secure MCP Tunnel";
                }
            }
        }

        public static void SetSelectedProvider(McpTransportProvider provider)
        {
            lock (Sync)
            {
                var previous = LoadProvider();
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(ProviderPath, provider.ToString(), new UTF8Encoding(false));
                if (previous != provider) ForgetChatGptRegistrationAcknowledgement();
            }
        }

        public static void TryAutoStartPreferred()
        {
            switch (SelectedProvider)
            {
                case McpTransportProvider.CloudflareNamedTunnel:
                    McpCloudflareAccountTunnelManager.TryAutoStart();
                    break;
                case McpTransportProvider.CloudflareQuickTunnel:
                    // Quick Tunnel hostnames rotate and are intentionally never auto-started.
                    break;
                default:
                    McpOpenAiSecureTunnelManager.TryAutoStart();
                    break;
            }
        }

        public static bool IsChatGptRegistrationAcknowledged()
        {
            var identity = CurrentRegistrationIdentity();
            if (string.IsNullOrWhiteSpace(identity)) return false;
            try
            {
                if (!File.Exists(RegistrationPath)) return false;
                return string.Equals(File.ReadAllText(RegistrationPath, Encoding.UTF8).Trim(), identity, StringComparison.Ordinal);
            }
            catch { return false; }
        }

        public static void MarkChatGptRegistrationAcknowledged()
        {
            var identity = CurrentRegistrationIdentity();
            if (string.IsNullOrWhiteSpace(identity))
            {
                if (SelectedProvider == McpTransportProvider.OpenAiSecureTunnel)
                    throw new InvalidOperationException("Chưa có OpenAI Tunnel ID hợp lệ để ghi nhận kết nối ChatGPT.");
                throw new InvalidOperationException("Chưa có public MCP URL để ghi nhận kết nối ChatGPT.");
            }
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(RegistrationPath, identity, new UTF8Encoding(false));
        }

        public static void ForgetChatGptRegistrationAcknowledgement()
        {
            try { if (File.Exists(RegistrationPath)) File.Delete(RegistrationPath); } catch { }
        }

        public static void StopAllForHostShutdown()
        {
            McpOpenAiSecureTunnelManager.StopForHostShutdown();
            McpCloudflareAccountTunnelManager.StopForHostShutdown();
            McpCloudflareTunnelManager.StopForHostShutdown();
        }

        private static McpTransportProvider LoadProvider()
        {
            try
            {
                if (File.Exists(ProviderPath))
                {
                    McpTransportProvider parsed;
                    if (Enum.TryParse(File.ReadAllText(ProviderPath, Encoding.UTF8).Trim(), true, out parsed))
                        return parsed;
                }
            }
            catch { }

            // Preserve existing production Named Tunnel users during upgrade. A clean install has
            // no saved hostname and therefore starts on the no-domain OpenAI path.
            try
            {
                if (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.SavedHostname))
                    return McpTransportProvider.CloudflareNamedTunnel;
            }
            catch { }
            return McpTransportProvider.OpenAiSecureTunnel;
        }

        private static string CurrentRegistrationIdentity()
        {
            switch (SelectedProvider)
            {
                case McpTransportProvider.OpenAiSecureTunnel:
                    var tunnelId = McpOpenAiSecureTunnelManager.SavedTunnelId;
                    return McpOpenAiSecureTunnelManager.IsValidTunnelId(tunnelId) ? "openai:" + tunnelId : string.Empty;
                case McpTransportProvider.CloudflareNamedTunnel:
                case McpTransportProvider.CloudflareQuickTunnel:
                    var publicUrl = McpPublicEndpointResolver.Resolve();
                    return string.IsNullOrWhiteSpace(publicUrl) ? string.Empty : "cloudflare:" + publicUrl.Trim().ToLowerInvariant();
                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>
    /// Process supervisor for the official OpenAI Secure MCP Tunnel client. QS3D persists only
    /// non-secret path/id/config metadata. Runtime API key and the local QS3D bearer are injected
    /// into the child process environment and are referenced from YAML via env: variables.
    /// </summary>
    internal static class McpOpenAiSecureTunnelManager
    {
        private const string PlatformTunnelsUrl = "https://platform.openai.com/settings/organization/tunnels";
        private const string RuntimeKeysUrl = "https://platform.openai.com/settings/organization/api-keys";
        private const string TunnelClientReleaseUrl = "https://github.com/openai/tunnel-client/releases/latest";
        private const string ChatGptConnectorsUrl = "https://chatgpt.com/#settings/Connectors";
        private const string ControlPlaneApiKeyEnvironment = "CONTROL_PLANE_API_KEY";
        private const string OpenAiApiKeyEnvironment = "OPENAI_API_KEY";
        private const string LocalBearerEnvironment = "QS3D_TUNNEL_MCP_AUTH";
        private static readonly Regex TunnelIdRegex = new Regex(
            "^tunnel_[0-9a-f]{32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly object Sync = new object();

        private static Process? _process;
        private static bool _stopping;
        private static string _lastError = string.Empty;
        private static DateTime _lastReadyProbeUtc = DateTime.MinValue;
        private static bool _lastReady;

        private static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "MCP", "OpenAiSecureTunnel");
        private static string ClientPathFile => Path.Combine(SettingsDirectory, "tunnel-client-path.txt");
        private static string TunnelIdFile => Path.Combine(SettingsDirectory, "tunnel-id.txt");
        private static string AutoStartFile => Path.Combine(SettingsDirectory, "autostart.txt");
        private static string ConfigPath => Path.Combine(SettingsDirectory, "tunnel-client.yaml");
        private static string HealthUrlPath => Path.Combine(SettingsDirectory, "health-url.txt");

        public static string SavedClientPath => ReadText(ClientPathFile);
        public static string SavedTunnelId => ReadText(TunnelIdFile);
        public static string LastError { get { lock (Sync) return _lastError; } }

        public static bool IsConfigured => IsValidTunnelId(SavedTunnelId) && IsUsableClientPath(SavedClientPath);

        public static bool IsRunning
        {
            get
            {
                lock (Sync)
                {
                    if (_process == null) return false;
                    try { return !_process.HasExited; } catch { return false; }
                }
            }
        }

        public static bool IsReady
        {
            get
            {
                if (!IsRunning) return false;
                lock (Sync)
                {
                    if (DateTime.UtcNow - _lastReadyProbeUtc < TimeSpan.FromSeconds(2)) return _lastReady;
                }
                var ready = ProbeReady();
                lock (Sync)
                {
                    _lastReadyProbeUtc = DateTime.UtcNow;
                    _lastReady = ready;
                }
                return ready;
            }
        }

        public static string HealthBaseUrl
        {
            get
            {
                var value = ReadText(HealthUrlPath).TrimEnd('/');
                Uri uri;
                if (!Uri.TryCreate(value, UriKind.Absolute, out uri)) return string.Empty;
                if (!uri.IsLoopback || !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)) return string.Empty;
                return uri.GetLeftPart(UriPartial.Authority);
            }
        }

        public static bool IsValidTunnelId(string value)
        {
            return TunnelIdRegex.IsMatch((value ?? string.Empty).Trim());
        }

        public static bool SaveClientPath(string path, out string message)
        {
            message = string.Empty;
            var fullPath = NormalizeClientPath(path);
            if (!IsUsableClientPath(fullPath))
            {
                message = "Hãy chọn file tunnel-client.exe chính thức đã tải từ OpenAI.";
                return false;
            }
            string version;
            if (!TryReadVersion(fullPath, out version))
            {
                message = "File đã chọn không chạy được như tunnel-client. Hãy tải lại bản chính thức từ OpenAI.";
                return false;
            }
            Directory.CreateDirectory(SettingsDirectory);
            WriteText(ClientPathFile, fullPath);
            message = "Đã chọn tunnel-client" + (string.IsNullOrWhiteSpace(version) ? "." : "; " + version + ".");
            return true;
        }

        public static bool Start(string tunnelId, string runtimeApiKey, out string message)
        {
            message = string.Empty;
            McpEmbeddedServer.EnsureStarted();

            var normalizedTunnelId = (tunnelId ?? string.Empty).Trim();
            if (!IsValidTunnelId(normalizedTunnelId))
            {
                message = "OpenAI Tunnel ID không hợp lệ. Dạng yêu cầu: tunnel_ + 32 ký tự hex chữ thường.";
                return false;
            }

            var clientPath = NormalizeClientPath(SavedClientPath);
            if (!IsUsableClientPath(clientPath))
            {
                message = "Chưa chọn tunnel-client.exe chính thức.";
                return false;
            }

            var key = ResolveRuntimeApiKey(runtimeApiKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                message = "Thiếu OpenAI Runtime API key. Nhập key cho phiên này hoặc đặt CONTROL_PLANE_API_KEY/OPENAI_API_KEY trong môi trường Windows.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                WriteText(TunnelIdFile, normalizedTunnelId);
                WriteRuntimeConfig(normalizedTunnelId, McpEmbeddedServer.Endpoint);
                try { if (File.Exists(HealthUrlPath)) File.Delete(HealthUrlPath); } catch { }

                // One selected transport should own external reachability at a time.
                McpCloudflareAccountTunnelManager.StopForHostShutdown();
                McpCloudflareTunnelManager.StopForHostShutdown();
                StopProcessOnly();

                var startInfo = new ProcessStartInfo
                {
                    FileName = clientPath,
                    Arguments = "run --config \"" + ConfigPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(clientPath) ?? SettingsDirectory
                };
                startInfo.EnvironmentVariables[ControlPlaneApiKeyEnvironment] = key;
                startInfo.EnvironmentVariables["CONTROL_PLANE_TUNNEL_ID"] = normalizedTunnelId;
                startInfo.EnvironmentVariables["MCP_SERVER_URL"] = McpEmbeddedServer.Endpoint.ToString();
                startInfo.EnvironmentVariables[LocalBearerEnvironment] = "Bearer " + McpEmbeddedServer.GetBearerToken();
                startInfo.EnvironmentVariables["HEALTH_LISTEN_ADDR"] = "127.0.0.1:0";
                startInfo.EnvironmentVariables["HEALTH_URL_FILE"] = HealthUrlPath;
                AddNoProxyLoopback(startInfo);

                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.Exited += (_, __) => HandleProcessExit(process);
                if (!process.Start())
                {
                    process.Dispose();
                    message = "Không khởi động được OpenAI tunnel-client.";
                    return false;
                }

                lock (Sync)
                {
                    _stopping = false;
                    _process = process;
                    _lastError = string.Empty;
                    _lastReady = false;
                    _lastReadyProbeUtc = DateTime.MinValue;
                }
                try
                {
                    if (process.HasExited)
                    {
                        HandleProcessExit(process);
                        message = string.IsNullOrWhiteSpace(LastError)
                            ? "OpenAI tunnel-client đã dừng ngay sau khi khởi động."
                            : LastError;
                        return false;
                    }
                }
                catch (InvalidOperationException)
                {
                    message = "Không đọc được trạng thái OpenAI tunnel-client sau khi khởi động.";
                    StopProcessOnly();
                    return false;
                }

                WriteText(AutoStartFile, "1");
                McpTransportCoordinator.SetSelectedProvider(McpTransportProvider.OpenAiSecureTunnel);
                message = "OpenAI Secure MCP Tunnel đang khởi động. QS3D không lưu Runtime API key; chờ trạng thái READY rồi kết nối ChatGPT bằng Connection = Tunnel.";
                return true;
            }
            catch (Exception ex)
            {
                SetLastError("OpenAI Secure MCP Tunnel: " + ex.Message);
                message = LastError;
                return false;
            }
        }

        public static void TryAutoStart()
        {
            if (ReadText(AutoStartFile) != "1" || !IsConfigured) return;
            var key = ResolveRuntimeApiKey(string.Empty);
            if (string.IsNullOrWhiteSpace(key))
            {
                SetLastError("OpenAI Secure MCP Tunnel chưa tự khởi động vì Runtime API key không được lưu. Đặt CONTROL_PLANE_API_KEY hoặc khởi động từ Agent Center.");
                return;
            }
            string ignored;
            Start(SavedTunnelId, key, out ignored);
        }

        public static void Stop()
        {
            WriteText(AutoStartFile, "0");
            StopProcessOnly();
        }

        public static void StopForHostShutdown()
        {
            StopProcessOnly();
        }

        public static void OpenPlatformTunnels() => OpenUrl(PlatformTunnelsUrl);
        public static void OpenRuntimeKeys() => OpenUrl(RuntimeKeysUrl);
        public static void OpenTunnelClientDownload() => OpenUrl(TunnelClientReleaseUrl);
        public static void OpenChatGptConnectors() => OpenUrl(ChatGptConnectorsUrl);

        public static bool OpenAdminUi(out string error)
        {
            error = string.Empty;
            var baseUrl = HealthBaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                error = "tunnel-client chưa công bố health/admin URL. Hãy khởi động tunnel và chờ READY.";
                return false;
            }
            try
            {
                OpenUrl(baseUrl.TrimEnd('/') + "/ui");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string Describe()
        {
            return "openai-secure=" + (IsRunning ? (IsReady ? "READY" : "RUNNING") : "STOPPED")
                   + "; configured=" + IsConfigured
                   + (IsValidTunnelId(SavedTunnelId) ? "; tunnelId=" + SavedTunnelId : string.Empty)
                   + (string.IsNullOrWhiteSpace(LastError) ? string.Empty : "; error=" + LastError);
        }

        private static void WriteRuntimeConfig(string tunnelId, Uri localEndpoint)
        {
            var yaml = new StringBuilder();
            yaml.AppendLine("config_version: 1");
            yaml.AppendLine("control_plane:");
            yaml.AppendLine("  tunnel_id: " + YamlQuote(tunnelId));
            yaml.AppendLine("  api_key: env:CONTROL_PLANE_API_KEY");
            yaml.AppendLine("health:");
            yaml.AppendLine("  listen_addr: 127.0.0.1:0");
            yaml.AppendLine("  url_file: " + YamlQuote(HealthUrlPath));
            yaml.AppendLine("admin_ui:");
            yaml.AppendLine("  open_browser: false");
            yaml.AppendLine("mcp:");
            yaml.AppendLine("  server_urls:");
            yaml.AppendLine("    - channel: main");
            yaml.AppendLine("      url: " + YamlQuote(localEndpoint.ToString()));
            yaml.AppendLine("  extra_headers:");
            yaml.AppendLine("    Authorization: env:" + LocalBearerEnvironment);
            yaml.AppendLine("  discovery_extra_headers:");
            yaml.AppendLine("    Authorization: env:" + LocalBearerEnvironment);
            File.WriteAllText(ConfigPath, yaml.ToString(), new UTF8Encoding(false));
        }

        private static bool ProbeReady()
        {
            var baseUrl = HealthBaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl)) return false;
            try
            {
#pragma warning disable SYSLIB0014
                var request = (HttpWebRequest)WebRequest.Create(baseUrl.TrimEnd('/') + "/readyz");
#pragma warning restore SYSLIB0014
                request.Method = "GET";
                request.Timeout = 350;
                request.ReadWriteTimeout = 350;
                request.Proxy = null;
                using (var response = (HttpWebResponse)request.GetResponse())
                    return (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;
            }
            catch { return false; }
        }

        private static void HandleProcessExit(Process process)
        {
            lock (Sync)
            {
                if (!ReferenceEquals(_process, process)) return;
                var intentional = _stopping;
                _process = null;
                _lastReady = false;
                _lastReadyProbeUtc = DateTime.MinValue;
                if (!intentional)
                {
                    try { _lastError = "OpenAI tunnel-client đã dừng (exit=" + process.ExitCode + "). Mở tunnel-client UI/log hoặc chạy lại từ Agent Center."; }
                    catch { _lastError = "OpenAI tunnel-client đã dừng ngoài dự kiến."; }
                }
            }
            try { process.Dispose(); } catch { }
        }

        private static void StopProcessOnly()
        {
            Process? process;
            lock (Sync)
            {
                _stopping = true;
                process = _process;
                _process = null;
                _lastReady = false;
                _lastReadyProbeUtc = DateTime.MinValue;
            }
            if (process != null)
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                try { process.WaitForExit(1500); } catch { }
                try { process.Dispose(); } catch { }
            }
            lock (Sync) _stopping = false;
        }

        private static bool TryReadVersion(string path, out string version)
        {
            version = string.Empty;
            Process? process = null;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                process = Process.Start(startInfo);
                if (process == null) return false;
                if (!process.WaitForExit(5000))
                {
                    try { process.Kill(); } catch { }
                    return false;
                }
                var output = (process.StandardOutput.ReadToEnd() + " " + process.StandardError.ReadToEnd()).Trim();
                if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output)) return false;
                version = output.Length <= 160 ? output : output.Substring(0, 160);
                return true;
            }
            catch { return false; }
            finally { try { if (process != null) process.Dispose(); } catch { } }
        }

        private static string ResolveRuntimeApiKey(string supplied)
        {
            var value = (supplied ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(value)) return value;
            try
            {
                value = (Environment.GetEnvironmentVariable(ControlPlaneApiKeyEnvironment) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(value)) return value;
                return (Environment.GetEnvironmentVariable(OpenAiApiKeyEnvironment) ?? string.Empty).Trim();
            }
            catch { return string.Empty; }
        }

        private static string NormalizeClientPath(string path)
        {
            try { return string.IsNullOrWhiteSpace(path) ? string.Empty : Path.GetFullPath(path.Trim()); }
            catch { return string.Empty; }
        }

        private static bool IsUsableClientPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
                var fileName = Path.GetFileName(path);
                return string.Equals(fileName, "tunnel-client.exe", StringComparison.OrdinalIgnoreCase)
                       || fileName.StartsWith("tunnel-client", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static void AddNoProxyLoopback(ProcessStartInfo startInfo)
        {
            try
            {
                var existing = startInfo.EnvironmentVariables["NO_PROXY"] ?? string.Empty;
                var suffix = "127.0.0.1,localhost";
                startInfo.EnvironmentVariables["NO_PROXY"] = string.IsNullOrWhiteSpace(existing) ? suffix : existing.TrimEnd(',') + "," + suffix;
            }
            catch { }
        }

        private static string YamlQuote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string ReadText(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : string.Empty; }
            catch { return string.Empty; }
        }

        private static void WriteText(string path, string value)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? SettingsDirectory);
                File.WriteAllText(path, value ?? string.Empty, new UTF8Encoding(false));
            }
            catch { }
        }

        private static void SetLastError(string value)
        {
            lock (Sync) _lastError = value ?? string.Empty;
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
