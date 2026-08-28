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
        private static readonly object Sync = new object();
        private static readonly Regex UuidRegex = new Regex(
            "(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Process? _process;
        private static string _lastMessage = string.Empty;
        private static string _lastError = string.Empty;

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
        public static bool StartQuickTunnel(out string error) => McpCloudflareTunnelManager.StartQuickTunnel(out error);

        public static void BeginBrowserLogin(Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable)) { completed(false, "Chưa cài Cloudflare Tunnel."); return; }
            SetState("Cloudflare login: đang mở trình duyệt...", string.Empty);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string output;
                string error;
                var ok = RunCommand(executable, "tunnel login", LoginTimeoutMs, out output, out error);
                var authenticated = ok && IsAuthenticated;
                var message = authenticated ? "Cloudflare login: thành công." : "Cloudflare login: " + (string.IsNullOrWhiteSpace(error) ? "chưa hoàn tất." : error);
                SetState(message, authenticated ? string.Empty : message);
                try { completed(authenticated, message); } catch { }
            });
        }

        public static void BeginProvision(string hostname, Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var normalized = McpCloudflareTunnelManager.NormalizeHostname(hostname);
            if (string.IsNullOrWhiteSpace(normalized)) { completed(false, "Hostname không hợp lệ. Ví dụ qs3d.example.com"); return; }
            if (!IsAuthenticated) { completed(false, "Hãy bấm Đăng nhập Cloudflare trước."); return; }
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable)) { completed(false, "Chưa cài Cloudflare Tunnel."); return; }
            SetState("Đang tự tạo/reuse tunnel và DNS route...", string.Empty);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string error;
                var ok = Provision(executable, normalized, out error);
                var message = ok ? "Named Tunnel đã kết nối: " + PublicMcpUrl : error;
                SetState(message, ok ? string.Empty : error);
                try { completed(ok, message); } catch { }
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
                   + (string.IsNullOrWhiteSpace(PublicMcpUrl) ? string.Empty : "; public=" + PublicMcpUrl)
                   + (string.IsNullOrWhiteSpace(LastError) ? string.Empty : "; error=" + LastError);
        }

        private static bool Provision(string executable, string hostname, out string error)
        {
            error = string.Empty;
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
                    error = "Không tạo được DNS route: " + combined;
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
            StopProcess();
            return StartProcess(executable, "tunnel --config \"" + ConfigPath + "\" run " + tunnelId, out error);
        }

        private static bool ResolveOrCreateTunnel(string executable, out string tunnelId, out string error)
        {
            error = string.Empty;
            tunnelId = ReadText(TunnelIdPath);
            if (IsUsableTunnelId(tunnelId)) return true;

            string output;
            string commandError;
            if (RunCommand(executable, "tunnel list", CommandTimeoutMs, out output, out commandError))
            {
                tunnelId = FindTunnelIdByName(output, TunnelName);
                if (IsUsableTunnelId(tunnelId)) { WriteText(TunnelIdPath, tunnelId); return true; }
            }

            if (!RunCommand(executable, "tunnel create " + TunnelName, CommandTimeoutMs, out output, out commandError))
            {
                string listOutput;
                string listError;
                if (RunCommand(executable, "tunnel list", CommandTimeoutMs, out listOutput, out listError))
                {
                    tunnelId = FindTunnelIdByName(listOutput, TunnelName);
                    if (IsUsableTunnelId(tunnelId)) { WriteText(TunnelIdPath, tunnelId); return true; }
                }
                error = "Không tạo/reuse được tunnel '" + TunnelName + "': " + commandError;
                return false;
            }

            var match = UuidRegex.Match(output ?? string.Empty);
            tunnelId = match.Success ? match.Groups["id"].Value : string.Empty;
            if (!IsUsableTunnelId(tunnelId))
            {
                string listOutput;
                string listError;
                if (RunCommand(executable, "tunnel list", CommandTimeoutMs, out listOutput, out listError))
                    tunnelId = FindTunnelIdByName(listOutput, TunnelName);
            }
            if (!IsUsableTunnelId(tunnelId)) { error = "Không đọc được tunnel UUID sau khi tạo."; return false; }
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
                    if (line.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var match = UuidRegex.Match(line);
                    if (match.Success) return match.Groups["id"].Value;
                }
            }
            return string.Empty;
        }

        private static bool IsUsableTunnelId(string value) => !string.IsNullOrWhiteSpace(value) && UuidRegex.IsMatch(value.Trim());

        private static bool RunCommand(string executable, string arguments, int timeoutMs, out string output, out string error)
        {
            output = string.Empty;
            error = string.Empty;
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
                    };
                    if (!process.Start()) { error = "Không khởi động được cloudflared."; return false; }
                    var stdout = process.StandardOutput.ReadToEnd();
                    var stderr = process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(timeoutMs))
                    {
                        try { process.Kill(); } catch { }
                        error = "Cloudflare thao tác quá thời gian chờ.";
                        return false;
                    }
                    output = stdout.Trim();
                    error = stderr.Trim();
                    if (process.ExitCode == 0) return true;
                    if (string.IsNullOrWhiteSpace(error)) error = output;
                    return false;
                }
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        private static bool StartProcess(string executable, string arguments, out string error)
        {
            error = string.Empty;
            StopProcess();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = arguments,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
                    },
                    EnableRaisingEvents = true
                };
                process.OutputDataReceived += (_, args) => HandleRunLine(args.Data, false);
                process.ErrorDataReceived += (_, args) => HandleRunLine(args.Data, true);
                process.Exited += (_, __) =>
                {
                    lock (Sync) { if (ReferenceEquals(_process, process)) _process = null; }
                    try { process.Dispose(); } catch { }
                };
                if (!process.Start()) { process.Dispose(); error = "Không khởi động được Named Tunnel."; return false; }
                lock (Sync) { _process = process; _lastMessage = "Named Tunnel đang kết nối..."; _lastError = string.Empty; }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return true;
            }
            catch (Exception ex) { error = ex.Message; SetState(error, error); return false; }
        }

        private static void HandleRunLine(string? line, bool stderr)
        {
            if (line == null || line.Length == 0) return;
            var clean = line.Trim();
            lock (Sync)
            {
                _lastMessage = clean.Length > 500 ? clean.Substring(0, 500) : clean;
                if (stderr && clean.IndexOf("ERR", StringComparison.OrdinalIgnoreCase) >= 0) _lastError = _lastMessage;
            }
        }

        private static void StopProcess()
        {
            Process? process;
            lock (Sync) { process = _process; _process = null; }
            if (process == null) return;
            try { if (!process.HasExited) process.Kill(); } catch { }
            try { process.Dispose(); } catch { }
        }

        private static void SetState(string message, string error)
        {
            lock (Sync) { _lastMessage = message ?? string.Empty; _lastError = error ?? string.Empty; }
        }

        private static void WriteText(string path, string value)
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(path, value ?? string.Empty, new UTF8Encoding(false));
        }

        private static string ReadText(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : string.Empty; } catch { return string.Empty; }
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
            Height = 560;
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
            _status.Text = "Cloudflare login: chờ trình duyệt...";
            McpCloudflareAccountTunnelManager.BeginBrowserLogin((ok, message) =>
                Dispatcher.BeginInvoke(new Action(() => { _status.Text = message; Refresh(); })));
        }

        private void Provision()
        {
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
            string error;
            if (!McpCloudflareAccountTunnelManager.StartQuickTunnel(out error))
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            _status.Text = "MCP local: " + McpEmbeddedServer.Endpoint
                           + "\nCloudflare installed: " + (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                           + "\nCloudflare login: " + McpCloudflareAccountTunnelManager.IsAuthenticated
                           + "\nNamed tunnel: " + (McpCloudflareAccountTunnelManager.IsRunning ? "RUNNING" : "STOPPED")
                           + "\nPublic MCP: " + (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.PublicMcpUrl) ? "chưa có" : McpCloudflareAccountTunnelManager.PublicMcpUrl)
                           + (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.LastError) ? string.Empty : "\nLỗi: " + McpCloudflareAccountTunnelManager.LastError);
        }
    }
}
