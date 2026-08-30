using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class McpCloudflareAccountOnboardingCommands
    {
        [CommandMethod("QS3DMCPACCOUNTSETUP", CommandFlags.Modal)]
        public void ShowAccountSetup()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                McpEmbeddedServer.EnsureStarted();
                new McpCloudflareAccountSetupWindow().ShowDialog();
                document.Editor.WriteMessage("\nQS3D MCP account setup: " + McpCloudflareAccountTunnelManager.Describe());
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3D MCP account setup lỗi: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Click-first Cloudflare account onboarding. Provider credentials are entered only in
    /// Cloudflare's browser flow. QS3D stores only cloudflared's provider-issued certificate,
    /// tunnel credential file and the minimum local tunnel configuration needed to reconnect.
    /// </summary>
    internal static class McpCloudflareAccountTunnelManager
    {
        private const string TunnelName = "qs3d-bricscad";
        private static string OriginUrl => McpEmbeddedServer.Endpoint.GetLeftPart(UriPartial.Authority);
        private const string ArgoTokenBegin = "-----BEGIN ARGO TUNNEL TOKEN-----";
        private const string ArgoTokenEnd = "-----END ARGO TUNNEL TOKEN-----";
        private const int CommandTimeoutMs = 60000;
        private const int LoginTimeoutMs = 10 * 60 * 1000;
        private const int MaxCapturedOutput = 256 * 1024;
        private const int MaxCertificateBytes = 1024 * 1024;
        private static readonly object Sync = new object();
        private static readonly Regex UuidRegex = new Regex(
            "^(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex UuidSearchRegex = new Regex(
            "(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static Process? _process;
        private static string _lastMessage = string.Empty;
        private static string _lastError = string.Empty;
        private static bool _certificateImportNeeded;
        private static int _setupOperationActive;

        private static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "MCP", "CloudflareAccount");
        private static string TunnelIdPath => Path.Combine(SettingsDirectory, "tunnel-id.txt");
        private static string HostnamePath => Path.Combine(SettingsDirectory, "hostname.txt");
        private static string ConfigPath => Path.Combine(SettingsDirectory, "config.yml");
        private static string AutoStartPath => Path.Combine(SettingsDirectory, "autostart.txt");

        public static string CloudflaredPath => McpCloudflareTunnelManager.CloudflaredPath;
        public static string CloudflaredDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cloudflared");
        public static string CertificatePath => Path.Combine(CloudflaredDirectory, "cert.pem");
        public static bool IsAuthenticated { get { try { return File.Exists(CertificatePath); } catch { return false; } } }
        public static bool IsSetupBusy => Volatile.Read(ref _setupOperationActive) != 0;
        public static bool CertificateImportNeeded { get { lock (Sync) return _certificateImportNeeded; } }
        public static string SavedHostname => ReadText(HostnamePath);
        public static string PublicMcpUrl => IsRunning && !string.IsNullOrWhiteSpace(SavedHostname) ? "https://" + SavedHostname + "/mcp" : string.Empty;
        public static string LastMessage { get { lock (Sync) return _lastMessage; } }
        public static string LastError { get { lock (Sync) return _lastError; } }

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

        public static void OpenInstallerPage() => McpCloudflareTunnelManager.OpenCloudflaredDownloadPage();
        public static void OpenCloudflareDashboard() => McpCloudflareTunnelManager.OpenCloudflareTunnelDashboard();
        public static void OpenChatGpt() => McpCloudflareTunnelManager.OpenChatGpt();

        public static bool StartQuickTunnel(out string error)
        {
            StopProcess();
            return McpCloudflareTunnelManager.StartQuickTunnel(out error);
        }

        public static void BeginBrowserLogin(Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (!TryEnterSetup(out var busyError)) { completed(false, busyError); return; }
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                ExitSetup();
                completed(false, "Chưa cài Cloudflare Tunnel.");
                return;
            }

            SetCertificateImportNeeded(false);
            SetState("Cloudflare login: đang mở trình duyệt...", string.Empty);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string output;
                    string error;
                    var ok = RunCommand(executable, "tunnel login", LoginTimeoutMs, out output, out error);
                    var authenticated = ok && IsAuthenticated;
                    var certificateFallback = !authenticated && LoginRequiresCertificateImport(output, error);
                    SetCertificateImportNeeded(certificateFallback);

                    string message;
                    string detail;
                    if (authenticated)
                    {
                        message = "Cloudflare login: thành công.";
                        detail = string.Empty;
                    }
                    else if (certificateFallback)
                    {
                        message = "Cloudflare đã tải cert.pem qua trình duyệt. Chọn file đó trong QS3D để hoàn tất đăng nhập.";
                        detail = FirstUsefulError(error, output, message);
                    }
                    else
                    {
                        message = "Cloudflare login: " + FirstUsefulError(error, output, "chưa hoàn tất.");
                        detail = message;
                    }

                    SetState(message, detail);
                    try { completed(authenticated, message); } catch { }
                }
                finally { ExitSetup(); }
            });
        }

        public static bool ImportDownloadedCertificate(string sourcePath, out string error)
        {
            error = string.Empty;
            string temporary = string.Empty;
            string backup = string.Empty;
            var hadExisting = false;
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    error = "Chưa chọn file cert.pem.";
                    return false;
                }

                var fullPath = Path.GetFullPath(sourcePath);
                var info = new FileInfo(fullPath);
                if (!info.Exists || info.Length < 64 || info.Length > MaxCertificateBytes)
                {
                    error = "File cert.pem không tồn tại hoặc có kích thước bất thường.";
                    return false;
                }

                var pem = File.ReadAllText(fullPath, Encoding.UTF8);
                var begin = pem.IndexOf(ArgoTokenBegin, StringComparison.Ordinal);
                var end = pem.IndexOf(ArgoTokenEnd, StringComparison.Ordinal);
                if (begin < 0 || end <= begin)
                {
                    error = "File được chọn không có Cloudflare ARGO TUNNEL TOKEN hợp lệ.";
                    return false;
                }

                var payloadStart = begin + ArgoTokenBegin.Length;
                var payload = pem.Substring(payloadStart, end - payloadStart);
                var compactPayload = Regex.Replace(payload, "\\s+", string.Empty);
                if (compactPayload.Length < 16)
                {
                    error = "Cloudflare tunnel token trong cert.pem bị rỗng hoặc quá ngắn.";
                    return false;
                }
                try
                {
                    var decoded = Convert.FromBase64String(compactPayload);
                    if (decoded.Length < 16) throw new FormatException("decoded token is too short");
                }
                catch (Exception)
                {
                    error = "Cloudflare tunnel token trong cert.pem không phải Base64 hợp lệ.";
                    return false;
                }

                Directory.CreateDirectory(CloudflaredDirectory);
                temporary = CertificatePath + ".import-" + Guid.NewGuid().ToString("N");
                backup = CertificatePath + ".previous";
                hadExisting = File.Exists(CertificatePath);
                if (hadExisting) File.Copy(CertificatePath, backup, true);
                File.WriteAllText(temporary, pem, new UTF8Encoding(false));
                File.Copy(temporary, CertificatePath, true);

                if (!File.Exists(CertificatePath))
                    throw new IOException("cert.pem was not written to the Cloudflare profile directory");

                SetCertificateImportNeeded(false);
                SetState("Cloudflare login: thành công sau khi nhập cert.pem.", string.Empty);
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    if (hadExisting && !string.IsNullOrWhiteSpace(backup) && File.Exists(backup))
                        File.Copy(backup, CertificatePath, true);
                    else if (!hadExisting && File.Exists(CertificatePath))
                        File.Delete(CertificatePath);
                }
                catch { }
                error = "Không nhập được cert.pem: " + ex.Message;
                SetState(error, error);
                return false;
            }
            finally
            {
                try { if (!string.IsNullOrWhiteSpace(temporary) && File.Exists(temporary)) File.Delete(temporary); } catch { }
                try { if (!string.IsNullOrWhiteSpace(backup) && File.Exists(backup)) File.Delete(backup); } catch { }
            }
        }

        public static void BeginProvision(string hostname, Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var normalized = McpCloudflareTunnelManager.NormalizeHostname(hostname);
            if (string.IsNullOrWhiteSpace(normalized)) { completed(false, "Hostname không hợp lệ. Ví dụ qs3d.example.com"); return; }
            if (!IsAuthenticated) { completed(false, "Hãy bấm Đăng nhập Cloudflare trước."); return; }
            if (!TryEnterSetup(out var busyError)) { completed(false, busyError); return; }

            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                ExitSetup();
                completed(false, "Chưa cài Cloudflare Tunnel.");
                return;
            }

            SetState("Đang xác minh tunnel Cloudflare và DNS route...", string.Empty);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string error;
                    var ok = Provision(executable, normalized, out error);
                    var message = ok ? "Named Tunnel đã kết nối: " + PublicMcpUrl : error;
                    SetState(message, ok ? string.Empty : error);
                    try { completed(ok, message); } catch { }
                }
                finally { ExitSetup(); }
            });
        }

        public static bool StartSaved(out string error)
        {
            error = string.Empty;
            McpEmbeddedServer.EnsureStarted();
            var executable = CloudflaredPath;
            var id = ReadText(TunnelIdPath);
            var hostname = McpCloudflareTunnelManager.NormalizeHostname(ReadText(HostnamePath));
            if (string.IsNullOrWhiteSpace(executable) || !IsUsableTunnelId(id)
                || string.IsNullOrWhiteSpace(hostname))
            {
                error = "Named Tunnel chưa được cấu hình đầy đủ.";
                return false;
            }
            var credentials = Path.Combine(CloudflaredDirectory, id + ".json");
            if (!File.Exists(credentials))
            {
                error = "Named Tunnel credentials không còn tồn tại. Hãy cấu hình lại tunnel.";
                return false;
            }
            try
            {
                WriteCanonicalConfig(id, hostname, credentials);
            }
            catch (Exception ex)
            {
                error = "Không ghi lại được Named Tunnel config an toàn: " + ex.Message;
                return false;
            }
            McpCloudflareTunnelManager.StopForHostShutdown();
            return StartProcess(executable, "tunnel --config \"" + ConfigPath + "\" run " + id, out error);
        }

        public static void TryAutoStart()
        {
            if (ReadText(AutoStartPath) != "1") return;
            string ignored;
            StartSaved(out ignored);
        }

        public static void StopForHostShutdown() => StopProcess();

        public static void Stop()
        {
            WriteText(AutoStartPath, "0");
            StopProcess();
            SetState("Named Tunnel đã dừng.", string.Empty);
        }

        public static string Describe()
        {
            return "running=" + IsRunning
                   + "; authenticated=" + IsAuthenticated
                   + "; setupBusy=" + IsSetupBusy
                   + (CertificateImportNeeded ? "; certImportNeeded=true" : string.Empty)
                   + (string.IsNullOrWhiteSpace(McpPublicEndpointResolver.Resolve()) ? string.Empty : "; public=" + McpPublicEndpointResolver.Resolve())
                   + (string.IsNullOrWhiteSpace(LastError) ? string.Empty : "; error=" + LastError);
        }

        private static bool Provision(string executable, string hostname, out string error)
        {
            error = string.Empty;
            McpEmbeddedServer.EnsureStarted();

            string tunnelId;
            if (!ResolveOrCreateTunnel(executable, out tunnelId, out error)) return false;
            var credentials = Path.Combine(CloudflaredDirectory, tunnelId + ".json");
            if (!File.Exists(credentials))
            {
                error = "Không tìm thấy credential của tunnel đã xác minh. Hãy đăng nhập Cloudflare lại.";
                return false;
            }

            string output;
            string routeError;
            if (!RunCommand(executable, "tunnel route dns " + tunnelId + " " + hostname, CommandTimeoutMs, out output, out routeError))
            {
                error = "Không tạo được DNS route. QS3D không tự bỏ qua xung đột DNS vì hostname có thể đang trỏ sang tunnel khác. "
                        + FirstUsefulError(routeError, output, "Hãy kiểm tra hostname trong Cloudflare Dashboard rồi thử lại.");
                return false;
            }

            try
            {
                WriteCanonicalConfig(tunnelId, hostname, credentials);
                WriteText(TunnelIdPath, tunnelId);
                WriteText(HostnamePath, hostname);
                WriteText(AutoStartPath, "1");
            }
            catch (Exception ex)
            {
                error = "Không lưu được Named Tunnel config: " + ex.Message;
                return false;
            }

            McpCloudflareTunnelManager.StopForHostShutdown();
            StopProcess();
            return StartProcess(executable, "tunnel --config \"" + ConfigPath + "\" run " + tunnelId, out error);
        }

        private static void WriteCanonicalConfig(string tunnelId, string hostname, string credentials)
        {
            if (!IsUsableTunnelId(tunnelId)) throw new InvalidOperationException("Tunnel UUID không hợp lệ.");
            var normalizedHostname = McpCloudflareTunnelManager.NormalizeHostname(hostname);
            if (string.IsNullOrWhiteSpace(normalizedHostname)) throw new InvalidOperationException("Tunnel hostname không hợp lệ.");
            var expectedCredentials = Path.Combine(CloudflaredDirectory, tunnelId + ".json");
            if (string.IsNullOrWhiteSpace(credentials)
                || !string.Equals(Path.GetFullPath(credentials), Path.GetFullPath(expectedCredentials), StringComparison.OrdinalIgnoreCase)
                || !File.Exists(expectedCredentials))
                throw new InvalidOperationException("Tunnel credential path không hợp lệ hoặc không tồn tại.");

            Directory.CreateDirectory(SettingsDirectory);
            var yamlCredentials = expectedCredentials.Replace('\\', '/').Replace("\"", "\\\"");
            var config = "tunnel: " + tunnelId + "\r\n"
                         + "credentials-file: \"" + yamlCredentials + "\"\r\n"
                         + "ingress:\r\n"
                         + "  - hostname: " + normalizedHostname + "\r\n"
                         + "    service: " + OriginUrl + "\r\n"
                         + "  - service: http_status:404\r\n";
            File.WriteAllText(ConfigPath, config, new UTF8Encoding(false));
        }

        private static bool ResolveOrCreateTunnel(string executable, out string tunnelId, out string error)
        {
            tunnelId = string.Empty;
            error = string.Empty;

            string listOutput;
            string listError;
            if (!RunCommand(executable, "tunnel list", CommandTimeoutMs, out listOutput, out listError))
            {
                error = "Không xác minh được danh sách tunnel hiện tại: "
                        + FirstUsefulError(listError, listOutput, "cloudflared tunnel list failed.");
                return false;
            }

            tunnelId = FindTunnelIdByName(listOutput, TunnelName);
            if (IsUsableTunnelId(tunnelId))
            {
                var existingCredentials = Path.Combine(CloudflaredDirectory, tunnelId + ".json");
                if (!File.Exists(existingCredentials))
                {
                    error = "Tunnel '" + TunnelName + "' tồn tại trên Cloudflare nhưng máy này thiếu credential " + tunnelId
                            + ". Hãy đăng nhập lại hoặc xử lý tunnel đó trong Cloudflare Dashboard; QS3D sẽ không tạo tunnel trùng tên.";
                    return false;
                }
                WriteText(TunnelIdPath, tunnelId);
                return true;
            }

            string createOutput;
            string createError;
            if (!RunCommand(executable, "tunnel create " + TunnelName, CommandTimeoutMs, out createOutput, out createError))
            {
                error = "Không tạo được tunnel '" + TunnelName + "': "
                        + FirstUsefulError(createError, createOutput, "Cloudflare tunnel create failed.");
                return false;
            }

            var match = UuidSearchRegex.Match(createOutput ?? string.Empty);
            tunnelId = match.Success ? match.Groups["id"].Value : string.Empty;
            if (!IsUsableTunnelId(tunnelId))
            {
                if (!RunCommand(executable, "tunnel list", CommandTimeoutMs, out listOutput, out listError))
                {
                    error = "Tunnel có thể đã được tạo nhưng QS3D không xác minh được UUID: "
                            + FirstUsefulError(listError, listOutput, "cloudflared tunnel list failed.");
                    return false;
                }
                tunnelId = FindTunnelIdByName(listOutput, TunnelName);
            }
            if (!IsUsableTunnelId(tunnelId))
            {
                error = "Không đọc được tunnel UUID sau khi tạo.";
                return false;
            }
            if (!File.Exists(Path.Combine(CloudflaredDirectory, tunnelId + ".json")))
            {
                error = "Tunnel đã được tạo nhưng credential file chưa xuất hiện. Hãy đăng nhập Cloudflare lại trước khi chạy tunnel.";
                return false;
            }
            WriteText(TunnelIdPath, tunnelId);
            return true;
        }

        private static string FindTunnelIdByName(string output, string name)
        {
            if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(name)) return string.Empty;
            using (var reader = new StringReader(output))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = SplitColumns(line);
                    if (parts.Count < 2) continue;
                    if (!IsUsableTunnelId(parts[0])) continue;
                    if (!string.Equals(parts[1], name, StringComparison.OrdinalIgnoreCase)) continue;
                    return parts[0].Trim();
                }
            }
            return string.Empty;
        }

        private static List<string> SplitColumns(string line)
        {
            var result = new List<string>();
            foreach (var part in Regex.Split((line ?? string.Empty).Trim(), "\\s+"))
                if (!string.IsNullOrWhiteSpace(part)) result.Add(part);
            return result;
        }

        private static bool IsUsableTunnelId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && UuidRegex.IsMatch(value.Trim());
        }

        private static bool LoginRequiresCertificateImport(string output, string error)
        {
            var combined = (output ?? string.Empty) + "\n" + (error ?? string.Empty);
            return combined.IndexOf("Failed to write the certificate", StringComparison.OrdinalIgnoreCase) >= 0
                   || combined.IndexOf("download the certificate instead", StringComparison.OrdinalIgnoreCase) >= 0
                   || combined.IndexOf("copy it to the following path", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool RunCommand(string executable, string arguments, int timeoutMs, out string output, out string error)
        {
            output = string.Empty;
            error = string.Empty;
            try
            {
                using (var process = new Process())
                {
                    var stdout = new StringBuilder();
                    var stderr = new StringBuilder();
                    process.StartInfo = CreateStartInfo(executable, arguments);
                    process.OutputDataReceived += (_, args) => { if (args.Data != null) AppendBounded(stdout, args.Data); };
                    process.ErrorDataReceived += (_, args) => { if (args.Data != null) AppendBounded(stderr, args.Data); };
                    if (!process.Start()) { error = "Không khởi động được cloudflared."; return false; }
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    var exited = process.WaitForExit(timeoutMs);
                    if (!exited)
                    {
                        try { process.Kill(); } catch { }
                        try { process.WaitForExit(3000); } catch { }
                    }
                    try { if (process.HasExited) process.WaitForExit(); } catch { }

                    output = ReadBuilder(stdout).Trim();
                    error = ReadBuilder(stderr).Trim();
                    if (!exited)
                    {
                        error = "Cloudflare thao tác quá thời gian chờ."
                                + (string.IsNullOrWhiteSpace(error) ? string.Empty : " " + Limit(error, 1000));
                        return false;
                    }
                    if (process.ExitCode == 0) return true;
                    if (string.IsNullOrWhiteSpace(error)) error = output;
                    return false;
                }
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        private static void AppendBounded(StringBuilder builder, string line)
        {
            lock (builder)
            {
                if (builder.Length >= MaxCapturedOutput) return;
                var remaining = MaxCapturedOutput - builder.Length;
                if (line.Length > remaining) line = line.Substring(0, remaining);
                builder.AppendLine(line);
            }
        }

        private static string ReadBuilder(StringBuilder builder)
        {
            lock (builder) return builder.ToString();
        }

        private static ProcessStartInfo CreateStartInfo(string executable, string arguments)
        {
            return new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
            };
        }

        private static bool StartProcess(string executable, string arguments, out string error)
        {
            error = string.Empty;
            StopProcess();
            Process? process = null;
            try
            {
                process = new Process { StartInfo = CreateStartInfo(executable, arguments), EnableRaisingEvents = false };
                process.OutputDataReceived += (_, args) => HandleRunLine(process, args.Data, false);
                process.ErrorDataReceived += (_, args) => HandleRunLine(process, args.Data, true);
                if (!process.Start())
                {
                    process.Dispose();
                    error = "Không khởi động được Named Tunnel.";
                    return false;
                }

                lock (Sync)
                {
                    _process = process;
                    _lastMessage = "Named Tunnel đang kết nối...";
                    _lastError = string.Empty;
                }
                process.Exited += (_, __) => HandleProcessExit(process);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.EnableRaisingEvents = true;
                if (process.HasExited) HandleProcessExit(process);
                return IsRunning;
            }
            catch (Exception ex)
            {
                lock (Sync) { if (ReferenceEquals(_process, process)) _process = null; }
                try { process?.Dispose(); } catch { }
                error = ex.Message;
                SetState(error, error);
                return false;
            }
        }

        private static void HandleProcessExit(Process process)
        {
            int? exitCode = null;
            try { exitCode = process.ExitCode; } catch { }
            var owned = false;
            lock (Sync)
            {
                if (ReferenceEquals(_process, process))
                {
                    owned = true;
                    _process = null;
                    if (exitCode.HasValue && exitCode.Value != 0 && string.IsNullOrWhiteSpace(_lastError))
                        _lastError = "Named Tunnel exited with code " + exitCode.Value.ToString() + ".";
                    _lastMessage = exitCode.HasValue
                        ? "Named Tunnel đã dừng (exit " + exitCode.Value.ToString() + ")."
                        : "Named Tunnel đã dừng.";
                }
            }
            if (owned) { try { process.Dispose(); } catch { } }
        }

        private static void HandleRunLine(Process process, string? line, bool stderr)
        {
            if (line == null || string.IsNullOrWhiteSpace(line)) return;
            var clean = line.Trim();
            if (clean.Length > 500) clean = clean.Substring(0, 500);
            lock (Sync)
            {
                if (!ReferenceEquals(_process, process)) return;
                _lastMessage = clean;
                if (stderr && (clean.IndexOf("ERR", StringComparison.OrdinalIgnoreCase) >= 0
                               || clean.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0))
                    _lastError = clean;
            }
        }

        private static void StopProcess()
        {
            Process? process;
            lock (Sync) { process = _process; _process = null; }
            if (process == null) return;
            try { process.EnableRaisingEvents = false; } catch { }
            try { if (!process.HasExited) process.Kill(); } catch { }
            try { if (!process.HasExited) process.WaitForExit(2000); } catch { }
            try { process.Dispose(); } catch { }
        }

        private static bool TryEnterSetup(out string error)
        {
            if (Interlocked.CompareExchange(ref _setupOperationActive, 1, 0) == 0)
            {
                error = string.Empty;
                return true;
            }
            error = "Một thao tác Cloudflare khác đang chạy. Hãy chờ thao tác đó hoàn tất.";
            return false;
        }

        private static void ExitSetup() => Interlocked.Exchange(ref _setupOperationActive, 0);

        private static void SetCertificateImportNeeded(bool value)
        {
            lock (Sync) _certificateImportNeeded = value;
        }

        private static string FirstUsefulError(string primary, string secondary, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary)) return Limit(primary.Trim(), 1200);
            if (!string.IsNullOrWhiteSpace(secondary)) return Limit(secondary.Trim(), 1200);
            return fallback;
        }

        private static string Limit(string value, int maximum)
        {
            return value.Length <= maximum ? value : value.Substring(0, maximum) + "...";
        }

        private static void SetState(string message, string error)
        {
            lock (Sync)
            {
                _lastMessage = message ?? string.Empty;
                _lastError = error ?? string.Empty;
            }
        }

        private static void WriteText(string path, string value)
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(path, value ?? string.Empty, new UTF8Encoding(false));
        }

        private static string ReadText(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : string.Empty; }
            catch { return string.Empty; }
        }
    }

    internal sealed class McpCloudflareAccountSetupWindow : Window
    {
        private readonly TextBox _hostname = new TextBox { Margin = new Thickness(0, 4, 0, 8) };
        private DispatcherTimer? _quickUrlTimer;
        private int _quickUrlPollTicks;
        private int _connectOperationActive;
        private string _lastUiDetail = string.Empty;

        public McpCloudflareAccountSetupWindow()
        {
            Title = "QS3D - Kết nối ChatGPT MCP";
            Width = 620;
            Height = 520;
            MinWidth = 540;
            MinHeight = 440;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Closed += (_, __) => StopQuickUrlPolling();

            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(new TextBlock
            {
                Text = "ChatGPT ↔ QS3D ↔ BricsCAD",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Bấm một nút để kết nối. QS3D tự lo MCP và Cloudflare; cấu hình cố định chỉ cần khi bạn muốn URL riêng dùng lâu dài. QS3D không hỏi và không lưu mật khẩu Cloudflare.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            });

            var connect = Button("Kết nối ChatGPT", (_, __) => ConnectChatGpt());
            connect.FontWeight = FontWeights.Bold;
            connect.MinHeight = 52;
            panel.Children.Add(connect);
            panel.Children.Add(Button("Sao chép cấu hình ChatGPT", (_, __) => CopyConfig()));
            panel.Children.Add(Button("Mở ChatGPT", (_, __) => McpCloudflareAccountTunnelManager.OpenChatGpt()));
            panel.Children.Add(Button("Ngắt kết nối", (_, __) => Disconnect()));

            var advancedPanel = new StackPanel { Margin = new Thickness(8, 8, 0, 4) };
            advancedPanel.Children.Add(Button("Cài / cập nhật Cloudflare Tunnel", (_, __) => InstallCloudflared()));
            advancedPanel.Children.Add(Button("Đăng nhập Cloudflare", (_, __) => Login()));
            advancedPanel.Children.Add(new TextBlock
            {
                Text = "Hostname public (ví dụ qs3d.example.com):",
                Margin = new Thickness(0, 8, 0, 0)
            });
            _hostname.Text = McpCloudflareAccountTunnelManager.SavedHostname;
            advancedPanel.Children.Add(_hostname);
            advancedPanel.Children.Add(Button("Tạo / reuse Named Tunnel", (_, __) => Provision()));
            advancedPanel.Children.Add(Button("Mở Cloudflare Dashboard", (_, __) => McpCloudflareAccountTunnelManager.OpenCloudflareDashboard()));

            panel.Children.Add(new Expander
            {
                Header = "Kết nối cố định (tùy chọn)",
                IsExpanded = false,
                Margin = new Thickness(0, 12, 0, 4),
                Content = advancedPanel
            });

            panel.Children.Add(Button("Kiểm tra MCP local", (_, __) => Probe()));
            panel.Children.Add(Button("Chi tiết kỹ thuật", (_, __) => ShowTechnicalDetails()));
            panel.Children.Add(Button("Đóng", (_, __) => Close()));

            Content = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        private static Button Button(string text, RoutedEventHandler handler)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(0, 4, 0, 4),
                MinHeight = 34,
                Padding = new Thickness(10, 5, 10, 5)
            };
            button.Click += handler;
            return button;
        }

        private void ConnectChatGpt()
        {
            if (Interlocked.CompareExchange(ref _connectOperationActive, 1, 0) != 0)
            {
                Notify("QS3D MCP", "Đang xử lý kết nối. Vui lòng chờ một chút.");
                return;
            }

            try
            {
                McpEmbeddedServer.EnsureStarted();

                var existingUrl = McpPublicEndpointResolver.Resolve();
                if ((McpCloudflareAccountTunnelManager.IsRunning || McpCloudflareTunnelManager.IsRunning)
                    && !string.IsNullOrWhiteSpace(existingUrl))
                {
                    _lastUiDetail = "Existing public endpoint reused: " + existingUrl;
                    Notify("Đã kết nối", "ChatGPT ↔ QS3D ↔ BricsCAD đã sẵn sàng. Bấm 'Sao chép cấu hình ChatGPT' để dùng endpoint hiện tại.");
                    EndConnectOperation();
                    return;
                }

                if (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                {
                    string adopted;
                    McpCloudflaredBootstrapper.AdoptExistingManagedBinary(out adopted);
                }

                if (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                {
                    if (McpCloudflaredBootstrapper.IsInstalling)
                    {
                        Notify("Đang chuẩn bị", "Cloudflare Tunnel đang được cài. Chờ hoàn tất rồi bấm 'Kết nối ChatGPT' lại.");
                        EndConnectOperation();
                        return;
                    }

                    Notify("Đang chuẩn bị", "QS3D đang cài Cloudflare Tunnel tự động...");
                    McpCloudflaredBootstrapper.BeginInstall((ok, message) =>
                    {
                        try
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                _lastUiDetail = message;
                                EndConnectOperation();
                                if (!ok)
                                {
                                    Notify("Không kết nối được", Friendly(message));
                                    return;
                                }
                                ConnectChatGpt();
                            }));
                        }
                        catch { EndConnectOperation(); }
                    });
                    return;
                }

                var namedError = string.Empty;
                if (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.SavedHostname)
                    && McpCloudflareAccountTunnelManager.StartSaved(out namedError))
                {
                    _lastUiDetail = "Named Tunnel started for " + McpCloudflareAccountTunnelManager.SavedHostname;
                    Notify("Đã kết nối", "Đã dùng kết nối Cloudflare cố định đã lưu. ChatGPT ↔ QS3D ↔ BricsCAD sẵn sàng.");
                    EndConnectOperation();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.SavedHostname))
                    _lastUiDetail = "Named Tunnel fallback: " + namedError;

                string quickError;
                if (!McpCloudflareAccountTunnelManager.StartQuickTunnel(out quickError))
                {
                    _lastUiDetail = quickError;
                    Notify("Không kết nối được", Friendly(quickError));
                    EndConnectOperation();
                    return;
                }

                Notify("Đang kết nối", "Cloudflare Quick Tunnel đang lấy URL an toàn cho ChatGPT...");
                StartQuickUrlPolling();
            }
            catch (Exception ex)
            {
                _lastUiDetail = ex.ToString();
                Notify("Không kết nối được", Friendly(ex.Message));
                EndConnectOperation();
            }
        }

        private void InstallCloudflared()
        {
            if (McpCloudflaredBootstrapper.IsInstalling)
            {
                Notify("QS3D MCP", "Cloudflare Tunnel đang được tải/cài.");
                return;
            }

            Notify("Đang cài đặt", "QS3D đang tải cloudflared chính thức và kiểm tra Authenticode...");
            McpCloudflaredBootstrapper.BeginInstall((ok, message) =>
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _lastUiDetail = message;
                        Notify(ok ? "Cài đặt hoàn tất" : "Cài đặt thất bại", Friendly(message));
                    }));
                }
                catch { }
            });
        }

        private void Login()
        {
            if (McpCloudflareAccountTunnelManager.IsSetupBusy)
            {
                Notify("QS3D MCP", "Một thao tác Cloudflare khác đang chạy.");
                return;
            }

            Notify("Đăng nhập Cloudflare", "Trình duyệt sẽ mở. Hãy đăng nhập trên trang Cloudflare; QS3D không đọc mật khẩu của bạn.");
            McpCloudflareAccountTunnelManager.BeginBrowserLogin((ok, message) =>
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _lastUiDetail = McpCloudflareAccountTunnelManager.LastError;
                        if (ok)
                        {
                            Notify("Đăng nhập thành công", "Cloudflare đã sẵn sàng để tạo kết nối cố định.");
                            return;
                        }

                        if (McpCloudflareAccountTunnelManager.CertificateImportNeeded)
                        {
                            Notify("Cần chọn cert.pem", "Cloudflare đã tải cert.pem bằng trình duyệt. Chọn file vừa tải để QS3D hoàn tất giúp bạn.");
                            ImportDownloadedCertificate();
                            return;
                        }

                        Notify("Đăng nhập chưa hoàn tất", Friendly(message));
                    }));
                }
                catch { }
            });
        }

        private void ImportDownloadedCertificate()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Chọn cert.pem vừa tải từ Cloudflare",
                Filter = "Cloudflare certificate (cert.pem)|cert.pem|PEM files (*.pem)|*.pem|All files (*.*)|*.*",
                FileName = "cert.pem",
                CheckFileExists = true,
                Multiselect = false
            };
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloads)) dialog.InitialDirectory = downloads;

            if (dialog.ShowDialog(this) != true)
            {
                Notify("Chưa hoàn tất", "Bạn có thể bấm 'Đăng nhập Cloudflare' lại và chọn cert.pem sau.");
                return;
            }

            string error;
            if (!McpCloudflareAccountTunnelManager.ImportDownloadedCertificate(dialog.FileName, out error))
            {
                _lastUiDetail = error;
                Notify("cert.pem không hợp lệ", Friendly(error));
                return;
            }

            _lastUiDetail = "Certificate imported to the current-user Cloudflare profile.";
            Notify("Đăng nhập thành công", "QS3D đã nhập cert.pem an toàn. Bây giờ bạn có thể tạo Named Tunnel.");
        }

        private void Provision()
        {
            StopQuickUrlPolling();
            if (McpCloudflareAccountTunnelManager.IsSetupBusy)
            {
                Notify("QS3D MCP", "Một thao tác Cloudflare khác đang chạy.");
                return;
            }

            Notify("Đang cấu hình", "QS3D đang tạo hoặc reuse Named Tunnel và DNS route...");
            McpCloudflareAccountTunnelManager.BeginProvision(_hostname.Text, (ok, message) =>
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _lastUiDetail = ok ? message : McpCloudflareAccountTunnelManager.LastError;
                        Notify(ok ? "Kết nối cố định sẵn sàng" : "Không tạo được kết nối cố định", Friendly(message));
                    }));
                }
                catch { }
            });
        }

        private void StartQuickUrlPolling()
        {
            StopQuickUrlPolling(false);
            _quickUrlPollTicks = 0;
            _quickUrlTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _quickUrlTimer.Tick += QuickUrlTimerOnTick;
            _quickUrlTimer.Start();
        }

        private void QuickUrlTimerOnTick(object? sender, EventArgs e)
        {
            _quickUrlPollTicks++;
            var publicUrl = McpCloudflareTunnelManager.PublicMcpUrl;
            if (!string.IsNullOrWhiteSpace(publicUrl))
            {
                _lastUiDetail = "Quick Tunnel ready: " + publicUrl;
                StopQuickUrlPolling(false);
                Notify("Đã kết nối", "ChatGPT ↔ QS3D ↔ BricsCAD đã sẵn sàng. Bấm 'Sao chép cấu hình ChatGPT' để tiếp tục.");
                EndConnectOperation();
                return;
            }

            if (!McpCloudflareTunnelManager.IsRunning)
            {
                _lastUiDetail = "Quick Tunnel process stopped before a public URL was discovered.";
                StopQuickUrlPolling(false);
                Notify("Không kết nối được", "Cloudflare Tunnel đã dừng trước khi nhận được URL. Bấm 'Kết nối ChatGPT' để thử lại.");
                EndConnectOperation();
                return;
            }

            if (_quickUrlPollTicks >= 20)
            {
                _lastUiDetail = "Quick Tunnel remained running but no public URL was discovered within 30 seconds.";
                StopQuickUrlPolling(false);
                Notify("Kết nối chưa sẵn sàng", "Tunnel đang chạy nhưng chưa lấy được URL sau 30 giây. Có thể thử lại hoặc mở 'Chi tiết kỹ thuật'.");
                EndConnectOperation();
            }
        }

        private void StopQuickUrlPolling() => StopQuickUrlPolling(true);

        private void StopQuickUrlPolling(bool endConnect)
        {
            var timer = _quickUrlTimer;
            _quickUrlTimer = null;
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= QuickUrlTimerOnTick;
            }
            if (endConnect) EndConnectOperation();
        }

        private void Disconnect()
        {
            StopQuickUrlPolling();
            McpCloudflareAccountTunnelManager.StopForHostShutdown();
            McpCloudflareTunnelManager.StopForHostShutdown();
            _lastUiDetail = "All QS3D-owned Cloudflare tunnel processes stopped for this BricsCAD session.";
            Notify("Đã ngắt kết nối", "Đã dừng các Cloudflare Tunnel do QS3D quản lý trong phiên này.");
        }

        private void Probe()
        {
            try
            {
                McpEmbeddedServer.EnsureStarted();
                var result = McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, 5000);
                _lastUiDetail = result.Message;
                Notify(result.Ready ? "MCP local hoạt động" : "MCP local chưa sẵn sàng", Friendly(result.Message));
            }
            catch (Exception ex)
            {
                _lastUiDetail = ex.ToString();
                Notify("MCP local lỗi", Friendly(ex.Message));
            }
        }

        private void CopyConfig()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                Notify("Chưa có kết nối", "Bấm 'Kết nối ChatGPT' trước, sau đó sao chép cấu hình.");
                return;
            }

            Clipboard.SetText("MCP URL: " + url + Environment.NewLine
                              + "Authorization: Bearer " + McpEmbeddedServer.GetBearerToken());
            Notify("Đã sao chép", "MCP URL và Bearer Token đã nằm trong clipboard. Không chia sẻ Bearer Token công khai.");
        }

        private void ShowTechnicalDetails()
        {
            MessageBox.Show(BuildTechnicalDetails(), "QS3D MCP - Chi tiết kỹ thuật",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string BuildTechnicalDetails()
        {
            var publicUrl = McpPublicEndpointResolver.Resolve();
            return "MCP local: " + McpEmbeddedServer.Endpoint
                   + "\nCloudflare installed: " + (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                   + "\nCloudflare login: " + McpCloudflareAccountTunnelManager.IsAuthenticated
                   + "\nCertificate import needed: " + McpCloudflareAccountTunnelManager.CertificateImportNeeded
                   + "\nSetup busy: " + McpCloudflareAccountTunnelManager.IsSetupBusy
                   + "\nNamed tunnel: " + (McpCloudflareAccountTunnelManager.IsRunning ? "RUNNING" : "STOPPED")
                   + "\nQuick/token tunnel: " + (McpCloudflareTunnelManager.IsRunning ? "RUNNING" : "STOPPED")
                   + "\nPublic MCP: " + (string.IsNullOrWhiteSpace(publicUrl) ? "chưa có" : publicUrl)
                   + (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.LastMessage) ? string.Empty : "\nStatus: " + McpCloudflareAccountTunnelManager.LastMessage)
                   + (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.LastError) ? string.Empty : "\nCloudflare detail: " + McpCloudflareAccountTunnelManager.LastError)
                   + (string.IsNullOrWhiteSpace(_lastUiDetail) ? string.Empty : "\nUI detail: " + _lastUiDetail);
        }

        private void Notify(string title, string message)
        {
            McpToastWindow.Show(this, title, Friendly(message));
        }

        private static string Friendly(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "Không có thêm chi tiết.";
            var clean = Regex.Replace(message.Trim(), "\\s+", " ");
            return clean.Length <= 320 ? clean : clean.Substring(0, 320) + "...";
        }

        private void EndConnectOperation() => Interlocked.Exchange(ref _connectOperationActive, 0);
    }

    internal sealed class McpToastWindow : Window
    {
        private readonly DispatcherTimer _timer;

        private McpToastWindow(Window owner, string title, string message)
        {
            Width = 420;
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.Manual;

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                Margin = new Thickness(0, 0, 0, 6)
            });
            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            });
            Content = new Border
            {
                BorderBrush = SystemColors.ActiveBorderBrush,
                BorderThickness = new Thickness(1),
                Background = SystemColors.WindowBrush,
                Padding = new Thickness(14),
                Child = panel
            };

            _timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(4500)
            };
            _timer.Tick += (_, __) =>
            {
                _timer.Stop();
                try { Close(); } catch { }
            };
            Closed += (_, __) => _timer.Stop();
            Loaded += (_, __) =>
            {
                try
                {
                    Left = owner.Left + Math.Max(12, owner.ActualWidth - ActualWidth - 24);
                    Top = owner.Top + Math.Max(12, owner.ActualHeight - ActualHeight - 48);
                }
                catch { }
                _timer.Start();
            };
        }

        public static void Show(Window owner, string title, string message)
        {
            if (owner == null) return;
            try
            {
                var toast = new McpToastWindow(owner, title, message);
                toast.Show();
            }
            catch { }
        }
    }
}
