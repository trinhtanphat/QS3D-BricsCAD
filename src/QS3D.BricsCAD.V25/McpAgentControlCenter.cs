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
    /// One-window, click-first operations center for non-technical MCP users.
    /// Provider credentials remain in provider-owned browser flows. The UI combines the
    /// production Agent-Center tabs/toasts/theme system with guided OAuth onboarding,
    /// explicit local desktop consent and bounded DWG recovery.
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
                new McpAgentControlCenterWindow().ShowDialog();
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
            public ToastVisual(Border card, DispatcherTimer timer)
            {
                Card = card;
                Timer = timer;
            }

            public Border Card { get; private set; }
            public DispatcherTimer Timer { get; set; }
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
        private int _localOperationActive;
        private DispatcherTimer _quickUrlTimer;
        private DispatcherTimer _liveRefreshTimer;
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

        private void OnWindowClosed(object sender, EventArgs e)
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

        private void LiveRefreshTimerOnTick(object sender, EventArgs e)
        {
            if (_closed) return;
            try { RefreshStatus(); } catch { }
        }

        private void SystemEventsOnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
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
            if (announce)
                ShowToast(ToastKind.Info, "Giao diện", "Theme hiện tại: " + GetThemeModeLabel(_themeMode) + ".");
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
                Text = "ChatGPT ↔ OAuth MCP ↔ QS3D ↔ BricsCAD",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = _palette.Accent,
                Margin = new Thickness(0, 4, 0, 0)
            });
            left.Children.Add(new TextBlock
            {
                Text = "Kết nối, kiểm tra, điều khiển Agent và khôi phục dự án từ một nơi. Password/login ở browser hệ thống; QS3D không scrape cookie hoặc hội thoại ChatGPT.",
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
            _navigationHost.Children.Add(CreateNavigationButton("Tổng quan", 0));
            _navigationHost.Children.Add(CreateNavigationButton("Cloudflare", 1));
            _navigationHost.Children.Add(CreateNavigationButton("ChatGPT Connector", 2));
            _navigationHost.Children.Add(CreateNavigationButton("Điều khiển Agent", 3));
            _navigationHost.Children.Add(CreateNavigationButton("Backup & khôi phục", 4));
            _navigationHost.Children.Add(CreateNavigationButton("Logs", 5));
        }

        private Button CreateNavigationButton(string text, int index)
        {
            return CreateActionButton(text, (_, __) => SetSelectedTab(index), ActionKind.Navigation, _selectedTab == index);
        }

        private void SetSelectedTab(int index)
        {
            if (index < 0 || index > 5 || _selectedTab == index) return;
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
                case 1: return CreateCloudflarePage();
                case 2: return CreateConnectorPage();
                case 3: return CreateAgentControlPage();
                case 4: return CreateRecoveryPage();
                case 5: return CreateLogsPage();
                default: return CreateOverviewPage();
            }
        }

        private UIElement CreateOverviewPage()
        {
            var grid = CreateTwoColumnGrid();
            var quick = new StackPanel();
            var onboarding = McpAgentExperience.DetermineOnboarding();
            quick.Children.Add(new TextBlock
            {
                Text = onboarding.Title,
                Foreground = _palette.TextPrimary,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            });
            quick.Children.Add(new TextBlock
            {
                Text = onboarding.Detail + Environment.NewLine + "Tiếp theo: " + onboarding.NextStep,
                Foreground = _palette.TextSecondary,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });
            quick.Children.Add(CreateActionButton("Cài / cập nhật Cloudflare Tunnel", InstallCloudflared, ActionKind.Primary));
            quick.Children.Add(CreateActionButton("Đăng nhập Cloudflare + tạo Named Tunnel", (_, __) => OpenAccountSetup(), ActionKind.Secondary));
            quick.Children.Add(CreateActionButton("Mở ChatGPT", (_, __) => OpenChatGpt(), ActionKind.Secondary));
            quick.Children.Add(CreateActionButton("Copy MCP URL", (_, __) => CopyUrl(), ActionKind.Secondary));
            AddGridCard(grid, CreateSectionCard("Bắt đầu nhanh", "Luồng production: Named Tunnel ổn định + ChatGPT URL/OAuth, không cần PowerShell hay CMD.", quick), 0);
            AddGridCard(grid, CreateSectionCard("Trạng thái kết nối", "MCP, tunnel, endpoint và desktop-consent hiện tại.", _statusRows), 1);
            return grid;
        }

        private UIElement CreateCloudflarePage()
        {
            var grid = CreateTwoColumnGrid();
            var actions = new StackPanel();
            actions.Children.Add(CreateActionButton("Cài / cập nhật Cloudflare Tunnel", InstallCloudflared, ActionKind.Primary));
            actions.Children.Add(CreateActionButton("Đăng nhập Cloudflare + tạo Named Tunnel", (_, __) => OpenAccountSetup(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Khởi động Named Tunnel đã lưu", (_, __) => StartNamedTunnel(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Quick Tunnel · test only", (_, __) => StartQuickTunnel(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Dừng tất cả tunnel", (_, __) => StopTunnels(), ActionKind.Secondary));
            AddGridCard(grid, CreateSectionCard("Thiết lập & Tunnel", "Cloudflare credential/password chỉ nhập trên trang Cloudflare trong browser. Named Tunnel là production path.", actions), 0);
            AddGridCard(grid, CreateSectionCard("Trạng thái Cloudflare", "Quick Tunnel chỉ dùng test vì hostname có thể thay đổi và làm resource-bound OAuth cần kết nối lại.", _statusRows), 1);
            return grid;
        }

        private UIElement CreateConnectorPage()
        {
            var grid = CreateTwoColumnGrid();
            var actions = new StackPanel();
            actions.Children.Add(CreateActionButton("Mở ChatGPT", (_, __) => OpenChatGpt(), ActionKind.Primary));
            actions.Children.Add(CreateActionButton("Copy MCP URL", (_, __) => CopyUrl(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Đã thêm MCP trong ChatGPT", (_, __) => MarkChatGptRegistered(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Kiểm tra MCP protocol", (_, __) => CheckProtocol(), ActionKind.Secondary));
            actions.Children.Add(CreateActionButton("Tự kiểm tra Agent · read-only", (_, __) => RunReadOnlySelfTest(), ActionKind.Secondary));
            AddGridCard(grid, CreateSectionCard("ChatGPT Connector", "Luồng chuẩn là URL + OAuth/DCR trên basic screen. QS3D không lấy ChatGPT password và không scrape browser cookie.", actions), 0);

            var right = new StackPanel();
            right.Children.Add(_statusRows);
            right.Children.Add(new Border
            {
                Background = _palette.SubtleBackground,
                BorderBrush = _palette.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 8, 0, 0),
                Child = new TextBlock
                {
                    Text = "Engineering bearer được chuyển xuống khu vực Nâng cao trong Logs. Với ChatGPT bình thường chỉ dùng public /mcp URL + OAuth.",
                    Foreground = _palette.TextSecondary,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11.5
                }
            });
            AddGridCard(grid, CreateSectionCard("Kết nối hiện tại", "Public MCP URL luôn lấy từ canonical endpoint resolver.", right), 1);
            return grid;
        }

        private UIElement CreateAgentControlPage()
        {
            var grid = CreateTwoColumnGrid();
            var danger = new StackPanel();
            _desktopConsentText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.TextPrimary,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            danger.Children.Add(_desktopConsentText);
            danger.Children.Add(new TextBlock
            {
                Text = "Desktop-wide input mặc định OFF sau mỗi lần mở BricsCAD. ChatGPT không có tool để tự bật. Khi chạy sẽ có viền xanh; Esc ×2 trong 1.2 giây dừng ngay.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.TextSecondary,
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 10)
            });
            danger.Children.Add(CreateActionButton("Bật quyền desktop", (_, __) => ToggleDesktopConsent(), ActionKind.Primary));
            danger.Children.Add(CreateActionButton("EMERGENCY STOP AGENT", (_, __) => EmergencyStop(), ActionKind.Danger));
            danger.Children.Add(CreateActionButton("Hủy command BricsCAD hiện tại · ESC x2", (_, __) => InvokeControlTool("cad_cancel_command", "{}"), ActionKind.Secondary));
            AddGridCard(grid, CreateSectionCard("Desktop control & khẩn cấp", "Local consent + MCP mutation epoch là hai lớp độc lập. Emergency Stop/ESC vẫn khả dụng khi self-test đang chạy.", danger, true), 0);

            var recovery = new StackPanel();
            recovery.Children.Add(CreateActionButton("Resume Agent", (_, __) => InvokeControlTool("cad_agent_resume", "{\"confirmMutation\":true}"), ActionKind.Primary));
            recovery.Children.Add(CreateActionButton("Mở thư mục audit MCP", (_, __) => OpenAuditFolder(), ActionKind.Secondary));
            recovery.Children.Add(new TextBlock
            {
                Text = "Đang làm: " + (string.IsNullOrWhiteSpace(McpAgentExperience.CurrentAction) ? "không có action local đang chạy" : McpAgentExperience.CurrentAction)
                       + Environment.NewLine + "Bước tiếp: " + (string.IsNullOrWhiteSpace(McpAgentExperience.NextStep) ? "theo trạng thái onboarding hiện tại" : McpAgentExperience.NextStep),
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.TextSecondary,
                FontSize = 11.5,
                Margin = new Thickness(0, 8, 0, 0)
            });
            AddGridCard(grid, CreateSectionCard("Phục hồi & Audit", "Khôi phục CAD Agent, theo dõi local execution state và mở audit khi cần kiểm tra.", recovery), 1);
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
                       + "Policy: giữ SAVETIME ngắn hơn; nếu disabled/>5 phút thì dùng 5 phút; bật ISAVEBAK; tối đa 30 snapshot ổn định/drawing.",
                Foreground = _palette.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11.5,
                Margin = new Thickness(0, 8, 0, 0)
            });
            AddGridCard(grid, CreateSectionCard("Recovery state", "Versioned recovery chạy bên cạnh native BricsCAD autosave/BAK.", status), 1);
            return grid;
        }

        private UIElement CreateLogsPage()
        {
            var grid = CreateTwoColumnGrid();
            _logsHost = new StackPanel();
            RenderActivityHistory();
            AddGridCard(grid, CreateSectionCard("Logs", "Lịch sử UI + local MCP event gần nhất trong phiên. MCP mutation audit đầy đủ vẫn ở audit log riêng.", _logsHost), 0);

            var advanced = new StackPanel();
            advanced.Children.Add(CreateActionButton("Copy Bearer Token · engineering compatibility", (_, __) => CopyToken(), ActionKind.Secondary));
            advanced.Children.Add(CreateActionButton("Copy URL + Authorization · engineering compatibility", (_, __) => CopyConfig(), ActionKind.Secondary));
            advanced.Children.Add(CreateActionButton("Mở thư mục audit MCP", (_, __) => OpenAuditFolder(), ActionKind.Secondary));
            advanced.Children.Add(new TextBlock
            {
                Text = "Nâng cao: static bearer chỉ dùng debug/backward compatibility. Production ChatGPT dùng Named Tunnel + OAuth/DCR. Secret không được đưa vào UI log/audit.",
                Foreground = _palette.TextSecondary,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11.5,
                Margin = new Thickness(0, 8, 0, 0)
            });
            AddGridCard(grid, CreateSectionCard("Nâng cao", "Engineering compatibility và chẩn đoán.", advanced), 1);
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

            var focus = new Trigger { Property = Button.IsKeyboardFocusedProperty, Value = true };
            focus.Setters.Add(new Setter(Control.BorderBrushProperty, _palette.FocusBorder));
            focus.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2)));
            style.Triggers.Add(focus);

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

        private UIElement CreateStatusRow(string label, string value, Brush valueBrush = null)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(122) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 11.5,
                Foreground = _palette.TextMuted,
                VerticalAlignment = VerticalAlignment.Top
            };
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
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };
            var refresh = CreateActionButton("Refresh", (_, __) =>
            {
                RefreshStatus();
                ShowToast(ToastKind.Info, "Trạng thái", "Đã làm mới trạng thái MCP, tunnel, consent và recovery.");
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
            while (_activityEntries.Count > MaxActivityEntries)
                _activityEntries.RemoveAt(_activityEntries.Count - 1);
            if (_selectedTab == 5 && _logsHost != null) RenderActivityHistory();
        }

        private void RenderActivityHistory()
        {
            if (_logsHost == null) return;
            _logsHost.Children.Clear();

            var localEvents = McpAgentExperience.Recent(12);
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
                    _logsHost.Children.Add(new TextBlock
                    {
                        Text = item.Utc.ToLocalTime().ToString("HH:mm:ss") + "  [" + item.Level + "/" + item.Category + "] " + item.Message,
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
                Brush accent;
                Brush surface;
                Brush border;
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

            Brush accent;
            Brush surface;
            Brush borderBrush;
            GetToastColors(kind, out accent, out surface, out borderBrush);
            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = accent,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12.5
            });
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
                var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
                {
                    Interval = GetToastLifetime(kind)
                };
                visual.Timer = timer;
                timer.Tick += (_, __) => DismissToast(visual);
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
                visual.Timer = null;
            }
            if (_toastHost != null) _toastHost.Children.Remove(visual.Card);
            _visibleToasts.Remove(visual);
        }

        private void ClearVisibleToasts()
        {
            foreach (var visual in new List<ToastVisual>(_visibleToasts))
                if (visual.Timer != null) visual.Timer.Stop();
            _visibleToasts.Clear();
            if (_toastHost != null) _toastHost.Children.Clear();
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
                case ToastKind.Success:
                    accent = _palette.Success;
                    surface = _palette.SuccessSoft;
                    border = _palette.SuccessBorder;
                    return;
                case ToastKind.Warning:
                    accent = _palette.Warning;
                    surface = _palette.WarningSoft;
                    border = _palette.WarningBorder;
                    return;
                case ToastKind.Error:
                    accent = _palette.Danger;
                    surface = _palette.DangerSoft;
                    border = _palette.DangerBorder;
                    return;
                default:
                    accent = _palette.Accent;
                    surface = _palette.SelectedBackground;
                    border = _palette.Accent;
                    return;
            }
        }

        private void InstallCloudflared(object sender, RoutedEventArgs args)
        {
            McpAgentExperience.ActionStarted("onboarding", "Đang cài/cập nhật cloudflared...", "Chờ kiểm tra Authenticode hoàn tất.");
            ShowToast(ToastKind.Info, "Cloudflare Tunnel", "Đang tải cloudflared chính thức và kiểm tra Authenticode...");
            McpCloudflaredBootstrapper.BeginInstall((ok, message) => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ok) McpAgentExperience.Success("onboarding", message, "Đăng nhập Cloudflare bằng browser.");
                else McpAgentExperience.Error("onboarding", message, "Kiểm tra mạng/chứng thư rồi thử lại.");
                ShowToast(ok ? ToastKind.Success : ToastKind.Error, ok ? "Cloudflare Tunnel" : "Cài Cloudflare thất bại", message);
                RefreshStatus();
            })));
        }

        private void OpenAccountSetup()
        {
            try
            {
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
            string error;
            if (!McpCloudflareAccountTunnelManager.StartQuickTunnel(out error))
            {
                StopQuickUrlPolling();
                ShowToast(ToastKind.Error, "Quick Tunnel", error);
                RefreshStatus();
                return;
            }
            McpAgentExperience.Warning("onboarding", "Quick Tunnel đang chạy để test.", "Dùng Named Tunnel ổn định cho production.");
            ShowToast(ToastKind.Info, "Quick Tunnel", "Đang khởi động và chờ public URL...");
            StartQuickUrlPolling();
            RefreshStatus();
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

        private void QuickUrlTimerOnTick(object sender, EventArgs e)
        {
            _quickUrlPollTicks++;
            RefreshStatus();
            var publicUrl = McpCloudflareTunnelManager.PublicMcpUrl;
            if (!string.IsNullOrWhiteSpace(publicUrl))
            {
                ShowToast(ToastKind.Success, "Quick Tunnel", "Public URL sẵn sàng: " + publicUrl);
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
            McpCloudflareAccountTunnelManager.StopForHostShutdown();
            McpCloudflareTunnelManager.StopForHostShutdown();
            McpAgentExperience.Warning("onboarding", "Đã dừng tunnel trong phiên này.", "Khởi động Named Tunnel khi cần kết nối lại.");
            ShowToast(ToastKind.Success, "Cloudflare", "Đã dừng tunnel của QS3D trong phiên BricsCAD này.");
            RefreshStatus();
        }

        private void OpenChatGpt()
        {
            try
            {
                McpCloudflareAccountTunnelManager.OpenChatGpt();
                McpAgentExperience.Info("onboarding", "Đã mở ChatGPT trong browser hệ thống.", string.Empty,
                    "Thêm public MCP URL bằng OAuth/DCR rồi đánh dấu đã thêm MCP.");
                ShowToast(ToastKind.Success, "ChatGPT", "Đã mở ChatGPT trong browser. Dùng URL + OAuth trên basic connector screen.");
            }
            catch (Exception ex)
            {
                ShowToast(ToastKind.Error, "ChatGPT", ex.Message);
            }
        }

        private void MarkChatGptRegistered()
        {
            try
            {
                McpAgentExperience.MarkChatGptRegistrationAcknowledged();
                ShowToast(ToastKind.Success, "ChatGPT Connector", "Đã ghi nhận MCP URL hiện tại được thêm trong ChatGPT. Chạy protocol/read-only self-test để xác minh local endpoint.");
            }
            catch (Exception ex)
            {
                ShowToast(ToastKind.Error, "ChatGPT Connector", ex.Message);
            }
            RefreshStatus();
        }

        private void CopyUrl()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowToast(ToastKind.Warning, "MCP URL", "Chưa có public MCP URL. Hãy tạo Named Tunnel trước.");
                return;
            }
            try
            {
                Clipboard.SetText(url);
                ShowToast(ToastKind.Success, "MCP URL", "Đã copy public MCP URL.");
            }
            catch (Exception ex)
            {
                ShowToast(ToastKind.Error, "Clipboard", ex.Message);
            }
        }

        private void CopyToken()
        {
            try
            {
                McpEmbeddedServer.EnsureStarted();
                Clipboard.SetText(McpEmbeddedServer.GetBearerToken());
                ShowToast(ToastKind.Warning, "Bearer Token", "Đã copy engineering bearer. Không chia sẻ token này công khai; ChatGPT production dùng OAuth.");
            }
            catch (Exception ex)
            {
                ShowToast(ToastKind.Error, "Bearer Token", ex.Message);
            }
        }

        private void CopyConfig()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowToast(ToastKind.Warning, "Engineering config", "Chưa có public MCP URL.");
                return;
            }
            try
            {
                Clipboard.SetText("MCP URL: " + url + Environment.NewLine
                                  + "Authorization: Bearer " + McpEmbeddedServer.GetBearerToken());
                ShowToast(ToastKind.Warning, "Engineering config", "Đã copy URL + Authorization cho compatibility/debug. Secret không được ghi vào Logs.");
            }
            catch (Exception ex)
            {
                ShowToast(ToastKind.Error, "Engineering config", ex.Message);
            }
        }

        private void ToggleDesktopConsent()
        {
            try
            {
                if (McpDesktopControlSession.IsEnabled)
                {
                    McpDesktopControlSession.DisableFromLocalUser("User tắt desktop control từ Agent Center.");
                    ShowToast(ToastKind.Warning, "Desktop control", "Đã tắt quyền desktop và emergency-stop mutation epoch.");
                }
                else
                {
                    McpDesktopControlSession.EnableFromLocalUser();
                    ShowToast(ToastKind.Success, "Desktop control", "Đã bật quyền desktop cho phiên BricsCAD hiện tại. Esc ×2 để dừng ngay.");
                }
            }
            catch (Exception ex)
            {
                ShowToast(ToastKind.Error, "Desktop control", ex.Message);
            }
            RefreshStatus();
        }

        private void EmergencyStop()
        {
            try { McpDesktopControlSession.DisableFromLocalUser("User bấm EMERGENCY STOP AGENT trong Agent Center."); } catch { }
            InvokeControlTool("cad_agent_stop", "{}");
            ShowToast(ToastKind.Warning, "Emergency Stop", "Đã yêu cầu dừng Agent và thu hồi desktop consent.", true);
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
            string path;
            string message;
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
                    ShowToast(ToastKind.Warning, "MCP local check", "Một MCP local check khác đang chạy; Emergency Stop/ESC vẫn luôn khả dụng.");
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
            catch (Exception ex)
            {
                ShowToast(ToastKind.Error, title, ex.Message);
            }
        }

        private void RefreshStatus()
        {
            var publicUrl = McpPublicEndpointResolver.Resolve();
            var mcpRunning = McpEmbeddedServer.IsRunning;
            var namedTunnelRunning = McpCloudflareAccountTunnelManager.IsRunning;
            var quickTunnelRunning = McpCloudflareTunnelManager.IsRunning;
            var tunnelRunning = namedTunnelRunning || quickTunnelRunning;
            var cloudflaredInstalled = !string.IsNullOrWhiteSpace(McpCloudflareAccountTunnelManager.CloudflaredPath);
            var authenticated = McpCloudflareAccountTunnelManager.IsAuthenticated;
            var desktopConsent = McpDesktopControlSession.IsEnabled;
            var onboarding = McpAgentExperience.DetermineOnboarding();

            _statusChips.Children.Clear();
            _statusChips.Children.Add(CreateStatusChip(mcpRunning ? "MCP online" : "MCP offline", mcpRunning));
            _statusChips.Children.Add(CreateStatusChip(tunnelRunning ? "Tunnel online" : "Tunnel offline", tunnelRunning));
            _statusChips.Children.Add(CreateStatusChip(string.IsNullOrWhiteSpace(publicUrl) ? "Public URL chưa có" : "Public URL sẵn sàng", !string.IsNullOrWhiteSpace(publicUrl)));
            _statusChips.Children.Add(CreateStatusChip(desktopConsent ? "Desktop ON" : "Desktop OFF", desktopConsent));

            _statusRows.Children.Clear();
            _statusRows.Children.Add(CreateStatusRow("MCP embedded", mcpRunning ? "RUNNING" : "STOPPED", mcpRunning ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Local endpoint", McpEmbeddedServer.Endpoint.ToString()));
            _statusRows.Children.Add(CreateStatusRow("Cloudflare", cloudflaredInstalled ? "Đã cài" : "Chưa cài", cloudflaredInstalled ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Browser login", authenticated ? "Đã đăng nhập" : "Chưa đăng nhập", authenticated ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Named Tunnel", namedTunnelRunning ? "RUNNING" : "STOPPED", namedTunnelRunning ? _palette.Success : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Quick Tunnel", quickTunnelRunning ? "RUNNING / test only" : "STOPPED", quickTunnelRunning ? _palette.Warning : _palette.TextMuted));
            _statusRows.Children.Add(CreateStatusRow("Public MCP", string.IsNullOrWhiteSpace(publicUrl) ? "Chưa có public URL" : publicUrl));
            _statusRows.Children.Add(CreateStatusRow("Onboarding", onboarding.Title));
            _statusRows.Children.Add(CreateStatusRow("Agent", McpEmbeddedServer.Describe()));

            if (_desktopConsentText != null)
            {
                _desktopConsentText.Text = desktopConsent
                    ? "● Desktop control: ON · chỉ phiên local hiện tại"
                    : "○ Desktop control: OFF · an toàn mặc định";
                _desktopConsentText.Foreground = desktopConsent ? _palette.Success : _palette.TextMuted;
            }
        }

        private static ThemePalette CreateLightPalette()
        {
            return new ThemePalette
            {
                WindowBackground = CreateBrush(0xF4, 0xF7, 0xFB),
                CardBackground = CreateBrush(0xFF, 0xFF, 0xFF),
                SubtleBackground = CreateBrush(0xF2, 0xF4, 0xF7),
                SelectedBackground = CreateBrush(0xEA, 0xF2, 0xFF),
                Border = CreateBrush(0xDC, 0xE4, 0xEF),
                StrongBorder = CreateBrush(0xA9, 0xB5, 0xC7),
                TextPrimary = CreateBrush(0x17, 0x20, 0x33),
                TextSecondary = CreateBrush(0x4B, 0x57, 0x6B),
                TextMuted = CreateBrush(0x66, 0x70, 0x85),
                Accent = CreateBrush(0x25, 0x63, 0xEB),
                AccentHover = CreateBrush(0x1D, 0x4E, 0xD8),
                AccentPressed = CreateBrush(0x1E, 0x40, 0xAF),
                AccentText = CreateBrush(0xFF, 0xFF, 0xFF),
                Success = CreateBrush(0x15, 0x80, 0x3D),
                SuccessSoft = CreateBrush(0xE9, 0xF8, 0xEF),
                SuccessBorder = CreateBrush(0xAB, 0xEF, 0xC6),
                Warning = CreateBrush(0x9A, 0x67, 0x00),
                WarningSoft = CreateBrush(0xFF, 0xF8, 0xE1),
                WarningBorder = CreateBrush(0xF0, 0xC3, 0x6B),
                Danger = CreateBrush(0xB4, 0x23, 0x18),
                DangerSoft = CreateBrush(0xFD, 0xEC, 0xEC),
                DangerHover = CreateBrush(0xB4, 0x23, 0x18),
                DangerPressed = CreateBrush(0x91, 0x1D, 0x14),
                DangerBorder = CreateBrush(0xFD, 0xB0, 0xA8),
                DangerStrongText = CreateBrush(0xFF, 0xFF, 0xFF),
                DisabledBackground = CreateBrush(0xEA, 0xEC, 0xF0),
                DisabledForeground = CreateBrush(0x98, 0xA2, 0xB3),
                DisabledBorder = CreateBrush(0xD0, 0xD5, 0xDD),
                FocusBorder = CreateBrush(0x15, 0x5E, 0xD8)
            };
        }

        private static ThemePalette CreateDarkPalette()
        {
            return new ThemePalette
            {
                WindowBackground = CreateBrush(0x0F, 0x14, 0x1F),
                CardBackground = CreateBrush(0x18, 0x20, 0x2E),
                SubtleBackground = CreateBrush(0x22, 0x2C, 0x3B),
                SelectedBackground = CreateBrush(0x1D, 0x35, 0x5F),
                Border = CreateBrush(0x34, 0x40, 0x52),
                StrongBorder = CreateBrush(0x5A, 0x69, 0x7F),
                TextPrimary = CreateBrush(0xF2, 0xF4, 0xF7),
                TextSecondary = CreateBrush(0xC1, 0xC9, 0xD6),
                TextMuted = CreateBrush(0x98, 0xA2, 0xB3),
                Accent = CreateBrush(0x6E, 0x9C, 0xFF),
                AccentHover = CreateBrush(0x86, 0xAC, 0xFF),
                AccentPressed = CreateBrush(0x4F, 0x7D, 0xE8),
                AccentText = CreateBrush(0x08, 0x12, 0x24),
                Success = CreateBrush(0x75, 0xE0, 0xA1),
                SuccessSoft = CreateBrush(0x14, 0x37, 0x28),
                SuccessBorder = CreateBrush(0x2B, 0x6E, 0x4A),
                Warning = CreateBrush(0xF6, 0xD3, 0x70),
                WarningSoft = CreateBrush(0x3C, 0x30, 0x12),
                WarningBorder = CreateBrush(0x78, 0x61, 0x24),
                Danger = CreateBrush(0xFF, 0x9D, 0x96),
                DangerSoft = CreateBrush(0x3B, 0x1E, 0x20),
                DangerHover = CreateBrush(0xC9, 0x43, 0x3A),
                DangerPressed = CreateBrush(0x9E, 0x2F, 0x28),
                DangerBorder = CreateBrush(0x7B, 0x36, 0x37),
                DangerStrongText = CreateBrush(0xFF, 0xFF, 0xFF),
                DisabledBackground = CreateBrush(0x20, 0x27, 0x33),
                DisabledForeground = CreateBrush(0x69, 0x74, 0x86),
                DisabledBorder = CreateBrush(0x31, 0x3A, 0x49),
                FocusBorder = CreateBrush(0x8B, 0xB1, 0xFF)
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
