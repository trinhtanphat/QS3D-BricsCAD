using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using Microsoft.Win32;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
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

    /// <summary>
    /// Guided MCP operations center. Primary onboarding is browser-owned Cloudflare login +
    /// stable Named Tunnel + OAuth/DCR registration in ChatGPT. Engineering bearer and Quick
    /// Tunnel compatibility controls are deliberately kept under Nâng cao.
    /// </summary>
    internal sealed class McpAgentControlCenterWindow : Window
    {
        private readonly McpUiPalette _palette = McpUiPalette.ForSystem();
        private readonly TextBlock _phaseTitle = new TextBlock();
        private readonly TextBlock _phaseDetail = new TextBlock();
        private readonly TextBlock _nextStep = new TextBlock();
        private readonly TextBlock _currentAction = new TextBlock();
        private readonly TextBlock _desktopState = new TextBlock();
        private readonly TextBlock _recoveryState = new TextBlock();
        private readonly TextBlock _eventTimeline = new TextBlock();
        private readonly TextBlock _advancedStatus = new TextBlock();
        private readonly TextBlock _publicUrl = new TextBlock();
        private readonly Button _desktopEnableButton;
        private readonly DispatcherTimer _refreshTimer;
        private int _localOperationActive;

        public McpAgentControlCenterWindow()
        {
            Title = "QS3D · ChatGPT MCP Control Center";
            Width = 980;
            Height = 760;
            MinWidth = 800;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = _palette.Window;
            Foreground = _palette.Text;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;

            _desktopEnableButton = ActionButton("Bật quyền desktop", (_, __) => ToggleDesktopConsent(), ActionKind.Primary);
            Content = BuildShell();

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += (_, __) => RefreshView();
            Loaded += (_, __) =>
            {
                RefreshView();
                _refreshTimer.Start();
            };
            Closed += (_, __) => _refreshTimer.Stop();
        }

        private UIElement BuildShell()
        {
            var root = new DockPanel { LastChildFill = true };
            var header = BuildHeader();
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            var footer = BuildFooter();
            DockPanel.SetDock(footer, Dock.Bottom);
            root.Children.Add(footer);

            var tabs = new TabControl
            {
                Margin = new Thickness(18, 8, 18, 10),
                Background = _palette.Window,
                BorderBrush = _palette.Border,
                Foreground = _palette.Text
            };
            tabs.Items.Add(Tab("Kết nối", BuildConnectionTab()));
            tabs.Items.Add(Tab("Agent", BuildAgentTab()));
            tabs.Items.Add(Tab("Backup & khôi phục", BuildRecoveryTab()));
            tabs.Items.Add(Tab("Nâng cao", BuildAdvancedTab()));
            root.Children.Add(tabs);
            return root;
        }

        private UIElement BuildHeader()
        {
            var panel = new StackPanel { Margin = new Thickness(22, 18, 22, 8) };
            panel.Children.Add(new TextBlock
            {
                Text = "QS3D · ChatGPT MCP Control Center",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = _palette.Text
            });
            panel.Children.Add(new TextBlock
            {
                Text = "ChatGPT ↔ OAuth MCP ↔ QS3D ↔ BricsCAD",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = _palette.Primary,
                Margin = new Thickness(0, 4, 0, 0)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Cloudflare và ChatGPT đăng nhập bằng browser hệ thống. QS3D không nhận mật khẩu, không scrape cookie và không cần terminal cho luồng chuẩn.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.Muted,
                FontSize = 12.5,
                Margin = new Thickness(0, 7, 0, 0)
            });
            return panel;
        }

        private UIElement BuildConnectionTab()
        {
            var root = TabStack();
            root.Children.Add(Card("Bước hiện tại", StatusPanel()));

            var actions = new StackPanel();
            actions.Children.Add(ActionButton("1 · Khởi động embedded MCP", (_, __) => StartEmbeddedMcp(), ActionKind.Secondary));
            actions.Children.Add(ActionButton("2 · Cài / cập nhật Cloudflare Tunnel", InstallCloudflared, ActionKind.Secondary));
            actions.Children.Add(ActionButton("3 · Đăng nhập Cloudflare", (_, __) => OpenAccountSetup(), ActionKind.Secondary));
            actions.Children.Add(ActionButton("4 · Tạo / sửa Named Tunnel", (_, __) => OpenAccountSetup(), ActionKind.Secondary));
            actions.Children.Add(ActionButton("5 · Khởi động Named Tunnel", (_, __) => StartNamedTunnel(), ActionKind.Secondary));
            actions.Children.Add(ActionButton("6 · Copy MCP URL", (_, __) => CopyPublicUrl(), ActionKind.Secondary));
            actions.Children.Add(ActionButton("7 · Mở ChatGPT", (_, __) => OpenChatGpt(), ActionKind.Primary));
            actions.Children.Add(ActionButton("8 · Đã thêm MCP trong ChatGPT", (_, __) => MarkChatGptRegistered(), ActionKind.Secondary));
            actions.Children.Add(ActionButton("9 · Kiểm tra MCP protocol", (_, __) => CheckProtocol(), ActionKind.Secondary));

            root.Children.Add(Card("Kết nối lần đầu",
                "Luồng khuyến nghị: embedded MCP → cloudflared → Cloudflare provider-browser login → Named Tunnel HTTPS ổn định → thêm MCP URL vào ChatGPT qua OAuth/DCR → protocol check.",
                actions));

            _publicUrl.TextWrapping = TextWrapping.Wrap;
            _publicUrl.FontFamily = new FontFamily("Consolas");
            _publicUrl.Foreground = _palette.Text;
            root.Children.Add(Card("Public MCP URL", _publicUrl));
            return Scroll(root);
        }

        private UIElement BuildAgentTab()
        {
            var root = TabStack();
            var consent = new StackPanel();
            consent.Children.Add(_desktopState);
            consent.Children.Add(new TextBlock
            {
                Text = "Desktop-wide mouse/keyboard/clipboard/screenshot mặc định OFF sau mỗi lần mở BricsCAD. ChatGPT không có tool để tự bật quyền này. Khi MCP thao tác sẽ xuất hiện viền xanh; Esc ×2 trong 1.2 giây dừng ngay.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = _palette.Muted,
                Margin = new Thickness(0, 6, 0, 10)
            });
            consent.Children.Add(_desktopEnableButton);
            consent.Children.Add(ActionButton("EMERGENCY STOP AGENT", (_, __) => EmergencyStop(), ActionKind.Danger));
            consent.Children.Add(ActionButton("Hủy command BricsCAD · ESC x2", (_, __) => InvokeControlTool("cad_cancel_command", "{}"), ActionKind.Secondary));
            consent.Children.Add(ActionButton("Resume CAD Agent", (_, __) => InvokeControlTool("cad_agent_resume", "{\"confirmMutation\":true}"), ActionKind.Secondary));
            root.Children.Add(Card("Quyền điều khiển desktop", consent));

            var live = new StackPanel();
            live.Children.Add(LabelValue("Đang làm", _currentAction));
            live.Children.Add(LabelValue("Bước tiếp", _nextStep));
            root.Children.Add(Card("Trạng thái thực thi local", live));

            _eventTimeline.TextWrapping = TextWrapping.Wrap;
            _eventTimeline.FontFamily = new FontFamily("Consolas");
            _eventTimeline.FontSize = 11.5;
            root.Children.Add(Card("Hoạt động gần đây", _eventTimeline));
            return Scroll(root);
        }

        private UIElement BuildRecoveryTab()
        {
            var root = TabStack();
            _recoveryState.TextWrapping = TextWrapping.Wrap;
            root.Children.Add(Card("Autosave + versioned recovery", _recoveryState));

            var actions = new StackPanel();
            actions.Children.Add(ActionButton("Backup ngay", (_, __) => BackupNow(), ActionKind.Primary));
            actions.Children.Add(ActionButton("Khôi phục snapshot mới nhất thành file mới", (_, __) => RecoverLatest(), ActionKind.Secondary));
            actions.Children.Add(ActionButton("Mở thư mục backup QS3D", (_, __) => OpenFolder(McpProjectRecoveryService.BackupRoot), ActionKind.Secondary));
            root.Children.Add(Card("Backup & khôi phục",
                "QS3D giữ SAVETIME tối đa 5 phút (không tăng setting ngắn hơn), bật ISAVEBAK và tạo tối đa 30 recovery copy ổn định mỗi drawing. Restore luôn tạo file Recovered mới, không tự ghi đè DWG gốc.",
                actions));
            return Scroll(root);
        }

        private UIElement BuildAdvancedTab()
        {
            var root = TabStack();
            var test = new StackPanel();
            test.Children.Add(ActionButton("Quick Tunnel · test only", (_, __) => StartQuickTunnel(), ActionKind.Secondary));
            test.Children.Add(ActionButton("Dừng tunnel trong phiên này", (_, __) => StopTunnels(), ActionKind.Secondary));
            test.Children.Add(ActionButton("Tự kiểm tra Agent · read-only", (_, __) => RunReadOnlySelfTest(), ActionKind.Secondary));
            test.Children.Add(ActionButton("Copy Bearer Token · engineering compatibility", (_, __) => CopyBearer(), ActionKind.Secondary));
            test.Children.Add(ActionButton("Copy URL + Authorization · engineering compatibility", (_, __) => CopyEngineeringConfig(), ActionKind.Secondary));
            test.Children.Add(ActionButton("Mở thư mục audit MCP", (_, __) => OpenAuditFolder(), ActionKind.Secondary));
            root.Children.Add(Card("Nâng cao",
                "Quick Tunnel và static bearer chỉ là fallback/test. Luồng production là Named Tunnel + OAuth/DCR. Không dùng phần này để thay thế browser-owned authentication.",
                test));

            _advancedStatus.TextWrapping = TextWrapping.Wrap;
            _advancedStatus.FontFamily = new FontFamily("Consolas");
            _advancedStatus.FontSize = 11.5;
            root.Children.Add(Card("Chẩn đoán", _advancedStatus));
            return Scroll(root);
        }

        private StackPanel StatusPanel()
        {
            var panel = new StackPanel();
            _phaseTitle.FontSize = 17;
            _phaseTitle.FontWeight = FontWeights.Bold;
            _phaseTitle.Foreground = _palette.Text;
            _phaseDetail.TextWrapping = TextWrapping.Wrap;
            _phaseDetail.Foreground = _palette.Muted;
            _phaseDetail.Margin = new Thickness(0, 6, 0, 6);
            _nextStep.TextWrapping = TextWrapping.Wrap;
            _nextStep.Foreground = _palette.Primary;
            _nextStep.FontWeight = FontWeights.SemiBold;
            panel.Children.Add(_phaseTitle);
            panel.Children.Add(_phaseDetail);
            panel.Children.Add(_nextStep);
            return panel;
        }

        private UIElement BuildFooter()
        {
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(18, 0, 18, 14)
            };
            var refresh = ActionButton("Refresh", (_, __) => RefreshView(), ActionKind.Secondary);
            refresh.MinWidth = 96;
            var close = ActionButton("Đóng", (_, __) => Close(), ActionKind.Secondary);
            close.MinWidth = 96;
            footer.Children.Add(refresh);
            footer.Children.Add(close);
            return footer;
        }

        private TabItem Tab(string title, UIElement content)
        {
            return new TabItem
            {
                Header = title,
                Content = content,
                Foreground = _palette.Text,
                Background = _palette.Panel,
                BorderBrush = _palette.Border,
                Padding = new Thickness(12, 7, 12, 7)
            };
        }

        private StackPanel TabStack()
        {
            return new StackPanel { Margin = new Thickness(8, 10, 8, 12) };
        }

        private UIElement Scroll(UIElement child)
        {
            return new ScrollViewer
            {
                Content = child,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
        }

        private Border Card(string title, UIElement body)
        {
            return Card(title, null, body);
        }

        private Border Card(string title, string description, UIElement body)
        {
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = _palette.Text
            });
            if (!string.IsNullOrWhiteSpace(description))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = description,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = _palette.Muted,
                    FontSize = 12,
                    LineHeight = 18,
                    Margin = new Thickness(0, 5, 0, 12)
                });
            }
            else if (body != null)
            {
                stack.Children.Add(new Border { Height = 9, Background = Brushes.Transparent });
            }
            stack.Children.Add(body);
            return new Border
            {
                Background = _palette.Panel,
                BorderBrush = _palette.Border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 10),
                Child = stack
            };
        }

        private UIElement LabelValue(string label, TextBlock value)
        {
            value.TextWrapping = TextWrapping.Wrap;
            value.Foreground = _palette.Text;
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var caption = new TextBlock { Text = label, Foreground = _palette.Muted };
            Grid.SetColumn(value, 1);
            grid.Children.Add(caption);
            grid.Children.Add(value);
            return grid;
        }

        private Button ActionButton(string text, RoutedEventHandler handler, ActionKind kind)
        {
            var button = new Button
            {
                Content = text,
                MinHeight = kind == ActionKind.Danger ? 44 : 38,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 7, 12, 7),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            ApplyButtonStyle(button, kind);
            button.Click += handler;
            return button;
        }

        private void ApplyButtonStyle(Button button, ActionKind kind)
        {
            Brush normal;
            Brush hover;
            Brush pressed;
            Brush foreground;
            Brush border;

            if (kind == ActionKind.Primary)
            {
                normal = _palette.Primary;
                hover = _palette.PrimaryHover;
                pressed = _palette.PrimaryPressed;
                foreground = Brushes.White;
                border = _palette.Primary;
            }
            else if (kind == ActionKind.Danger)
            {
                normal = _palette.DangerSoft;
                hover = _palette.DangerHover;
                pressed = _palette.DangerPressed;
                foreground = _palette.DangerText;
                border = _palette.DangerBorder;
            }
            else
            {
                normal = _palette.Button;
                hover = _palette.ButtonHover;
                pressed = _palette.ButtonPressed;
                foreground = _palette.Text;
                border = _palette.Border;
            }

            button.Background = normal;
            button.Foreground = foreground;
            button.BorderBrush = border;
            button.MouseEnter += (_, __) => button.Background = hover;
            button.MouseLeave += (_, __) => button.Background = normal;
            button.PreviewMouseLeftButtonDown += (_, __) => button.Background = pressed;
            button.PreviewMouseLeftButtonUp += (_, __) => button.Background = button.IsMouseOver ? hover : normal;
        }

        private void RefreshView()
        {
            try
            {
                var snapshot = McpAgentExperience.DetermineOnboarding();
                _phaseTitle.Text = snapshot.Title;
                _phaseDetail.Text = snapshot.Detail;
                _nextStep.Text = "Tiếp theo: " + snapshot.NextStep;
                _publicUrl.Text = string.IsNullOrWhiteSpace(snapshot.PublicUrl) ? "Chưa có public URL." : snapshot.PublicUrl;

                var consent = McpDesktopControlSession.IsEnabled;
                _desktopState.Text = consent
                    ? "● Desktop control: ON · phiên local hiện tại"
                    : "○ Desktop control: OFF · an toàn mặc định";
                _desktopState.Foreground = consent ? _palette.Success : _palette.Muted;
                _desktopState.FontWeight = FontWeights.SemiBold;
                _desktopEnableButton.Content = consent ? "Tắt quyền desktop" : "Bật quyền desktop";

                _currentAction.Text = string.IsNullOrWhiteSpace(McpAgentExperience.CurrentAction)
                    ? "Không có action đang chạy."
                    : McpAgentExperience.CurrentAction;
                _nextStep.Text = "Tiếp theo: " + (string.IsNullOrWhiteSpace(McpAgentExperience.NextStep)
                    ? snapshot.NextStep
                    : McpAgentExperience.NextStep);

                _recoveryState.Text = McpProjectRecoveryService.Describe()
                    + Environment.NewLine + "Backup root: " + McpProjectRecoveryService.BackupRoot;
                _eventTimeline.Text = BuildEventTimeline();
                _advancedStatus.Text = McpEmbeddedServer.Describe()
                    + Environment.NewLine + McpCloudflareAccountTunnelManager.Describe()
                    + Environment.NewLine + "desktopConsent=" + consent;
            }
            catch (Exception ex)
            {
                _advancedStatus.Text = "Refresh lỗi: " + ex.Message;
            }
        }

        private string BuildEventTimeline()
        {
            var events = McpAgentExperience.Recent(12);
            if (events.Length == 0) return "Chưa có event local.";
            var lines = new string[events.Length];
            for (var i = 0; i < events.Length; i++)
            {
                var item = events[i];
                lines[i] = item.Utc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                           + "  [" + item.Level + "/" + item.Category + "] " + item.Message;
            }
            return string.Join(Environment.NewLine, lines);
        }

        private void StartEmbeddedMcp()
        {
            try
            {
                McpEmbeddedServer.EnsureStarted();
                McpAgentExperience.Success("onboarding", "Embedded MCP đã chạy.", "Tiếp tục Cloudflare/ChatGPT onboarding.");
            }
            catch (Exception ex) { ShowError("Không khởi động được MCP", ex); }
            RefreshView();
        }

        private void InstallCloudflared(object sender, RoutedEventArgs args)
        {
            McpAgentExperience.ActionStarted("onboarding", "Đang cài/cập nhật cloudflared...", "Chờ kiểm tra Authenticode hoàn tất.");
            McpCloudflaredBootstrapper.BeginInstall((ok, message) => Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ok) McpAgentExperience.Success("onboarding", message, "Đăng nhập Cloudflare bằng browser.");
                else McpAgentExperience.Error("onboarding", message, "Kiểm tra mạng/chứng thư rồi thử lại.");
                RefreshView();
                if (!ok) MessageBox.Show(message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                McpAgentExperience.Info("onboarding", "Mở Cloudflare setup. Credential/password chỉ nhập trên browser do Cloudflare sở hữu.", string.Empty,
                    "Hoàn tất provider-browser login và Named Tunnel trong cửa sổ setup.");
                new McpCloudflareAccountSetupWindow().ShowDialog();
            }
            catch (Exception ex) { ShowError("Cloudflare setup lỗi", ex); }
            RefreshView();
        }

        private void StartNamedTunnel()
        {
            try
            {
                string error;
                if (!McpCloudflareAccountTunnelManager.StartSaved(out error))
                    throw new InvalidOperationException(error);
                McpAgentExperience.Success("onboarding", "Named Tunnel đang chạy.", "Copy MCP URL và mở ChatGPT.");
            }
            catch (Exception ex) { ShowError("Không khởi động được Named Tunnel", ex); }
            RefreshView();
        }

        private void OpenChatGpt()
        {
            try
            {
                McpCloudflareAccountTunnelManager.OpenChatGpt();
                McpAgentExperience.Info("onboarding", "Đã mở ChatGPT bằng browser hệ thống.", string.Empty,
                    "Thêm public MCP URL bằng OAuth/DCR rồi quay lại bấm “Đã thêm MCP trong ChatGPT”.");
            }
            catch (Exception ex) { ShowError("Không mở được ChatGPT", ex); }
            RefreshView();
        }

        private void MarkChatGptRegistered()
        {
            try { McpAgentExperience.MarkChatGptRegistrationAcknowledged(); }
            catch (Exception ex) { ShowError("Không ghi nhận được ChatGPT MCP", ex); }
            RefreshView();
        }

        private void CopyPublicUrl()
        {
            try
            {
                var url = McpPublicEndpointResolver.Resolve();
                if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Chưa có public MCP URL.");
                Clipboard.SetText(url);
                McpAgentExperience.Success("onboarding", "Đã copy public MCP URL.", "Mở ChatGPT và thêm custom MCP qua OAuth/DCR.");
            }
            catch (Exception ex) { ShowError("Copy MCP URL lỗi", ex); }
            RefreshView();
        }

        private void ToggleDesktopConsent()
        {
            try
            {
                if (McpDesktopControlSession.IsEnabled)
                    McpDesktopControlSession.DisableFromLocalUser("User đã tắt desktop control từ Agent Center.");
                else
                    McpDesktopControlSession.EnableFromLocalUser();
            }
            catch (Exception ex) { ShowError("Desktop control lỗi", ex); }
            RefreshView();
        }

        private void EmergencyStop()
        {
            McpDesktopControlSession.DisableFromLocalUser("User bấm EMERGENCY STOP AGENT trong QS3D.");
            try { InvokeControlTool("cad_agent_stop", "{}"); } catch { }
            RefreshView();
        }

        private void BackupNow()
        {
            string message;
            McpProjectRecoveryService.BackupNow(out message);
            MessageBox.Show(message, "QS3D Backup", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshView();
        }

        private void RecoverLatest()
        {
            string path;
            string message;
            var ok = McpProjectRecoveryService.RecoverLatestToCopy(out path, out message);
            MessageBox.Show(message, "QS3D Recovery", MessageBoxButton.OK,
                ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
            if (ok && !string.IsNullOrWhiteSpace(path))
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory)) OpenFolder(directory);
            }
            RefreshView();
        }

        private void StartQuickTunnel()
        {
            try
            {
                string error;
                if (!McpCloudflareAccountTunnelManager.StartQuickTunnel(out error))
                    throw new InvalidOperationException(error);
                McpAgentExperience.Warning("onboarding", "Quick Tunnel đã khởi động để test.",
                    "Không dùng Quick Tunnel làm production endpoint; chuyển về Named Tunnel ổn định sau test.");
            }
            catch (Exception ex) { ShowError("Quick Tunnel lỗi", ex); }
            RefreshView();
        }

        private void StopTunnels()
        {
            try
            {
                McpCloudflareAccountTunnelManager.StopForHostShutdown();
                McpCloudflareTunnelManager.StopForHostShutdown();
                McpAgentExperience.Warning("onboarding", "Đã dừng tunnel trong phiên hiện tại.", "Khởi động Named Tunnel khi cần kết nối lại.");
            }
            catch (Exception ex) { ShowError("Dừng tunnel lỗi", ex); }
            RefreshView();
        }

        private void CopyBearer()
        {
            try
            {
                McpEmbeddedServer.EnsureStarted();
                Clipboard.SetText(McpEmbeddedServer.GetBearerToken());
                McpAgentExperience.Warning("advanced", "Đã copy engineering bearer vào clipboard.", "Ưu tiên OAuth/DCR cho ChatGPT; không chia sẻ token.");
            }
            catch (Exception ex) { ShowError("Copy bearer lỗi", ex); }
        }

        private void CopyEngineeringConfig()
        {
            try
            {
                var url = McpPublicEndpointResolver.Resolve();
                if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Chưa có public MCP URL.");
                Clipboard.SetText("MCP URL: " + url + Environment.NewLine
                                  + "Authorization: Bearer " + McpEmbeddedServer.GetBearerToken());
                McpAgentExperience.Warning("advanced", "Đã copy engineering URL + Authorization.", "Dùng OAuth/DCR cho onboarding chuẩn.");
            }
            catch (Exception ex) { ShowError("Copy engineering config lỗi", ex); }
        }

        private void CheckProtocol()
        {
            RunLocalOperation("Đang kiểm tra MCP protocol...", () =>
            {
                McpEmbeddedServer.EnsureStarted();
                return McpProtocolProbe.Check(McpEmbeddedServer.Endpoint, 5000).Message;
            });
        }

        private void RunReadOnlySelfTest()
        {
            RunLocalOperation("Đang chạy read-only Agent self-test...", () =>
            {
                McpEmbeddedServer.EnsureStarted();
                return McpLocalAgentClient.RunReadOnlySelfTest(McpEmbeddedServer.Endpoint, 7000);
            });
        }

        private void InvokeControlTool(string tool, string arguments)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string message;
                try
                {
                    McpEmbeddedServer.EnsureStarted();
                    message = McpLocalAgentClient.CallOne(McpEmbeddedServer.Endpoint, 6000, tool, arguments);
                    McpAgentExperience.Success("agent", message, "Refresh status hoặc tiếp tục thao tác.");
                }
                catch (Exception ex)
                {
                    message = tool + " FAIL: " + ex.Message;
                    McpAgentExperience.Error("agent", message, "Kiểm tra Agent status rồi thử lại.");
                }
                Dispatcher.BeginInvoke(new Action(RefreshView));
            });
        }

        private void RunLocalOperation(string pending, Func<string> action)
        {
            if (Interlocked.CompareExchange(ref _localOperationActive, 1, 0) != 0)
            {
                McpAgentExperience.Warning("advanced", "Một local MCP check khác đang chạy.", "Emergency Stop/ESC vẫn khả dụng.");
                RefreshView();
                return;
            }

            McpAgentExperience.ActionStarted("advanced", pending, "Chờ local loopback check hoàn tất.");
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var result = action();
                    McpAgentExperience.Success("advanced", result, "Tiếp tục onboarding/Agent workflow.");
                }
                catch (Exception ex)
                {
                    McpAgentExperience.Error("advanced", "MCP local check FAIL: " + ex.Message, "Kiểm tra embedded MCP/tunnel và thử lại.");
                }
                finally
                {
                    Interlocked.Exchange(ref _localOperationActive, 0);
                    Dispatcher.BeginInvoke(new Action(RefreshView));
                }
            });
        }

        private void OpenAuditFolder()
        {
            var directory = Path.GetDirectoryName(McpEmbeddedServer.AuditFilePath);
            if (!string.IsNullOrWhiteSpace(directory)) OpenFolder(directory);
        }

        private void OpenFolder(string directory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory)) return;
                Directory.CreateDirectory(directory);
                Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
            }
            catch (Exception ex) { ShowError("Không mở được thư mục", ex); }
        }

        private void ShowError(string title, Exception error)
        {
            var message = error == null ? title : title + ": " + error.Message;
            McpAgentExperience.Error("ui", message, "Xem tab Nâng cao hoặc thử lại thao tác.");
            MessageBox.Show(message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private enum ActionKind { Primary, Secondary, Danger }

        private sealed class McpUiPalette
        {
            public Brush Window;
            public Brush Panel;
            public Brush Text;
            public Brush Muted;
            public Brush Border;
            public Brush Button;
            public Brush ButtonHover;
            public Brush ButtonPressed;
            public Brush Primary;
            public Brush PrimaryHover;
            public Brush PrimaryPressed;
            public Brush Success;
            public Brush DangerSoft;
            public Brush DangerHover;
            public Brush DangerPressed;
            public Brush DangerText;
            public Brush DangerBorder;

            public static McpUiPalette ForSystem()
            {
                var dark = IsSystemDark();
                return dark
                    ? new McpUiPalette
                    {
                        Window = Brush(0x0F, 0x15, 0x20), Panel = Brush(0x16, 0x20, 0x2D), Text = Brush(0xF1, 0xF5, 0xF9),
                        Muted = Brush(0xA7, 0xB3, 0xC5), Border = Brush(0x33, 0x43, 0x57), Button = Brush(0x20, 0x2D, 0x3D),
                        ButtonHover = Brush(0x2A, 0x3A, 0x4D), ButtonPressed = Brush(0x19, 0x24, 0x31), Primary = Brush(0x25, 0x63, 0xEB),
                        PrimaryHover = Brush(0x3B, 0x82, 0xF6), PrimaryPressed = Brush(0x1D, 0x4E, 0xD8), Success = Brush(0x4A, 0xD6, 0x83),
                        DangerSoft = Brush(0x45, 0x1E, 0x23), DangerHover = Brush(0x5C, 0x25, 0x2C), DangerPressed = Brush(0x36, 0x18, 0x1C),
                        DangerText = Brush(0xFF, 0xA8, 0xA8), DangerBorder = Brush(0x8F, 0x3B, 0x46)
                    }
                    : new McpUiPalette
                    {
                        Window = Brush(0xF4, 0xF7, 0xFB), Panel = Brushes.White, Text = Brush(0x17, 0x20, 0x33),
                        Muted = Brush(0x66, 0x70, 0x85), Border = Brush(0xDC, 0xE4, 0xEF), Button = Brushes.White,
                        ButtonHover = Brush(0xF1, 0xF5, 0xF9), ButtonPressed = Brush(0xE2, 0xE8, 0xF0), Primary = Brush(0x25, 0x63, 0xEB),
                        PrimaryHover = Brush(0x1D, 0x4E, 0xD8), PrimaryPressed = Brush(0x1E, 0x40, 0xAF), Success = Brush(0x15, 0x80, 0x3D),
                        DangerSoft = Brush(0xFD, 0xEC, 0xEC), DangerHover = Brush(0xFE, 0xDC, 0xDC), DangerPressed = Brush(0xFC, 0xC8, 0xC8),
                        DangerText = Brush(0xB4, 0x23, 0x18), DangerBorder = Brush(0xFD, 0xB0, 0xA8)
                    };
            }

            private static bool IsSystemDark()
            {
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

            private static Brush Brush(byte r, byte g, byte b)
            {
                var value = new SolidColorBrush(Color.FromRgb(r, g, b));
                value.Freeze();
                return value;
            }
        }
    }
}
