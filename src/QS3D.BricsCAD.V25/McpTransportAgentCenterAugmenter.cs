using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Adds transport-hardening affordances to the existing Agent Center without changing the
    /// public MCP surface: cloudflared busy/progress/recovery state, bounded OpenAI tunnel
    /// diagnostics, secure Runtime API-key persistence, and a local foreground-access toggle.
    /// The bootstrapper owns the dynamic Cloudflare cancel button so this augmenter never creates
    /// a second competing cancel control.
    /// </summary>
    internal static class McpTransportAgentCenterAugmenter
    {
        private const string AgentCenterTitle = "QS3D - ChatGPT MCP Agent Center";
        private const string InstallCloudflaredLabel = "Cài / cập nhật Cloudflare Tunnel";
        private const string OpenAiAdminAnchorLabel = "Mở tunnel-client UI";
        private const string ResumeDesktopLabel = "Resume desktop";
        private const string CloudflareRecoveryTag = "QS3D_MCP_CLOUDFLARED_RECOVERY";
        private const string CloudflareStatusTag = "QS3D_MCP_CLOUDFLARED_STATUS";
        private const string OpenAiCopyDiagnosticsTag = "QS3D_MCP_OPENAI_COPY_DIAGNOSTICS";
        private const string OpenAiOpenLogsTag = "QS3D_MCP_OPENAI_OPEN_LOGS";
        private const string OpenAiRestartTag = "QS3D_MCP_OPENAI_RESTART";
        private const string OpenAiStatusTag = "QS3D_MCP_OPENAI_DIAGNOSTIC_STATUS";
        private const string DesktopForegroundToggleTag = "QS3D_MCP_DESKTOP_FOREGROUND_TOGGLE";
        private const string RuntimeKeyCaptureTag = "QS3D_MCP_RUNTIME_KEY_CAPTURE";
        private const string WingetRecoveryCommand = "winget install --id Cloudflare.cloudflared --source winget";
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
            // Local-host renewal removes the historical surprise 10-minute expiry while keeping
            // the fail-safe expiry if the augmenter itself stops unexpectedly.
            try { McpDesktopControlSession.RenewConsentLeaseFromLocalHost(); } catch { }

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
            var passwordBox = root as PasswordBox;
            if (passwordBox != null) AttachRuntimeKeyCapture(passwordBox);

            var textBlock = root as TextBlock;
            if (textBlock != null) RefreshConsentCopy(textBlock);

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
                if (string.Equals(text, ResumeDesktopLabel, StringComparison.Ordinal))
                {
                    RefreshDesktopForegroundToggle(panel, button);
                }
            }
        }

        private static void AttachRuntimeKeyCapture(PasswordBox passwordBox)
        {
            if (string.Equals(passwordBox.Tag as string, RuntimeKeyCaptureTag, StringComparison.Ordinal)) return;
            passwordBox.Tag = RuntimeKeyCaptureTag;
            passwordBox.LostKeyboardFocus += (_, __) => CaptureRuntimeKey(passwordBox);
        }

        private static void CaptureRuntimeKey(PasswordBox passwordBox)
        {
            try
            {
                var value = (passwordBox.Password ?? string.Empty).Trim();
                if (value.Length == 0) return;
                McpPersistentUserSettings.SaveOpenAiRuntimeApiKey(value);
                McpAgentExperience.Info(
                    "onboarding",
                    "Runtime API key đã được lưu an toàn trong Windows Credential Manager cho user hiện tại.",
                    string.Empty,
                    "QS3D sẽ tự nạp credential này khi BricsCAD khởi động lại; key không được ghi plaintext vào file cấu hình/log.");
            }
            catch (Exception ex)
            {
                McpAgentExperience.Error(
                    "onboarding",
                    "Không lưu được Runtime API key vào Windows Credential Manager: " + ex.Message,
                    "Tunnel vẫn có thể chạy trong phiên hiện tại; kiểm tra Windows Credential Manager rồi thử lại.");
            }
        }

        private static void RefreshConsentCopy(TextBlock textBlock)
        {
            var text = textBlock.Text ?? string.Empty;
            if (text.IndexOf("Consent tự hết hạn sau 10 phút", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                textBlock.Text = text.Replace(
                    "Consent tự hết hạn sau 10 phút không có desktop action mới.",
                    "Khi user bật foreground access, QS3D tự renew consent trong suốt phiên BricsCAD; permission vẫn reset OFF sau restart.");
            }
            else if (text.IndexOf("Idle timeout 10 phút", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                textBlock.Text = text.Replace("Idle timeout 10 phút", "QS3D auto-renew consent trong phiên");
            }
        }

        private static void RefreshDesktopForegroundToggle(Panel panel, Button resumeButton)
        {
            var toggle = FindTaggedButton(panel, DesktopForegroundToggleTag);
            if (toggle == null)
            {
                toggle = CloneActionButton(resumeButton, string.Empty, DesktopForegroundToggleTag);
                toggle.Click += (_, __) => ToggleDesktopForegroundAccess();
                InsertAfter(panel, resumeButton, toggle);
            }

            var allowed = McpDesktopControlSession.IsEnabled && IsForegroundFallbackEnabled();
            toggle.Content = allowed
                ? "Cho phép chuột / bàn phím / màn hình user: BẬT"
                : "Cho phép chuột / bàn phím / màn hình user: TẮT";
        }

        private static bool IsForegroundFallbackEnabled()
        {
            try
            {
                var result = McpBackgroundHostRuntime.Call(
                    "bricscad_interaction_policy_get", "{}", null, null);
                return result.IndexOf("\"mode\":\"foreground_fallback\"", StringComparison.Ordinal) >= 0;
            }
            catch { return false; }
        }

        private static void ToggleDesktopForegroundAccess()
        {
            var currentlyAllowed = McpDesktopControlSession.IsEnabled && IsForegroundFallbackEnabled();
            if (currentlyAllowed)
            {
                try
                {
                    TrySetInteractionPolicy("background_only");
                }
                finally
                {
                    // Disable desktop-wide reads/input without stopping API-first CAD automation.
                    McpDesktopControlSession.DisableForegroundAccessFromLocalUser(
                        "User đã tắt quyền dùng chuột / bàn phím / màn hình desktop; background CAD/API vẫn được phép chạy.");
                }
                McpAgentExperience.Info(
                    "desktop-control",
                    "Foreground desktop access đã TẮT; background_only đang được ưu tiên.",
                    string.Empty,
                    "ChatGPT vẫn có thể dùng CAD/QS3D API, bounded command dispatch và same-process BricsCAD UI controls mà không chiếm chuột/bàn phím.");
                return;
            }

            try
            {
                McpDesktopControlSession.ResumeFromLocalUser();
                TrySetInteractionPolicy("foreground_fallback");
                McpAgentExperience.Success(
                    "desktop-control",
                    "Foreground desktop access đã BẬT theo thao tác local của user.",
                    "QS3D tự renew consent trong phiên; Esc ×2, nút toggle OFF hoặc đóng BricsCAD sẽ khóa lại.");
            }
            catch
            {
                try
                {
                    McpDesktopControlSession.DisableForegroundAccessFromLocalUser(
                        "Không bật hoàn chỉnh được foreground fallback nên QS3D đã fail-closed về desktop OFF.");
                }
                catch { }
                throw;
            }
        }

        private static void TrySetInteractionPolicy(string mode)
        {
            McpEmbeddedServer.EnsureStarted();
            var payload = "{\"mode\":\"" + mode + "\",\"confirmMutation\":true}";
            McpLocalAgentClient.CallOne(
                McpEmbeddedServer.Endpoint,
                6000,
                "bricscad_interaction_policy_set",
                payload);
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
                restart.Click += (_, __) => RestartOpenAiTunnelFromSavedOrEnvironmentKey();
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
                   + " · saved credential=" + (McpPersistentUserSettings.HasSavedOpenAiRuntimeApiKey ? "YES" : "NO")
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

        private static void RestartOpenAiTunnelFromSavedOrEnvironmentKey()
        {
            try
            {
                if (McpTransportCoordinator.SelectedProvider != McpTransportProvider.OpenAiSecureTunnel)
                {
                    MessageBox.Show("Hãy chọn OpenAI Secure Tunnel trước khi restart.", "QS3D MCP", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                McpPersistentUserSettings.ApplyStartupSecretsToProcessEnvironment();
                var hasRuntimeKey = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CONTROL_PLANE_API_KEY"))
                                    || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
                if (!hasRuntimeKey)
                {
                    MessageBox.Show(
                        "Chưa có Runtime API key trong Windows Credential Manager hoặc environment. Nhập key một lần ở Agent Center; QS3D sẽ lưu an toàn cho các lần restart sau.",
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
