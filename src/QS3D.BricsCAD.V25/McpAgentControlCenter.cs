using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using Application = Bricscad.ApplicationServices.Application;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Canonical task-oriented MCP operations center. Provider credentials remain in
    /// provider-owned browser sessions; QS3D mirrors local MCP execution/status only.
    /// </summary>
    public sealed class McpAgentControlCenterCommands
    {
        [CommandMethod("QS3DMCPAGENTCENTER", CommandFlags.Modal)]
        public void ShowControlCenter()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                McpEmbeddedServer.EnsureStarted();
                new McpAgentControlCenterWindow().Show();
            }
            catch (Exception ex)
            {
                document.Editor.WriteMessage("\nQS3D MCP Agent Center lỗi: " + ex.Message);
            }
        }
    }

    internal sealed class McpAgentControlCenterWindow : Window
    {
        private const int MaxActivityEntries = 50;
        private const int MaxVisibleToasts = 4;
        private static readonly TimeSpan OAuthMcpActivityFreshness = TimeSpan.FromMinutes(2);

        private enum ThemeMode { System, Dark, Light }
        private enum ToastKind { Info, Success, Warning, Error }
        private enum ActionKind { Primary, Secondary, Danger, Utility, Navigation, ThemeChoice }

        private sealed class ActivityEntry
        {
            public ActivityEntry(DateTime timestamp, ToastKind kind, string title, string message)
            {
                Timestamp = timestamp;
                Kind = kind;
                Title = title ?? string.Empty;
                Message = message ?? string.Empty;
            }
            public DateTime Timestamp { get; private set; }
            public ToastKind Kind { get; private set; }
            public string Title { get; private set; }
            public string Message { get; private set; }
        }

        private sealed class ToastVisual
        {
            public ToastVisual(Border card, DispatcherTimer? timer)
            {
                Card = card;
                Timer = timer;
            }
            public Border Card { get; private set; }
            public DispatcherTimer? Timer { get; set; }
            public EventHandler? TimerHandler { get; set; }
        }

        private sealed class ThemePalette
        {
            public Brush WindowBackground { get; set; } = Brushes.White;
            public Brush CardBackground { get; set; } = Brushes.White;
            public Brush SubtleBackground { get; set; } = Brushes.WhiteSmoke;
            public Brush SelectedBackground { get; set; } = Brushes.AliceBlue;
            public Brush Border { get; set; } = Brushes.LightGray;
            public Brush StrongBorder { get; set; } = Brushes.Gray;
            public Brush TextPrimary { get; set; } = Brushes.Black;
            public Brush TextSecondary { get; set; } = Brushes.DimGray;
            public Brush TextMuted { get; set; } = Brushes.Gray;
            public Brush Accent { get; set; } = Brushes.RoyalBlue;
            public Brush AccentHover { get; set; } = Brushes.Blue;
            public Brush AccentPressed { get; set; } = Brushes.Navy;
            public Brush AccentText { get; set; } = Brushes.White;
            public Brush Success { get; set; } = Brushes.Green;
            public Brush SuccessSoft { get; set; } = Brushes.Honeydew;
            public Brush SuccessBorder { get; set; } = Brushes.LightGreen;
            public Brush Warning { get; set; } = Brushes.DarkGoldenrod;
            public Brush WarningSoft { get; set; } = Brushes.LemonChiffon;
            public Brush WarningBorder { get; set; } = Brushes.Goldenrod;
            public Brush Danger { get; set; } = Brushes.Firebrick;
            public Brush DangerSoft { get; set; } = Brushes.MistyRose;
            public Brush DangerHover { get; set; } = Brushes.Firebrick;
            public Brush DangerPressed { get; set; } = Brushes.DarkRed;
            public Brush DangerBorder { get; set; } = Brushes.IndianRed;
            public Brush DangerStrongText { get; set; } = Brushes.White;
            public Brush DisabledBackground { get; set; } = Brushes.Gainsboro;
            public Brush DisabledForeground { get; set; } = Brushes.Gray;
            public Brush DisabledBorder { get; set; } = Brushes.DarkGray;
            public Brush FocusBorder { get; set; } = Brushes.DodgerBlue;
        }

        private ThemeMode _themeMode = ThemeMode.System;
        private ThemePalette _palette = CreateLightPalette();
        private int _selectedTab;
        private readonly List<ActivityEntry> _activityEntries = new List<ActivityEntry>();
        private readonly List<ToastVisual> _visibleToasts = new List<ToastVisual>();
        private StackPanel _statusRows = new StackPanel();
        private WrapPanel _statusChips = new WrapPanel();
        private StackPanel _toastHost = new StackPanel();
        private StackPanel _logsHost = new StackPanel();
        private StackPanel _navigationHost = new StackPanel();
        private ContentControl _pageHost = new ContentControl();
        private TextBlock _desktopConsentText = new TextBlock();
        private TextBlock _desktopActivityText = new TextBlock();
        private TextBox _openAiTunnelIdText = new TextBox();
        private PasswordBox _openAiRuntimeKeyBox = new PasswordBox();
        private TextBlock _openAiClientPathText = new TextBlock();
        private int _localOperationActive;
        private DispatcherTimer? _quickUrlTimer;
        private DispatcherTimer? _liveRefreshTimer;
        private int _quickUrlPollTicks;
        private bool _closed;

        public McpAgentControlCenterWindow()
        {
            Title = "QS3D - ChatGPT MCP Agent Center";
            Width = 1080;
            Height = 800;
            MinWidth = 820;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            SystemEvents.UserPreferenceChanged += SystemEventsOnUserPreferenceChanged;
            Closed += OnWindowClosed;
            ApplyThemeAndRebuild(false);
            StartLiveRefresh();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _closed = true;
            StopQuickUrlPolling();
            StopLiveRefresh();
            ClearVisibleToasts();
            try { SystemEvents.UserPreferenceChanged -= SystemEventsOnUserPreferenceChanged; } catch { }
        }

        private void StartLiveRefresh()
        {
            StopLiveRefresh();
            _liveRefreshTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _liveRefreshTimer.Tick += LiveRefreshTimerOnTick;
            _liveRefreshTimer.Start();
        }

        private void StopLiveRefresh()
        {
            var timer = _liveRefreshTimer;
            _liveRefreshTimer = null;
            if (timer == null) return;
            timer.Stop();
            timer.Tick -= LiveRefreshTimerOnTick;
        }

        private void LiveRefreshTimerOnTick(object? sender, EventArgs e)
        {
            if (_closed) return;
            try { RefreshStatus(); } catch { }
        }

        private void SystemEventsOnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            if (_closed || _themeMode != ThemeMode.System) return;
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_closed && _themeMode == ThemeMode.System) ApplyThemeAndRebuild(false);
                }));
            }
            catch { }
        }

        private bool ResolveEffectiveDarkTheme()
        {
            if (_themeMode == ThemeMode.Dark) return true;
            if (_themeMode == ThemeMode.Light) return false;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    if (value is int) return (int)value == 0;
                }
            }
            catch { }
            return false;
        }

        private void SetThemeMode(ThemeMode mode)
        {
            if (_themeMode == mode) return;
            _themeMode = mode;
            ApplyThemeAndRebuild(false);
            ShowToast(ToastKind.Info, "Giao diện", "Đã chuyển theme sang " + GetThemeModeLabel(mode) + ".");
        }

        private static string GetThemeModeLabel(ThemeMode mode)
        {
            switch (mode)
            {
                case ThemeMode.Dark: return "Dark";
                case ThemeMode.Light: return "Light";
                default: return "System";
            }
        }

        private void ApplyThemeAndRebuild(bool announce)
        {
            ClearVisibleToasts();
            _palette = ResolveEffectiveDarkTheme() ? CreateDarkPalette() : CreateLightPalette();
            Background = _palette.WindowBackground;
            Foreground = _palette.TextPrimary;
            Content = CreateDashboardShell();
            RefreshStatus();
            if (announce) ShowToast(ToastKind.Info, "Giao diện", "Theme hiện tại: " + GetThemeModeLabel(_themeMode) + ".");
        }

        private UIElement CreateDashboardShell()
        {
            _statusRows = new StackPanel();
            _statusChips = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
            _toastHost = new StackPanel
            {
                Width = 360,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 118, 18, 0)
            };
            _pageHost = new ContentControl();

            var root = new Grid { Background = _palette.WindowBackground };
            var main = new Grid { Margin = new Thickness(24, 20, 24, 18) };
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = CreateHeaderBar();
            Grid.SetRow(header, 0);
            main.Children.Add(header);
            var navigation = CreateTabNavigation();
            Grid.SetRow(navigation, 1);
            main.Children.Add(navigation);
            _pageHost.Content = CreateActivePage();
            var scroller = new ScrollViewer
            {
                Content = _pageHost,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 14, 0, 0)
            };
            Grid.SetRow(scroller, 2);
            main.Children.Add(scroller);
            var footer = CreateFooter();
            Grid.SetRow(footer, 3);
            main.Children.Add(footer);
            root.Children.Add(main);
            Grid.SetZIndex(_toastHost, 100);
            root.Children.Add(_toastHost);
            return root;
        }

        private UIElement CreateHeaderBar()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = "QS3D · ChatGPT MCP Agent Center",
                FontSize = 25,
                FontWeight = FontWeights.Bold,
                Foreground = _palette.TextPrimary
            });
            left.Children.Add(new TextBlock
            {
                Text = "ChatGPT ↔ MCP transport ↔ QS3D ↔ BricsCAD",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = _palette.Accent,
                Margin = new Thickness(0, 4, 0, 0)
            });
            left.Children.Add(new TextBlock
            {
                Text = "Kết nối, Agent desktop, backup/recovery và chẩn đoán từ một nơi. Login/password/API key ở provider flow; QS3D không scrape cookie hoặc nội dung hội thoại ChatGPT.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.TextSecondary,
                FontSize = 12.5,
                Margin = new Thickness(0, 6, 16, 0)
            });
            left.Children.Add(_statusChips);
            grid.Children.Add(left);
            var theme = CreateThemeSelector();
            Grid.SetColumn(theme, 1);
            grid.Children.Add(theme);
            return grid;
        }

        private UIElement CreateThemeSelector()
        {
            var outer = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            outer.Children.Add(new TextBlock
            {
                Text = "Theme",
                Foreground = _palette.TextMuted,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 5)
            });
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(CreateActionButton("System", (_, __) => SetThemeMode(ThemeMode.System), ActionKind.ThemeChoice, _themeMode == ThemeMode.System));
            panel.Children.Add(CreateActionButton("Dark", (_, __) => SetThemeMode(ThemeMode.Dark), ActionKind.ThemeChoice, _themeMode == ThemeMode.Dark));
            panel.Children.Add(CreateActionButton("Light", (_, __) => SetThemeMode(ThemeMode.Light), ActionKind.ThemeChoice, _themeMode == ThemeMode.Light));
            outer.Children.Add(panel);
            return outer;
        }

        private UIElement CreateTabNavigation()
        {
            var border = new Border
            {
                Background = _palette.CardBackground,
                BorderBrush = _palette.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(7),
                Margin = new Thickness(0, 14, 0, 0)
            };
            _navigationHost = new StackPanel { Orientation = Orientation.Horizontal };
            PopulateTabNavigation();
            border.Child = _navigationHost;
            return border;
        }

        private void PopulateTabNavigation()
        {
            _navigationHost.Children.Add(CreateNavigationButton("Kết nối", 0));
            _navigationHost.Children.Add(CreateNavigationButton("Agent", 1));
            _navigationHost.Children.Add(CreateNavigationButton("Backup & khôi phục", 2));
            _navigationHost.Children.Add(CreateNavigationButton("Nâng cao", 3));
        }

        private Button CreateNavigationButton(string text, int index)
        {
            return CreateActionButton(text, (_, __) => SetSelectedTab(index), ActionKind.Navigation, _selectedTab == index);
        }

        private void SetSelectedTab(int index)
        {
            if (index < 0 || index > 3 || _selectedTab == index) return;
            _selectedTab = index;
            _navigationHost.Children.Clear();
            PopulateTabNavigation();
            _pageHost.Content = CreateActivePage();
            RefreshStatus();
        }

        private UIElement CreateActivePage()
        {
            _statusRows = new StackPanel();
            switch (_selectedTab)
            {
                case 1: return CreateAgentPage();
                case 2: return CreateRecoveryPage();
                case 3: return CreateAdvancedPage();
                default: return CreateConnectionPage();
            }
        }

        private UIElement CreateConnectionPage()
        {
            var grid = CreateTwoColumnGrid();
            var provider = McpTransportCoordinator.SelectedProvider;
            var actions = new StackPanel();
            actions.Children.Add(CreateTransportProviderSelector(provider));

            string title, detail, nextStep;
            GetTransportOnboarding(provider, out title, out detail, out nextStep);
            actions.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = _palette.TextPrimary,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 5)
            });
            actions.Children.Add(new TextBlock
            {
                Text = detail + Environment.NewLine + "Tiếp theo: " + nextStep,
                Foreground = _palette.TextSecondary,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            if (provider == McpTransportProvider.OpenAiSecureTunnel)
                PopulateOpenAiSecureTunnelActions(actions);
            else if (provider == McpTransportProvider.CloudflareQuickTunnel)
                PopulateCloudflareQuickActions(actions);
            else
                PopulateCloudflareNamedActions(actions);

            AddGridCard(grid, CreateSectionCard(
                "Kết nối",
                "Chọn một transport. OpenAI Secure MCP Tunnel là đường không cần domain/public MCP; Cloudflare Named Tunnel giữ đường public URL + OAuth ổn định; Quick Tunnel chỉ test.",
                actions), 0);
            AddGridCard(grid, CreateSectionCard(
                "Trạng thái kết nối",
                "Transport READY và việc user xác nhận đã thêm ChatGPT là trạng thái riêng. Cloudflare còn có bằng chứng OAuth traffic; Secure Tunnel không suy đoán tool traffic chỉ từ tiến trình tunnel.",
                _statusRows), 1);
            return grid;
        }

        private UIElement CreateTransportProviderSelector(McpTransportProvider provider)
        {
            var panel = new WrapPanel { Margin = new Thickness(0, 0, 0, 2) };
            panel.Children.Add(CreateActionButton("OpenAI Secure Tunnel", (_, __) => SelectTransportProvider(McpTransportProvider.OpenAiSecureTunnel), ActionKind.ThemeChoice, provider == McpTransportProvider.OpenAiSecureTunnel));
            panel.Children.Add(CreateActionButton("Cloudflare Named", (_, __) => SelectTransportProvider(McpTransportProvider.CloudflareNamedTunnel), ActionKind.ThemeChoice, provider == McpTransportProvider.CloudflareNamedTunnel));
            panel.Children.Add(CreateActionButton("Cloudflare Quick · test", (_, __) => SelectTransportProvider(McpTransportProvider.CloudflareQuickTunnel), ActionKind.ThemeChoice, provider == McpTransportProvider.CloudflareQuickTunnel));
            return panel;
        }

        private void PopulateOpenAiSecureTunnelActions(StackPanel actions)
        {
            _openAiClientPathText = new TextBlock
            {
                Text = "tunnel-client: " + (string.IsNullOrWhiteSpace(McpOpenAiSecureTunnelManager.SavedClientPath) ? "chưa chọn" : McpOpenAiSecureTunnelManager.SavedClientPath),
                Foreground = _palette.TextMuted,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 6)
            };
            actions.Children.Add(_openAiClientPathText);

            actions.Children.Add(CreateInputLabel("OpenAI Tunnel ID"));
            _openAiTunnelIdText = CreateTextInput(McpOpenAiSecureTunnelManager.SavedTunnelId);
            actions.Children.Add(_openAiTunnelIdText);

            actions.Children.Add(CreateInputLabel("Runtime API key · lưu bảo mật trong Windows Credential Manager sau khi xác minh; để trống để dùng key đã lưu hoặc CONTROL_PLANE_API_KEY/OPENAI_API_KEY"));
            _openAiRuntimeKeyBox = new PasswordBox
            {
                MinHeight = 34,
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 9),
                Background = _palette.SubtleBackground,
                Foreground = _palette.TextPrimary,
                BorderBrush = _palette.Border,
                BorderThickness = new Thickness(1)
            };
            actions.Children.Add(_openAiRuntimeKeyBox);

            actions.Children.Add(CreateActionButton("Mở OpenAI Tunnels", (_, __) => OpenOpenAiPlatformTunnels(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Mở Runtime API keys", (_, __) => OpenOpenAiRuntimeKeys(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Tải tunnel-client chính thức", (_, __) => OpenOpenAiTunnelClientDownload(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Chọn tunnel-client.exe", (_, __) => SelectOpenAiTunnelClient(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Khởi động OpenAI Secure MCP Tunnel", (_, __) => StartOpenAiSecureTunnel(), ActionKind.Primary));
            actions.Children.Add(CreateActionButton("Mở tunnel-client UI", (_, __) => OpenOpenAiAdminUi(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Mở ChatGPT · Connection = Tunnel", (_, __) => OpenChatGpt(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Đã thêm MCP trong ChatGPT", (_, __) => MarkChatGptRegistered(), ActionKind.Secondary));
            actions.Children.Add(new TextBlock
            {
                Text = "Secure Tunnel: ChatGPT chọn Connection = Tunnel và Tunnel ID tương ứng. Không cấu hình QS3D public OAuth URL cho đường này. Runtime API key được xác minh rồi lưu trong Windows Credential Manager; child process nhận key qua environment. Local bearer cũng chỉ truyền qua child environment; không ghi secret vào config/timeline.",
                Foreground = _palette.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        private void PopulateCloudflareNamedActions(StackPanel actions)
        {
            actions.Children.Add(CreateActionButton("Cài / cập nhật Cloudflare Tunnel", InstallCloudflared, ActionKind.Primary));
            actions.Children.Add(CreateActionButton("Đăng nhập Cloudflare + tạo Named Tunnel", (_, __) => OpenAccountSetup(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Khởi động Named Tunnel đã lưu", (_, __) => StartNamedTunnel(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Mở ChatGPT", (_, __) => OpenChatGpt(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Copy MCP URL", (_, __) => CopyUrl(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Đã thêm MCP trong ChatGPT", (_, __) => MarkChatGptRegistered(), ActionKind.Secondary));
        }

        private void PopulateCloudflareQuickActions(StackPanel actions)
        {
            actions.Children.Add(CreateActionButton("Cài / cập nhật Cloudflare Tunnel", InstallCloudflared, ActionKind.Primary));
            actions.Children.Add(CreateActionButton("Khởi động Quick Tunnel · test only", (_, __) => StartQuickTunnel(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Mở ChatGPT", (_, __) => OpenChatGpt(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Copy MCP URL", (_, __) => CopyUrl(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Đã thêm MCP trong ChatGPT", (_, __) => MarkChatGptRegistered(), ActionKind.Secondary));
            actions.Children.Add(new TextBlock
            {
                Text = "Quick Tunnel có hostname thay đổi; khi URL đổi phải reconnect ChatGPT. Không dùng làm transport production ổn định.",
                Foreground = _palette.Warning,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        private TextBlock CreateInputLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = _palette.TextSecondary,
                FontSize = 11.5,
                Margin = new Thickness(0, 1, 0, 4),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private TextBox CreateTextInput(string value)
        {
            return new TextBox
            {
                Text = value ?? string.Empty,
                MinHeight = 34,
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 0, 9),
                Background = _palette.SubtleBackground,
                Foreground = _palette.TextPrimary,
                BorderBrush = _palette.Border,
                BorderThickness = new Thickness(1)
            };
        }

        private void GetTransportOnboarding(McpTransportProvider provider, out string title, out string detail, out string nextStep)
        {
            if (!McpEmbeddedServer.IsRunning)
            {
                title = "MCP local chưa chạy";
                detail = "QS3D sẽ khởi động embedded MCP trên loopback; không cần cài MCP server riêng.";
                nextStep = "Khởi động embedded MCP rồi Refresh.";
                return;
            }

            if (provider == McpTransportProvider.OpenAiSecureTunnel)
            {
                if (string.IsNullOrWhiteSpace(McpOpenAiSecureTunnelManager.SavedClientPath))
                {
                    title = "Cần OpenAI tunnel-client";
                    detail = "Tải tunnel-client chính thức, chọn tunnel-client.exe, sau đó nhập Tunnel ID. Không cần domain hoặc Cloudflare account riêng.";
                    nextStep = "Mở OpenAI Tunnels/tải tunnel-client rồi chọn file executable.";
                    return;
                }
                var tunnelId = string.IsNullOrWhiteSpace(_openAiTunnelIdText.Text) ? McpOpenAiSecureTunnelManager.SavedTunnelId : _openAiTunnelIdText.Text;
                if (!McpOpenAiSecureTunnelManager.IsValidTunnelId(tunnelId))
                {
                    title = "Cần OpenAI Tunnel ID";
                    detail = "Tunnel ID được tạo/tra cứu trong OpenAI Platform Tunnels; Runtime API key cần quyền Tunnels Read + Use.";
                    nextStep = "Nhập tunnel_... và Runtime API key rồi khởi động Secure Tunnel.";
                    return;
                }
                if (!McpOpenAiSecureTunnelManager.IsRunning)
                {
                    title = "Secure Tunnel đã cấu hình";
                    detail = "QS3D sẽ kết nối outbound qua OpenAI tunnel-client đến embedded MCP local; local MCP không cần public URL.";
                    nextStep = "Bấm “Khởi động OpenAI Secure MCP Tunnel”.";
                    return;
                }
                if (!McpOpenAiSecureTunnelManager.IsReady)
                {
                    title = "Secure Tunnel đang khởi động";
                    detail = "tunnel-client đang chạy nhưng /readyz chưa READY.";
                    nextStep = "Chờ vài giây, Refresh hoặc mở tunnel-client UI nếu trạng thái không chuyển.";
                    return;
                }
                if (!McpTransportCoordinator.IsChatGptRegistrationAcknowledged())
                {
                    title = "Secure Tunnel READY · kết nối ChatGPT";
                    detail = "Trong ChatGPT tạo MCP/App với Connection = Tunnel và chọn/paste Tunnel ID hiện tại.";
                    nextStep = "Mở ChatGPT, thêm tunnel connector rồi bấm “Đã thêm MCP trong ChatGPT”.";
                    return;
                }
                title = "Secure Tunnel sẵn sàng";
                detail = "Embedded MCP + OpenAI tunnel-client READY + user đã xác nhận cấu hình ChatGPT. Đây chưa phải bằng chứng một tools/call cụ thể đã chạy.";
                nextStep = "Prompt trong ChatGPT; bật desktop consent local chỉ khi cần thao tác ngoài BricsCAD.";
                return;
            }

            if (provider == McpTransportProvider.CloudflareQuickTunnel)
            {
                var cloudflaredInstalled = !string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath);
                if (!cloudflaredInstalled)
                {
                    title = "Cần Cloudflare Tunnel";
                    detail = "Quick Tunnel dùng cloudflared nhưng không cần domain/login Cloudflare.";
                    nextStep = "Bấm “Cài / cập nhật Cloudflare Tunnel”.";
                    return;
                }
                var publicUrl = McpPublicEndpointResolver.Resolve();
                if (!McpCloudflareTunnelManager.IsRunning || string.IsNullOrWhiteSpace(publicUrl))
                {
                    title = "Quick Tunnel chưa chạy";
                    detail = "Quick Tunnel chỉ dành cho test và URL có thể đổi sau mỗi lần chạy.";
                    nextStep = "Bấm “Khởi động Quick Tunnel · test only”.";
                    return;
                }
                if (!McpTransportCoordinator.IsChatGptRegistrationAcknowledged())
                {
                    title = "Quick Tunnel có public URL";
                    detail = "Thêm URL hiện tại vào ChatGPT bằng OAuth/DCR; nếu URL đổi phải reconnect.";
                    nextStep = "Copy MCP URL, thêm vào ChatGPT rồi xác nhận.";
                    return;
                }
                title = "Quick Tunnel đã cấu hình";
                detail = "Transport test đã có URL và user đã xác nhận ChatGPT; chờ authenticated OAuth MCP traffic để có bằng chứng live.";
                nextStep = "Giữ ChatGPT connector mở và chờ tools/list hoặc tools/call.";
                return;
            }

            var onboarding = McpAgentExperience.DetermineOnboarding();
            title = onboarding.Title;
            detail = onboarding.Detail;
            nextStep = onboarding.NextStep;
        }

        private void SelectTransportProvider(McpTransportProvider provider)
        {
            if (McpTransportCoordinator.SelectedProvider == provider) return;
            McpTransportCoordinator.SetSelectedProvider(provider);
            StopQuickUrlPolling();
            _pageHost.Content = CreateActivePage();
            RefreshStatus();
            ShowToast(ToastKind.Info, "MCP transport", "Đã chọn " + McpTransportCoordinator.SelectedProviderLabel + ". Transport đang chạy khác không tự bị coi là selected/ready.");
        }

        private UIElement CreateAgentPage()
        {
            var grid = CreateTwoColumnGrid();
            var controls = new StackPanel();
            _desktopConsentText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.TextPrimary,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            controls.Children.Add(_desktopConsentText);
            controls.Children.Add(new TextBlock
            {
                Text = "Desktop-wide input mặc định OFF sau mỗi lần mở BricsCAD. Sau khi user Resume, consent tự giữ ON và auto-renew trong suốt phiên BricsCAD; không còn giới hạn idle 10 phút. ChatGPT không có tool để Resume quyền này; chỉ user tại máy local mới bật lại được. Khi thao tác sẽ có viền xanh; Esc ×2 trong 1.2 giây dừng ngay.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.TextSecondary,
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 10)
            });
            controls.Children.Add(CreateActionButton("Resume desktop", (_, __) => ResumeDesktopConsent(), ActionKind.Primary));
            controls.Children.Add(CreateActionButton("Pause desktop", (_, __) => PauseDesktopConsent(), ActionKind.Secondary));
            controls.Children.Add(CreateActionButton("EMERGENCY STOP AGENT", (_, __) => EmergencyStop(), ActionKind.Danger));
            controls.Children.Add(CreateActionButton("Hủy command BricsCAD hiện tại · ESC x2", (_, __) => InvokeControlTool("cad_cancel_command", "{}"), ActionKind.Secondary));
            AddGridCard(grid, CreateSectionCard("Desktop control & khẩn cấp", "Local consent + confirmMutation/sensitive-read + mutation epoch là các lớp độc lập.", controls, true), 0);

            var activity = new StackPanel();
            _desktopActivityText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.TextPrimary,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5
            };
            activity.Children.Add(_desktopActivityText);
            activity.Children.Add(new Border
            {
                Background = _palette.WarningSoft,
                BorderBrush = _palette.WarningBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(11),
                Margin = new Thickness(0, 10, 0, 8),
                Child = new TextBlock
                {
                    Text = "Sau PAUSED / Emergency Stop / failed: Kiểm tra drawing/backup trước. Nếu trạng thái CAD đúng, user có thể Resume desktop local để ChatGPT tiếp tục.",
                    Foreground = _palette.TextPrimary,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11.5
                }
            });
            activity.Children.Add(CreateActionButton("Mở thư mục audit MCP", (_, __) => OpenAuditFolder(), ActionKind.Secondary));
            AddGridCard(grid, CreateSectionCard("Đang làm gì?", "QS3D mirror metadata của MCP action local; không scrape prose từ ChatGPT Web.", activity), 1);
            return grid;
        }

        private UIElement CreateRecoveryPage()
        {
            var grid = CreateTwoColumnGrid();
            var actions = new StackPanel();
            actions.Children.Add(CreateActionButton("Backup ngay", (_, __) => BackupNow(), ActionKind.Primary));
            actions.Children.Add(CreateActionButton("Khôi phục snapshot mới nhất thành file mới", (_, __) => RecoverLatest(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Mở thư mục backup QS3D", (_, __) => OpenFolder(McpProjectRecoveryService.BackupRoot, "Backup"), ActionKind.Secondary));
            AddGridCard(grid, CreateSectionCard("Backup & khôi phục", "Restore luôn tạo Recovered copy mới, không tự ghi đè DWG gốc/đang mở.", actions), 0);

            var status = new StackPanel();
            status.Children.Add(new TextBlock
            {
                Text = McpProjectRecoveryService.Describe(),
                Foreground = _palette.TextPrimary,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5
            });
            status.Children.Add(new TextBlock
            {
                Text = "Backup root: " + McpProjectRecoveryService.BackupRoot + Environment.NewLine
                       + "Policy: giữ SAVETIME ngắn hơn; nếu disabled/>5 phút thì dùng 5 phút; bật ISAVEBAK; tối đa 30 snapshot ổn định/drawing. Recovery luôn ghi sang file mới.",
                Foreground = _palette.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11.5,
                Margin = new Thickness(0, 8, 0, 0)
            });
            AddGridCard(grid, CreateSectionCard("Recovery state", "Versioned recovery chạy bên cạnh native BricsCAD autosave/BAK.", status), 1);
            return grid;
        }

        private UIElement CreateAdvancedPage()
        {
            var grid = CreateTwoColumnGrid();
            _logsHost = new StackPanel();
            RenderActivityHistory();
            AddGridCard(grid, CreateSectionCard("Logs & trạng thái", "Local MCP timeline gần nhất. Mutation audit đầy đủ vẫn ở audit log riêng.", _logsHost), 0);

            var advanced = new StackPanel();
            advanced.Children.Add(CreateActionButton("Kiểm tra MCP protocol", (_, __) => CheckProtocol(), ActionKind.Primary));
            advanced.Children.Add(CreateActionButton("Tự kiểm tra Agent · read-only", (_, __) => RunReadOnlySelfTest(), ActionKind.Secondary));
            advanced.Children.Add(CreateActionButton("Quick Tunnel · test only", (_, __) => StartQuickTunnel(), ActionKind.Secondary));
            advanced.Children.Add(CreateActionButton("Dừng tất cả tunnel", (_, __) => StopTunnels(), ActionKind.Secondary));
            advanced.Children.Add(CreateActionButton("Copy Bearer Token · engineering compatibility", (_, __) => CopyToken(), ActionKind.Secondary));
            advanced.Children.Add(CreateActionButton("Copy URL + Authorization · engineering compatibility", (_, __) => CopyConfig(), ActionKind.Secondary));
            advanced.Children.Add(CreateActionButton("Mở thư mục audit MCP", (_, __) => OpenAuditFolder(), ActionKind.Secondary));
            advanced.Children.Add(new TextBlock
            {
                Text = "Nâng cao: static bearer + Quick Tunnel chỉ dùng debug/backward compatibility. OpenAI Secure Tunnel giữ MCP local khỏi public Internet; Cloudflare Named Tunnel giữ lựa chọn public URL + OAuth/DCR. Completion Pack A dùng explicit desktop tools; Approach B expose một desktop_sequence bounded single-target; desktop_macro không được expose.",
                Foreground = _palette.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11.5,
                Margin = new Thickness(0, 8, 0, 0)
            });
            AddGridCard(grid, CreateSectionCard("Nâng cao", "Chẩn đoán và engineering compatibility.", advanced), 1);
            return grid;
        }

        private Grid CreateTwoColumnGrid()
        {
            var grid = new Grid { Margin = new Thickness(-6, -6, -6, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            return grid;
        }

        private static void AddGridCard(Grid grid, UIElement card, int column)
        {
            Grid.SetColumn(card, column);
            grid.Children.Add(card);
        }

        private Border CreateSectionCard(string title, string description, UIElement body, bool danger = false)
        {
            var content = new StackPanel();
            content.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = danger ? _palette.Danger : _palette.TextPrimary
            });
            content.Children.Add(new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.TextSecondary,
                FontSize = 12,
                LineHeight = 18,
                Margin = new Thickness(0, 5, 0, 12)
            });
            content.Children.Add(body);
            return new Border
            {
                Background = _palette.CardBackground,
                BorderBrush = danger ? _palette.DangerBorder : _palette.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18),
                Margin = new Thickness(6),
                Child = content
            };
        }

        private Button CreateActionButton(string text, RoutedEventHandler handler, ActionKind kind, bool selected = false)
        {
            var compact = kind == ActionKind.Utility || kind == ActionKind.Navigation || kind == ActionKind.ThemeChoice;
            var button = new Button
            {
                Content = text,
                MinHeight = kind == ActionKind.Danger ? 44 : (compact ? 34 : 38),
                Margin = compact ? new Thickness(0, 0, 6, 0) : new Thickness(0, 0, 0, 8),
                Padding = compact ? new Thickness(11, 6, 11, 6) : new Thickness(12, 7, 12, 7),
                HorizontalContentAlignment = compact ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = compact ? 11.5 : 12.5,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(1),
                FocusVisualStyle = null,
                Style = CreateButtonStyle(kind, selected)
            };
            button.Click += handler;
            return button;
        }

        private Style CreateButtonStyle(ActionKind kind, bool selected)
        {
            Brush background;
            Brush foreground;
            Brush border;
            Brush hoverBackground;
            Brush hoverForeground;
            Brush hoverBorder;
            Brush pressedBackground;
            Brush pressedForeground;
            Brush pressedBorder;

            if (kind == ActionKind.Primary)
            {
                background = _palette.Accent;
                foreground = _palette.AccentText;
                border = _palette.Accent;
                hoverBackground = _palette.AccentHover;
                hoverForeground = _palette.AccentText;
                hoverBorder = _palette.AccentHover;
                pressedBackground = _palette.AccentPressed;
                pressedForeground = _palette.AccentText;
                pressedBorder = _palette.AccentPressed;
            }
            else if (kind == ActionKind.Danger)
            {
                background = _palette.DangerSoft;
                foreground = _palette.Danger;
                border = _palette.DangerBorder;
                hoverBackground = _palette.DangerHover;
                hoverForeground = _palette.DangerStrongText;
                hoverBorder = _palette.DangerHover;
                pressedBackground = _palette.DangerPressed;
                pressedForeground = _palette.DangerStrongText;
                pressedBorder = _palette.DangerPressed;
            }
            else if ((kind == ActionKind.Navigation || kind == ActionKind.ThemeChoice) && selected)
            {
                background = _palette.SelectedBackground;
                foreground = _palette.Accent;
                border = _palette.Accent;
                hoverBackground = _palette.SelectedBackground;
                hoverForeground = _palette.Accent;
                hoverBorder = _palette.AccentHover;
                pressedBackground = _palette.SubtleBackground;
                pressedForeground = _palette.Accent;
                pressedBorder = _palette.AccentPressed;
            }
            else
            {
                background = kind == ActionKind.Navigation || kind == ActionKind.ThemeChoice
                    ? CreateBrush(0x00, 0x00, 0x00, 0x00)
                    : _palette.CardBackground;
                foreground = _palette.TextPrimary;
                border = kind == ActionKind.Navigation || kind == ActionKind.ThemeChoice
                    ? CreateBrush(0x00, 0x00, 0x00, 0x00)
                    : _palette.Border;
                hoverBackground = _palette.SubtleBackground;
                hoverForeground = _palette.TextPrimary;
                hoverBorder = _palette.StrongBorder;
                pressedBackground = _palette.SelectedBackground;
                pressedForeground = _palette.TextPrimary;
                pressedBorder = _palette.Accent;
            }

            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.BackgroundProperty, background));
            style.Setters.Add(new Setter(Control.ForegroundProperty, foreground));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, border));
            style.Setters.Add(new Setter(Control.TemplateProperty, CreateButtonTemplate()));

            var focus = new Trigger { Property = Button.IsKeyboardFocusedProperty, Value = true };
            focus.Setters.Add(new Setter(Control.BackgroundProperty, background));
            focus.Setters.Add(new Setter(Control.ForegroundProperty, foreground));
            focus.Setters.Add(new Setter(Control.BorderBrushProperty, _palette.FocusBorder));
            focus.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2)));
            style.Triggers.Add(focus);

            var hover = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Control.BackgroundProperty, hoverBackground));
            hover.Setters.Add(new Setter(Control.ForegroundProperty, hoverForeground));
            hover.Setters.Add(new Setter(Control.BorderBrushProperty, hoverBorder));
            style.Triggers.Add(hover);

            var pressed = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressed.Setters.Add(new Setter(Control.BackgroundProperty, pressedBackground));
            pressed.Setters.Add(new Setter(Control.ForegroundProperty, pressedForeground));
            pressed.Setters.Add(new Setter(Control.BorderBrushProperty, pressedBorder));
            style.Triggers.Add(pressed);

            var disabled = new Trigger { Property = Button.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(Control.BackgroundProperty, _palette.DisabledBackground));
            disabled.Setters.Add(new Setter(Control.ForegroundProperty, _palette.DisabledForeground));
            disabled.Setters.Add(new Setter(Control.BorderBrushProperty, _palette.DisabledBorder));
            style.Triggers.Add(disabled);
            return style;
        }

        private static ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetBinding(Border.BackgroundProperty, TemplatedParentBinding("Background"));
            border.SetBinding(Border.BorderBrushProperty, TemplatedParentBinding("BorderBrush"));
            border.SetBinding(Border.BorderThicknessProperty, TemplatedParentBinding("BorderThickness"));
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetBinding(ContentPresenter.ContentProperty, TemplatedParentBinding("Content"));
            presenter.SetBinding(ContentPresenter.ContentTemplateProperty, TemplatedParentBinding("ContentTemplate"));
            presenter.SetBinding(ContentPresenter.MarginProperty, TemplatedParentBinding("Padding"));
            presenter.SetBinding(ContentPresenter.HorizontalAlignmentProperty, TemplatedParentBinding("HorizontalContentAlignment"));
            presenter.SetBinding(ContentPresenter.VerticalAlignmentProperty, TemplatedParentBinding("VerticalContentAlignment"));
            presenter.SetBinding(TextElement.ForegroundProperty, TemplatedParentBinding("Foreground"));
            border.AppendChild(presenter);
            template.VisualTree = border;
            return template;
        }

        private static Binding TemplatedParentBinding(string path)
        {
            return new Binding(path) { RelativeSource = RelativeSource.TemplatedParent };
        }

        private Border CreateStatusChip(string text, bool active)
        {
            return new Border
            {
                Background = active ? _palette.SuccessSoft : _palette.SubtleBackground,
                BorderBrush = active ? _palette.SuccessBorder : _palette.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(0, 0, 7, 6),
                Child = new TextBlock
                {
                    Text = (active ? "● " : "○ ") + text,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = active ? _palette.Success : _palette.TextMuted
                }
            };
        }

        private UIElement CreateStatusRow(string label, string value, Brush? valueBrush = null)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(122) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var labelBlock = new TextBlock { Text = label, FontSize = 11.5, Foreground = _palette.TextMuted, VerticalAlignment = VerticalAlignment.Top };
            var valueBlock = new TextBlock
            {
                Text = value,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = valueBrush ?? _palette.TextPrimary,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(valueBlock, 1);
            row.Children.Add(labelBlock);
            row.Children.Add(valueBlock);
            return row;
        }

        private UIElement CreateFooter()
        {
            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var refresh = CreateActionButton("Refresh", (_, __) =>
            {
                RefreshStatus();
                ShowToast(ToastKind.Info, "Trạng thái", "Đã làm mới MCP, tunnel, consent, action và recovery.");
            }, ActionKind.Utility);
            refresh.MinWidth = 88;
            var close = CreateActionButton("Đóng", (_, __) => Close(), ActionKind.Utility);
            close.MinWidth = 80;
            footer.Children.Add(refresh);
            footer.Children.Add(close);
            return footer;
        }

        private void AddActivityEntry(ToastKind kind, string title, string message)
        {
            _activityEntries.Insert(0, new ActivityEntry(DateTime.Now, kind, title, message));
            while (_activityEntries.Count > MaxActivityEntries) _activityEntries.RemoveAt(_activityEntries.Count - 1);
            if (_selectedTab == 3 && _logsHost != null) RenderActivityHistory();
        }

        private void RenderActivityHistory()
        {
            if (_logsHost == null) return;
            _logsHost.Children.Clear();
            var localEvents = McpAgentExperience.Recent(16);
            if (localEvents.Length > 0)
            {
                _logsHost.Children.Add(new TextBlock
                {
                    Text = "MCP local timeline",
                    Foreground = _palette.TextPrimary,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 6)
                });
                foreach (var item in localEvents)
                {
                    var action = string.IsNullOrWhiteSpace(item.ActionId)
                        ? string.Empty
                        : " · Action ID=" + item.ActionId + " · " + item.TerminalState + " · " + item.DurationMilliseconds + "ms";
                    _logsHost.Children.Add(new TextBlock
                    {
                        Text = item.Utc.ToLocalTime().ToString("HH:mm:ss") + "  [" + item.Level + "/" + item.Category + "] " + item.Message + action,
                        Foreground = _palette.TextSecondary,
                        TextWrapping = TextWrapping.Wrap,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 10.8,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
                _logsHost.Children.Add(new Border { Height = 10, Background = Brushes.Transparent });
            }
            if (_activityEntries.Count == 0)
            {
                _logsHost.Children.Add(new TextBlock
                {
                    Text = localEvents.Length == 0 ? "Chưa có hoạt động nào trong phiên này." : "Chưa có toast/UI activity bổ sung.",
                    Foreground = _palette.TextMuted,
                    FontSize = 12
                });
                return;
            }
            foreach (var entry in _activityEntries)
            {
                Brush accent, surface, border;
                GetToastColors(entry.Kind, out accent, out surface, out border);
                var stack = new StackPanel();
                stack.Children.Add(new TextBlock
                {
                    Text = entry.Timestamp.ToString("HH:mm:ss") + "  ·  " + entry.Kind + "  ·  " + entry.Title,
                    Foreground = accent,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                });
                stack.Children.Add(new TextBlock
                {
                    Text = entry.Message,
                    Foreground = _palette.TextPrimary,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11.5,
                    Margin = new Thickness(0, 3, 0, 0)
                });
                _logsHost.Children.Add(new Border
                {
                    Background = surface,
                    BorderBrush = border,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(11, 9, 11, 9),
                    Margin = new Thickness(0, 0, 0, 7),
                    Child = stack
                });
            }
        }

        private void ShowToast(ToastKind kind, string title, string message, bool sticky = false)
        {
            AddActivityEntry(kind, title, message);
            if (_toastHost == null || _closed) return;
            while (_visibleToasts.Count >= MaxVisibleToasts) DismissToast(_visibleToasts[0]);
            Brush accent, surface, borderBrush;
            GetToastColors(kind, out accent, out surface, out borderBrush);
            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock { Text = title, Foreground = accent, FontWeight = FontWeights.SemiBold, FontSize = 12.5 });
            textStack.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = _palette.TextPrimary,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11.5,
                Margin = new Thickness(0, 3, 0, 0)
            });
            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentGrid.Children.Add(textStack);
            var close = CreateActionButton("×", (_, __) => { }, ActionKind.Utility);
            close.MinWidth = 30;
            close.Width = 30;
            close.Height = 30;
            close.Margin = new Thickness(8, 0, 0, 0);
            Grid.SetColumn(close, 1);
            contentGrid.Children.Add(close);
            var card = new Border
            {
                Background = surface,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(13, 11, 11, 11),
                Margin = new Thickness(0, 0, 0, 8),
                Child = contentGrid
            };
            var visual = new ToastVisual(card, null);
            close.Click += (_, __) => DismissToast(visual);
            if (!sticky)
            {
                var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = GetToastLifetime(kind) };
                EventHandler handler = (_, __) => DismissToast(visual);
                visual.Timer = timer;
                visual.TimerHandler = handler;
                timer.Tick += handler;
                timer.Start();
            }
            _visibleToasts.Add(visual);
            _toastHost.Children.Add(card);
        }

        private void DismissToast(ToastVisual visual)
        {
            if (visual == null) return;
            if (visual.Timer != null)
            {
                visual.Timer.Stop();
                if (visual.TimerHandler != null) visual.Timer.Tick -= visual.TimerHandler;
                visual.TimerHandler = null;
                visual.Timer = null;
            }
            else visual.TimerHandler = null;
            if (_toastHost != null) _toastHost.Children.Remove(visual.Card);
            _visibleToasts.Remove(visual);
        }

        private void ClearVisibleToasts()
        {
            foreach (var visual in new List<ToastVisual>(_visibleToasts)) DismissToast(visual);
        }

        private static TimeSpan GetToastLifetime(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Warning: return TimeSpan.FromSeconds(7);
                case ToastKind.Error: return TimeSpan.FromSeconds(8);
                default: return TimeSpan.FromSeconds(4);
            }
        }

        private void GetToastColors(ToastKind kind, out Brush accent, out Brush surface, out Brush border)
        {
            switch (kind)
            {
                case ToastKind.Success: accent = _palette.Success; surface = _palette.SuccessSoft; border = _palette.SuccessBorder; return;
                case ToastKind.Warning: accent = _palette.Warning; surface = _palette.WarningSoft; border = _palette.WarningBorder; return;
                case ToastKind.Error: accent = _palette.Danger; surface = _palette.DangerSoft; border = _palette.DangerBorder; return;
                default: accent = _palette.Accent; surface = _palette.SelectedBackground; border = _palette.Accent; return;
            }
        }

        private void InstallCloudflared(object? sender, RoutedEventArgs args)
        {
            if (McpCloudflaredBootstrapper.IsInstalling)
            {
                McpAgentExperience.Info("onboarding", "Cloudflare Tunnel đang được tải/cài; bỏ qua click lặp.", string.Empty,
                    "Chờ download + Authenticode hoàn tất rồi Refresh.");
                ShowToast(ToastKind.Info, "Cloudflare Tunnel", "Cloudflare Tunnel đang được tải/cài. Vui lòng chờ; đây không phải lỗi.");
                return;
            }

            McpAgentExperience.ActionStarted("onboarding", "Đang cài/cập nhật cloudflared...", "Chờ kiểm tra Authenticode hoàn tất.");
            ShowToast(ToastKind.Info, "Cloudflare Tunnel", "Đang tải cloudflared chính thức và kiểm tra Authenticode...");
            var started = McpCloudflaredBootstrapper.BeginInstall((ok, message) => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ok) McpAgentExperience.Success("onboarding", message, "Đăng nhập Cloudflare bằng browser.");
                else McpAgentExperience.Error("onboarding", message, "Kiểm tra mạng/chứng thư rồi thử lại.");
                ShowToast(ok ? ToastKind.Success : ToastKind.Error, ok ? "Cloudflare Tunnel" : "Cài Cloudflare thất bại", message);
                RefreshStatus();
            })));
            if (!started)
            {
                McpAgentExperience.Info("onboarding", "Cloudflare Tunnel đã có install đang chạy; không tạo request cài thứ hai.", string.Empty,
                    "Chờ download + Authenticode hoàn tất rồi Refresh.");
                ShowToast(ToastKind.Info, "Cloudflare Tunnel", "Cloudflare Tunnel đang được tải/cài. Vui lòng chờ; đây không phải lỗi.");
            }
        }

        private void OpenOpenAiPlatformTunnels()
        {
            try { McpOpenAiSecureTunnelManager.OpenPlatformTunnels(); }
            catch (Exception ex) { ShowToast(ToastKind.Error, "OpenAI Tunnels", ex.Message); }
        }

        private void OpenOpenAiRuntimeKeys()
        {
            try { McpOpenAiSecureTunnelManager.OpenRuntimeKeys(); }
            catch (Exception ex) { ShowToast(ToastKind.Error, "OpenAI Runtime API key", ex.Message); }
        }

        private void OpenOpenAiTunnelClientDownload()
        {
            try { McpOpenAiSecureTunnelManager.OpenTunnelClientDownload(); }
            catch (Exception ex) { ShowToast(ToastKind.Error, "OpenAI tunnel-client", ex.Message); }
        }

        private void SelectOpenAiTunnelClient()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Chọn tunnel-client.exe chính thức của OpenAI",
                    Filter = "OpenAI tunnel-client (tunnel-client*.exe)|tunnel-client*.exe|Executable (*.exe)|*.exe",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog(this) != true) return;
                string message;
                var ok = McpOpenAiSecureTunnelManager.SaveClientPath(dialog.FileName, out message);
                ShowToast(ok ? ToastKind.Success : ToastKind.Error, "OpenAI tunnel-client", message);
                if (ok)
                {
                    _openAiClientPathText.Text = "tunnel-client: " + McpOpenAiSecureTunnelManager.SavedClientPath;
                    McpTransportCoordinator.SetSelectedProvider(McpTransportProvider.OpenAiSecureTunnel);
                }
            }
            catch (Exception ex) { ShowToast(ToastKind.Error, "OpenAI tunnel-client", ex.Message); }
            RefreshStatus();
        }

        private void StartOpenAiSecureTunnel()
        {
            try
            {
                McpTransportCoordinator.SetSelectedProvider(McpTransportProvider.OpenAiSecureTunnel);
                var tunnelId = (_openAiTunnelIdText.Text ?? string.Empty).Trim();
                var runtimeKey = _openAiRuntimeKeyBox.Password;
                string message;
                var ok = McpOpenAiSecureTunnelManager.Start(tunnelId, runtimeKey, out message);
                _openAiRuntimeKeyBox.Password = string.Empty;
                if (ok)
                    McpAgentExperience.Success("onboarding", "OpenAI Secure MCP Tunnel đang khởi động; Runtime API key đã được xác minh và lưu bảo mật cho các lần restart.", "Chờ tunnel-client READY rồi kết nối ChatGPT bằng Connection = Tunnel.");
                else
                    McpAgentExperience.Error("onboarding", message, "Kiểm tra tunnel-client, Tunnel ID, Runtime API key và quyền Tunnels Read + Use.");
                ShowToast(ok ? ToastKind.Success : ToastKind.Error, "OpenAI Secure MCP Tunnel", message);
            }
            catch (Exception ex)
            {
                try { _openAiRuntimeKeyBox.Password = string.Empty; } catch { }
                ShowToast(ToastKind.Error, "OpenAI Secure MCP Tunnel", ex.Message);
            }
            RefreshStatus();
        }

        private void OpenOpenAiAdminUi()
        {
            string error;
            if (!McpOpenAiSecureTunnelManager.OpenAdminUi(out error))
                ShowToast(ToastKind.Warning, "tunnel-client UI", error);
            else
                ShowToast(ToastKind.Success, "tunnel-client UI", "Đã mở UI local của tunnel-client.");
        }

        private void OpenAccountSetup()
        {
            try
            {
                McpTransportCoordinator.SetSelectedProvider(McpTransportProvider.CloudflareNamedTunnel);
                if (string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath))
                {
                    string adopted;
                    McpCloudflaredBootstrapper.AdoptExistingManagedBinary(out adopted);
                }
                McpAgentExperience.Info("onboarding", "Mở Cloudflare setup bằng provider-owned browser flow.", string.Empty,
                    "Hoàn tất login và Named Tunnel rồi quay lại Agent Center.");
                new McpCloudflareAccountSetupWindow().ShowDialog();
                ShowToast(ToastKind.Info, "Cloudflare", "Đã đóng cửa sổ thiết lập; trạng thái kết nối được làm mới.");
            }
            catch (Exception ex)
            {
                McpAgentExperience.Error("onboarding", "Cloudflare setup: " + ex.Message, "Kiểm tra Cloudflare setup rồi thử lại.");
                ShowToast(ToastKind.Error, "Cloudflare", ex.Message);
            }
            RefreshStatus();
        }

        private void StartNamedTunnel()
        {
            StopQuickUrlPolling();
            McpTransportCoordinator.SetSelectedProvider(McpTransportProvider.CloudflareNamedTunnel);
            McpOpenAiSecureTunnelManager.StopForHostShutdown();
            string error;
            if (!McpCloudflareAccountTunnelManager.StartSaved(out error))
            {
                McpAgentExperience.Error("onboarding", error, "Mở Cloudflare setup và kiểm tra Named Tunnel.");
                ShowToast(ToastKind.Error, "Named Tunnel", error);
            }
            else
            {
                McpAgentExperience.Success("onboarding", "Named Tunnel đang khởi động.", "Copy MCP URL và mở ChatGPT.");
                ShowToast(ToastKind.Success, "Named Tunnel", "Named Tunnel đang khởi động.");
            }
            RefreshStatus();
        }

        private void StartQuickTunnel()
        {
            McpTransportCoordinator.SetSelectedProvider(McpTransportProvider.CloudflareQuickTunnel);
            McpOpenAiSecureTunnelManager.StopForHostShutdown();
            string error;
            if (!McpCloudflareAccountTunnelManager.StartQuickTunnel(out error))
            {
                StopQuickUrlPolling();
                ShowToast(ToastKind.Error, "Quick Tunnel", error);
                RefreshStatus();
                return;
            }
            McpAgentExperience.Warning("onboarding", "Quick Tunnel đang chạy để test.", "Dùng OpenAI Secure Tunnel hoặc Named Tunnel ổn định cho production.");
            ShowToast(ToastKind.Info, "Quick Tunnel", "Đang khởi động và chờ public URL...");
            StartQuickUrlPolling();
            RefreshStatus();
        }

        private void StartQuickUrlPolling()
        {
            StopQuickUrlPolling();
            _quickUrlPollTicks = 0;
            _quickUrlTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromMilliseconds(1500) };
            _quickUrlTimer.Tick += QuickUrlTimerOnTick;
            _quickUrlTimer.Start();
        }

        private void QuickUrlTimerOnTick(object? sender, EventArgs e)
        {
            _quickUrlPollTicks++;
            RefreshStatus();
            var publicUrl = McpCloudflareTunnelManager.PublicMcpUrl;
            if (!string.IsNullOrWhiteSpace(publicUrl))
            {
                ShowToast(ToastKind.Success, "Quick Tunnel",
                    "Public URL đã có. Khi bấm Connect trong ChatGPT, hãy chờ OAuth + quét tool hoàn tất; Quick Tunnel chỉ để test: " + publicUrl);
                StopQuickUrlPolling();
                return;
            }
            if (!McpCloudflareTunnelManager.IsRunning || _quickUrlPollTicks >= 20)
            {
                ShowToast(ToastKind.Warning, "Quick Tunnel",
                    McpCloudflareTunnelManager.IsRunning
                        ? "Tunnel đang chạy nhưng chưa nhận được public URL trong thời gian chờ. Bấm Refresh để kiểm tra lại."
                        : "Quick Tunnel đã dừng trước khi nhận được public URL.");
                StopQuickUrlPolling();
            }
        }

        private void StopQuickUrlPolling()
        {
            var timer = _quickUrlTimer;
            _quickUrlTimer = null;
            if (timer == null) return;
            timer.Stop();
            timer.Tick -= QuickUrlTimerOnTick;
        }

        private void StopTunnels()
        {
            StopQuickUrlPolling();
            McpOpenAiSecureTunnelManager.StopForHostShutdown();
            McpCloudflareAccountTunnelManager.StopForHostShutdown();
            McpCloudflareTunnelManager.StopForHostShutdown();
            McpAgentExperience.Warning("onboarding", "Đã dừng mọi tunnel trong phiên này.", "Khởi động lại transport đã chọn khi cần kết nối.");
            ShowToast(ToastKind.Success, "MCP transport", "Đã dừng OpenAI Secure Tunnel và các Cloudflare tunnel trong phiên BricsCAD này.");
            RefreshStatus();
        }

        private void OpenChatGpt()
        {
            try
            {
                if (McpTransportCoordinator.SelectedProvider == McpTransportProvider.OpenAiSecureTunnel)
                {
                    McpOpenAiSecureTunnelManager.OpenChatGptConnectors();
                    McpAgentExperience.Info("onboarding", "Đã mở ChatGPT connector settings.", string.Empty,
                        "Tạo MCP/App với Connection = Tunnel và chọn Tunnel ID hiện tại.");
                    ShowToast(ToastKind.Success, "ChatGPT", "Đã mở ChatGPT. Với Secure Tunnel hãy chọn Connection = Tunnel, không dùng public URL/OAuth QS3D.");
                }
                else
                {
                    McpCloudflareAccountTunnelManager.OpenChatGpt();
                    McpAgentExperience.Info("onboarding", "Đã mở ChatGPT trong browser hệ thống.", string.Empty,
                        "Thêm public MCP URL bằng OAuth/DCR rồi đánh dấu đã thêm MCP.");
                    ShowToast(ToastKind.Success, "ChatGPT", "Đã mở ChatGPT trong browser. Dùng URL + OAuth trên basic connector screen.");
                }
            }
            catch (Exception ex) { ShowToast(ToastKind.Error, "ChatGPT", ex.Message); }
        }

        private void MarkChatGptRegistered()
        {
            try
            {
                McpTransportCoordinator.MarkChatGptRegistrationAcknowledged();
                if (McpTransportCoordinator.SelectedProvider == McpTransportProvider.OpenAiSecureTunnel)
                {
                    McpAgentExperience.Success("onboarding", "Đã ghi nhận user cấu hình ChatGPT cho OpenAI Tunnel ID hiện tại.", "Giữ tunnel-client READY và thử tools/list/tool call từ ChatGPT.");
                    ShowToast(ToastKind.Success, "ChatGPT Connector",
                        "Đã ghi nhận Tunnel connector hiện tại. Đây là xác nhận cài đặt của user, chưa phải bằng chứng một tools/call đã chạy.");
                }
                else
                {
                    McpAgentExperience.MarkChatGptRegistrationAcknowledged();
                    ShowToast(ToastKind.Success, "ChatGPT Connector",
                        "Đã ghi nhận bạn đã thêm MCP URL hiện tại. Đây là xác nhận cài đặt, chưa phải bằng chứng traffic; OAuth MCP traffic sẽ tự hiện khi ChatGPT gọi server.");
                }
            }
            catch (Exception ex) { ShowToast(ToastKind.Error, "ChatGPT Connector", ex.Message); }
            RefreshStatus();
        }

        private void CopyUrl()
        {
            if (McpTransportCoordinator.SelectedProvider == McpTransportProvider.OpenAiSecureTunnel)
            {
                ShowToast(ToastKind.Info, "MCP URL", "OpenAI Secure Tunnel không cần public MCP URL. Trong ChatGPT chọn Connection = Tunnel và Tunnel ID hiện tại.");
                return;
            }
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowToast(ToastKind.Warning, "MCP URL", "Chưa có public MCP URL. Hãy khởi động Cloudflare transport đã chọn trước.");
                return;
            }
            try { Clipboard.SetText(url); ShowToast(ToastKind.Success, "MCP URL", "Đã copy public MCP URL."); }
            catch (Exception ex) { ShowToast(ToastKind.Error, "Clipboard", ex.Message); }
        }

        private void CopyToken()
        {
            try
            {
                McpEmbeddedServer.EnsureStarted();
                Clipboard.SetText(McpEmbeddedServer.GetBearerToken());
                ShowToast(ToastKind.Warning, "Bearer Token", "Đã copy engineering bearer. Không chia sẻ token công khai; Secure Tunnel tự inject local bearer, Cloudflare production dùng OAuth.");
            }
            catch (Exception ex) { ShowToast(ToastKind.Error, "Bearer Token", ex.Message); }
        }

        private void CopyConfig()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowToast(ToastKind.Warning, "Engineering config", "Chưa có public MCP URL. Secure Tunnel không dùng clipboard config này.");
                return;
            }
            try
            {
                Clipboard.SetText("MCP URL: " + url + Environment.NewLine + "Authorization: Bearer " + McpEmbeddedServer.GetBearerToken());
                ShowToast(ToastKind.Warning, "Engineering config", "Đã copy URL + Authorization cho compatibility/debug. Secret không được ghi vào Logs.");
            }
            catch (Exception ex) { ShowToast(ToastKind.Error, "Engineering config", ex.Message); }
        }

        private void ResumeDesktopConsent()
        {
            try
            {
                McpDesktopControlSession.ResumeFromLocalUser();
                ShowToast(ToastKind.Success, "Resume desktop", "Desktop consent ON · auto-renew trong phiên, không còn timeout 10 phút; Esc ×2 hoặc Pause desktop để dừng ngay.");
            }
            catch (Exception ex) { ShowToast(ToastKind.Error, "Resume desktop", ex.Message); }
            RefreshStatus();
        }

        private void PauseDesktopConsent()
        {
            try
            {
                McpDesktopControlSession.PauseFromLocalUser("User bấm Pause desktop trong Agent Center.");
                ShowToast(ToastKind.Warning, "Pause desktop", "Đã PAUSED desktop control và emergency-stop mutation. Kiểm tra drawing/backup trước khi Resume.");
            }
            catch (Exception ex) { ShowToast(ToastKind.Error, "Pause desktop", ex.Message); }
            RefreshStatus();
        }

        private void EmergencyStop()
        {
            try { McpDesktopControlSession.DisableFromLocalUser("User bấm EMERGENCY STOP AGENT trong Agent Center."); } catch { }
            InvokeControlTool("cad_agent_stop", "{}");
            ShowToast(ToastKind.Warning, "Emergency Stop", "Đã dừng Agent và thu hồi desktop consent. Kiểm tra drawing/backup trước khi bật lại.", true);
            RefreshStatus();
        }

        private void BackupNow()
        {
            string message;
            var ok = McpProjectRecoveryService.BackupNow(out message);
            ShowToast(ok ? ToastKind.Success : ToastKind.Warning, "Backup", message);
            RefreshStatus();
        }

        private void RecoverLatest()
        {
            string path, message;
            var ok = McpProjectRecoveryService.RecoverLatestToCopy(out path, out message);
            ShowToast(ok ? ToastKind.Success : ToastKind.Warning, "Recovery", message);
            if (ok && !string.IsNullOrWhiteSpace(path))
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory)) OpenFolder(directory, "Recovery");
            }
            RefreshStatus();
        }

        private void CheckProtocol()
        {
            RunLocalOperation("Đang kiểm tra MCP protocol...", () =>
            {
                McpEmbeddedServer.EnsureStarted();
                return McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, 5000).Message;
            }, true);
        }

        private void RunReadOnlySelfTest()
        {
            RunLocalOperation("Đang chạy Agent self-test read-only...", () =>
            {
                McpEmbeddedServer.EnsureStarted();
                return McpLocalAgentClient.RunReadOnlySelfTest(McpEmbeddedServer.Endpoint, 7000);
            }, true);
        }

        private void InvokeControlTool(string tool, string arguments)
        {
            RunLocalOperation("Đang gọi " + tool + "...", () =>
            {
                McpEmbeddedServer.EnsureStarted();
                return McpLocalAgentClient.CallOne(McpEmbeddedServer.Endpoint, 6000, tool, arguments);
            }, false);
        }

        private void RunLocalOperation(string pendingMessage, Func<string> action, bool serialize)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var ownsSlot = false;
            if (serialize)
            {
                if (Interlocked.CompareExchange(ref _localOperationActive, 1, 0) != 0)
                {
                    ShowToast(ToastKind.Warning, "MCP local check", "Một MCP local check khác đang chạy; Emergency Stop/ESC vẫn khả dụng.");
                    return;
                }
                ownsSlot = true;
            }
            McpAgentExperience.ActionStarted("agent", pendingMessage, "Chờ local loopback operation hoàn tất.");
            ShowToast(ToastKind.Info, "MCP local check", pendingMessage);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string message;
                try
                {
                    message = action();
                    McpAgentExperience.Success("agent", message, "Refresh status hoặc tiếp tục workflow.");
                }
                catch (Exception ex)
                {
                    message = "MCP local operation FAIL: " + ex.Message;
                    McpAgentExperience.Error("agent", message, "Kiểm tra embedded MCP/tunnel và thử lại.");
                }
                finally
                {
                    if (ownsSlot) Interlocked.Exchange(ref _localOperationActive, 0);
                }
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var failed = message.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0
                                     || message.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0;
                        ShowToast(failed ? ToastKind.Error : ToastKind.Success,
                            failed ? "MCP operation thất bại" : "MCP operation hoàn tất", message);
                        RefreshStatus();
                    }));
                }
                catch { }
            });
        }

        private void OpenAuditFolder()
        {
            var directory = Path.GetDirectoryName(McpEmbeddedServer.AuditFilePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                ShowToast(ToastKind.Warning, "Audit", "Không xác định được thư mục audit MCP.");
                return;
            }
            OpenFolder(directory, "Audit");
        }

        private void OpenFolder(string directory, string title)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    ShowToast(ToastKind.Warning, title, "Không xác định được thư mục.");
                    return;
                }
                Directory.CreateDirectory(directory);
                Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
                ShowToast(ToastKind.Success, title, "Đã mở thư mục.");
            }
            catch (Exception ex) { ShowToast(ToastKind.Error, title, ex.Message); }
        }

        private void RefreshStatus()
        {
            McpDesktopControlSession.ExpireConsentIfIdle();
            var provider = McpTransportCoordinator.SelectedProvider;
            var publicUrl = McpPublicEndpointResolver.Resolve();
            var mcpRunning = McpEmbeddedServer.IsRunning;
            var openAiRunning = McpOpenAiSecureTunnelManager.IsRunning;
            var openAiReady = openAiRunning && McpOpenAiSecureTunnelManager.IsReady;
            var namedTunnelRunning = McpCloudflareAccountTunnelManager.IsRunning;
            var quickTunnelRunning = McpCloudflareTunnelManager.IsRunning;
            var selectedTunnelRunning = provider == McpTransportProvider.OpenAiSecureTunnel
                ? openAiRunning
                : provider == McpTransportProvider.CloudflareQuickTunnel ? quickTunnelRunning : namedTunnelRunning;
            var cloudflaredInstalled = !string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath);
            var authenticated = McpCloudflareAccountTunnelManager.IsAuthenticated;
            var desktopConsent = McpDesktopControlSession.IsEnabled;
            var desktopState = McpDesktopControlSession.ConsentState;
            var idleText = desktopConsent ? "AUTO-RENEW" : "—";
            var transportReady = provider == McpTransportProvider.OpenAiSecureTunnel
                ? mcpRunning && openAiReady
                : mcpRunning && selectedTunnelRunning && !string.IsNullOrWhiteSpace(publicUrl);
            var chatGptRegistered = McpTransportCoordinator.IsChatGptRegistrationAcknowledged();
            var recentOAuthMcpActivity = provider != McpTransportProvider.OpenAiSecureTunnel && mcpRunning && HasRecentOAuthMcpActivity(publicUrl);
            var connectionEvidenceText = provider == McpTransportProvider.OpenAiSecureTunnel
                ? (openAiReady
                    ? "tunnel-client READY; QS3D không suy đoán tools/call chỉ từ readiness."
                    : openAiRunning ? "tunnel-client RUNNING · chờ /readyz." : "Secure Tunnel chưa chạy.")
                : FormatOAuthMcpActivity(publicUrl);
            string onboardingTitle, onboardingDetail, onboardingNext;
            GetTransportOnboarding(provider, out onboardingTitle, out onboardingDetail, out onboardingNext);

            _statusChips.Children.Clear();
            _statusChips.Children.Add(CreateStatusChip(mcpRunning ? "MCP online" : "MCP offline", mcpRunning));
            _statusChips.Children.Add(CreateStatusChip(selectedTunnelRunning ? "Tunnel online" : "Tunnel offline", selectedTunnelRunning));
            _statusChips.Children.Add(CreateStatusChip(transportReady ? "Transport sẵn sàng" : "Transport chưa sẵn sàng", transportReady));
            _statusChips.Children.Add(CreateStatusChip(
                provider == McpTransportProvider.OpenAiSecureTunnel
                    ? (chatGptRegistered ? "ChatGPT Tunnel đã xác nhận" : "ChatGPT Tunnel chưa xác nhận")
                    : recentOAuthMcpActivity ? "ChatGPT OAuth traffic gần đây"
                    : chatGptRegistered ? "ChatGPT đã đăng ký · chờ traffic" : "ChatGPT chưa xác nhận",
                provider == McpTransportProvider.OpenAiSecureTunnel ? chatGptRegistered && transportReady : recentOAuthMcpActivity));
            _statusChips.Children.Add(CreateStatusChip("Desktop " + desktopState, desktopConsent));

            _statusRows.Children.Clear();
            _statusRows.Children.Add(CreateStatusRow("Transport", McpTransportCoordinator.SelectedProviderLabel, _palette.Accent));
            _statusRows.Children.Add(CreateStatusRow("MCP embedded", mcpRunning ? "RUNNING" : "STOPPED", mcpRunning ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Local endpoint", McpEmbeddedServer.Endpoint.ToString()));
            _statusRows.Children.Add(CreateStatusRow("OpenAI client", string.IsNullOrWhiteSpace(McpOpenAiSecureTunnelManager.SavedClientPath) ? "Chưa chọn" : McpOpenAiSecureTunnelManager.SavedClientPath));
            _statusRows.Children.Add(CreateStatusRow("OpenAI Tunnel", openAiRunning ? (openAiReady ? "READY" : "RUNNING / chờ READY") : "STOPPED", openAiReady ? _palette.Success : (openAiRunning ? _palette.Warning : _palette.TextMuted)));
            _statusRows.Children.Add(CreateStatusRow("Tunnel ID", McpOpenAiSecureTunnelManager.IsValidTunnelId(McpOpenAiSecureTunnelManager.SavedTunnelId) ? McpOpenAiSecureTunnelManager.SavedTunnelId : "Chưa cấu hình"));
            _statusRows.Children.Add(CreateStatusRow("Cloudflare", cloudflaredInstalled ? "Đã cài" : "Chưa cài", cloudflaredInstalled ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Browser login", authenticated ? "Đã đăng nhập" : "Chưa đăng nhập", authenticated ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Named Tunnel", namedTunnelRunning ? "RUNNING" : "STOPPED", namedTunnelRunning ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Quick Tunnel", quickTunnelRunning ? "RUNNING / test only" : "STOPPED", quickTunnelRunning ? _palette.Warning : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Public MCP", provider == McpTransportProvider.OpenAiSecureTunnel ? "Không cần public URL" : string.IsNullOrWhiteSpace(publicUrl) ? "Chưa có public URL" : publicUrl));
            _statusRows.Children.Add(CreateStatusRow("Transport sẵn sàng", transportReady
                ? provider == McpTransportProvider.OpenAiSecureTunnel ? "CÓ · MCP + Secure Tunnel READY" : "CÓ · MCP + tunnel + public URL"
                : "CHƯA", transportReady ? _palette.Success : _palette.Warning));
            _statusRows.Children.Add(CreateStatusRow("ChatGPT đăng ký", chatGptRegistered
                ? provider == McpTransportProvider.OpenAiSecureTunnel ? "Đã xác nhận Tunnel ID hiện tại" : "Đã xác nhận URL hiện tại"
                : "Chưa xác nhận", chatGptRegistered ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow(provider == McpTransportProvider.OpenAiSecureTunnel ? "Tunnel evidence" : "OAuth MCP traffic", connectionEvidenceText,
                provider == McpTransportProvider.OpenAiSecureTunnel ? (openAiReady ? _palette.Success : _palette.TextMuted) : (recentOAuthMcpActivity ? _palette.Success : _palette.TextMuted)));
            _statusRows.Children.Add(CreateStatusRow("Onboarding", onboardingTitle));
            _statusRows.Children.Add(CreateStatusRow("Desktop consent", desktopState, desktopConsent ? _palette.Success : _palette.Warning));
            _statusRows.Children.Add(CreateStatusRow("Gia hạn", idleText));
            _statusRows.Children.Add(CreateStatusRow("Action ID", string.IsNullOrWhiteSpace(McpAgentExperience.LastActionId) ? "—" : McpAgentExperience.LastActionId));
            _statusRows.Children.Add(CreateStatusRow("Action state", string.IsNullOrWhiteSpace(McpAgentExperience.LastTerminalState)
                ? "—" : McpAgentExperience.LastTerminalState + " · " + McpAgentExperience.LastDurationMilliseconds + " ms"));
            _statusRows.Children.Add(CreateStatusRow("Agent", McpEmbeddedServer.Describe()));

            if (_desktopConsentText != null)
            {
                _desktopConsentText.Text = "Desktop control: " + desktopState + (desktopConsent ? " · " + idleText : " · local Resume required");
                _desktopConsentText.Foreground = desktopConsent ? _palette.Success : (desktopState == "PAUSED" ? _palette.Warning : _palette.TextMuted);
            }
            if (_desktopActivityText != null)
            {
                _desktopActivityText.Text =
                    "Đang làm: " + (string.IsNullOrWhiteSpace(McpAgentExperience.CurrentAction) ? "không có desktop action đang chạy" : McpAgentExperience.CurrentAction)
                    + Environment.NewLine + "Action ID: " + (string.IsNullOrWhiteSpace(McpAgentExperience.LastActionId) ? "—" : McpAgentExperience.LastActionId)
                    + Environment.NewLine + "Trạng thái cuối: " + (string.IsNullOrWhiteSpace(McpAgentExperience.LastTerminalState) ? "—" : McpAgentExperience.LastTerminalState)
                    + Environment.NewLine + "Duration: " + McpAgentExperience.LastDurationMilliseconds + " ms"
                    + Environment.NewLine + "Bước tiếp: " + (string.IsNullOrWhiteSpace(McpAgentExperience.NextStep) ? onboardingNext : McpAgentExperience.NextStep);
            }
            if (_selectedTab == 3 && _logsHost != null) RenderActivityHistory();
        }

        private static bool HasRecentOAuthMcpActivity(string publicUrl)
        {
            if (string.IsNullOrWhiteSpace(publicUrl)) return false;
            var lastUtc = McpEmbeddedServer.LastOAuthMcpActivityUtc;
            if (lastUtc == DateTime.MinValue) return false;
            if (!string.Equals(McpEmbeddedServer.LastOAuthMcpPublicUrl, publicUrl, StringComparison.OrdinalIgnoreCase)) return false;
            var age = DateTime.UtcNow - lastUtc;
            return age >= TimeSpan.Zero && age <= OAuthMcpActivityFreshness;
        }

        private static string FormatOAuthMcpActivity(string publicUrl)
        {
            var lastUtc = McpEmbeddedServer.LastOAuthMcpActivityUtc;
            if (lastUtc == DateTime.MinValue) return "Chưa quan sát request /mcp đã xác thực bằng OAuth.";
            if (string.IsNullOrWhiteSpace(publicUrl)
                || !string.Equals(McpEmbeddedServer.LastOAuthMcpPublicUrl, publicUrl, StringComparison.OrdinalIgnoreCase))
                return "Traffic OAuth gần nhất thuộc public URL trước; cần reconnect URL hiện tại.";
            var method = string.IsNullOrWhiteSpace(McpEmbeddedServer.LastOAuthMcpMethod) ? "MCP" : McpEmbeddedServer.LastOAuthMcpMethod;
            var prefix = HasRecentOAuthMcpActivity(publicUrl) ? "Gần đây" : "Đã thấy trước đó";
            return prefix + " · " + method + " · " + lastUtc.ToLocalTime().ToString("HH:mm:ss");
        }

        private static string FormatIdle(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "0:00";
            return ((int)remaining.TotalMinutes).ToString() + ":" + remaining.Seconds.ToString("00");
        }

        private static ThemePalette CreateLightPalette()
        {
            return new ThemePalette
            {
                WindowBackground = CreateBrush(0xF4, 0xF7, 0xFB), CardBackground = CreateBrush(0xFF, 0xFF, 0xFF),
                SubtleBackground = CreateBrush(0xF2, 0xF4, 0xF7), SelectedBackground = CreateBrush(0xEA, 0xF2, 0xFF),
                Border = CreateBrush(0xDC, 0xE4, 0xEF), StrongBorder = CreateBrush(0xA9, 0xB5, 0xC7),
                TextPrimary = CreateBrush(0x17, 0x20, 0x33), TextSecondary = CreateBrush(0x4B, 0x57, 0x6B), TextMuted = CreateBrush(0x66, 0x70, 0x85),
                Accent = CreateBrush(0x25, 0x63, 0xEB), AccentHover = CreateBrush(0x1D, 0x4E, 0xD8), AccentPressed = CreateBrush(0x1E, 0x40, 0xAF), AccentText = CreateBrush(0xFF, 0xFF, 0xFF),
                Success = CreateBrush(0x15, 0x80, 0x3D), SuccessSoft = CreateBrush(0xE9, 0xF8, 0xEF), SuccessBorder = CreateBrush(0xAB, 0xEF, 0xC6),
                Warning = CreateBrush(0x9A, 0x67, 0x00), WarningSoft = CreateBrush(0xFF, 0xF8, 0xE1), WarningBorder = CreateBrush(0xF0, 0xC3, 0x6B),
                Danger = CreateBrush(0xB4, 0x23, 0x18), DangerSoft = CreateBrush(0xFD, 0xEC, 0xEC), DangerHover = CreateBrush(0xB4, 0x23, 0x18), DangerPressed = CreateBrush(0x91, 0x1D, 0x14), DangerBorder = CreateBrush(0xFD, 0xB0, 0xA8), DangerStrongText = CreateBrush(0xFF, 0xFF, 0xFF),
                DisabledBackground = CreateBrush(0xEA, 0xEC, 0xF0), DisabledForeground = CreateBrush(0x98, 0xA2, 0xB3), DisabledBorder = CreateBrush(0xD0, 0xD5, 0xDD), FocusBorder = CreateBrush(0x15, 0x5E, 0xD8)
            };
        }

        private static ThemePalette CreateDarkPalette()
        {
            return new ThemePalette
            {
                WindowBackground = CreateBrush(0x0F, 0x14, 0x1F), CardBackground = CreateBrush(0x18, 0x20, 0x2E),
                SubtleBackground = CreateBrush(0x22, 0x2C, 0x3B), SelectedBackground = CreateBrush(0x1D, 0x35, 0x5F),
                Border = CreateBrush(0x34, 0x40, 0x52), StrongBorder = CreateBrush(0x5A, 0x69, 0x7F),
                TextPrimary = CreateBrush(0xF2, 0xF4, 0xF7), TextSecondary = CreateBrush(0xC1, 0xC9, 0xD6), TextMuted = CreateBrush(0x98, 0xA2, 0xB3),
                Accent = CreateBrush(0x6E, 0x9C, 0xFF), AccentHover = CreateBrush(0x86, 0xAC, 0xFF), AccentPressed = CreateBrush(0x4F, 0x7D, 0xE8), AccentText = CreateBrush(0x08, 0x12, 0x24),
                Success = CreateBrush(0x75, 0xE0, 0xA1), SuccessSoft = CreateBrush(0x14, 0x37, 0x28), SuccessBorder = CreateBrush(0x2B, 0x6E, 0x4A),
                Warning = CreateBrush(0xF6, 0xD3, 0x70), WarningSoft = CreateBrush(0x3C, 0x30, 0x12), WarningBorder = CreateBrush(0x78, 0x61, 0x24),
                Danger = CreateBrush(0xFF, 0x9D, 0x96), DangerSoft = CreateBrush(0x3B, 0x1E, 0x20), DangerHover = CreateBrush(0xC9, 0x43, 0x3A), DangerPressed = CreateBrush(0x9E, 0x2F, 0x28), DangerBorder = CreateBrush(0x7B, 0x36, 0x37), DangerStrongText = CreateBrush(0xFF, 0xFF, 0xFF),
                DisabledBackground = CreateBrush(0x20, 0x27, 0x33), DisabledForeground = CreateBrush(0x69, 0x74, 0x86), DisabledBorder = CreateBrush(0x31, 0x3A, 0x49), FocusBorder = CreateBrush(0x8B, 0xB1, 0xFF)
            };
        }

        private static Brush CreateBrush(byte red, byte green, byte blue)
        {
            return CreateBrush(0xFF, red, green, blue);
        }

        private static Brush CreateBrush(byte alpha, byte red, byte green, byte blue)
        {
            var brush = new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
            brush.Freeze();
            return brush;
        }
    }
}
