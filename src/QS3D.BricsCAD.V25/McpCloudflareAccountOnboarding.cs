using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Default end-user setup: all provider credentials are entered in Cloudflare's browser UI.
    /// QS3D runs cloudflared commands hidden and never asks for or stores the Cloudflare password.
    /// </summary>
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

    internal static class McpCloudflareAccountTunnelManager
    {
        private const string TunnelName = "qs3d-bricscad";
        private const string OriginUrl = "http://127.0.0.1:8765";
        private const int ShortCommandTimeoutMs = 60000;
        private const int LoginTimeoutMs = 10 * 60 * 1000;
        private static readonly object Sync = new object();
        private static readonly Regex UuidRegex = new Regex(
            "(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Process? _process;
        private static string _lastMessage = string.Empty;
        private static string _lastError = string.Empty;

        public static event Action? StateChanged;

        private static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QS3D", "MCP", "CloudflareAccount");
        private static string TunnelIdPath => Path.Combine(SettingsDirectory, "tunnel-id.txt");
        private static string HostnamePath => Path.Combine(SettingsDirectory, "hostname.txt");
        private static string ConfigPath => Path.Combine(SettingsDirectory, "config.yml");
        private static string AutoStartPath => Path.Combine(SettingsDirectory, "autostart.txt");

        public static string CloudflaredPath => McpCloudflareTunnelManager.CloudflaredPath;

        public static string CloudflaredDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cloudflared");

        public static string CertificatePath => Path.Combine(CloudflaredDirectory, "cert.pem");

        public static bool IsAuthenticated
        {
            get { try { return File.Exists(CertificatePath); } catch { return false; } }
        }

        public static bool IsRunning
        {
            get
            {
                lock (Sync)
                {
                    if (_process == null) return false;
                    try { return !_process.HasExited; }
                    catch { return false; }
                }
            }
        }

        public static string SavedHostname
        {
            get
            {
                try { return File.Exists(HostnamePath) ? File.ReadAllText(HostnamePath, Encoding.UTF8).Trim() : string.Empty; }
                catch { return string.Empty; }
            }
        }

        public static string PublicMcpUrl => string.IsNullOrWhiteSpace(SavedHostname)
            ? string.Empty
            : "https://" + SavedHostname + "/mcp";

        public static string LastMessage { get { lock (Sync) return _lastMessage; } }
        public static string LastError { get { lock (Sync) return _lastError; } }

        public static void OpenInstallerPage() => McpCloudflareTunnelManager.OpenCloudflaredDownloadPage();
        public static void OpenCloudflareDashboard() => McpCloudflareTunnelManager.OpenCloudflareTunnelDashboard();
        public static void OpenChatGpt() => McpCloudflareTunnelManager.OpenChatGpt();

        public static void BeginBrowserLogin(Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                completed(false, "Chưa cài Cloudflare Tunnel.");
                return;
            }

            SetState("Đang mở trình duyệt đăng nhập Cloudflare...", string.Empty);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string output;
                string error;
                var ok = RunCommand(executable, "tunnel login", LoginTimeoutMs, out output, out error);
                var authenticated = IsAuthenticated;
                var message = authenticated
                    ? "Đăng nhập Cloudflare thành công."
                    : (ok ? "Cloudflare login kết thúc nhưng chưa thấy cert.pem." : error);
                SetState(message, authenticated ? string.Empty : message);
                try { completed(authenticated, message); } catch { }
            });
        }

        public static void BeginProvision(string hostname, Action<bool, string> completed)
        {
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            var normalized = McpCloudflareTunnelManager.NormalizeHostname(hostname);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                completed(false, "Hostname không hợp lệ. Ví dụ: qs3d.example.com");
                return;
            }
            if (!IsAuthenticated)
            {
                completed(false, "Hãy bấm Đăng nhập Cloudflare trước.");
                return;
            }
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                completed(false, "Chưa cài Cloudflare Tunnel.");
                return;
            }

            SetState("Đang tự tạo/cấu hình Named Tunnel...", string.Empty);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string error;
                var ok = Provision(executable, normalized, out error);
                SetState(ok ? "Named Tunnel đã sẵn sàng." : error, ok ? string.Empty : error);
                try { completed(ok, ok ? "Named Tunnel đã sẵn sàng." : error); } catch { }
            });
        }

        public static bool StartSaved(out string error)
        {
            error = string.Empty;
            McpEmbeddedServer.EnsureStarted();
            var executable = CloudflaredPath;
            var id = ReadText(TunnelIdPath);
            if (string.IsNullOrWhiteSpace(executable) || string.IsNullOrWhiteSpace(id) || !File.Exists(ConfigPath))
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
                   + (string.IsNullOrWhiteSpace(SavedHostname) ? string.Empty : "; public=" + PublicMcpUrl)
                   + (string.IsNullOrWhiteSpace(LastError) ? string.Empty : "; error=" + LastError);
        }

        private static bool Provision(string executable, string hostname, out string error)
        {
            error = string.Empty;
            string tunnelId;
            if (!TryResolveOrCreateTunnel(executable, out tunnelId, out error)) return false;

            var credentials = Path.Combine(CloudflaredDirectory, tunnelId + ".json");
            if (!File.Exists(credentials))
            {
                error = "Không tìm thấy tunnel credentials cho " + tunnelId + ". Hãy đăng nhập lại Cloudflare rồi thử lại.";
                return false;
            }

            string output;
            string routeError;
            var routed = RunCommand(
                executable,
                "tunnel route dns " + tunnelId + " " + hostname,
                ShortCommandTimeoutMs,
                out output,
                out routeError);
            if (!routed)
            {
                var combined = (output + "\n" + routeError).Trim();
                // Re-running setup after a successful route may report that the DNS record exists.
                // Accept only the known idempotent case; other DNS errors remain fail-closed.
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

        private static bool TryResolveOrCreateTunnel(string executable, out string tunnelId, out string error)
        {
            tunnelId = ReadText(TunnelIdPath);
            error = string.Empty;
            if (IsUsableTunnelId(tunnelId)) return true;

            string output;
            string commandError;
            if (RunCommand(executable, "tunnel list", ShortCommandTimeoutMs, out output, out commandError))
            {
                tunnelId = FindTunnelIdByName(output, TunnelName);
                if (IsUsableTunnelId(tunnelId))
                {
                    WriteText(TunnelIdPath, tunnelId);
                    return true;
                }
            }

            if (!RunCommand(executable, "tunnel create " + TunnelName, ShortCommandTimeoutMs, out output, out commandError))
            {
                // A previous setup may already own this name. Resolve it from the authoritative list.
                string listOutput;
                string listError;
                if (RunCommand(executable, "tunnel list", ShortCommandTimeoutMs, out listOutput, out listError))
                {
                    tunnelId = FindTunnelIdByName(listOutput, TunnelName);
                    if (IsUsableTunnelId(tunnelId))
                    {
                        WriteText(TunnelIdPath, tunnelId);
                        return true;
                    }
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
                if (RunCommand(executable, "tunnel list", ShortCommandTimeoutMs, out listOutput, out listError))
                    tunnelId = FindTunnelIdByName(listOutput, TunnelName);
            }
            if (!IsUsableTunnelId(tunnelId))
            {
                error = "Cloudflare đã chạy create tunnel nhưng QS3D không đọc được tunnel UUID.";
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
                    if (line.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var match = UuidRegex.Match(line);
                    if (match.Success) return match.Groups["id"].Value;
                }
            }
            return string.Empty;
        }

        private static bool IsUsableTunnelId(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && UuidRegex.IsMatch(value.Trim());
        }

        private static bool RunCommand(string executable, string arguments, int timeoutMs, out string output, out string error)
        {
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
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
                    process.OutputDataReceived += (_, args) => { if (!string.IsNullOrWhiteSpace(args.Data)) lock (stdout) stdout.AppendLine(args.Data); };
                    process.ErrorDataReceived += (_, args) => { if (!string.IsNullOrWhiteSpace(args.Data)) lock (stderr) stderr.AppendLine(args.Data); };
                    if (!process.Start())
                    {
                        error = "Không khởi động được cloudflared.";
                        return false;
                    }
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    if (!process.WaitForExit(timeoutMs))
                    {
                        try { process.Kill(); } catch { }
                        error = "Cloudflare thao tác quá thời gian chờ.";
                        return false;
                    }
                    process.WaitForExit();
                    lock (stdout) output = stdout.ToString().Trim();
                    lock (stderr) error = stderr.ToString().Trim();
                    if (process.ExitCode == 0) return true;
                    if (string.IsNullOrWhiteSpace(error)) error = output;
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
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
                    lock (Sync)
                    {
                        if (ReferenceEquals(_process, process)) _process = null;
                    }
                    try { process.Dispose(); } catch { }
                    RaiseChanged();
                };
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
                RaiseChanged();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                SetState(error, error);
                return false;
            }
        }

        private static void HandleRunLine(string? line, bool stderr)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            var clean = line.Trim();
            lock (Sync)
            {
                _lastMessage = clean.Length > 500 ? clean.Substring(0, 500) : clean;
                if (stderr && clean.IndexOf("ERR", StringComparison.OrdinalIgnoreCase) >= 0) _lastError = _lastMessage;
            }
            RaiseChanged();
        }

        private static void StopProcess()
        {
            Process? process;
            lock (Sync)
            {
                process = _process;
                _process = null;
            }
            if (process == null) return;
            try { if (!process.HasExited) process.Kill(); } catch { }
            try { process.Dispose(); } catch { }
            RaiseChanged();
        }

        private static string ReadText(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : string.Empty; }
            catch { return string.Empty; }
        }

        private static void WriteText(string path, string value)
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(path, value ?? string.Empty, new UTF8Encoding(false));
        }

        private static void SetState(string message, string error)
        {
            lock (Sync)
            {
                _lastMessage = message ?? string.Empty;
                _lastError = error ?? string.Empty;
            }
            RaiseChanged();
        }

        private static void RaiseChanged()
        {
            try { StateChanged?.Invoke(); } catch { }
        }
    }

    internal sealed class McpCloudflareAccountSetupWindow : Window
    {
        private readonly TextBlock _status;
        private readonly TextBox _hostname;
        private readonly Button _login;
        private readonly Button _connect;
        private readonly Button _quick;
        private readonly Button _copyUrl;
        private readonly Button _copyBearer;
        private readonly Button _openChatGpt;

        public McpCloudflareAccountSetupWindow()
        {
            Title = "QS3D - Kết nối ChatGPT";
            Width = 700;
            Height = 620;
            MinWidth = 620;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.White;

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(24) };
            scroll.Content = panel;
            Content = scroll;

            panel.Children.Add(new TextBlock
            {
                Text = "Kết nối ChatGPT với QS3D",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Không cần PowerShell/CMD. QS3D không hỏi và không lưu mật khẩu Cloudflare. Bạn chỉ đăng nhập username/password trong trình duyệt Cloudflare chính thức.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 0, 0, 18)
            });

            _status = new TextBlock
            {
                Background = Brushes.WhiteSmoke,
                Padding = new Thickness(12),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18)
            };
            panel.Children.Add(_status);

            panel.Children.Add(TitleBlock("1. Cài Cloudflare Tunnel"));
            panel.Children.Add(new TextBlock
            {
                Text = "Chỉ làm một lần. Bấm nút dưới đây, cài bằng giao diện Windows rồi quay lại bấm Làm mới.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 8)
            });
            var installRow = new WrapPanel();
            installRow.Children.Add(ActionButton("Cài Cloudflare Tunnel", (_, __) => McpCloudflareAccountTunnelManager.OpenInstallerPage()));
            installRow.Children.Add(ActionButton("Làm mới", (_, __) => RefreshUi()));
            panel.Children.Add(installRow);

            panel.Children.Add(TitleBlock("2. Đăng nhập Cloudflare"));
            panel.Children.Add(new TextBlock
            {
                Text = "Bấm Đăng nhập. Browser sẽ mở; nhập tài khoản/mật khẩu Cloudflare và chọn domain. Khi browser báo thành công, quay lại QS3D.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 8)
            });
            _login = ActionButton("Đăng nhập Cloudflare", (_, __) => BeginLogin());
            panel.Children.Add(_login);

            panel.Children.Add(TitleBlock("3. Chọn địa chỉ cho QS3D"));
            panel.Children.Add(new TextBlock
            {
                Text = "Nhập một subdomain thuộc domain đang quản lý trên Cloudflare, ví dụ qs3d.example.com. QS3D sẽ tự tạo/reuse tunnel, DNS route và nối về MCP local.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 6)
            });
            _hostname = new TextBox
            {
                Text = McpCloudflareAccountTunnelManager.SavedHostname,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(_hostname);
            _connect = ActionButton("Tạo và kết nối tự động", (_, __) => BeginProvision());
            panel.Children.Add(_connect);

            panel.Children.Add(TitleBlock("Không có domain? Test nhanh"));
            panel.Children.Add(new TextBlock
            {
                Text = "Quick Tunnel tạo URL trycloudflare.com tạm thời bằng một click. Chỉ dùng thử; URL sẽ đổi khi dừng/restart.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 8)
            });
            _quick = ActionButton("Tạo Quick Tunnel", (_, __) =>
            {
                McpCloudflareAccountTunnelManager.StopForHostShutdown();
                string error;
                if (!McpCloudflareTunnelManager.StartQuickTunnel(out error))
                    MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshUi();
            });
            panel.Children.Add(_quick);

            panel.Children.Add(TitleBlock("4. Mở ChatGPT"));
            panel.Children.Add(new TextBlock
            {
                Text = "Khi Public MCP URL xuất hiện, copy URL và Bearer token rồi bấm Mở ChatGPT. Trong ChatGPT tạo custom MCP/App bằng hai giá trị này.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 8)
            });
            var chatRow = new WrapPanel();
            _copyUrl = ActionButton("Copy MCP URL", (_, __) => CopyUrl(true));
            _copyBearer = ActionButton("Copy Bearer Token", (_, __) =>
            {
                Clipboard.SetText(McpEmbeddedServer.GetBearerToken());
                MessageBox.Show("Đã copy Bearer token.", "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            _openChatGpt = ActionButton("Mở ChatGPT", (_, __) =>
            {
                CopyUrl(false);
                McpCloudflareAccountTunnelManager.OpenChatGpt();
            });
            chatRow.Children.Add(_copyUrl);
            chatRow.Children.Add(_copyBearer);
            chatRow.Children.Add(_openChatGpt);
            panel.Children.Add(chatRow);

            McpCloudflareAccountTunnelManager.StateChanged += OnChanged;
            McpCloudflareTunnelManager.StateChanged += OnChanged;
            Closed += (_, __) =>
            {
                McpCloudflareAccountTunnelManager.StateChanged -= OnChanged;
                McpCloudflareTunnelManager.StateChanged -= OnChanged;
            };
            RefreshUi();
        }

        private void BeginLogin()
        {
            SetBusy(true);
            McpCloudflareAccountTunnelManager.BeginBrowserLogin((ok, message) => Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    SetBusy(false);
                    RefreshUi();
                    MessageBox.Show(message, "QS3D MCP", MessageBoxButton.OK,
                        ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
                })));
        }

        private void BeginProvision()
        {
            SetBusy(true);
            McpCloudflareTunnelManager.StopForHostShutdown();
            McpCloudflareAccountTunnelManager.BeginProvision(_hostname.Text, (ok, message) => Dispatcher.BeginInvoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    SetBusy(false);
                    RefreshUi();
                    MessageBox.Show(message, "QS3D MCP", MessageBoxButton.OK,
                        ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
                })));
        }

        private void SetBusy(bool busy)
        {
            _login.IsEnabled = !busy && !string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath);
            _connect.IsEnabled = !busy && McpCloudflareAccountTunnelManager.IsAuthenticated;
            _quick.IsEnabled = !busy && !string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath);
        }

        private string CurrentPublicUrl()
        {
            if (McpCloudflareAccountTunnelManager.IsRunning && !string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.PublicMcpUrl))
                return McpCloudflareAccountTunnelManager.PublicMcpUrl;
            return McpCloudflareTunnelManager.PublicMcpUrl;
        }

        private void CopyUrl(bool showMessage)
        {
            var url = CurrentPublicUrl();
            if (string.IsNullOrWhiteSpace(url))
            {
                if (showMessage) MessageBox.Show("Chưa có Public MCP URL.", "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Clipboard.SetText(url);
            if (showMessage) MessageBox.Show("Đã copy: " + url, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshUi));
                return;
            }
            RefreshUi();
        }

        private void RefreshUi()
        {
            var protocol = McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, 2000);
            var executable = McpCloudflareAccountTunnelManager.CloudflaredPath;
            var publicUrl = CurrentPublicUrl();
            var builder = new StringBuilder();
            builder.AppendLine("QS3D MCP: " + (protocol.Ready ? "READY" : "NOT READY"));
            builder.AppendLine("Cloudflare Tunnel: " + (string.IsNullOrWhiteSpace(executable) ? "chưa cài" : "đã cài"));
            builder.AppendLine("Cloudflare login: " + (McpCloudflareAccountTunnelManager.IsAuthenticated ? "Đã đăng nhập" : "Chưa đăng nhập"));
            builder.AppendLine("Named Tunnel: " + (McpCloudflareAccountTunnelManager.IsRunning ? "RUNNING" : "STOPPED"));
            if (McpCloudflareTunnelManager.IsQuickMode) builder.AppendLine("Quick Tunnel: RUNNING");
            if (!string.IsNullOrWhiteSpace(publicUrl)) builder.AppendLine("Public MCP URL: " + publicUrl);
            if (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.LastMessage)) builder.AppendLine("Status: " + McpCloudflareAccountTunnelManager.LastMessage);
            if (!string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.LastError)) builder.AppendLine("Lỗi: " + McpCloudflareAccountTunnelManager.LastError);
            _status.Text = builder.ToString().TrimEnd();

            _login.IsEnabled = !string.IsNullOrWhiteSpace(executable);
            _connect.IsEnabled = !string.IsNullOrWhiteSpace(executable) && McpCloudflareAccountTunnelManager.IsAuthenticated;
            _quick.IsEnabled = !string.IsNullOrWhiteSpace(executable);
            _copyUrl.IsEnabled = !string.IsNullOrWhiteSpace(publicUrl);
            _openChatGpt.IsEnabled = !string.IsNullOrWhiteSpace(publicUrl);
        }

        private static TextBlock TitleBlock(string text) => new TextBlock
        {
            Text = text,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 4)
        };

        private static Button ActionButton(string text, RoutedEventHandler click)
        {
            var button = new Button
            {
                Content = text,
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 8, 8),
                MinWidth = 130
            };
            button.Click += click;
            return button;
        }
    }
}
