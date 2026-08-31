using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Adds transport-hardening affordances to the existing Agent Center without changing the
    /// public MCP surface: cloudflared busy/progress/recovery state plus bounded OpenAI tunnel
    /// diagnostics and saved/environment-key restart. The bootstrapper owns the dynamic Cloudflare
    /// cancel button so this augmenter never creates a second competing cancel control.
    /// </summary>
    internal static class McpTransportAgentCenterAugmenter
    {
        private const string AgentCenterTitle = "QS3D - ChatGPT MCP Agent Center";
        private const string InstallCloudflaredLabel = "Cài / cập nhật Cloudflare Tunnel";
        private const string OpenAiAdminAnchorLabel = "Mở tunnel-client UI";
        private const string PendingChatGptTunnelLabel = "ChatGPT Tunnel chưa xác nhận";
        private const string WaitingChatGptTunnelTrafficLabel = "ChatGPT Tunnel · chờ MCP traffic";
        private const string CloudflareRecoveryTag = "QS3D_MCP_CLOUDFLARED_RECOVERY";
        private const string CloudflareStatusTag = "QS3D_MCP_CLOUDFLARED_STATUS";
        private const string OpenAiCopyDiagnosticsTag = "QS3D_MCP_OPENAI_COPY_DIAGNOSTICS";
        private const string OpenAiOpenLogsTag = "QS3D_MCP_OPENAI_OPEN_LOGS";
        private const string OpenAiRestartTag = "QS3D_MCP_OPENAI_RESTART";
        private const string OpenAiStatusTag = "QS3D_MCP_OPENAI_DIAGNOSTIC_STATUS";
        private const string WingetRecoveryCommand = "winget install --id Cloudflare.cloudflared --source winget";
        private static readonly object Sync = new object();
        private static readonly HashSet<string> KnownMcpSessionIds = new HashSet<string>(StringComparer.Ordinal);
        private static DispatcherTimer? _timer;
        private static EventHandler? _tickHandler;
        private static bool _sessionSnapshotInitialized;
        private static bool _sessionReflectionWarningPublished;

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
                KnownMcpSessionIds.Clear();
                _sessionSnapshotInitialized = false;
                _sessionReflectionWarningPublished = false;
            }
            if (timer == null) return;
            try { timer.Stop(); } catch { }
            try { if (handler != null) timer.Tick -= handler; } catch { }
        }

        private static void Refresh()
        {
            try
            {
                RefreshChatGptTunnelTrafficEvidence();

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

        private static void RefreshChatGptTunnelTrafficEvidence()
        {
            var currentSessionIds = SnapshotEmbeddedMcpSessionIds();
            if (currentSessionIds == null) return;

            var provider = McpTransportCoordinator.SelectedProvider;
            var openAiTunnelRunning = provider == McpTransportProvider.OpenAiSecureTunnel
                                      && McpOpenAiSecureTunnelManager.IsRunning;
            var sawNewSession = false;

            lock (Sync)
            {
                if (!_sessionSnapshotInitialized || !openAiTunnelRunning)
                {
                    ReplaceKnownSessionIds(currentSessionIds);
                    _sessionSnapshotInitialized = true;
                    return;
                }

                foreach (var sessionId in currentSessionIds)
                {
                    if (!KnownMcpSessionIds.Contains(sessionId))
                    {
                        sawNewSession = true;
                        break;
                    }
                }
                ReplaceKnownSessionIds(currentSessionIds);
            }

            if (!sawNewSession || McpTransportCoordinator.IsChatGptRegistrationAcknowledged()) return;

            try
            {
                McpTransportCoordinator.MarkChatGptRegistrationAcknowledged();
                McpAgentExperience.Success(
                    "onboarding",
                    "Đã tự xác nhận OpenAI Tunnel sau khi embedded MCP nhận một session initialize mới trong lúc tunnel-client đang chạy.",
                    "ChatGPT Tunnel đã có MCP traffic; tiếp tục tools/list hoặc tool call để xác nhận end-to-end.");
            }
            catch (Exception ex)
            {
                McpAgentExperience.Warning(
                    "onboarding",
                    "Không thể tự ghi nhận MCP traffic cho OpenAI Tunnel: " + ex.Message,
                    "Giữ tunnel-client chạy và thử initialize/tools/list lại; nút xác nhận thủ công vẫn là fallback.");
            }
        }

        private static HashSet<string>? SnapshotEmbeddedMcpSessionIds()
        {
            try
            {
                var field = typeof(McpEmbeddedServer).GetField("Sessions", BindingFlags.NonPublic | BindingFlags.Static);
                var sessions = field == null ? null : field.GetValue(null);
                var keysProperty = sessions == null ? null : sessions.GetType().GetProperty("Keys", BindingFlags.Public | BindingFlags.Instance);
                var keys = keysProperty == null ? null : keysProperty.GetValue(sessions, null) as IEnumerable;
                if (keys == null) throw new InvalidOperationException("Không đọc được MCP session keys.");

                var result = new HashSet<string>(StringComparer.Ordinal);
                foreach (var key in keys)
                {
                    var sessionId = key as string;
                    if (!string.IsNullOrWhiteSpace(sessionId)) result.Add(sessionId!);
                }
                return result;
            }
            catch (Exception ex)
            {
                var publishWarning = false;
                lock (Sync)
                {
                    if (!_sessionReflectionWarningPublished)
                    {
                        _sessionReflectionWarningPublished = true;
                        publishWarning = true;
                    }
                }
                if (publishWarning)
                {
                    McpAgentExperience.Warning(
                        "onboarding",
                        "Không bật được auto-confirm ChatGPT Tunnel traffic: " + ex.Message,
                        "MCP vẫn hoạt động; dùng nút xác nhận thủ công và kiểm tra preflight nếu source layout đã đổi.");
                }
                return null;
            }
        }

        private static void ReplaceKnownSessionIds(HashSet<string> currentSessionIds)
        {
            KnownMcpSessionIds.Clear();
            foreach (var sessionId in currentSessionIds) KnownMcpSessionIds.Add(sessionId);
        }

        private static void RefreshTree(DependencyObject root)
        {
            var text = root as TextBlock;
            if (text != null && string.Equals(text.Text, PendingChatGptTunnelLabel, StringComparison.Ordinal))
                text.Text = WaitingChatGptTunnelTrafficLabel;

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
            installButton.IsEnabled = !busy;
            installButton.Content = busy
                ? "Đang cài Cloudflare... " + McpCloudflaredBootstrapper.InstallProgressPercent + "%"
                : InstallCloudflaredLabel;

            // McpCloudflaredBootstrapper.PublishInstallerUiState owns the one dynamic Cancel button.
            // Keeping cancel creation in one place prevents the two independent tags/controls that
            // previously could render duplicate "Hủy cài Cloudflare Tunnel" buttons.
            var recovery = FindTaggedButton(panel, CloudflareRecoveryTag);
            if (recovery == null)
            {
                recovery = CloneActionButton(installButton, "Copy WinGet recovery command", CloudflareRecoveryTag);
                recovery.Click += (_, __) => CopyWingetRecoveryCommand();
                InsertAfter(panel, installButton, recovery);
            }

            var status = FindTaggedTextBlock(panel, CloudflareStatusTag);
            if (status == null)
            {
                status = new TextBlock
                {
                    Tag = CloudflareStatusTag,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11,
                    Margin = new Thickness(0, 1, 0, 8)
                };
                InsertAfter(panel, recovery, status);
            }
            status.Text = BuildCloudflareBinaryStatus();
        }

        private static string BuildCloudflareBinaryStatus()
        {
            string path;
            string source;
            string message;
            if (!McpCloudflaredBootstrapper.TryResolveTrustedInstalledBinary(out path, out source, out message))
                return "Cloudflare binary · Trust=NOT_READY · " + message;

            return "Cloudflare binary · Trust=VERIFIED · Source=" + source
                   + " · Path=" + path
                   + (string.IsNullOrWhiteSpace(message) ? string.Empty : " · " + message);
        }

        private static void CopyWingetRecoveryCommand()
        {
            try
            {
                Clipboard.SetText(WingetRecoveryCommand);
                McpAgentExperience.Info("onboarding", "Đã copy lệnh WinGet recovery cho cloudflared.", string.Empty,
                    "Mở PowerShell khi downloader bị chặn, chạy lệnh đã copy, sau đó quay lại Agent Center và Refresh.");
            }
            catch (Exception ex)
            {
                McpAgentExperience.Error("onboarding", "Không copy được lệnh WinGet recovery: " + ex.Message,
                    "Có thể chạy thủ công: " + WingetRecoveryCommand);
            }
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

            var openLogs = FindTaggedButton(panel, OpenAiOpenLogsTag);
            if (openLogs == null)
            {
                openLogs = CloneActionButton(anchor, "Open tunnel logs", OpenAiOpenLogsTag);
                openLogs.Click += (_, __) => OpenOpenAiLogs();
                InsertAfter(panel, copy, openLogs);
            }

            var restart = FindTaggedButton(panel, OpenAiRestartTag);
            if (restart == null)
            {
                restart = CloneActionButton(anchor, "Restart tunnel · saved/env key", OpenAiRestartTag);
                restart.Click += (_, __) => RestartOpenAiTunnelFromEnvironment();
                InsertAfter(panel, openLogs, restart);
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

        private static void OpenOpenAiLogs()
        {
            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QS3D", "MCP", "OpenAiSecureTunnel", "Support");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "tunnel-diagnostics.log");
                File.WriteAllText(path, McpOpenAiSecureTunnelManager.GetDiagnosticBundle(), new UTF8Encoding(false));
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                McpAgentExperience.Info("onboarding", "Đã materialize và mở tunnel diagnostics đã sanitize.", string.Empty,
                    "Log support này chỉ được tạo khi người dùng bấm Open tunnel logs; Runtime API key/local bearer không được cố ý ghi vào bundle.");
            }
            catch (Exception ex)
            {
                McpAgentExperience.Error("onboarding", "Không mở được tunnel diagnostics log: " + ex.Message,
                    "Dùng Copy tunnel diagnostics làm fallback.");
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

                // Re-project a restart-safe saved credential before checking the process environment.
                // This is idempotent for environment-backed setups and keeps the actual child launch
                // on the same verified CONTROL_PLANE_API_KEY path as normal autostart.
                McpPersistentUserSettings.ApplyStartupSecretsToProcessEnvironment();
                var hasRuntimeKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONTROL_PLANE_API_KEY"))
                                    || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
                if (!hasRuntimeKey)
                {
                    MessageBox.Show(
                        "Không có Runtime API key đã lưu trong Windows Credential Manager hoặc biến môi trường Windows. Nhập key và bấm Khởi động để lưu/xác minh trước khi restart tunnel.",
                        "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                McpOpenAiSecureTunnelManager.StopForHostShutdown();
                string message;
                var ok = McpOpenAiSecureTunnelManager.Start(McpOpenAiSecureTunnelManager.SavedTunnelId, string.Empty, out message);
                if (ok)
                    McpAgentExperience.Success("onboarding", message, "Chờ tunnel-client READY rồi tiếp tục ChatGPT.");
                else
                    McpAgentExperience.Error("onboarding", message, "Kiểm tra saved/environment key, Tunnel ID, trust verification và diagnostics.");
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