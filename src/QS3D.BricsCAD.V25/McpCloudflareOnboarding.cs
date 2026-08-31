using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class McpCloudflareOnboardingCommands
    {
        [CommandMethod("QS3DMCPSETUP", CommandFlags.Modal)]
        public void ShowSetupWizard()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                McpEmbeddedServer.EnsureStarted();
                new McpCloudflareSetupWindow().ShowDialog();
                document.Editor.WriteMessage("\nQS3D MCP fallback setup: " + McpCloudflareTunnelManager.Describe());
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3D MCP fallback setup lỗi: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Advanced fallback for dashboard-issued remote tunnel tokens and Quick Tunnel testing.
    /// The default end-user path is McpCloudflareAccountTunnelManager browser login.
    /// </summary>
    internal static class McpCloudflareTunnelManager
    {
        private const string DownloadUrl = "https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/";
        private const string DashboardUrl = "https://dash.cloudflare.com/?to=/:account/networks/tunnels";
        private const string ChatGptUrl = "https://chatgpt.com/";
        private static string OriginUrl => McpEmbeddedServer.Endpoint.GetLeftPart(UriPartial.Authority);
        private static string OriginHostHeader => McpEmbeddedServer.Endpoint.Authority;
        private const string QuickTunnelHostSuffix = ".trycloudflare.com";
        private static readonly object Sync = new object();
        private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("QS3D-BricsCAD/MCP/CloudflareTunnel/v1");
        private static Process? _process;
        private static string _quickBaseUrl = string.Empty;
        private static string _lastError = string.Empty;
        private static bool _quickMode;

        private static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "MCP", "Cloudflare");
        private static string TokenPath => Path.Combine(SettingsDirectory, "cloudflare-tunnel-token.bin");
        private static string HostnamePath => Path.Combine(SettingsDirectory, "cloudflare-public-hostname.txt");
        private static string AutoStartPath => Path.Combine(SettingsDirectory, "cloudflare-autostart.txt");

        public static string CloudflaredPath => FindCloudflared() ?? string.Empty;
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
        public static bool IsQuickMode { get { lock (Sync) return IsRunning && _quickMode; } }
        public static string LastError { get { lock (Sync) return _lastError; } }
        public static string SavedHostname => ReadText(HostnamePath);
        public static string PublicMcpUrl
        {
            get
            {
                if (!IsRunning) return string.Empty;
                lock (Sync)
                {
                    if (!string.IsNullOrWhiteSpace(_quickBaseUrl)) return _quickBaseUrl.TrimEnd('/') + "/mcp";
                }
                return string.IsNullOrWhiteSpace(SavedHostname) ? string.Empty : "https://" + SavedHostname + "/mcp";
            }
        }

        public static string Describe()
        {
            var mode = IsRunning ? (IsQuickMode ? "QUICK" : "TOKEN") : "STOPPED";
            string path;
            string source;
            string discovery;
            McpCloudflaredBootstrapper.TryResolveTrustedInstalledBinary(out path, out source, out discovery);
            return "tunnel=" + mode
                   + "; cloudflared=" + (string.IsNullOrWhiteSpace(path) ? "not-installed/trusted" : path)
                   + (string.IsNullOrWhiteSpace(source) ? string.Empty : "; source=" + source)
                   + (string.IsNullOrWhiteSpace(PublicMcpUrl) ? string.Empty : "; public=" + PublicMcpUrl)
                   + (string.IsNullOrWhiteSpace(LastError) ? string.Empty : "; error=" + LastError);
        }

        public static void OpenCloudflaredDownloadPage() => OpenUrl(DownloadUrl);
        public static void OpenCloudflareTunnelDashboard() => OpenUrl(DashboardUrl);
        public static void OpenChatGpt() => OpenUrl(ChatGptUrl);

        public static string NormalizeHostname(string value)
        {
            var raw = (value ?? string.Empty).Trim().TrimEnd('.');
            if (raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) raw = raw.Substring(8);
            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) raw = raw.Substring(7);
            var slash = raw.IndexOf('/');
            if (slash >= 0) raw = raw.Substring(0, slash);
            return Uri.CheckHostName(raw) == UriHostNameType.Dns ? raw.ToLowerInvariant() : string.Empty;
        }

        public static bool SaveNamedTunnelSettings(string hostname, string pastedTokenOrCommand, out string error)
        {
            error = string.Empty;
            var normalized = NormalizeHostname(hostname);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                error = "Hostname không hợp lệ.";
                return false;
            }
            var token = ExtractTunnelToken(pastedTokenOrCommand);
            if (token.Length < 24)
            {
                error = "Không nhận ra Cloudflare tunnel token.";
                return false;
            }
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var protectedBytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(token),
                    DpapiEntropy,
                    DataProtectionScope.CurrentUser);
                File.WriteAllBytes(TokenPath, protectedBytes);
                WriteText(HostnamePath, normalized);
                WriteText(AutoStartPath, "1");
                return true;
            }
            catch (Exception ex)
            {
                error = "Không lưu được tunnel settings: " + ex.Message;
                return false;
            }
        }

        public static bool StartNamedTunnel(out string error)
        {
            error = string.Empty;
            McpEmbeddedServer.EnsureStarted();
            if (!McpEmbeddedServer.IsPreferredPort)
            {
                error = "Token tunnel đang được Cloudflare cấu hình cố định tới 127.0.0.1:8765, nhưng QS3D MCP phải dùng cổng dự phòng "
                        + McpEmbeddedServer.Endpoint.Port.ToString() + ". Hãy dùng Kết nối ChatGPT/Quick Tunnel hoặc giải phóng cổng 8765 trước khi chạy token tunnel.";
                return false;
            }
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                error = "Chưa có Cloudflare Tunnel qua trust verification. Hãy dùng nút Cài/cập nhật hoặc cài bằng WinGet rồi Refresh.";
                return false;
            }
            string token;
            try { token = ReadProtectedTunnelToken(); }
            catch (Exception ex) { error = "Không đọc được tunnel token: " + ex.Message; return false; }
            if (string.IsNullOrWhiteSpace(token)) { error = "Chưa lưu tunnel token."; return false; }

            McpCloudflareAccountTunnelManager.StopForHostShutdown();
            StopProcessOnly();
            var startInfo = CreateCloudflaredStartInfo(executable, "tunnel --no-autoupdate run");
            startInfo.EnvironmentVariables["TUNNEL_TOKEN"] = token;
            lock (Sync) { _quickMode = false; _quickBaseUrl = string.Empty; _lastError = string.Empty; }
            return StartProcess(startInfo, false, out error);
        }

        public static bool StartQuickTunnel(out string error)
        {
            error = string.Empty;
            McpEmbeddedServer.EnsureStarted();
            var executable = CloudflaredPath;
            if (string.IsNullOrWhiteSpace(executable))
            {
                error = "Chưa có Cloudflare Tunnel qua trust verification. Hãy dùng nút Cài/cập nhật hoặc cài bằng WinGet rồi Refresh.";
                return false;
            }
            McpCloudflareAccountTunnelManager.StopForHostShutdown();
            StopProcessOnly();
            WriteText(AutoStartPath, "0");
            lock (Sync) { _quickMode = true; _quickBaseUrl = string.Empty; _lastError = string.Empty; }
            return StartProcess(
                CreateCloudflaredStartInfo(executable, "tunnel --no-autoupdate --url " + OriginUrl + " --http-host-header " + OriginHostHeader),
                true,
                out error);
        }

        public static void TryAutoStart()
        {
            if (ReadText(AutoStartPath) != "1" || !File.Exists(TokenPath)) return;
            string ignored;
            StartNamedTunnel(out ignored);
        }

        public static void Stop()
        {
            WriteText(AutoStartPath, "0");
            StopProcessOnly();
        }

        public static void StopForHostShutdown() => StopProcessOnly();

        private static string ExtractTunnelToken(string value)
        {
            var raw = (value ?? string.Empty).Trim();
            var tokenFlag = Regex.Match(raw, "--token\\s+[\\\"']?(?<token>[^\\s\\\"']+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (tokenFlag.Success) return tokenFlag.Groups["token"].Value.Trim();
            var serviceInstall = Regex.Match(raw, "service\\s+install\\s+[\\\"']?(?<token>[^\\s\\\"']+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (serviceInstall.Success) return serviceInstall.Groups["token"].Value.Trim();
            return raw.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) < 0 ? raw : string.Empty;
        }

        private static string ReadProtectedTunnelToken()
        {
            if (!File.Exists(TokenPath)) return string.Empty;
            var clear = ProtectedData.Unprotect(File.ReadAllBytes(TokenPath), DpapiEntropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clear).Trim();
        }

        private static bool StartProcess(ProcessStartInfo startInfo, bool discoverQuickUrl, out string error)
        {
            error = string.Empty;
            Process? process = null;
            try
            {
                process = new Process { StartInfo = startInfo, EnableRaisingEvents = false };
                process.OutputDataReceived += (_, args) => HandleLine(process, args.Data, discoverQuickUrl);
                process.ErrorDataReceived += (_, args) => HandleLine(process, args.Data, discoverQuickUrl);
                if (!process.Start())
                {
                    process.Dispose();
                    error = "Không khởi động được cloudflared.";
                    return false;
                }

                lock (Sync) _process = process;
                process.Exited += (_, __) => HandleProcessExit(process);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.EnableRaisingEvents = true;
                try
                {
                    if (process.HasExited) HandleProcessExit(process);
                }
                catch (ObjectDisposedException)
                {
                    HandleProcessExit(process);
                }
                return IsRunning;
            }
            catch (Exception ex)
            {
                lock (Sync)
                {
                    if (ReferenceEquals(_process, process)) _process = null;
                    _lastError = ex.Message;
                    if (discoverQuickUrl)
                    {
                        _quickBaseUrl = string.Empty;
                        _quickMode = false;
                    }
                }
                try { process?.Dispose(); } catch { }
                error = ex.Message;
                return false;
            }
        }

        private static void HandleProcessExit(Process process)
        {
            var owned = false;
            lock (Sync)
            {
                if (ReferenceEquals(_process, process))
                {
                    _process = null;
                    _quickBaseUrl = string.Empty;
                    _quickMode = false;
                    owned = true;
                }
            }
            if (owned) { try { process.Dispose(); } catch { } }
        }

        private static ProcessStartInfo CreateCloudflaredStartInfo(string executable, string arguments) => new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
        };

        private static void HandleLine(Process process, string? line, bool discoverQuickUrl)
        {
            if (line == null || string.IsNullOrWhiteSpace(line)) return;
            var clean = line.Trim();
            if (clean.Length > 1000) clean = clean.Substring(0, 1000);
            lock (Sync)
            {
                if (!ReferenceEquals(_process, process)) return;
                if (clean.IndexOf("ERR", StringComparison.OrdinalIgnoreCase) >= 0
                    || clean.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0) _lastError = clean;
                if (discoverQuickUrl)
                {
                    var match = Regex.Match(
                        clean,
                        "https://[A-Za-z0-9-]+" + Regex.Escape(QuickTunnelHostSuffix),
                        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (match.Success) _quickBaseUrl = match.Value.TrimEnd('/');
                }
            }
        }

        private static void StopProcessOnly()
        {
            Process? process;
            lock (Sync) { process = _process; _process = null; _quickBaseUrl = string.Empty; _quickMode = false; }
            if (process == null) return;
            try { process.EnableRaisingEvents = false; } catch { }
            try { if (!process.HasExited) process.Kill(); } catch { }
            try { if (!process.HasExited) process.WaitForExit(2000); } catch { }
            try { process.Dispose(); } catch { }
        }

        private static string? FindCloudflared()
        {
            string path;
            string source;
            string message;
            return McpCloudflaredBootstrapper.TryResolveTrustedInstalledBinary(out path, out source, out message)
                ? path
                : null;
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
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

    internal sealed class McpCloudflareSetupWindow : Window
    {
        private readonly TextBox _hostname = new TextBox { Margin = new Thickness(0, 4, 0, 8) };
        private readonly PasswordBox _token = new PasswordBox { Margin = new Thickness(0, 4, 0, 8) };
        private readonly TextBlock _status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
        private Button? _installButton;
        private Button? _cancelInstallButton;
        private DispatcherTimer? _installTimer;
        private DispatcherTimer? _quickUrlTimer;
        private int _quickUrlPollTicks;

        public McpCloudflareSetupWindow()
        {
            Title = "QS3D MCP - Advanced / Quick Tunnel";
            Width = 620;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Closed += (_, __) => { StopQuickUrlPolling(); StopInstallPolling(); };
            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(new TextBlock { Text = "Luồng mặc định nên dùng Cài đặt MCP -> Đăng nhập Cloudflare. Màn hình này chỉ dành cho Quick Tunnel hoặc token nâng cao.", TextWrapping = TextWrapping.Wrap });
            _installButton = Button("Cài / cập nhật Cloudflare Tunnel tự động", (_, __) => InstallCloudflared());
            panel.Children.Add(_installButton);
            _cancelInstallButton = Button("Hủy cài Cloudflare Tunnel", (_, __) => CancelCloudflaredInstall());
            panel.Children.Add(_cancelInstallButton);
            panel.Children.Add(Button("Mở Cloudflare Dashboard", (_, __) => McpCloudflareTunnelManager.OpenCloudflareTunnelDashboard()));
            panel.Children.Add(new TextBlock { Text = "Hostname (token mode):" });
            panel.Children.Add(_hostname);
            panel.Children.Add(new TextBlock { Text = "Tunnel token hoặc nguyên dòng Cloudflare:" });
            panel.Children.Add(_token);
            panel.Children.Add(Button("Lưu + chạy token tunnel", (_, __) => SaveAndStart()));
            panel.Children.Add(Button("Quick Tunnel (chỉ test)", (_, __) => StartQuick()));
            panel.Children.Add(Button("Dừng tunnel", (_, __) => { StopQuickUrlPolling(); McpCloudflareTunnelManager.Stop(); Refresh(); }));
            panel.Children.Add(Button("Mở ChatGPT", (_, __) => McpCloudflareTunnelManager.OpenChatGpt()));
            panel.Children.Add(_status);
            panel.Children.Add(Button("Đóng", (_, __) => Close()));
            Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Refresh();
        }

        private static Button Button(string text, RoutedEventHandler handler)
        {
            var button = new Button { Content = text, Margin = new Thickness(0, 4, 0, 4), MinHeight = 30 };
            button.Click += handler;
            return button;
        }

        private void InstallCloudflared()
        {
            if (McpCloudflaredBootstrapper.IsInstalling)
            {
                Refresh();
                return;
            }
            _status.Text = "Đang kiểm tra/cài cloudflared...";
            var started = McpCloudflaredBootstrapper.BeginInstall((ok, message) =>
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        StopInstallPolling();
                        Refresh();
                        if (!ok && !McpCloudflaredBootstrapper.WasLastInstallCancelled)
                            MessageBox.Show(message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }));
                }
                catch { }
            });
            if (started) StartInstallPolling();
            Refresh();
        }

        private void CancelCloudflaredInstall()
        {
            string message;
            McpCloudflaredBootstrapper.CancelInstall(out message);
            _status.Text = message;
            RefreshInstallButtons();
        }

        private void StartInstallPolling()
        {
            StopInstallPolling();
            _installTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _installTimer.Tick += InstallTimerOnTick;
            _installTimer.Start();
        }

        private void InstallTimerOnTick(object? sender, EventArgs e)
        {
            Refresh();
            if (!McpCloudflaredBootstrapper.IsInstalling) StopInstallPolling();
        }

        private void StopInstallPolling()
        {
            var timer = _installTimer;
            _installTimer = null;
            if (timer == null) return;
            timer.Stop();
            timer.Tick -= InstallTimerOnTick;
        }

        private void RefreshInstallButtons()
        {
            var busy = McpCloudflaredBootstrapper.IsInstalling;
            if (_installButton != null)
            {
                _installButton.IsEnabled = !busy;
                _installButton.Content = busy
                    ? "Đang cài Cloudflare... " + McpCloudflaredBootstrapper.InstallProgressPercent + "%"
                    : "Cài / cập nhật Cloudflare Tunnel tự động";
            }
            if (_cancelInstallButton != null) _cancelInstallButton.IsEnabled = busy;
        }

        private void SaveAndStart()
        {
            StopQuickUrlPolling();
            string error;
            if (!McpCloudflareTunnelManager.SaveNamedTunnelSettings(_hostname.Text, _token.Password, out error)
                || !McpCloudflareTunnelManager.StartNamedTunnel(out error))
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
            Refresh();
        }

        private void StartQuick()
        {
            string error;
            if (!McpCloudflareTunnelManager.StartQuickTunnel(out error))
            {
                StopQuickUrlPolling();
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                Refresh();
                return;
            }
            StartQuickUrlPolling();
            Refresh();
        }

        private void StartQuickUrlPolling()
        {
            StopQuickUrlPolling();
            _quickUrlPollTicks = 0;
            _quickUrlTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _quickUrlTimer.Tick += QuickUrlTimerOnTick;
            _quickUrlTimer.Start();
        }

        private void QuickUrlTimerOnTick(object? sender, EventArgs e)
        {
            _quickUrlPollTicks++;
            Refresh();
            if (!string.IsNullOrWhiteSpace(McpCloudflareTunnelManager.PublicMcpUrl)
                || !McpCloudflareTunnelManager.IsRunning
                || _quickUrlPollTicks >= 20)
                StopQuickUrlPolling();
        }

        private void StopQuickUrlPolling()
        {
            var timer = _quickUrlTimer;
            _quickUrlTimer = null;
            if (timer == null) return;
            timer.Stop();
            timer.Tick -= QuickUrlTimerOnTick;
        }

        private void Refresh()
        {
            RefreshInstallButtons();
            var installer = McpCloudflaredBootstrapper.IsInstalling
                ? Environment.NewLine + "installer=" + McpCloudflaredBootstrapper.InstallProgressPercent + "% · " + McpCloudflaredBootstrapper.InstallStatus
                : string.Empty;
            _status.Text = McpCloudflareTunnelManager.Describe() + installer;
        }
    }
}
