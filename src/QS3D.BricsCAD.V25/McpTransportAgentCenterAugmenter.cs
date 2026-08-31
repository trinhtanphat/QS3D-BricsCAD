using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Adds transport-hardening affordances to the existing Agent Center without changing the
    /// public MCP surface: cloudflared busy/progress/cancel state plus bounded OpenAI tunnel
    /// diagnostics and an environment-key-only restart action. The augmenter runs only on the
    /// BricsCAD UI dispatcher and only touches the canonical Agent Center window.
    /// </summary>
    internal static class McpTransportAgentCenterAugmenter
    {
        private const string AgentCenterTitle = "QS3D - ChatGPT MCP Agent Center";
        private const string InstallCloudflaredLabel = "Cài / cập nhật Cloudflare Tunnel";
        private const string OpenAiAdminAnchorLabel = "Mở tunnel-client UI";
        private const string CloudflareCancelTag = "QS3D_MCP_CLOUDFLARED_CANCEL";
        private const string OpenAiCopyDiagnosticsTag = "QS3D_MCP_OPENAI_COPY_DIAGNOSTICS";
        private const string OpenAiRestartTag = "QS3D_MCP_OPENAI_RESTART";
        private const string OpenAiStatusTag = "QS3D_MCP_OPENAI_DIAGNOSTIC_STATUS";
        private static readonly object Sync = new object();
        private static DispatcherTimer? _timer;
        private static EventHandler? _tickHandler;

        public static void Start()
        {
            lock (Sync)
            {
                if (_timer != null) return;
                var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                EventHandler handler = (_, __) => Refresh();
                _timer = timer;
                _tickHandler = handler;
                timer.Tick += handler;
                timer.Start();
            }
            Refresh();
        }

        public static void Stop()
        {
            DispatcherTimer? timer;
            EventHandler? handler;
            lock (Sync)
            {
                timer = _timer;
                handler = _tickHandler;
                _timer = null;
                _tickHandler = null;
            }
            if (timer == null) return;
            try { timer.Stop(); } catch { }
            try { if (handler != null) timer.Tick -= handler; } catch { }
        }

        private static void Refresh()
        {
            try
            {
                var sources = new List<PresentationSource>();
                foreach (PresentationSource source in PresentationSource.CurrentSources) sources.Add(source);
                foreach (var source in sources)
                {
                    var root = source.RootVisual as DependencyObject;
                    if (root == null) continue;
                    var window = root as Window;
                    if (window == null)
                    {
                        try { window = Window.GetWindow(root); } catch { }
                    }
                    if (window == null || !string.Equals(window.Title, AgentCenterTitle, StringComparison.Ordinal)) continue;
                    RefreshTree(root);
                }
            }
            catch
            {
                // UI augmentation is optional. It must never make the Agent Center or BricsCAD fail.
            }
        }

        private static void RefreshTree(DependencyObject root)
        {
            var panel = root as Panel;
            if (panel != null) RefreshPanel(panel);

            var children = new List<DependencyObject>();
            try
            {
                var count = VisualTreeHelper.GetChildrenCount(root);
                for (var i = 0; i < count; i++) children.Add(VisualTreeHelper.GetChild(root, i));
            }
            catch { }
            foreach (var child in children) RefreshTree(child);
        }

        private static void RefreshPanel(Panel panel)
        {
            var snapshot = new List<UIElement>();
            foreach (UIElement child in panel.Children) snapshot.Add(child);
            foreach (var child in snapshot)
            {
                var button = child as Button;
                if (button == null) continue;
                var text = button.Content as string ?? string.Empty;
                if (string.Equals(text, InstallCloudflaredLabel, StringComparison.Ordinal)
                    || text.StartsWith("Đang cài Cloudflare...", StringComparison.Ordinal))
                {
                    RefreshCloudflaredControls(panel, button);
                }
                if (string.Equals(text, OpenAiAdminAnchorLabel, StringComparison.Ordinal))
                {
                    RefreshOpenAiDiagnosticsControls(panel, button);
                }
            }
        }

        private static void RefreshCloudflaredControls(Panel panel, Button installButton)
        {
            var busy = McpCloudflaredBootstrapper.IsInstalling;
            var cancelling = busy && McpCloudflaredBootstrapper.WasLastInstallCancelled;
            installButton.IsEnabled = !busy;
            installButton.Content = busy
                ? "Đang cài Cloudflare... " + McpCloudflaredBootstrapper.InstallProgressPercent + "%"
                : InstallCloudflaredLabel;

            var cancel = FindTaggedButton(panel, CloudflareCancelTag);
            if (!busy)
            {
                if (cancel != null) panel.Children.Remove(cancel);
                return;
            }
            if (cancel == null)
            {
                cancel = CloneActionButton(installButton, "Hủy cài Cloudflare Tunnel", CloudflareCancelTag);
                cancel.Click += (_, __) =>
                {
                    string message;
                    if (McpCloudflaredBootstrapper.CancelInstall(out message))
                        McpAgentExperience.Info("onboarding", message, string.Empty, "Chờ installer dọn file tạm rồi Refresh.");
                    else
                        McpAgentExperience.Warning("onboarding", message, "Refresh trạng thái Cloudflare installer.");
                };
                InsertAfter(panel, installButton, cancel);
            }
            cancel.IsEnabled = !cancelling;
            cancel.Content = cancelling ? "Đang hủy Cloudflare..." : "Hủy cài Cloudflare Tunnel";
        }

        private static void RefreshOpenAiDiagnosticsControls(Panel panel, Button anchor)
        {
            var copy = FindTaggedButton(panel, OpenAiCopyDiagnosticsTag);
            if (copy == null)
            {
                copy = CloneActionButton(anchor, "Copy tunnel diagnostics", OpenAiCopyDiagnosticsTag);
                copy.Click += (_, __) => CopyOpenAiDiagnostics();
                InsertAfter(panel, anchor, copy);
            }

            var restart = FindTaggedButton(panel, OpenAiRestartTag);
            if (restart == null)
            {
                restart = CloneActionButton(anchor, "Restart tunnel · env key", OpenAiRestartTag);
                restart.Click += (_, __) => RestartOpenAiTunnelFromEnvironment();
                InsertAfter(panel, copy, restart);
            }

            var status = FindTaggedTextBlock(panel, OpenAiStatusTag);
            if (status == null)
            {
                status = new TextBlock
                {
                    Tag = OpenAiStatusTag,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11,
                    Margin = new Thickness(0, 1, 0, 8)
                };
                InsertAfter(panel, restart, status);
            }
            status.Text = BuildOpenAiStatusText();
        }

        private static string BuildOpenAiStatusText()
        {
            var trust = McpOpenAiSecureTunnelManager.ClientTrustSummary;
            var exit = McpOpenAiSecureTunnelManager.LastExitCode;
            var error = McpOpenAiSecureTunnelManager.LastError;
            return "Tunnel diagnostics · trust=" + (string.IsNullOrWhiteSpace(trust) ? "chưa xác minh trong phiên" : trust)
                   + " · exit=" + (exit.HasValue ? exit.Value.ToString() : "n/a")
                   + (string.IsNullOrWhiteSpace(error) ? string.Empty : " · last error=" + error);
        }

        private static void CopyOpenAiDiagnostics()
        {
            try
            {
                Clipboard.SetText(McpOpenAiSecureTunnelManager.GetDiagnosticBundle());
                McpAgentExperience.Info("onboarding", "Đã copy OpenAI tunnel diagnostics đã sanitize.", string.Empty,
                    "Dán diagnostic bundle khi cần support; kiểm tra lại trước khi chia sẻ ra ngoài.");
            }
            catch (Exception ex)
            {
                McpAgentExperience.Error("onboarding", "Không copy được tunnel diagnostics: " + ex.Message,
                    "Thử lại từ Agent Center trên UI thread.");
            }
        }

        private static void RestartOpenAiTunnelFromEnvironment()
        {
            try
            {
                if (McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel)
                {
                    MessageBox.Show("Hãy chọn OpenAI Secure Tunnel trước khi restart.", "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var hasEnvironmentKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONTROL_PLANE_API_KEY"))
                                        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
                if (!hasEnvironmentKey)
                {
                    MessageBox.Show(
                        "Restart tự động chỉ dùng Runtime API key đã có trong môi trường Windows. QS3D không lưu key đã nhập trong UI. Hãy nhập lại key và bấm Khởi động nếu không dùng environment key.",
                        "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                McpOpenAiSecureTunnelManager.StopForHostShutdown();
                string message;
                var ok = McpOpenAiSecureTunnelManager.Start(McpOpenAiSecureTunnelManager.SavedTunnelId, string.Empty, out message);
                if (ok)
                    McpAgentExperience.Success("onboarding", message, "Chờ tunnel-client READY rồi tiếp tục ChatGPT.");
                else
                    McpAgentExperience.Error("onboarding", message, "Kiểm tra environment key, Tunnel ID, trust verification và diagnostics.");
                MessageBox.Show(message, "QS3D MCP", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                McpAgentExperience.Error("onboarding", "Restart OpenAI tunnel lỗi: " + ex.Message,
                    "Kiểm tra diagnostics rồi khởi động lại thủ công.");
                try { MessageBox.Show(ex.Message, "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Warning); } catch { }
            }
        }

        private static Button CloneActionButton(Button source, string content, string tag)
        {
            return new Button
            {
                Content = content,
                Tag = tag,
                MinHeight = source.MinHeight,
                Margin = source.Margin,
                Padding = source.Padding,
                HorizontalContentAlignment = source.HorizontalContentAlignment,
                VerticalContentAlignment = source.VerticalContentAlignment,
                FontSize = source.FontSize,
                FontWeight = source.FontWeight,
                BorderThickness = source.BorderThickness,
                FocusVisualStyle = source.FocusVisualStyle,
                Style = source.Style
            };
        }

        private static Button? FindTaggedButton(Panel panel, string tag)
        {
            foreach (UIElement child in panel.Children)
            {
                var button = child as Button;
                if (button != null && string.Equals(button.Tag as string, tag, StringComparison.Ordinal)) return button;
            }
            return null;
        }

        private static TextBlock? FindTaggedTextBlock(Panel panel, string tag)
        {
            foreach (UIElement child in panel.Children)
            {
                var text = child as TextBlock;
                if (text != null && string.Equals(text.Tag as string, tag, StringComparison.Ordinal)) return text;
            }
            return null;
        }

        private static void InsertAfter(Panel panel, UIElement anchor, UIElement value)
        {
            var index = panel.Children.IndexOf(anchor);
            if (index < 0 || index + 1 >= panel.Children.Count) panel.Children.Add(value);
            else panel.Children.Insert(index + 1, value);
        }
    }
}
