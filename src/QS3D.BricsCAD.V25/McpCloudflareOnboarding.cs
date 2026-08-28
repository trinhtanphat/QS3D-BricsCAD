using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Click-first onboarding entry point for TOOL > MCP (AI). End users never need a shell:
    /// browser login stays on Cloudflare/ChatGPT, while QS3D manages the local cloudflared process.
    /// </summary>
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
                var window = new McpCloudflareSetupWindow();
                window.ShowDialog();
                document.Editor.WriteMessage("\nQS3D MCP setup: " + McpCloudflareTunnelManager.Describe());
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3D MCP setup lỗi: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Manages cloudflared as a child process. Cloudflare account passwords are never requested,
    /// intercepted or stored by QS3D: authentication happens only in the provider's browser UI.
    /// A remotely-managed tunnel token is protected with Windows DPAPI for CurrentUser.
    /// </summary>
    internal static class McpCloudflareTunnelManager
    {
        private const string CloudflareDownloadUrl = "https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/";
        private const string CloudflareTunnelDashboardUrl = "https://dash.cloudflare.com/?to=/:account/networks/tunnels";
        private const string ChatGptUrl = "https://chatgpt.com/";
        private const string TunnelOrigin = "http://127.0.0.1:8765";
        private const string ProtectedTokenFileName = "cloudflare-tunnel-token.bin";
        private const string HostnameFileName = "cloudflare-public-hostname.txt";
        private const string AutoStartFileName = "cloudflare-autostart.txt";
        private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("QS3D-BricsCAD/MCP/CloudflareTunnel/v1");
        private static readonly object Sync = new object();
        private static Process? _process;
        private static string _quickBaseUrl = string.Empty;
        private static string _lastError = string.Empty;
        private static string _lastLine = string.Empty;
        private static bool _quickMode;

        public static event Action? StateChanged;

        public static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QS3D",
            "MCP",
            "Cloudflare");

        private static string ProtectedTokenPath => Path.Combine(SettingsDirectory, ProtectedTokenFileName);
        private static string HostnamePath => Path.Combine(SettingsDirectory, HostnameFileName);
        private static string AutoStartPath => Path.Combine(SettingsDirectory, AutoStartFileName);

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

        public static bool IsQuickMode
        {
            get { lock (Sync) return IsRunning && _quickMode; }
        }

        public static string LastError
        {
            get { lock (Sync) return _lastError; }
        }

        public static string LastLine
        {
            get { lock (Sync) return _lastLine; }
        }

        public static string SavedHostname
        {
            get
            {
                try
                {
                    return File.Exists(HostnamePath)
                        ? File.ReadAllText(HostnamePath, Encoding.UTF8).Trim()
                        : string.Empty;
                }
                catch { return string.Empty; }
            }
        }

        public static bool HasSavedTunnelToken
        {
            get
            {
                try { return File.Exists(ProtectedTokenPath) && File.ReadAllBytes(ProtectedTokenPath).Length > 0; }
                catch { return false; }
            }
        }

        public static string PublicMcpUrl
        {
            get
            {
                lock (Sync)
                {
                    if (!string.IsNullOrWhiteSpace(_quickBaseUrl)) return _quickBaseUrl.TrimEnd('/') + "/mcp";
                }

                var host = SavedHostname;
                return string.IsNullOrWhiteSpace(host) ? string.Empty : "https://" + host + "/mcp";
            }
        }

        public static string CloudflaredPath => FindCloudflared() ?? string.Empty;

        public static string Describe()
        {
            var mode = IsRunning ? (IsQuickMode ? "QUICK" : "NAMED") : "STOPPED";
            var executable = string.IsNullOrWhiteSpace(CloudflaredPath) ? "not-installed" : CloudflaredPath;
            var url = PublicMcpUrl;
            var error = LastError;
            return "tunnel=" + mode
                   + "; cloudflared=" + executable
                   + (string.IsNullOrWhiteSpace(url) ? string.Empty : "; public=" + url)
                   + (string.IsNullOrWhiteSpace(error) ? string.Empty : "; error=" + error);
        }

        public static void OpenCloudflaredDownloadPage() => OpenUrl(CloudflareDownloadUrl);
        public static void OpenCloudflareTunnelDashboard() => OpenUrl(CloudflareTunnelDashboardUrl);
        public static void OpenChatGpt() => OpenUrl(ChatGptUrl);

        public static bool SaveNamedTunnelSettings(string hostname, string pastedTokenOrCommand, out string error)
        {
            error = string.Empty;
            var normalizedHost = NormalizeHostname(hostname);
            if (string.IsNullOrWhiteSpace(normalizedHost))
            {
                error = "Hostname không hợp lệ. Ví dụ: qs3d.example.com";
                return false;
            }

            var token = ExtractTunnelToken(pastedTokenOrCommand);
            if (token.Length < 24)
            {
                error = "Không nhận ra Cloudflare tunnel token. Có thể dán token hoặc nguyên dòng lệnh Cloudflare hiển thị.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                var protectedBytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(token),
                    DpapiEntropy,
                    DataProtectionScope.CurrentUser);
                File.WriteAllBytes(ProtectedTokenPath, protectedBytes);
                File.WriteAllText(HostnamePath, normalizedHost, new UTF8Encoding(false));
                File.WriteAllText(AutoStartPath, "1", new UTF8Encoding(false));
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
            var executable = FindCloudflared();
            if (string.IsNullOrWhiteSpace(executable))
            {
                error = "Chưa tìm thấy cloudflared. Bấm 'Cài Cloudflare Tunnel' trước.";
                SetError(error);
                return false;
            }

            string token;
            try { token = ReadProtectedTunnelToken(); }
            catch (Exception ex)
            {
                error = "Không đọc được tunnel token đã mã hóa: " + ex.Message;
                SetError(error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                error = "Chưa lưu Cloudflare tunnel token.";
                SetError(error);
                return false;
            }

            StopProcessOnly();
            lock (Sync)
            {
                _quickMode = false;
                _quickBaseUrl = string.Empty;
                _lastError = string.Empty;
                _lastLine = "Đang kết nối Cloudflare Named Tunnel...";
            }

            var startInfo = CreateCloudflaredStartInfo(executable, "tunnel --no-autoupdate run");
            startInfo.EnvironmentVariables["TUNNEL_TOKEN"] = token;
            return StartProcess(startInfo, false, out error);
        }

        public static bool StartQuickTunnel(out string error)
        {
            error = string.Empty;
            McpEmbeddedServer.EnsureStarted();
            var executable = FindCloudflared();
            if (string.IsNullOrWhiteSpace(executable))
            {
                error = "Chưa tìm thấy cloudflared. Bấm 'Cài Cloudflare Tunnel' trước.";
                SetError(error);
                return false;
            }

            StopProcessOnly();
            lock (Sync)
            {
                _quickMode = true;
                _quickBaseUrl = string.Empty;
                _lastError = string.Empty;
                _lastLine = "Đang tạo Quick Tunnel dùng thử...";
            }
            SetAutoStart(false);

            var startInfo = CreateCloudflaredStartInfo(
                executable,
                "tunnel --no-autoupdate --url " + TunnelOrigin + " --http-host-header 127.0.0.1:8765");
            return StartProcess(startInfo, true, out error);
        }

        public static void Stop()
        {
            SetAutoStart(false);
            StopProcessOnly();
        }

        public static void StopForHostShutdown()
        {
            StopProcessOnly();
        }

        public static void TryAutoStart()
        {
            if (!ReadAutoStart() || !HasSavedTunnelToken) return;
            string ignored;
            StartNamedTunnel(out ignored);
        }

        public static string NormalizeHostname(string value)
        {
            var raw = (value ?? string.Empty).Trim().TrimEnd('.');
            if (raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) raw = raw.Substring(8);
            if (raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) raw = raw.Substring(7);
            var slash = raw.IndexOf('/');
            if (slash >= 0) raw = raw.Substring(0, slash);
            if (Uri.CheckHostName(raw) != UriHostNameType.Dns) return string.Empty;
            return raw.ToLowerInvariant();
        }

        private static string ExtractTunnelToken(string value)
        {
            var raw = (value ?? string.Empty).Trim();
            if (raw.Length == 0) return string.Empty;

            var tokenFlag = Regex.Match(raw, "--token\\s+[\\\"']?(?<token>[^\\s\\\"']+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (tokenFlag.Success) return tokenFlag.Groups["token"].Value.Trim();

            var serviceInstall = Regex.Match(raw, "service\\s+install\\s+[\\\"']?(?<token>[^\\s\\\"']+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (serviceInstall.Success) return serviceInstall.Groups["token"].Value.Trim();

            return raw.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) < 0 ? raw : string.Empty;
        }

        private static string ReadProtectedTunnelToken()
        {
            if (!File.Exists(ProtectedTokenPath)) return string.Empty;
            var encrypted = File.ReadAllBytes(ProtectedTokenPath);
            var clear = ProtectedData.Unprotect(encrypted, DpapiEntropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(clear).Trim();
        }

        private static bool StartProcess(ProcessStartInfo startInfo, bool discoverQuickUrl, out string error)
        {
            error = string.Empty;
            try
            {
                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.OutputDataReceived += (_, args) => HandleProcessLine(args.Data, discoverQuickUrl);
                process.ErrorDataReceived += (_, args) => HandleProcessLine(args.Data, discoverQuickUrl);
                process.Exited += (_, __) =>
                {
                    lock (Sync)
                    {
                        if (ReferenceEquals(_process, process))
                        {
                            try
                            {
                                if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(_lastError))
                                    _lastError = "cloudflared dừng với exit code " + process.ExitCode + ".";
                            }
                            catch { }
                            _process = null;
                        }
                    }
                    RaiseStateChanged();
                    try { process.Dispose(); } catch { }
                };

                if (!process.Start())
                {
                    process.Dispose();
                    error = "Không khởi động được cloudflared.";
                    SetError(error);
                    return false;
                }

                lock (Sync) _process = process;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                RaiseStateChanged();
                return true;
            }
            catch (Exception ex)
            {
                error = "Không chạy được cloudflared: " + ex.Message;
                SetError(error);
                return false;
            }
        }

        private static ProcessStartInfo CreateCloudflaredStartInfo(string executable, string arguments)
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

        private static void HandleProcessLine(string? line, bool discoverQuickUrl)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            var clean = line.Trim();
            lock (Sync)
            {
                _lastLine = clean.Length > 400 ? clean.Substring(0, 400) : clean;
                if (clean.IndexOf("ERR ", StringComparison.OrdinalIgnoreCase) >= 0)
                    _lastError = _lastLine;

                if (discoverQuickUrl)
                {
                    var match = Regex.Match(clean, "https://[A-Za-z0-9-]+\\.trycloudflare\\.com", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (match.Success) _quickBaseUrl = match.Value.TrimEnd('/');
                }
            }
            RaiseStateChanged();
        }

        private static void StopProcessOnly()
        {
            Process? process;
            lock (Sync)
            {
                process = _process;
                _process = null;
                _quickBaseUrl = string.Empty;
                _quickMode = false;
            }

            if (process != null)
            {
                try { if (!process.HasExited) process.Kill(); }
                catch { }
                try { process.Dispose(); }
                catch { }
            }
            RaiseStateChanged();
        }

        private static void SetAutoStart(bool enabled)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(AutoStartPath, enabled ? "1" : "0", new UTF8Encoding(false));
            }
            catch { }
        }

        private static bool ReadAutoStart()
        {
            try { return File.Exists(AutoStartPath) && File.ReadAllText(AutoStartPath, Encoding.UTF8).Trim() == "1"; }
            catch { return false; }
        }

        private static string? FindCloudflared()
        {
            var explicitPath = (Environment.GetEnvironmentVariable("QS3D_CLOUDFLARED_PATH") ?? string.Empty).Trim();
            if (IsExecutableFile(explicitPath)) return explicitPath;

            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "cloudflared", "cloudflared.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "cloudflared", "cloudflared.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links", "cloudflared.exe")
            };
            foreach (var candidate in candidates)
                if (IsExecutableFile(candidate)) return candidate;

            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var segment in path.Split(Path.PathSeparator))
            {
                var directory = segment.Trim().Trim('"');
                if (directory.Length == 0) continue;
                try
                {
                    var candidate = Path.Combine(directory, "cloudflared.exe");
                    if (IsExecutableFile(candidate)) return candidate;
                }
                catch { }
            }
            return null;
        }

        private static bool IsExecutableFile(string path)
        {
            try { return !string.IsNullOrWhiteSpace(path) && File.Exists(path); }
            catch { return false; }
        }

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private static void SetError(string error)
        {
            lock (Sync) _lastError = error ?? string.Empty;
            RaiseStateChanged();
        }

        private static void RaiseStateChanged()
        {
            try { StateChanged?.Invoke(); }
            catch { }
        }
    }

    internal sealed class McpCloudflareSetupWindow : Window
    {
        private readonly TextBlock _status;
        private readonly TextBox _hostname;
        private readonly PasswordBox _tunnelToken;
        private readonly Button _installButton;
        private readonly Button _connectButton;
        private readonly Button _quickButton;
        private readonly Button _stopButton;
        private readonly Button _copyUrlButton;
        private readonly Button _copyBearerButton;
        private readonly Button _openChatGptButton;

        public McpCloudflareSetupWindow()
        {
            Title = "QS3D - Kết nối ChatGPT qua MCP";
            Width = 720;
            Height = 660;
            MinWidth = 640;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.White;

            var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var panel = new StackPanel { Margin = new Thickness(24) };
            root.Content = panel;
            Content = root;

            panel.Children.Add(new TextBlock
            {
                Text = "Kết nối ChatGPT với QS3D",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Không cần PowerShell. Mật khẩu Cloudflare chỉ nhập trên trang đăng nhập Cloudflare; QS3D không đọc hoặc lưu password.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 0, 0, 18)
            });

            _status = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(12),
                Background = Brushes.WhiteSmoke,
                Margin = new Thickness(0, 0, 0, 18)
            };
            panel.Children.Add(_status);

            panel.Children.Add(SectionTitle("1. Cloudflare Tunnel"));
            var installRow = new WrapPanel { Margin = new Thickness(0, 4, 0, 14) };
            _installButton = Button("Cài Cloudflare Tunnel", (_, __) =>
            {
                McpCloudflareTunnelManager.OpenCloudflaredDownloadPage();
                MessageBox.Show(
                    "Trình duyệt đã mở trang tải Cloudflare Tunnel. Cài cloudflared bằng giao diện Windows, sau đó quay lại đây và bấm Làm mới.",
                    "QS3D MCP",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
            installRow.Children.Add(_installButton);
            installRow.Children.Add(Button("Làm mới", (_, __) => RefreshUi()));
            panel.Children.Add(installRow);

            panel.Children.Add(SectionTitle("2. Dùng lâu dài - Named Tunnel"));
            panel.Children.Add(new TextBlock
            {
                Text = "Bấm Mở Cloudflare. Trong browser: đăng nhập → Networking/Tunnels → Create Tunnel → tạo Published application trỏ Service URL về http://127.0.0.1:8765. Sau đó dán hostname và tunnel token vào đây.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 10)
            });
            panel.Children.Add(Button("Mở Cloudflare và đăng nhập", (_, __) => McpCloudflareTunnelManager.OpenCloudflareTunnelDashboard()));

            panel.Children.Add(Label("Public hostname (ví dụ qs3d.example.com)"));
            _hostname = new TextBox
            {
                Text = McpCloudflareTunnelManager.SavedHostname,
                Margin = new Thickness(0, 4, 0, 10),
                Padding = new Thickness(8)
            };
            panel.Children.Add(_hostname);

            panel.Children.Add(Label("Tunnel token hoặc nguyên dòng lệnh Cloudflare có token"));
            _tunnelToken = new PasswordBox
            {
                Margin = new Thickness(0, 4, 0, 6),
                Padding = new Thickness(8)
            };
            panel.Children.Add(_tunnelToken);
            panel.Children.Add(new TextBlock
            {
                Text = McpCloudflareTunnelManager.HasSavedTunnelToken
                    ? "Đã có token lưu trên máy này (mã hóa Windows DPAPI). Để trống nếu chỉ muốn dùng lại token đã lưu."
                    : "Token sẽ được mã hóa theo tài khoản Windows hiện tại; không ghi vào project/repository.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var namedRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
            _connectButton = Button("Lưu và kết nối", (_, __) => SaveAndConnect());
            _stopButton = Button("Dừng kết nối", (_, __) =>
            {
                McpCloudflareTunnelManager.Stop();
                RefreshUi();
            });
            namedRow.Children.Add(_connectButton);
            namedRow.Children.Add(_stopButton);
            panel.Children.Add(namedRow);

            panel.Children.Add(SectionTitle("3. Test nhanh - Quick Tunnel"));
            panel.Children.Add(new TextBlock
            {
                Text = "Dùng để thử ngay, không cần account/domain. URL trycloudflare.com là tạm thời và sẽ đổi khi dừng.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 10)
            });
            _quickButton = Button("Tạo Quick Tunnel", (_, __) =>
            {
                string error;
                if (!McpCloudflareTunnelManager.StartQuickTunnel(out error))
                    MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshUi();
            });
            panel.Children.Add(_quickButton);

            panel.Children.Add(SectionTitle("4. Kết nối trong ChatGPT"));
            panel.Children.Add(new TextBlock
            {
                Text = "Khi Public MCP URL đã hiện READY, bấm Copy URL và Copy Bearer Token. Trong ChatGPT mở Apps/Create custom MCP rồi dán hai giá trị này.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 10)
            });
            var chatRow = new WrapPanel();
            _copyUrlButton = Button("Copy MCP URL", (_, __) => CopyPublicUrl());
            _copyBearerButton = Button("Copy Bearer Token", (_, __) =>
            {
                Clipboard.SetText(McpEmbeddedServer.GetBearerToken());
                MessageBox.Show("Đã copy Bearer token của QS3D MCP.", "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            _openChatGptButton = Button("Mở ChatGPT", (_, __) =>
            {
                CopyPublicUrl(false);
                McpCloudflareTunnelManager.OpenChatGpt();
            });
            chatRow.Children.Add(_copyUrlButton);
            chatRow.Children.Add(_copyBearerButton);
            chatRow.Children.Add(_openChatGptButton);
            panel.Children.Add(chatRow);

            McpCloudflareTunnelManager.StateChanged += OnTunnelStateChanged;
            Closed += (_, __) => McpCloudflareTunnelManager.StateChanged -= OnTunnelStateChanged;
            RefreshUi();
        }

        private void SaveAndConnect()
        {
            var host = McpCloudflareTunnelManager.NormalizeHostname(_hostname.Text);
            var pasted = _tunnelToken.Password;
            string error;

            if (string.IsNullOrWhiteSpace(pasted) && McpCloudflareTunnelManager.HasSavedTunnelToken)
            {
                if (string.IsNullOrWhiteSpace(host))
                {
                    MessageBox.Show("Nhập public hostname của tunnel.", "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    Directory.CreateDirectory(McpCloudflareTunnelManager.SettingsDirectory);
                    File.WriteAllText(
                        Path.Combine(McpCloudflareTunnelManager.SettingsDirectory, "cloudflare-public-hostname.txt"),
                        host,
                        new UTF8Encoding(false));
                    File.WriteAllText(
                        Path.Combine(McpCloudflareTunnelManager.SettingsDirectory, "cloudflare-autostart.txt"),
                        "1",
                        new UTF8Encoding(false));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không lưu được hostname: " + ex.Message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else if (!McpCloudflareTunnelManager.SaveNamedTunnelSettings(host, pasted, out error))
            {
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _tunnelToken.Clear();
            if (!McpCloudflareTunnelManager.StartNamedTunnel(out error))
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
            RefreshUi();
        }

        private void CopyPublicUrl(bool showMessage = true)
        {
            var url = McpCloudflareTunnelManager.PublicMcpUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                if (showMessage)
                    MessageBox.Show("Chưa có Public MCP URL. Kết nối Named Tunnel hoặc Quick Tunnel trước.", "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Clipboard.SetText(url);
            if (showMessage)
                MessageBox.Show("Đã copy: " + url, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnTunnelStateChanged()
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
            var cloudflared = McpCloudflareTunnelManager.CloudflaredPath;
            var publicUrl = McpCloudflareTunnelManager.PublicMcpUrl;
            var running = McpCloudflareTunnelManager.IsRunning;
            var mode = running ? (McpCloudflareTunnelManager.IsQuickMode ? "Quick Tunnel" : "Named Tunnel") : "Đã dừng";
            var protocol = McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, 2000);

            var builder = new StringBuilder();
            builder.AppendLine("Embedded MCP: " + (protocol.Ready ? "READY" : "NOT READY") + " - " + McpEmbeddedServer.Endpoint);
            builder.AppendLine("Cloudflare Tunnel: " + mode);
            builder.AppendLine("cloudflared: " + (string.IsNullOrWhiteSpace(cloudflared) ? "chưa cài" : "đã cài"));
            if (!string.IsNullOrWhiteSpace(publicUrl)) builder.AppendLine("Public MCP URL: " + publicUrl);
            if (!string.IsNullOrWhiteSpace(McpCloudflareTunnelManager.LastLine)) builder.AppendLine("Status: " + McpCloudflareTunnelManager.LastLine);
            if (!string.IsNullOrWhiteSpace(McpCloudflareTunnelManager.LastError)) builder.AppendLine("Lỗi: " + McpCloudflareTunnelManager.LastError);
            _status.Text = builder.ToString().TrimEnd();

            _connectButton.IsEnabled = !string.IsNullOrWhiteSpace(cloudflared);
            _quickButton.IsEnabled = !string.IsNullOrWhiteSpace(cloudflared);
            _stopButton.IsEnabled = running;
            _copyUrlButton.IsEnabled = !string.IsNullOrWhiteSpace(publicUrl);
            _openChatGptButton.IsEnabled = !string.IsNullOrWhiteSpace(publicUrl);
        }

        private static TextBlock SectionTitle(string text) => new TextBlock
        {
            Text = text,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 4)
        };

        private static TextBlock Label(string text) => new TextBlock
        {
            Text = text,
            Margin = new Thickness(0, 4, 0, 0)
        };

        private static Button Button(string text, RoutedEventHandler onClick)
        {
            var button = new Button
            {
                Content = text,
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 8, 8),
                MinWidth = 120
            };
            button.Click += onClick;
            return button;
        }
    }
}
