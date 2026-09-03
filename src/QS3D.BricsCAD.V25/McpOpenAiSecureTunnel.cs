using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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
            McpTransportSupervisor.TryAutoStartPreferred(SelectedProvider);
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
            McpTransportSupervisor.StopForHostShutdown();
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
    /// Process supervisor for the official OpenAI Secure MCP Tunnel client. QS3D persists
    /// non-secret path/id/config metadata separately and stores the Runtime API key only in the
    /// current Windows user's Credential Manager after exact read-back verification. The verified
    /// key and local QS3D bearer are injected into the child environment; neither is written to YAML.
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
        private const string LocalTunnelAuthorizationHeader = "X-QS3D-MCP-Local-Authorization";
        private const string ExpectedSha256Environment = "QS3D_OPENAI_TUNNEL_CLIENT_SHA256";
        private const int MaxDiagnosticLines = 80;
        private const int WatchdogPeriodMilliseconds = 5000;
        private const int UnreadyRestartThreshold = 3;
        private const int RestartBackoffBaseSeconds = 5;
        private const int RestartBackoffMaxSeconds = 120;
        private static readonly Regex TunnelIdRegex = new Regex(
            "^tunnel_[0-9a-f]{32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex Sha256Regex = new Regex(
            "^[0-9a-fA-F]{64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ApiKeyRegex = new Regex(
            "\\bsk-[A-Za-z0-9_\\-]{8,}\\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex AuthorizationRegex = new Regex(
            "(?i)(authorization\\s*[:=]\\s*)(bearer\\s+)?[^\\s,;]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly object Sync = new object();
        private static readonly object WatchdogRecoverySync = new object();
        private static readonly List<string> DiagnosticLines = new List<string>();

        private static Process? _process;
        private static Timer? _watchdogTimer;
        private static bool _stopping;
        private static bool _watchdogEnabled;
        private static int _watchdogBusy;
        private static int _consecutiveUnready;
        private static int _restartAttempt;
        private static DateTime _nextRestartUtc = DateTime.MinValue;
        private static string _lastError = string.Empty;
        private static string _clientTrustSummary = string.Empty;
        private static int? _lastExitCode;
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
        public static string ClientTrustSummary { get { lock (Sync) return _clientTrustSummary; } }
        public static int? LastExitCode { get { lock (Sync) return _lastExitCode; } }
        public static string LastDiagnostics
        {
            get
            {
                lock (Sync) return DiagnosticLines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, DiagnosticLines.ToArray());
            }
        }

        public static bool IsConfigured => IsValidTunnelId(SavedTunnelId) && IsUsableClientPath(SavedClientPath);
        internal static Process? OwnedProcess { get { lock (Sync) return _process; } }

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
            string trustSummary;
            string trustError;
            if (!TryVerifyClientTrust(fullPath, out trustSummary, out trustError))
            {
                message = "Không chấp nhận tunnel-client: " + trustError;
                return false;
            }
            try
            {
                WriteTextVerified(ClientPathFile, fullPath);
            }
            catch (Exception ex)
            {
                message = "Không lưu/xác minh được đường dẫn tunnel-client: " + ex.Message;
                SetLastError(message);
                return false;
            }
            lock (Sync) _clientTrustSummary = trustSummary;
            message = "Đã chọn tunnel-client qua trust verification; " + trustSummary + ".";
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
            string trustSummary;
            string trustError;
            if (!TryVerifyClientTrust(clientPath, out trustSummary, out trustError))
            {
                message = "OpenAI tunnel-client không qua trust verification trước khi chạy: " + trustError;
                SetLastError(message);
                return false;
            }

            var suppliedKey = (runtimeApiKey ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(suppliedKey))
            {
                try
                {
                    McpPersistentUserSettings.SaveOpenAiRuntimeApiKey(suppliedKey);
                }
                catch (Exception ex)
                {
                    message = "Không lưu/xác minh được OpenAI Runtime API key an toàn trước khi khởi động tunnel: " + ex.Message;
                    SetLastError(message);
                    return false;
                }
            }

            // A supplied key is never used directly for launch. SaveOpenAiRuntimeApiKey performs
            // Credential Manager write + exact read-back verification and only then projects the
            // verified value into CONTROL_PLANE_API_KEY. Empty input reuses saved/environment state.
            var key = ResolveRuntimeApiKey(string.Empty);
            if (string.IsNullOrWhiteSpace(key))
            {
                message = "Thiếu OpenAI Runtime API key. Nhập key để lưu bảo mật hoặc cấu hình CONTROL_PLANE_API_KEY/OPENAI_API_KEY trong môi trường Windows.";
                return false;
            }

            try
            {
                WriteTextVerified(TunnelIdFile, normalizedTunnelId);
                WriteRuntimeConfig(normalizedTunnelId, McpEmbeddedServer.Endpoint);
                try { if (File.Exists(HealthUrlPath)) File.Delete(HealthUrlPath); } catch { }

                // One selected transport should own external reachability at a time.
                McpCloudflareAccountTunnelManager.StopForHostShutdown();
                McpCloudflareTunnelManager.StopForHostShutdown();
                StopProcessOnly();
                ClearDiagnostics();
                string staleCleanup;
                if (!McpTransportSupervisor.TryCleanupStaleOwnedProcess(
                        McpTransportProvider.OpenAiSecureTunnel, clientPath, out staleCleanup))
                {
                    message = staleCleanup;
                    SetLastError(message);
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = clientPath,
                    Arguments = "run --config \"" + ConfigPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(clientPath) ?? SettingsDirectory
                };
                startInfo.EnvironmentVariables[ControlPlaneApiKeyEnvironment] = key;
                startInfo.EnvironmentVariables["CONTROL_PLANE_TUNNEL_ID"] = normalizedTunnelId;
                startInfo.EnvironmentVariables["MCP_SERVER_URL"] = McpEmbeddedServer.Endpoint.ToString();
                startInfo.EnvironmentVariables[LocalBearerEnvironment] = "Bearer " + McpEmbeddedServer.GetBearerToken();
                startInfo.EnvironmentVariables["HEALTH_LISTEN_ADDR"] = "127.0.0.1:0";
                startInfo.EnvironmentVariables["HEALTH_URL_FILE"] = HealthUrlPath;
                AddNoProxyLoopback(startInfo);

                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = false };
                process.OutputDataReceived += (_, args) => HandleDiagnosticLine(process, "stdout", args.Data);
                process.ErrorDataReceived += (_, args) => HandleDiagnosticLine(process, "stderr", args.Data);
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
                    _clientTrustSummary = trustSummary;
                    _lastExitCode = null;
                    _lastReady = false;
                    _lastReadyProbeUtc = DateTime.MinValue;
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.EnableRaisingEvents = true;
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

                string ownerError;
                if (!McpTransportSupervisor.RegisterOwnedProcess(
                        McpTransportProvider.OpenAiSecureTunnel, process, clientPath, out ownerError))
                {
                    message = "Không thể xác minh quyền sở hữu tunnel-client: " + ownerError;
                    SetLastError(message);
                    StopProcessOnly();
                    return false;
                }

                WriteTextVerified(AutoStartFile, "1");
                McpTransportCoordinator.SetSelectedProvider(McpTransportProvider.OpenAiSecureTunnel);
                EnsureWatchdogStarted();
                message = "OpenAI Secure MCP Tunnel đang khởi động. Runtime API key đã được xác minh và lưu bảo mật trong Windows Credential Manager; không ghi secret vào config/timeline. Chờ READY rồi kết nối ChatGPT bằng Connection = Tunnel.";
                return true;
            }
            catch (Exception ex)
            {
                SetLastError("OpenAI Secure MCP Tunnel: " + ex.Message);
                message = LastError;
                return false;
            }
        }

        internal static bool StartForSupervisor(out string message)
        {
            return Start(SavedTunnelId, string.Empty, out message);
        }

        public static void TryAutoStart()
        {
            if (ReadText(AutoStartFile) != "1" || !IsConfigured) return;
            var key = ResolveRuntimeApiKey(string.Empty);
            if (string.IsNullOrWhiteSpace(key))
            {
                SetLastError("OpenAI Secure MCP Tunnel chưa tự khởi động vì không có Runtime API key đã lưu hoặc biến môi trường CONTROL_PLANE_API_KEY/OPENAI_API_KEY.");
                return;
            }
            EnsureWatchdogStarted();
            string ignored;
            Start(SavedTunnelId, string.Empty, out ignored);
        }

        public static void Stop()
        {
            Exception? persistenceError = null;
            lock (WatchdogRecoverySync)
            {
                StopWatchdog();
                try
                {
                    WriteTextVerified(AutoStartFile, "0");
                }
                catch (Exception ex)
                {
                    persistenceError = ex;
                    SetLastError("Không lưu/xác minh được trạng thái autostart=OFF: " + ex.Message);
                }
                StopProcessOnly();
            }
            if (persistenceError != null)
                throw new InvalidOperationException("OpenAI Secure MCP Tunnel đã dừng nhưng không lưu được trạng thái autostart=OFF.", persistenceError);
        }

        public static void StopForHostShutdown()
        {
            lock (WatchdogRecoverySync)
            {
                StopWatchdog();
                StopProcessOnly();
            }
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

        public static string GetDiagnosticBundle()
        {
            var builder = new StringBuilder();
            builder.AppendLine("OpenAI Secure MCP Tunnel diagnostics");
            builder.AppendLine("state=" + (IsRunning ? (IsReady ? "READY" : "RUNNING") : "STOPPED"));
            builder.AppendLine("client=" + (string.IsNullOrWhiteSpace(SavedClientPath) ? "not-selected" : SavedClientPath));
            builder.AppendLine("trust=" + (string.IsNullOrWhiteSpace(ClientTrustSummary) ? "not-verified-in-this-session" : ClientTrustSummary));
            builder.AppendLine("tunnelId=" + (IsValidTunnelId(SavedTunnelId) ? SavedTunnelId : "not-configured"));
            builder.AppendLine("exit=" + (LastExitCode.HasValue ? LastExitCode.Value.ToString() : "n/a"));
            builder.AppendLine("lastError=" + (string.IsNullOrWhiteSpace(LastError) ? "none" : LastError));
            var diagnostics = LastDiagnostics;
            builder.AppendLine("--- tunnel-client stdout/stderr (sanitized, bounded) ---");
            builder.AppendLine(string.IsNullOrWhiteSpace(diagnostics) ? "<none>" : diagnostics);
            return builder.ToString().TrimEnd();
        }

        public static string Describe()
        {
            return "openai-secure=" + (IsRunning ? (IsReady ? "READY" : "RUNNING") : "STOPPED")
                   + "; configured=" + IsConfigured
                   + (IsValidTunnelId(SavedTunnelId) ? "; tunnelId=" + SavedTunnelId : string.Empty)
                   + (string.IsNullOrWhiteSpace(ClientTrustSummary) ? string.Empty : "; trust=" + ClientTrustSummary)
                   + (LastExitCode.HasValue ? "; exit=" + LastExitCode.Value : string.Empty)
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
            yaml.AppendLine("    " + LocalTunnelAuthorizationHeader + ": env:" + LocalBearerEnvironment);
            yaml.AppendLine("    Content-Type: application/json");
            yaml.AppendLine("  discovery_extra_headers:");
            yaml.AppendLine("    " + LocalTunnelAuthorizationHeader + ": env:" + LocalBearerEnvironment);
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

        private static void EnsureWatchdogStarted()
        {
            lock (Sync)
            {
                _watchdogEnabled = true;
                if (_watchdogTimer != null) return;
                _watchdogTimer = new Timer(WatchdogTick, null, WatchdogPeriodMilliseconds, WatchdogPeriodMilliseconds);
            }
        }

        private static void StopWatchdog()
        {
            Timer? timer;
            lock (Sync)
            {
                _watchdogEnabled = false;
                timer = _watchdogTimer;
                _watchdogTimer = null;
                _consecutiveUnready = 0;
                _restartAttempt = 0;
                _nextRestartUtc = DateTime.MinValue;
            }
            if (timer != null)
            {
                try { timer.Dispose(); } catch { }
            }
        }

        private static void ResetWatchdogFailures()
        {
            lock (Sync)
            {
                _consecutiveUnready = 0;
                _restartAttempt = 0;
                _nextRestartUtc = DateTime.MinValue;
            }
        }

        private static void WatchdogTick(object? state)
        {
            if (Interlocked.CompareExchange(ref _watchdogBusy, 1, 0) != 0) return;
            try
            {
                if (!ShouldWatchdogRun()) return;

                if (IsRunning)
                {
                    if (IsReady)
                    {
                        ResetWatchdogFailures();
                        return;
                    }

                    lock (Sync)
                    {
                        _consecutiveUnready++;
                        if (_consecutiveUnready < UnreadyRestartThreshold) return;
                        if (DateTime.UtcNow < _nextRestartUtc) return;
                    }
                    TryRecoverTunnel("persistent unready");
                    return;
                }

                lock (Sync)
                {
                    _consecutiveUnready = UnreadyRestartThreshold;
                    if (DateTime.UtcNow < _nextRestartUtc) return;
                }
                TryRecoverTunnel("unexpected process exit");
            }
            catch (Exception ex)
            {
                lock (Sync)
                {
                    _restartAttempt = Math.Min(_restartAttempt + 1, 30);
                    _nextRestartUtc = DateTime.UtcNow + ComputeRestartBackoff(_restartAttempt);
                }
                SetLastError("OpenAI MCP tunnel watchdog error: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _watchdogBusy, 0);
            }
        }

        private static bool ShouldWatchdogRun()
        {
            lock (Sync)
            {
                if (!_watchdogEnabled || _stopping) return false;
            }
            if (McpTransportSupervisor.IsManaging) return false;
            if (ReadText(AutoStartFile) != "1") return false;
            if (McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel) return false;
            return IsConfigured;
        }

        private static void TryRecoverTunnel(string reason)
        {
            if (!ShouldWatchdogRun()) return;
            lock (WatchdogRecoverySync)
            {
                if (!ShouldWatchdogRun()) return;
                StopProcessOnly();
                if (!ShouldWatchdogRun()) return;

                string message;
                bool restarted;
                try
                {
                    restarted = Start(SavedTunnelId, string.Empty, out message);
                }
                catch (Exception ex)
                {
                    restarted = false;
                    message = ex.Message;
                }

                TimeSpan backoff;
                lock (Sync)
                {
                    _consecutiveUnready = restarted ? 0 : UnreadyRestartThreshold;
                    _restartAttempt = Math.Min(_restartAttempt + 1, 30);
                    backoff = ComputeRestartBackoff(_restartAttempt);
                    _nextRestartUtc = DateTime.UtcNow + backoff;
                }
                if (restarted) return;

                SetLastError("OpenAI MCP tunnel self-heal failed after " + reason + "; retry in "
                             + ((int)backoff.TotalSeconds).ToString() + "s: " + message);
            }
        }

        private static TimeSpan ComputeRestartBackoff(int attempt)
        {
            var boundedAttempt = Math.Max(1, Math.Min(attempt, 16));
            var exponent = Math.Min(boundedAttempt - 1, 10);
            var seconds = RestartBackoffBaseSeconds * (1 << exponent);
            return TimeSpan.FromSeconds(Math.Min(RestartBackoffMaxSeconds, seconds));
        }

        private static void HandleDiagnosticLine(Process process, string stream, string? line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            var clean = SanitizeDiagnosticLine(line);
            if (clean.Length > 1200) clean = clean.Substring(0, 1200);
            lock (Sync)
            {
                if (!ReferenceEquals(_process, process)) return;
                DiagnosticLines.Add(DateTime.Now.ToString("HH:mm:ss") + " [" + stream + "] " + clean);
                while (DiagnosticLines.Count > MaxDiagnosticLines) DiagnosticLines.RemoveAt(0);
                if (LooksLikeError(clean)) _lastError = clean;
            }
        }

        private static void HandleProcessExit(Process process)
        {
            var dispose = false;
            lock (Sync)
            {
                if (!ReferenceEquals(_process, process)) return;
                var intentional = _stopping;
                _process = null;
                _lastReady = false;
                _lastReadyProbeUtc = DateTime.MinValue;
                try { _lastExitCode = process.ExitCode; } catch { _lastExitCode = null; }
                if (!intentional)
                {
                    _consecutiveUnready = UnreadyRestartThreshold;
                    var exit = _lastExitCode.HasValue ? _lastExitCode.Value.ToString() : "unknown";
                    var tail = DiagnosticTailUnsafe(4);
                    _lastError = "OpenAI tunnel-client đã dừng (exit=" + exit + ")."
                                 + (string.IsNullOrWhiteSpace(tail) ? string.Empty : " Diagnostic cuối: " + tail);
                }
                dispose = true;
            }
            if (dispose)
            {
                McpTransportSupervisor.ClearOwnedProcess(McpTransportProvider.OpenAiSecureTunnel);
                try { process.Dispose(); } catch { }
            }
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
                try { process.EnableRaisingEvents = false; } catch { }
                try { process.CancelOutputRead(); } catch { }
                try { process.CancelErrorRead(); } catch { }
                try { if (!process.HasExited) process.Kill(); } catch { }
                try { process.WaitForExit(1500); } catch { }
                try { process.Dispose(); } catch { }
                McpTransportSupervisor.ClearOwnedProcess(McpTransportProvider.OpenAiSecureTunnel);
            }
            lock (Sync) _stopping = false;
        }

        private static bool TryVerifyClientTrust(string path, out string summary, out string error)
        {
            summary = string.Empty;
            error = string.Empty;
            if (!IsUsableClientPath(path))
            {
                error = "file phải là tunnel-client*.exe tồn tại trên máy.";
                return false;
            }

            string version;
            if (!TryReadVersion(path, out version))
            {
                error = "file không chạy được với --version như tunnel-client.";
                return false;
            }

            var trust = VerifyAuthenticode(path);
            if (trust == 0)
            {
                try
                {
                    using (var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))
                    {
                        var signer = certificate.Subject ?? string.Empty;
                        if (signer.IndexOf("OpenAI", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            error = "Authenticode hợp lệ nhưng signer không phải OpenAI: " + signer + ".";
                            return false;
                        }
                        summary = "version=" + version + "; Authenticode signer=" + signer;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    error = "không đọc được signer certificate: " + ex.Message;
                    return false;
                }
            }

            var expected = (Environment.GetEnvironmentVariable(ExpectedSha256Environment) ?? string.Empty).Trim();
            if (!Sha256Regex.IsMatch(expected))
            {
                error = "binary không có Authenticode OpenAI hợp lệ (WinVerifyTrust=0x" + trust.ToString("X8")
                        + "). Nếu release chính thức dùng binary không ký, đặt " + ExpectedSha256Environment
                        + " bằng SHA-256 chính thức của đúng release rồi chọn lại file.";
                return false;
            }

            string actual;
            try { actual = ComputeSha256(path); }
            catch (Exception ex)
            {
                error = "không tính được SHA-256: " + ex.Message;
                return false;
            }
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            {
                error = "SHA-256 không khớp pinned release hash. expected=" + expected.ToLowerInvariant()
                        + "; actual=" + actual.ToLowerInvariant() + ".";
                return false;
            }
            summary = "version=" + version + "; SHA-256 verified via " + ExpectedSha256Environment + "=" + actual.ToLowerInvariant();
            return true;
        }

        private static uint VerifyAuthenticode(string path)
        {
            var fileInfo = new WinTrustFileInfo(path);
            var fileInfoPointer = IntPtr.Zero;
            var dataPointer = IntPtr.Zero;
            try
            {
                fileInfoPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(fileInfo, fileInfoPointer, false);
                var data = new WinTrustData(fileInfoPointer);
                dataPointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustData)));
                Marshal.StructureToPtr(data, dataPointer, false);
                var action = WinTrustActionGenericVerifyV2;
                return WinVerifyTrust(IntPtr.Zero, ref action, dataPointer);
            }
            catch { return 0xFFFFFFFF; }
            finally
            {
                if (dataPointer != IntPtr.Zero) Marshal.FreeHGlobal(dataPointer);
                if (fileInfoPointer != IntPtr.Zero) Marshal.FreeHGlobal(fileInfoPointer);
            }
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(stream);
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) builder.Append(value.ToString("x2"));
                return builder.ToString();
            }
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
                output = SanitizeDiagnosticLine(output);
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

                string saved;
                if (McpPersistentUserSettings.TryReadOpenAiRuntimeApiKey(out saved))
                {
                    value = (saved ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                }

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

        private static void ClearDiagnostics()
        {
            lock (Sync)
            {
                DiagnosticLines.Clear();
                _lastExitCode = null;
            }
        }

        private static string SanitizeDiagnosticLine(string? value)
        {
            var clean = (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            clean = ApiKeyRegex.Replace(clean, "sk-<redacted>");
            clean = AuthorizationRegex.Replace(clean, "$1<redacted>");
            return clean;
        }

        private static bool LooksLikeError(string value)
        {
            return value.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                   || value.IndexOf("fatal", StringComparison.OrdinalIgnoreCase) >= 0
                   || value.IndexOf("unauthorized", StringComparison.OrdinalIgnoreCase) >= 0
                   || value.IndexOf("forbidden", StringComparison.OrdinalIgnoreCase) >= 0
                   || value.IndexOf("refused", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string DiagnosticTailUnsafe(int count)
        {
            if (DiagnosticLines.Count == 0 || count <= 0) return string.Empty;
            var start = Math.Max(0, DiagnosticLines.Count - count);
            var items = new List<string>();
            for (var i = start; i < DiagnosticLines.Count; i++) items.Add(DiagnosticLines[i]);
            return string.Join(" | ", items.ToArray());
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

        private static void WriteTextVerified(string path, string value)
        {
            var expected = value ?? string.Empty;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? SettingsDirectory);
            File.WriteAllText(path, expected, new UTF8Encoding(false));
            var verified = File.ReadAllText(path, Encoding.UTF8);
            if (!string.Equals(verified, expected, StringComparison.Ordinal))
                throw new IOException("Persistence read-back verification failed for " + Path.GetFileName(path) + ".");
        }

        private static void SetLastError(string value)
        {
            lock (Sync) _lastError = SanitizeDiagnosticLine(value ?? string.Empty);
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private static readonly Guid WinTrustActionGenericVerifyV2 = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class WinTrustFileInfo
        {
            public WinTrustFileInfo(string filePath)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo));
                FilePath = filePath;
                FileHandle = IntPtr.Zero;
                KnownSubject = IntPtr.Zero;
            }
            public uint StructSize;
            [MarshalAs(UnmanagedType.LPWStr)] public string FilePath;
            public IntPtr FileHandle;
            public IntPtr KnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class WinTrustData
        {
            public WinTrustData(IntPtr fileInfo)
            {
                StructSize = (uint)Marshal.SizeOf(typeof(WinTrustData));
                PolicyCallbackData = IntPtr.Zero;
                SipClientData = IntPtr.Zero;
                UiChoice = 2;
                RevocationChecks = 0;
                UnionChoice = 1;
                FileInfoPointer = fileInfo;
                StateAction = 0;
                StateData = IntPtr.Zero;
                UrlReference = IntPtr.Zero;
                ProviderFlags = 0;
                UiContext = 0;
            }
            public uint StructSize;
            public IntPtr PolicyCallbackData;
            public IntPtr SipClientData;
            public uint UiChoice;
            public uint RevocationChecks;
            public uint UnionChoice;
            public IntPtr FileInfoPointer;
            public uint StateAction;
            public IntPtr StateData;
            public IntPtr UrlReference;
            public uint ProviderFlags;
            public uint UiContext;
        }

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, ref Guid actionId, IntPtr trustData);
    }
}
