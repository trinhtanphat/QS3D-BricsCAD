using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
    /// Default non-technical onboarding. Cloudflare username/password are entered only in the
    /// provider browser opened by cloudflared tunnel login. QS3D không hỏi và không lưu mật khẩu Cloudflare.
    /// </summary>
    internal static class McpCloudflareAccountTunnelManager
    {
        private const string TunnelName = "qs3d-bricscad";
        private const string OriginUrl = "http://127.0.0.1:8765";
        private const int CommandTimeoutMs = 60000;
        private const int LoginTimeoutMs = 10 * 60 * 1000;
        private const int MaxCapturedOutput = 256 * 1024;
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
        public static string SavedHostname => ReadText(HostnamePath);
        public static string PublicMcpUrl => string.IsNullOrWhiteSpace(SavedHostname) ? string.Empty : "https://" + SavedHostname + "/mcp";
        public static string LastMessage { get { lock (Sync) return _lastMessage; } }
        public static string LastError { get { lock (Sync) return _lastError; } }

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
            if (Interlocked.CompareExchange(ref _setupOperationActive, 1, 0) != 0)
            {
                completed(false, "Một thao tác Cloudflare khác đang chạy. Hãy chờ thao tác đó hoàn tất.");
                return;
            }
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                Interlocked.Exchange(ref _setupOperationActive, 0);
                completed(false, "Chưa cài Cloudflare Tunnel.");
                return;
            }
            SetState("Cloudflare login: đang mở trình duyệt...", string.Empty);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string output;
                    string error;
                    var ok = RunCommand(executable, "tunnel login", LoginTimeoutMs, out output, out error);
                    var authenticated = ok && IsAuthenticated;
                    var message = authenticated
                        ? "Cloudflare login: thành công."
                        : "Cloudflare login: " + FirstUsefulError(error, output, "chưa hoàn tất.");
                    SetState(message, authenticated ? string.Empty : message);
                    try { completed(authenticated, message); } catch { }
                }
                finally { Interlocked.Exchange(ref _setupOperationActive, 0); }
            });
        }

        public static void BeginProvision(string hostname, Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var normalized = McpCloudflareTunnelManager.NormalizeHostname(hostname);
            if (string.IsNullOrWhiteSpace(normalized)) { completed(false, "Hostname không hợp lệ. Ví dụ qs3d.example.com"); return; }
            if (!IsAuthenticated) { completed(false, "Hãy bấm Đăng nhập Cloudflare trước."); return; }
            if (Interlocked.CompareExchange(ref _setupOperationActive, 1, 0) != 0)
            {
                completed(false, "Một thao tác Cloudflare khác đang chạy. Hãy chờ thao tác đó hoàn tất.");
                return;
            }
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                Interlocked.Exchange(ref _setupOperationActive, 0);
                completed(false, "Chưa cài Cloudflare Tunnel.");
                return;
            }
            SetState("Đang tự tạo/reuse tunnel và DNS route...", string.Empty);
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
                finally { Interlocked.Exchange(ref _setupOperationActive, 0); }
            });
        }

        public static bool StartSaved(out string error)
        {
            error = string.Empty;
            McpEmbeddedServer.EnsureStarted();
            var executable = CloudflaredPath;
            var id = ReadText(TunnelIdPath);
            if (string.IsNullOrWhiteSpace(executable) || !IsUsableTunnelId(id) || !File.Exists(ConfigPath))
            {
                error = "Named Tunnel chưa được cấu hình.";
                return false;
            }
            var credentials = Path.Combine(CloudflaredDirectory, id + ".json");
            if (!File.Exists(credentials))
            {
                error = "Named Tunnel credentials không còn tồn tại. Hãy cấu hình lại tunnel.";
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
                   + (string.IsNullOrWhiteSpace(PublicMcpUrl) ? string.Empty : "; public=" + PublicMcpUrl)
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
                error = "Không tìm thấy tunnel credentials. Hãy đăng nhập Cloudflare lại.";
                return false;
            }

            string output;
            string routeError;
            if (!RunCommand(executable, "tunnel route dns " + tunnelId + " " + hostname, CommandTimeoutMs, out output, out routeError))
            {
                var combined = (output + "\n" + routeError).Trim();
                if (combined.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    error = "Không tạo được DNS route: " + FirstUsefulError(routeError, output, "Cloudflare route dns failed.");
                    return false;
                }
            }

            Directory.CreateDirectory(SettingsDirectory);
            var yamlCredentials = credentials.Replace('\\', '/').Replace("\"", "\\\"");
            var config = "tunnel: " + tunnelId + "\r\n"
                         + "credentials-file: \"" + yamlCredentials + "\"\r\n"
                         + "url: " + OriginUrl + "\r\n";
            File.WriteAllText(ConfigPath, config, new UTF8Encoding(false));
            WriteText(TunnelIdPath, tunnelId);
            WriteText(HostnamePath, hostname);
            WriteText(AutoStartPath, "1");
            McpCloudflareTunnelManager.StopForHostShutdown();
            StopProcess();
            return StartProcess(executable, "tunnel --config \"" + ConfigPath + "\" run " + tunnelId, out error);
        }

        private static bool ResolveOrCreateTunnel(string executable, out string tunnelId, out string error)
        {
            error = string.Empty;
            tunnelId = ReadText(TunnelIdPath);
            if (IsUsableTunnelId(tunnelId)
                && File.Exists(Path.Combine(CloudflaredDirectory, tunnelId + ".json")))
                return true;

            string output;
            string commandError;
            if (RunCommand(executable, "tunnel list", CommandTimeoutMs, out output, out commandError))
            {
                tunnelId = FindTunnelIdByName(output, TunnelName);
                if (IsUsableTunnelId(tunnelId)
                    && File.Exists(Path.Combine(CloudflaredDirectory, tunnelId + ".json")))
                {
                    WriteText(TunnelIdPath, tunnelId);
                    return true;
                }
            }

            if (!RunCommand(executable, "tunnel create " + TunnelName, CommandTimeoutMs, out output, out commandError))
            {
                string listOutput;
                string listError;
                if (RunCommand(executable, "tunnel list", CommandTimeoutMs, out listOutput, out listError))
                {
                    tunnelId = FindTunnelIdByName(listOutput, TunnelName);
                    if (IsUsableTunnelId(tunnelId)
                        && File.Exists(Path.Combine(CloudflaredDirectory, tunnelId + ".json")))
                    {
                        WriteText(TunnelIdPath, tunnelId);
                        return true;
                    }
                }
                error = "Không tạo/reuse được tunnel '" + TunnelName + "': " + FirstUsefulError(commandError, output, "Cloudflare tunnel create failed.");
                return false;
            }

            var match = UuidSearchRegex.Match(output ?? string.Empty);
            tunnelId = match.Success ? match.Groups["id"].Value : string.Empty;
            if (!IsUsableTunnelId(tunnelId))
            {
                string listOutput;
                string listError;
                if (RunCommand(executable, "tunnel list", CommandTimeoutMs, out listOutput, out listError))
                    tunnelId = FindTunnelIdByName(listOutput, TunnelName);
            }
            if (!IsUsableTunnelId(tunnelId))
            {
                error = "Không đọc được tunnel UUID sau khi tạo.";
                return false;
            }
            WriteText(TunnelIdPath, tunnelId);
            return true;
        }

        private static string FindTunnelIdByName(string output, string name)
        {
            if (string.IsNullOrWhiteSpace(output)) return string.Empty;
            using (var reader = new StringReader(output))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!LineContainsTunnelName(line, name)) continue;
                    var match = UuidSearchRegex.Match(line);
                    if (match.Success) return match.Groups["id"].Value;
                }
            }
            return string.Empty;
        }

        private static bool LineContainsTunnelName(string line, string name)
        {
            if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(name)) return false;
            return Regex.IsMatch(line, "(^|\\s)" + Regex.Escape(name) + "(\\s|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static bool IsUsableTunnelId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && UuidRegex.IsMatch(value.Trim());
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

                    // The parameterless WaitForExit is the documented drain boundary for async
                    // BeginOutputReadLine/BeginErrorReadLine callbacks. Call it only after exit was
                    // observed (or after a successful kill) so no callback can race a disposed object.
                    try
                    {
                        if (process.HasExited) process.WaitForExit();
                    }
                    catch { }

                    output = stdout.ToString().Trim();
                    error = stderr.ToString().Trim();
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
            try
            {
                var process = new Process
                {
                    StartInfo = CreateStartInfo(executable, arguments),
                    EnableRaisingEvents = true
                };
                process.OutputDataReceived += (_, args) => HandleRunLine(args.Data, false);
                process.ErrorDataReceived += (_, args) => HandleRunLine(args.Data, true);
                process.Exited += (_, __) => HandleProcessExit(process);
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
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                SetState(error, error);
                return false;
            }
        }

        private static void HandleProcessExit(Process process)
        {
            int? exitCode = null;
            try { exitCode = process.ExitCode; } catch { }
            lock (Sync)
            {
                if (ReferenceEquals(_process, process))
                {
                    _process = null;
                    if (exitCode.HasValue && exitCode.Value != 0 && string.IsNullOrWhiteSpace(_lastError))
                        _lastError = "Named Tunnel exited with code " + exitCode.Value.ToString() + ".";
                    _lastMessage = exitCode.HasValue
                        ? "Named Tunnel đã dừng (exit " + exitCode.Value.ToString() + ")."
                        : "Named Tunnel đã dừng.";
                }
            }
            try { process.Dispose(); } catch { }
        }

        private static void HandleRunLine(string? line, bool stderr)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            var clean = line.Trim();
            if (clean.Length > 500) clean = clean.Substring(0, 500);
            lock (Sync)
            {
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
            try { if (!process.HasExited) process.Kill(); } catch { }
            try { if (!process.HasExited) process.WaitForExit(2000); } catch { }
            try { process.Dispose(); } catch { }
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
        private readonly TextBlock _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10) };

        public McpCloudflareAccountSetupWindow()
        {
            Title = "QS3D - Kết nối ChatGPT MCP";
            Width = 640;
            Height = 590;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(new TextBlock
            {
                Text = "Thiết lập bằng click: cài Cloudflare Tunnel nếu cần, đăng nhập trong trình duyệt, nhập hostname rồi bấm kết nối. QS3D không hỏi và không lưu mật khẩu Cloudflare.",
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            });
            panel.Children.Add(Button("1. Cài Cloudflare Tunnel", (_, __) => McpCloudflareAccountTunnelManager.OpenInstallerPage()));
            panel.Children.Add(Button("2. Đăng nhập Cloudflare", (_, __) => Login()));
            panel.Children.Add(new TextBlock { Text = "3. Hostname public (ví dụ qs3d.example.com):", Margin = new Thickness(0, 10, 0, 0) });
            _hostname.Text = McpCloudflareAccountTunnelManager.SavedHostname;
            panel.Children.Add(_hostname);
            panel.Children.Add(Button("4. Tạo / reuse tunnel + kết nối", (_, __) => Provision()));
            panel.Children.Add(Button("Quick Tunnel - chỉ dùng thử", (_, __) => Quick()));
            panel.Children.Add(Button("Copy MCP URL", (_, __) => CopyUrl()));
            panel.Children.Add(Button("Copy Bearer Token", (_, __) => Clipboard.SetText(McpEmbeddedServer.GetBearerToken())));
            panel.Children.Add(Button("Kiểm tra MCP local", (_, __) => Probe()));
            panel.Children.Add(Button("Mở ChatGPT", (_, __) => McpCloudflareAccountTunnelManager.OpenChatGpt()));
            panel.Children.Add(Button("Mở Cloudflare Dashboard", (_, __) => McpCloudflareAccountTunnelManager.OpenCloudflareDashboard()));
            panel.Children.Add(Button("Dừng Named Tunnel", (_, __) => { McpCloudflareAccountTunnelManager.Stop(); Refresh(); }));
            panel.Children.Add(_status);
            panel.Children.Add(Button("Đóng", (_, __) => Close()));
            Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Refresh();
        }

        private static Button Button(string text, RoutedEventHandler handler)
        {
            var button = new Button { Content = text, Margin = new Thickness(0, 4, 0, 4), MinHeight = 32 };
            button.Click += handler;
            return button;
        }

        private void Login()
        {
            if (McpCloudflareAccountTunnelManager.IsSetupBusy) { MessageBox.Show("Một thao tác Cloudflare đang chạy.", "QS3D MCP"); return; }
            _status.Text = "Cloudflare login: chờ trình duyệt...";
            McpCloudflareAccountTunnelManager.BeginBrowserLogin((ok, message) =>
                Dispatcher.BeginInvoke(new Action(() => { _status.Text = message; Refresh(); })));
        }

        private void Provision()
        {
            if (McpCloudflareAccountTunnelManager.IsSetupBusy) { MessageBox.Show("Một thao tác Cloudflare đang chạy.", "QS3D MCP"); return; }
            _status.Text = "Đang cấu hình Named Tunnel...";
            McpCloudflareAccountTunnelManager.BeginProvision(_hostname.Text, (ok, message) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _status.Text = message;
                    if (!ok) MessageBox.Show(message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Refresh();
                })));
        }

        private void Quick()
        {
            if (McpCloudflareAccountTunnelManager.IsSetupBusy) { MessageBox.Show("Một thao tác Cloudflare đang chạy.", "QS3D MCP"); return; }
            string error;
            if (!McpCloudflareAccountTunnelManager.StartQuickTunnel(out error))
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
            Refresh();
        }

        private void Probe()
        {
            McpEmbeddedServer.EnsureStarted();
            var result = McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, 5000);
            MessageBox.Show(result.Message, "QS3D MCP local check", MessageBoxButton.OK,
                result.Ready ? MessageBoxImage.Information : MessageBoxImage.Warning);
            Refresh();
        }

        private void CopyUrl()
        {
            var url = McpCloudflareAccountTunnelManager.PublicMcpUrl;
            if (string.IsNullOrWhiteSpace(url)) url = McpCloudflareTunnelManager.PublicMcpUrl;
            if (string.IsNullOrWhiteSpace(url)) { MessageBox.Show("Chưa có public MCP URL.", "QS3D MCP"); return; }
            Clipboard.SetText(url);
        }

        private void Refresh()
        {
            var publicUrl = McpCloudflareAccountTunnelManager.PublicMcpUrl;
            if (string.IsNullOrWhiteSpace(publicUrl)) publicUrl = McpCloudflareTunnelManager.PublicMcpUrl;
            _status.Text = "MCP local: " + McpEmbeddedServer.Endpoint
                           + "\nCloudflare installed: " + (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                           + "\nCloudflare login: " + McpCloudflareAccountTunnelManager.IsAuthenticated
                           + "\nSetup busy: " + McpCloudflareAccountTunnelManager.IsSetupBusy
                           + "\nNamed tunnel: " + (McpCloudflareAccountTunnelManager.IsRunning ? "RUNNING" : "STOPPED")
                           + "\nQuick/token tunnel: " + (McpCloudflareTunnelManager.IsRunning ? "RUNNING" : "STOPPED")
                           + "\nPublic MCP: " + (string.IsNullOrWhiteSpace(publicUrl) ? "chưa có" : publicUrl)
                           + (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.LastMessage) ? string.Empty : "\nStatus: " + McpCloudflareAccountTunnelManager.LastMessage)
                           + (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.LastError) ? string.Empty : "\nLỗi: " + McpCloudflareAccountTunnelManager.LastError);
        }
    }
}
