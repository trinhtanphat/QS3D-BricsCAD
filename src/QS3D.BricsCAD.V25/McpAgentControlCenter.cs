using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
    /// One-window, click-first operations center for non-technical MCP users.
    /// It deliberately keeps Cloudflare credentials in the provider browser and
    /// never asks the user to open PowerShell/CMD.
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
        private static readonly Brush SurfaceBrush = CreateBrush(0xF4, 0xF7, 0xFB);
        private static readonly Brush CardBrush = CreateBrush(0xFF, 0xFF, 0xFF);
        private static readonly Brush CardBorderBrush = CreateBrush(0xDC, 0xE4, 0xEF);
        private static readonly Brush TextBrush = CreateBrush(0x17, 0x20, 0x33);
        private static readonly Brush MutedTextBrush = CreateBrush(0x66, 0x70, 0x85);
        private static readonly Brush PrimaryBrush = CreateBrush(0x25, 0x63, 0xEB);
        private static readonly Brush PrimarySoftBrush = CreateBrush(0xEA, 0xF2, 0xFF);
        private static readonly Brush SuccessBrush = CreateBrush(0x15, 0x80, 0x3D);
        private static readonly Brush SuccessSoftBrush = CreateBrush(0xE9, 0xF8, 0xEF);
        private static readonly Brush DangerBrush = CreateBrush(0xB4, 0x23, 0x18);
        private static readonly Brush DangerSoftBrush = CreateBrush(0xFD, 0xEC, 0xEC);
        private static readonly Brush NeutralBrush = CreateBrush(0xF2, 0xF4, 0xF7);

        private readonly StackPanel _statusRows = new StackPanel();
        private readonly WrapPanel _statusChips = new WrapPanel
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        private readonly TextBlock _activity = new TextBlock
        {
            Text = "Sẵn sàng. Chọn một tác vụ để bắt đầu.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = TextBrush,
            FontSize = 13,
            LineHeight = 20
        };
        private int _localOperationActive;
        private DispatcherTimer? _quickUrlTimer;
        private int _quickUrlPollTicks;

        private enum ActionKind
        {
            Primary,
            Secondary,
            Danger
        }

        public McpAgentControlCenterWindow()
        {
            Title = "QS3D - ChatGPT MCP Agent Center";
            Width = 980;
            Height = 780;
            MinWidth = 780;
            MinHeight = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = SurfaceBrush;
            Foreground = TextBrush;
            UseLayoutRounding = true;
            SnapsToDevicePixels = true;
            Closed += (_, __) => StopQuickUrlPolling();

            Content = CreateDashboardShell();
            RefreshStatus();
        }

        private UIElement CreateDashboardShell()
        {
            var root = new StackPanel
            {
                Margin = new Thickness(24, 22, 24, 18)
            };

            root.Children.Add(CreateHeader());

            var dashboard = new Grid
            {
                Margin = new Thickness(-6, 12, -6, 0)
            };
            dashboard.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dashboard.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dashboard.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            dashboard.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var setupActions = new StackPanel();
            setupActions.Children.Add(CreateActionButton("Cài / cập nhật Cloudflare Tunnel", InstallCloudflared, ActionKind.Primary));
            setupActions.Children.Add(CreateActionButton("Đăng nhập Cloudflare + tạo Named Tunnel", (_, __) => OpenAccountSetup(), ActionKind.Secondary));
            setupActions.Children.Add(CreateActionButton("Khởi động Named Tunnel đã lưu", (_, __) => StartNamedTunnel(), ActionKind.Secondary));
            setupActions.Children.Add(CreateActionButton("Quick Tunnel · chỉ dùng để test", (_, __) => StartQuickTunnel(), ActionKind.Secondary));
            setupActions.Children.Add(CreateActionButton("Dừng tất cả tunnel", (_, __) => StopTunnels(), ActionKind.Secondary));
            var setupCard = CreateCard("Kết nối Cloudflare",
                "Thiết lập public MCP endpoint theo luồng click-first. Mật khẩu chỉ nhập trên trang Cloudflare trong browser.",
                setupActions);
            Grid.SetRow(setupCard, 0);
            Grid.SetColumn(setupCard, 0);
            dashboard.Children.Add(setupCard);

            var connectorActions = new StackPanel();
            connectorActions.Children.Add(CreateActionButton("Mở ChatGPT", (_, __) => McpCloudflareAccountTunnelManager.OpenChatGpt(), ActionKind.Primary));
            connectorActions.Children.Add(CreateActionButton("Copy MCP URL", (_, __) => CopyUrl(), ActionKind.Secondary));
            connectorActions.Children.Add(CreateActionButton("Copy Bearer Token", (_, __) => CopyToken(), ActionKind.Secondary));
            connectorActions.Children.Add(CreateActionButton("Copy URL + Authorization", (_, __) => CopyConfig(), ActionKind.Secondary));
            connectorActions.Children.Add(CreateActionButton("Kiểm tra MCP protocol", (_, __) => CheckProtocol(), ActionKind.Secondary));
            connectorActions.Children.Add(CreateActionButton("Tự kiểm tra Agent · read-only", (_, __) => RunReadOnlySelfTest(), ActionKind.Secondary));
            var connectorCard = CreateCard("ChatGPT Connector",
                "Copy thông tin kết nối, mở ChatGPT và kiểm tra MCP mà không cần PowerShell hay CMD.",
                connectorActions);
            Grid.SetRow(connectorCard, 0);
            Grid.SetColumn(connectorCard, 1);
            dashboard.Children.Add(connectorCard);

            var agentActions = new StackPanel();
            agentActions.Children.Add(CreateActionButton("EMERGENCY STOP AGENT", (_, __) => InvokeControlTool("cad_agent_stop", "{}"), ActionKind.Danger));
            agentActions.Children.Add(CreateActionButton("Hủy command BricsCAD hiện tại · ESC x2", (_, __) => InvokeControlTool("cad_cancel_command", "{}"), ActionKind.Secondary));
            agentActions.Children.Add(CreateActionButton("Resume Agent", (_, __) => InvokeControlTool("cad_agent_resume", "{\"confirmMutation\":true}"), ActionKind.Primary));
            agentActions.Children.Add(CreateActionButton("Mở thư mục audit MCP", (_, __) => OpenAuditFolder(), ActionKind.Secondary));
            var agentCard = CreateCard("Điều khiển Agent",
                "Các điều khiển vận hành luôn nằm riêng và dễ nhận biết. Emergency Stop/ESC vẫn khả dụng khi self-test đang chạy.",
                agentActions);
            Grid.SetRow(agentCard, 1);
            Grid.SetColumn(agentCard, 0);
            dashboard.Children.Add(agentCard);

            var statusCard = CreateCard("Trạng thái hệ thống",
                "Thông tin MCP, tunnel và endpoint hiện tại được gom thành các dòng ngắn để quét nhanh.",
                _statusRows);
            Grid.SetRow(statusCard, 1);
            Grid.SetColumn(statusCard, 1);
            dashboard.Children.Add(statusCard);

            root.Children.Add(dashboard);
            root.Children.Add(CreateActivityPanel());
            root.Children.Add(CreateFooter());

            return new ScrollViewer
            {
                Content = root,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
        }

        private UIElement CreateHeader()
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "QS3D · ChatGPT MCP Agent Center",
                FontSize = 25,
                FontWeight = FontWeights.Bold,
                Foreground = TextBrush
            });
            panel.Children.Add(new TextBlock
            {
                Text = "ChatGPT ↔ QS3D ↔ BricsCAD",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = PrimaryBrush,
                Margin = new Thickness(0, 4, 0, 0)
            });
            panel.Children.Add(new TextBlock
            {
                Text = "Thiết lập kết nối, kiểm tra Agent và xử lý tình huống khẩn cấp từ một màn hình duy nhất.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = MutedTextBrush,
                FontSize = 13,
                Margin = new Thickness(0, 7, 0, 0)
            });
            panel.Children.Add(_statusChips);
            return panel;
        }

        private static Border CreateCard(string title, string description, UIElement body)
        {
            var content = new StackPanel();
            content.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextBrush
            });
            content.Children.Add(new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = MutedTextBrush,
                FontSize = 12,
                LineHeight = 18,
                Margin = new Thickness(0, 5, 0, 12)
            });
            content.Children.Add(body);

            return new Border
            {
                Background = CardBrush,
                BorderBrush = CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(18),
                Margin = new Thickness(6),
                Child = content
            };
        }

        private static Button CreateActionButton(string text, RoutedEventHandler handler, ActionKind kind)
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
                BorderThickness = new Thickness(1)
            };

            if (kind == ActionKind.Primary)
            {
                button.Background = PrimaryBrush;
                button.BorderBrush = PrimaryBrush;
                button.Foreground = Brushes.White;
            }
            else if (kind == ActionKind.Danger)
            {
                button.Background = DangerSoftBrush;
                button.BorderBrush = CreateBrush(0xFD, 0xB0, 0xA8);
                button.Foreground = DangerBrush;
            }
            else
            {
                button.Background = CardBrush;
                button.BorderBrush = CardBorderBrush;
                button.Foreground = TextBrush;
            }

            button.Click += handler;
            return button;
        }

        private static Border CreateStatusChip(string text, bool active)
        {
            return new Border
            {
                Background = active ? SuccessSoftBrush : NeutralBrush,
                BorderBrush = active ? CreateBrush(0xAB, 0xEF, 0xC6) : CardBorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(0, 0, 7, 6),
                Child = new TextBlock
                {
                    Text = (active ? "● " : "○ ") + text,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = active ? SuccessBrush : MutedTextBrush
                }
            };
        }

        private static UIElement CreateStatusRow(string label, string value, Brush? valueBrush = null)
        {
            var row = new Grid
            {
                Margin = new Thickness(0, 0, 0, 8)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(122) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 11.5,
                Foreground = MutedTextBrush,
                VerticalAlignment = VerticalAlignment.Top
            };
            var valueBlock = new TextBlock
            {
                Text = value,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = valueBrush ?? TextBrush,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(valueBlock, 1);
            row.Children.Add(labelBlock);
            row.Children.Add(valueBlock);
            return row;
        }

        private UIElement CreateActivityPanel()
        {
            var content = new StackPanel();
            content.Children.Add(new TextBlock
            {
                Text = "Hoạt động gần nhất",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = TextBrush,
                Margin = new Thickness(0, 0, 0, 5)
            });
            content.Children.Add(_activity);

            return new Border
            {
                Background = PrimarySoftBrush,
                BorderBrush = CreateBrush(0xBF, 0xD3, 0xFF),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16, 13, 16, 13),
                Margin = new Thickness(0, 12, 0, 0),
                Child = content
            };
        }

        private UIElement CreateFooter()
        {
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };
            var refresh = CreateActionButton("Refresh", (_, __) => RefreshStatus(), ActionKind.Secondary);
            refresh.MinWidth = 92;
            refresh.HorizontalContentAlignment = HorizontalAlignment.Center;
            refresh.Margin = new Thickness(0, 0, 8, 0);
            var close = CreateActionButton("Đóng", (_, __) => Close(), ActionKind.Secondary);
            close.MinWidth = 92;
            close.HorizontalContentAlignment = HorizontalAlignment.Center;
            close.Margin = new Thickness(0);
            footer.Children.Add(refresh);
            footer.Children.Add(close);
            return footer;
        }

        private static Brush CreateBrush(byte red, byte green, byte blue)
        {
            var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
            brush.Freeze();
            return brush;
        }

        private void InstallCloudflared(object? sender, RoutedEventArgs args)
        {
            _activity.Text = "Đang tải cloudflared chính thức và kiểm tra Authenticode...";
            McpCloudflaredBootstrapper.BeginInstall((ok, message) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _activity.Text = message;
                    RefreshStatus();
                    if (!ok)
                    {
                        MessageBox.Show(
                            message + "\n\nBạn có thể thử lại nút cài tự động hoặc kiểm tra chính sách mạng/chứng thư trên máy.",
                            "QS3D MCP",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
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
                new McpCloudflareAccountSetupWindow().ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            RefreshStatus();
        }

        private void StartNamedTunnel()
        {
            StopQuickUrlPolling();
            string error;
            if (!McpCloudflareAccountTunnelManager.StartSaved(out error))
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                _activity.Text = "Named Tunnel đang khởi động.";
            RefreshStatus();
        }

        private void StartQuickTunnel()
        {
            string error;
            if (!McpCloudflareAccountTunnelManager.StartQuickTunnel(out error))
            {
                StopQuickUrlPolling();
                MessageBox.Show(error, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
                RefreshStatus();
                return;
            }
            _activity.Text = "Quick Tunnel đang khởi động; đang chờ public URL...";
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

        private void QuickUrlTimerOnTick(object? sender, EventArgs e)
        {
            _quickUrlPollTicks++;
            RefreshStatus();
            var publicUrl = McpCloudflareTunnelManager.PublicMcpUrl;
            if (!string.IsNullOrWhiteSpace(publicUrl))
            {
                _activity.Text = "Quick Tunnel sẵn sàng: " + publicUrl;
                StopQuickUrlPolling();
                return;
            }
            if (!McpCloudflareTunnelManager.IsRunning || _quickUrlPollTicks >= 20)
            {
                if (McpCloudflareTunnelManager.IsRunning)
                    _activity.Text = "Quick Tunnel đang chạy nhưng chưa nhận được public URL trong thời gian chờ; có thể bấm Refresh để kiểm tra lại.";
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
            _activity.Text = "Đã dừng tunnel của QS3D trong phiên BricsCAD này.";
            RefreshStatus();
        }

        private void CopyUrl()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Chưa có public MCP URL. Hãy tạo Named Tunnel hoặc Quick Tunnel trước.", "QS3D MCP");
                return;
            }
            Clipboard.SetText(url);
            _activity.Text = "Đã copy MCP URL.";
        }

        private void CopyToken()
        {
            McpEmbeddedServer.EnsureStarted();
            Clipboard.SetText(McpEmbeddedServer.GetBearerToken());
            _activity.Text = "Đã copy Bearer Token. Không chia sẻ token này công khai.";
        }

        private void CopyConfig()
        {
            var url = McpPublicEndpointResolver.Resolve();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Chưa có public MCP URL.", "QS3D MCP");
                return;
            }
            Clipboard.SetText("MCP URL: " + url + Environment.NewLine
                              + "Authorization: Bearer " + McpEmbeddedServer.GetBearerToken());
            _activity.Text = "Đã copy URL + Authorization cho ChatGPT.";
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
                return McpLocalAgentClient.RunReadOnlySelfTest(McpEmbeddedServer.Endpoint, 6000);
            }, true);
        }

        private void InvokeControlTool(string tool, string arguments)
        {
            // Emergency stop/cancel must not be blocked by an observation self-test that is already
            // waiting on CAD context. They run on a worker too, but deliberately bypass the UI-only slot.
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
                    _activity.Text = "Một MCP local check khác đang chạy; Emergency Stop/ESC vẫn luôn khả dụng.";
                    return;
                }
                ownsSlot = true;
            }

            _activity.Text = pendingMessage;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string message;
                try { message = action(); }
                catch (Exception ex) { message = "MCP local operation FAIL: " + ex.Message; }
                finally
                {
                    if (ownsSlot) Interlocked.Exchange(ref _localOperationActive, 0);
                }

                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _activity.Text = message;
                        RefreshStatus();
                    }));
                }
                catch { }
            });
        }

        private void OpenAuditFolder()
        {
            try
            {
                var path = McpEmbeddedServer.AuditFilePath;
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directory)) return;
                Directory.CreateDirectory(directory);
                Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            _statusChips.Children.Clear();
            _statusChips.Children.Add(CreateStatusChip(mcpRunning ? "MCP online" : "MCP offline", mcpRunning));
            _statusChips.Children.Add(CreateStatusChip(tunnelRunning ? "Tunnel online" : "Tunnel offline", tunnelRunning));
            _statusChips.Children.Add(CreateStatusChip(string.IsNullOrWhiteSpace(publicUrl) ? "Public URL chưa có" : "Public URL sẵn sàng", !string.IsNullOrWhiteSpace(publicUrl)));

            _statusRows.Children.Clear();
            _statusRows.Children.Add(CreateStatusRow("MCP embedded", mcpRunning ? "RUNNING" : "STOPPED", mcpRunning ? SuccessBrush : MutedTextBrush));
            _statusRows.Children.Add(CreateStatusRow("Local endpoint", McpEmbeddedServer.Endpoint.ToString()));
            _statusRows.Children.Add(CreateStatusRow("Cloudflare", cloudflaredInstalled ? "Đã cài" : "Chưa cài", cloudflaredInstalled ? SuccessBrush : MutedTextBrush));
            _statusRows.Children.Add(CreateStatusRow("Browser login", authenticated ? "Đã đăng nhập" : "Chưa đăng nhập", authenticated ? SuccessBrush : MutedTextBrush));
            _statusRows.Children.Add(CreateStatusRow("Named Tunnel", namedTunnelRunning ? "RUNNING" : "STOPPED", namedTunnelRunning ? SuccessBrush : MutedTextBrush));
            _statusRows.Children.Add(CreateStatusRow("Quick Tunnel", quickTunnelRunning ? "RUNNING" : "STOPPED", quickTunnelRunning ? SuccessBrush : MutedTextBrush));
            _statusRows.Children.Add(CreateStatusRow("Public MCP", string.IsNullOrWhiteSpace(publicUrl) ? "Chưa có public URL" : publicUrl));
            _statusRows.Children.Add(CreateStatusRow("Agent", McpEmbeddedServer.Describe()));
        }
    }

    /// <summary>
    /// Local-only MCP client used by the GUI for emergency controls and a read-only
    /// end-to-end self-test. It talks to the same loopback endpoint ChatGPT uses.
    /// </summary>
    internal static class McpLocalAgentClient
    {
        private const string ProtocolVersion = "2025-06-18";

        public static string CallOne(Uri endpoint, int timeoutMilliseconds, string tool, string argumentsJson)
        {
            string? session = null;
            try
            {
                session = Initialize(endpoint, timeoutMilliseconds);
                NotifyInitialized(endpoint, timeoutMilliseconds, session);
                return Call(endpoint, timeoutMilliseconds, session, tool, argumentsJson);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(session))
                {
                    try { Send(endpoint, "DELETE", string.Empty, timeoutMilliseconds, session); } catch { }
                }
            }
        }

        public static string RunReadOnlySelfTest(Uri endpoint, int timeoutMilliseconds)
        {
            string? session = null;
            try
            {
                session = Initialize(endpoint, timeoutMilliseconds);
                NotifyInitialized(endpoint, timeoutMilliseconds, session);
                var list = Send(endpoint, "POST",
                    "{\"jsonrpc\":\"2.0\",\"id\":20,\"method\":\"tools/list\",\"params\":{}}",
                    timeoutMilliseconds,
                    session);
                RequireSuccess(list, "tools/list");

                var required = new[]
                {
                    "connector_info", "qs3d_status", "cad_active_document", "cad_selection",
                    "cad_database_snapshot", "cad_entity_inspect", "cad_view_state", "cad_wait_idle",
                    "cad_create_line", "cad_create_circle", "cad_create_polyline", "cad_create_text",
                    "cad_entity_transform", "cad_entity_delete", "cad_layer", "cad_command_sequence",
                    "qs3d_run_command", "cad_ui_click", "cad_ui_type", "cad_ui_key",
                    "cad_agent_stop", "cad_agent_resume", "cad_audit_tail", "cad_cancel_command"
                };
                var missing = new List<string>();
                foreach (var name in required)
                    if (list.Body.IndexOf("\\\"name\\\":\\\"" + name + "\\\"", StringComparison.Ordinal) < 0
                        && list.Body.IndexOf("\"name\":\"" + name + "\"", StringComparison.Ordinal) < 0)
                        missing.Add(name);
                if (missing.Count > 0)
                    throw new InvalidOperationException("tools/list thiếu: " + string.Join(", ", missing));

                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "connector_info", "{}"), "connector_info");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "qs3d_status", "{}"), "qs3d_status");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_active_document", "{}"), "cad_active_document");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_view_state", "{}"), "cad_view_state");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_database_snapshot", "{\"limit\":20}"), "cad_database_snapshot");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_command_catalog", "{}"), "cad_command_catalog");
                RequireToolSuccess(Call(endpoint, timeoutMilliseconds, session, "cad_audit_tail", "{\"limit\":3}"), "cad_audit_tail");

                return "SELF-TEST PASS: MCP initialize/session/tools/list + 7 read-only CAD/agent calls đều thành công; mutation không được chạy.";
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(session))
                {
                    try { Send(endpoint, "DELETE", string.Empty, timeoutMilliseconds, session); } catch { }
                }
            }
        }

        private static string Initialize(Uri endpoint, int timeoutMilliseconds)
        {
            var body = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\""
                       + ProtocolVersion
                       + "\",\"capabilities\":{},\"clientInfo\":{\"name\":\"QS3D-Agent-Center\",\"version\":\"1\"}}}";
            var result = Send(endpoint, "POST", body, timeoutMilliseconds, null);
            RequireSuccess(result, "initialize");
            if (string.IsNullOrWhiteSpace(result.SessionId))
                throw new InvalidOperationException("initialize không trả Mcp-Session-Id.");
            return result.SessionId!;
        }

        private static void NotifyInitialized(Uri endpoint, int timeoutMilliseconds, string session)
        {
            var result = Send(endpoint, "POST",
                "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\",\"params\":{}}",
                timeoutMilliseconds,
                session);
            if (result.StatusCode != 202 && result.StatusCode != 204 && result.StatusCode != 200)
                throw new InvalidOperationException("notifications/initialized HTTP " + result.StatusCode + ".");
        }

        private static string Call(Uri endpoint, int timeoutMilliseconds, string session, string tool, string argumentsJson)
        {
            var safeArguments = string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson.Trim();
            if (!safeArguments.StartsWith("{", StringComparison.Ordinal) || !safeArguments.EndsWith("}", StringComparison.Ordinal))
                throw new InvalidOperationException("Local MCP tool arguments must be a JSON object.");
            var request = "{\"jsonrpc\":\"2.0\",\"id\":30,\"method\":\"tools/call\",\"params\":{\"name\":\""
                          + McpEmbeddedServer.JsonEscape(tool)
                          + "\",\"arguments\":" + safeArguments + "}}";
            var result = Send(endpoint, "POST", request, timeoutMilliseconds, session);
            RequireSuccess(result, tool);
            RequireToolSuccess(result.Body, tool);
            return tool + ": OK";
        }

        private static void RequireToolSuccess(string body, string operation)
        {
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException(operation + " returned an empty response.");
            if (body.IndexOf("\\\"isError\\\":true", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("\"isError\":true", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException(operation + " returned MCP isError=true.");
        }

        private static void RequireSuccess(LocalHttpResult result, string operation)
        {
            if (result.StatusCode < 200 || result.StatusCode >= 300)
                throw new InvalidOperationException(operation + " HTTP " + result.StatusCode + ".");
            if (!string.IsNullOrWhiteSpace(result.Body)
                && Regex.IsMatch(result.Body, "\\\"error\\\"\\s*:", RegexOptions.IgnoreCase))
                throw new InvalidOperationException(operation + " returned JSON-RPC error.");
        }

        private static LocalHttpResult Send(Uri endpoint, string method, string body, int timeoutMilliseconds, string? session)
        {
#pragma warning disable SYSLIB0014
            var request = (HttpWebRequest)WebRequest.Create(endpoint);
#pragma warning restore SYSLIB0014
            request.Method = method;
            request.Accept = "application/json, text/event-stream";
            request.Timeout = timeoutMilliseconds;
            request.ReadWriteTimeout = timeoutMilliseconds;
            request.Headers["MCP-Protocol-Version"] = ProtocolVersion;
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + McpEmbeddedServer.GetBearerToken();
            if (!string.IsNullOrWhiteSpace(session)) request.Headers["Mcp-Session-Id"] = session;

            if (!string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                request.ContentType = "application/json";
                var payload = Encoding.UTF8.GetBytes(body ?? string.Empty);
                request.ContentLength = payload.Length;
                using (var stream = request.GetRequestStream()) stream.Write(payload, 0, payload.Length);
            }
            else request.ContentLength = 0;

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var responseBody = string.Empty;
                if (response.ContentLength != 0)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        using (var reader = new StreamReader(stream, Encoding.UTF8)) responseBody = NormalizeBody(reader.ReadToEnd());
                    }
                }
                return new LocalHttpResult((int)response.StatusCode, response.Headers["Mcp-Session-Id"], responseBody);
            }
        }

        private static string NormalizeBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body) || body.IndexOf("data:", StringComparison.OrdinalIgnoreCase) < 0)
                return body == null ? string.Empty : body.Trim();
            var builder = new StringBuilder();
            using (var reader = new StringReader(body))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                    if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) builder.Append(line.Substring(5).Trim());
            }
            return builder.Length == 0 ? body.Trim() : builder.ToString();
        }

        private sealed class LocalHttpResult
        {
            public LocalHttpResult(int statusCode, string? sessionId, string body)
            {
                StatusCode = statusCode;
                SessionId = sessionId;
                Body = body ?? string.Empty;
            }

            public int StatusCode { get; private set; }
            public string? SessionId { get; private set; }
            public string Body { get; private set; }
        }
    }
}
