using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25
{
    internal static class McpFirstRunExperience
    {
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan ReminderInterval = TimeSpan.FromDays(2);
        private static readonly object Sync = new object();
        private static DispatcherTimer? _timer;
        private static McpToastNotificationWindow? _toast;

        private static string MarkerPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QS3D",
                    "MCP",
                    "Experience",
                    "onboarding-toast-utc.txt");
            }
        }

        public static void Start()
        {
            lock (Sync)
            {
                if (_timer != null) return;
                var dispatcher = ResolveDispatcher();
                _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
                {
                    Interval = InitialDelay
                };
                _timer.Tick += OnTick;
                _timer.Start();
            }
        }

        public static void Stop()
        {
            DispatcherTimer? timer;
            McpToastNotificationWindow? toast;
            lock (Sync)
            {
                timer = _timer;
                _timer = null;
                toast = _toast;
                _toast = null;
            }
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= OnTick;
            }
            try { if (toast != null) toast.Close(); } catch { }
        }

        private static void OnTick(object sender, EventArgs e)
        {
            DispatcherTimer? timer;
            lock (Sync)
            {
                timer = _timer;
                _timer = null;
            }
            if (timer != null)
            {
                timer.Stop();
                timer.Tick -= OnTick;
            }

            try
            {
                var provider = McpTransportCoordinator.SelectedProvider;
                var onboarding = McpAgentExperience.DetermineOnboarding();
                if (IsSelectedTransportReady(provider, onboarding)) return;
                if (!ReminderDue()) return;

                var message = BuildMessage(provider, onboarding);
                var toast = new McpToastNotificationWindow(
                    "Kết nối ChatGPT ↔ QS3D",
                    message,
                    "Mở MCP Agent Center",
                    OpenAgentCenter);
                lock (Sync) _toast = toast;
                toast.Closed += (_, __) =>
                {
                    lock (Sync) if (ReferenceEquals(_toast, toast)) _toast = null;
                };
                WriteMarker();
                toast.Show();
            }
            catch { }
        }

        private static bool IsSelectedTransportReady(McpTransportProvider provider, McpOnboardingSnapshot cloudflareOnboarding)
        {
            if (!McpEmbeddedServer.IsRunning) return false;
            if (provider == McpTransportProvider.OpenAiSecureTunnel)
                return McpOpenAiSecureTunnelManager.IsRunning
                       && McpOpenAiSecureTunnelManager.IsReady
                       && McpTransportCoordinator.IsChatGptRegistrationAcknowledged();
            if (provider == McpTransportProvider.CloudflareQuickTunnel)
                return McpCloudflareTunnelManager.IsRunning
                       && !string.IsNullOrWhiteSpace(McpPublicEndpointResolver.Resolve())
                       && McpTransportCoordinator.IsChatGptRegistrationAcknowledged();
            return cloudflareOnboarding.Phase == McpOnboardingPhase.Ready;
        }

        private static string BuildMessage(McpTransportProvider provider, McpOnboardingSnapshot onboarding)
        {
            if (provider == McpTransportProvider.OpenAiSecureTunnel)
            {
                if (string.IsNullOrWhiteSpace(McpOpenAiSecureTunnelManager.SavedClientPath))
                    return "QS3D MCP đã sẵn sàng local. OpenAI Secure MCP Tunnel không cần domain riêng: mở Agent Center để tải/chọn tunnel-client chính thức, tạo Tunnel ID và kết nối ChatGPT.";
                if (!McpOpenAiSecureTunnelManager.IsValidTunnelId(McpOpenAiSecureTunnelManager.SavedTunnelId))
                    return "OpenAI Secure MCP Tunnel đã được chọn. Mở Agent Center, nhập Tunnel ID từ OpenAI Platform và Runtime API key cho phiên kết nối; QS3D không lưu key này.";
                if (!McpOpenAiSecureTunnelManager.IsRunning)
                    return "OpenAI Secure MCP Tunnel đã cấu hình nhưng chưa chạy. Mở Agent Center để khởi động tunnel-client; không cần Cloudflare hoặc public MCP URL.";
                if (!McpOpenAiSecureTunnelManager.IsReady)
                    return "OpenAI tunnel-client đang chạy nhưng chưa READY. Mở Agent Center để xem trạng thái hoặc tunnel-client UI trước khi kết nối ChatGPT.";
                return "OpenAI Secure MCP Tunnel đã READY. Mở ChatGPT connector settings, chọn Connection = Tunnel với Tunnel ID hiện tại rồi xác nhận lại trong Agent Center.";
            }

            if (provider == McpTransportProvider.CloudflareQuickTunnel)
            {
                if (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                    return "Cloudflare Quick Tunnel được chọn để test nhưng máy chưa có cloudflared. Mở Agent Center để cài rồi khởi động Quick Tunnel; không cần domain/login Cloudflare.";
                if (!McpCloudflareTunnelManager.IsRunning || string.IsNullOrWhiteSpace(McpPublicEndpointResolver.Resolve()))
                    return "Cloudflare Quick Tunnel là transport test-only và chưa có public URL. Mở Agent Center để khởi động; khi URL đổi phải reconnect ChatGPT.";
                return "Quick Tunnel đã có public URL. Thêm URL hiện tại vào ChatGPT bằng OAuth, sau đó xác nhận lại trong Agent Center. Quick Tunnel chỉ dùng test.";
            }

            if (onboarding.Phase == McpOnboardingPhase.CloudflaredMissing)
                return "QS3D MCP đã sẵn sàng local nhưng máy chưa có cloudflared. Mở Agent Center để cài tự động, sau đó đăng nhập Cloudflare trên browser và kết nối ChatGPT.";
            if (onboarding.Phase == McpOnboardingPhase.CloudflareLoginRequired)
                return "cloudflared đã có. Bước tiếp theo là đăng nhập Cloudflare bằng browser do provider mở; QS3D không lưu mật khẩu.";
            if (onboarding.Phase == McpOnboardingPhase.NamedTunnelRequired || onboarding.Phase == McpOnboardingPhase.PublicEndpointReady)
                return "Cloudflare login đã có. Tạo/khởi động Named Tunnel HTTPS ổn định rồi thêm public MCP URL vào ChatGPT.";
            if (onboarding.Phase == McpOnboardingPhase.ChatGptRegistrationRequired)
                return "Public MCP URL đã sẵn sàng. Mở ChatGPT bằng browser hệ thống và thêm QS3D MCP qua OAuth/custom MCP.";
            return onboarding.Detail + " " + onboarding.NextStep;
        }

        private static void OpenAgentCenter()
        {
            try
            {
                McpToastNotificationWindow? toast = null;
                lock (Sync)
                {
                    toast = _toast;
                    _toast = null;
                }
                try { if (toast != null) toast.Close(); } catch { }
                McpEmbeddedServer.EnsureStarted();
                new McpAgentControlCenterWindow().Show();
            }
            catch (Exception ex)
            {
                McpAgentExperience.Error("onboarding", "Không mở được MCP Agent Center từ toast: " + ex.Message,
                    "Chạy command QS3DMCPAGENTCENTER trong BricsCAD.");
            }
        }

        private static bool ReminderDue()
        {
            try
            {
                if (!File.Exists(MarkerPath)) return true;
                DateTime utc;
                if (!DateTime.TryParse(File.ReadAllText(MarkerPath, Encoding.UTF8).Trim(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utc)) return true;
                return DateTime.UtcNow - utc >= ReminderInterval;
            }
            catch { return true; }
        }

        private static void WriteMarker()
        {
            try
            {
                var directory = Path.GetDirectoryName(MarkerPath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(MarkerPath, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), new UTF8Encoding(false));
            }
            catch { }
        }

        private static Dispatcher ResolveDispatcher()
        {
            try
            {
                return System.Windows.Application.Current == null
                    ? Dispatcher.CurrentDispatcher
                    : System.Windows.Application.Current.Dispatcher;
            }
            catch { return Dispatcher.CurrentDispatcher; }
        }
    }

    internal sealed class McpToastNotificationWindow : Window
    {
        private readonly Action _primaryAction;

        public McpToastNotificationWindow(string title, string message, string buttonText, Action primaryAction)
        {
            _primaryAction = primaryAction;
            Width = 390;
            SizeToContent = SizeToContent.Height;
            MaxHeight = 300;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            WindowStartupLocation = WindowStartupLocation.Manual;

            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x13, 0x1A, 0x27)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x35, 0x47, 0x63)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Effect = null
            };
            var root = new StackPanel();

            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var titleBlock = new TextBlock
            {
                Text = title ?? "QS3D MCP",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            var close = new Button
            {
                Content = "×",
                Width = 28,
                Height = 28,
                Padding = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0xC9, 0xD2, 0xE3)),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                FontSize = 18,
                ToolTip = "Đóng thông báo"
            };
            close.Click += (_, __) => Close();
            Grid.SetColumn(close, 1);
            header.Children.Add(titleBlock);
            header.Children.Add(close);
            root.Children.Add(header);

            root.Children.Add(new TextBlock
            {
                Text = message ?? string.Empty,
                Foreground = new SolidColorBrush(Color.FromRgb(0xD7, 0xE0, 0xED)),
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 19,
                Margin = new Thickness(0, 9, 0, 13)
            });

            var action = new Button
            {
                Content = buttonText ?? "Mở",
                Height = 36,
                Padding = new Thickness(12, 5, 12, 5),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                FontWeight = FontWeights.SemiBold
            };
            action.Click += (_, __) =>
            {
                try { if (_primaryAction != null) _primaryAction(); }
                catch { }
            };
            root.Children.Add(action);
            card.Child = root;
            Content = card;

            Loaded += (_, __) => PositionBottomRight();
        }

        private void PositionBottomRight()
        {
            var work = SystemParameters.WorkArea;
            Left = Math.Max(work.Left + 12, work.Right - ActualWidth - 18);
            Top = Math.Max(work.Top + 12, work.Bottom - ActualHeight - 18);
        }
    }
}
