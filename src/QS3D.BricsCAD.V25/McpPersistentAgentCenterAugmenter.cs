using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Local-only Agent Center augmentation for restart-safe Runtime API-key persistence and the
    /// explicit Background/Foreground BricsCAD control split. Kept separate from transport
    /// diagnostics so concurrent transport/tunnel UI hardening can evolve without overwriting
    /// these local permission controls.
    /// </summary>
    internal static class McpPersistentAgentCenterAugmenter
    {
        private const string AgentCenterTitle = "QS3D - ChatGPT MCP Agent Center";
        private const string ResumeDesktopLabel = "Resume desktop";
        private const string PauseDesktopLabel = "Pause desktop";
        private const string EmergencyStopLabel = "EMERGENCY STOP AGENT";
        private const string DualControlPanelTag = "QS3D_MCP_DUAL_CONTROL_PANEL";
        private const string BackgroundControlCheckTag = "QS3D_MCP_BACKGROUND_CONTROL_CHECK";
        private const string ForegroundControlCheckTag = "QS3D_MCP_FOREGROUND_CONTROL_CHECK";
        private const string BackgroundSummaryTag = "QS3D_MCP_BACKGROUND_CONTROL_SUMMARY";
        private const string ForegroundSummaryTag = "QS3D_MCP_FOREGROUND_CONTROL_SUMMARY";
        private const string RuntimeKeyCaptureTag = "QS3D_MCP_RUNTIME_KEY_CAPTURE";
        private const string RuntimeKeyLabelPrefix = "Runtime API key";
        private const string BackgroundOnLabel = "Background Control · BricsCAD/API trong nền: BẬT";
        private const string ForegroundOnLabel = "Foreground Control · chuột / bàn phím / màn hình user: BẬT";
        private const string ForegroundOffLabel = "Foreground Control · chuột / bàn phím / màn hình user: TẮT";
        private const string ForegroundPermissionToolTip = "Cho phép chuột / bàn phím / màn hình user";
        private static readonly object Sync = new object();
        private static readonly HashSet<Button> DisableSyncButtons = new HashSet<Button>();
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
                DisableSyncButtons.Clear();
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
                // Local UI augmentation is optional and must never make Agent Center/BricsCAD fail.
            }
        }

        private static void RefreshTree(DependencyObject root)
        {
            var passwordBox = root as PasswordBox;
            if (passwordBox != null) AttachRuntimeKeyCapture(passwordBox);

            var textBlock = root as TextBlock;
            if (textBlock != null && (textBlock.Text ?? string.Empty).StartsWith(RuntimeKeyLabelPrefix, StringComparison.Ordinal))
            {
                textBlock.Text = "Runtime API key · lưu an toàn trong Windows Credential Manager cho user hiện tại; để trống nếu saved/environment key đã có";
            }

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

            Button? resumeButton = null;
            foreach (var child in snapshot)
            {
                var button = child as Button;
                if (button == null) continue;
                var text = button.Content as string ?? string.Empty;
                if (string.Equals(text, ResumeDesktopLabel, StringComparison.Ordinal))
                {
                    resumeButton = button;
                }
                else if (string.Equals(text, PauseDesktopLabel, StringComparison.Ordinal)
                         || string.Equals(text, EmergencyStopLabel, StringComparison.Ordinal))
                {
                    WireDisableForegroundSync(button);
                }
            }

            if (resumeButton == null) return;
            RefreshDualControlPanel(panel, resumeButton);
        }

        private static void WireDisableForegroundSync(Button button)
        {
            lock (Sync)
            {
                if (!DisableSyncButtons.Add(button)) return;
            }

            button.Click += (_, __) =>
            {
                try { McpBackgroundHostRuntime.DisableForegroundFromLocalUser(); } catch { }
            };
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
                    "Key mới không được dùng cho tunnel trong phiên này; kiểm tra Windows Credential Manager rồi thử lại.");
            }
        }

        private static void RefreshDualControlPanel(Panel panel, Button resumeButton)
        {
            if (!McpDesktopControlSession.IsEnabled && McpBackgroundHostRuntime.IsForegroundPolicyEnabled)
            {
                // Esc x2, Pause/Emergency, or another local safety path may revoke desktop
                // consent independently. Never leave a stale foreground policy armed.
                McpBackgroundHostRuntime.DisableForegroundFromLocalUser();
            }

            var controlPanel = FindTaggedPanel(panel, DualControlPanelTag);
            if (controlPanel == null)
            {
                controlPanel = new StackPanel
                {
                    Tag = DualControlPanelTag,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var background = CreatePermissionCheckBox(BackgroundControlCheckTag, BackgroundOnLabel, resumeButton);
                background.IsChecked = true;
                background.IsEnabled = false;
                controlPanel.Children.Add(background);
                controlPanel.Children.Add(CreateSummaryText(BackgroundSummaryTag, resumeButton));

                var foreground = CreatePermissionCheckBox(ForegroundControlCheckTag, ForegroundOffLabel, resumeButton);
                foreground.ToolTip = ForegroundPermissionToolTip + " khi cần thao tác trực tiếp; Background Control vẫn chạy.";
                foreground.Click += (_, __) => ToggleDesktopForegroundAccess();
                controlPanel.Children.Add(foreground);
                controlPanel.Children.Add(CreateSummaryText(ForegroundSummaryTag, resumeButton));

                InsertBefore(panel, resumeButton, controlPanel);
            }

            var backgroundCheck = FindTaggedCheckBox(controlPanel, BackgroundControlCheckTag);
            if (backgroundCheck != null)
            {
                backgroundCheck.IsChecked = true;
                backgroundCheck.IsEnabled = false;
                backgroundCheck.Content = BackgroundOnLabel;
            }

            var foregroundCheck = FindTaggedCheckBox(controlPanel, ForegroundControlCheckTag);
            if (foregroundCheck != null)
            {
                var available = McpBackgroundHostRuntime.IsForegroundAvailable;
                foregroundCheck.IsChecked = available;
                foregroundCheck.Content = available ? ForegroundOnLabel : ForegroundOffLabel;
            }

            var backgroundText = FindTaggedText(controlPanel, BackgroundSummaryTag);
            if (backgroundText != null)
            {
                backgroundText.Text =
                    "Thao tác nền · Background Control\n"
                    + "AVAILABLE · ưu tiên mặc định · cad_*/qs3d_*/bounded command/same-process UI; không chiếm global mouse/keyboard/focus/màn hình user và không tự chuyển sang thao tác trực tiếp.";
            }

            var foregroundText = FindTaggedText(controlPanel, ForegroundSummaryTag);
            if (foregroundText != null)
            {
                foregroundText.Text = McpBackgroundHostRuntime.IsForegroundAvailable
                    ? "Thao tác trực tiếp · Foreground Control\nON · desktop_* có thể dùng chuột/bàn phím/focus/màn hình user theo local consent. Background Control vẫn khả dụng."
                    : "Thao tác trực tiếp · Foreground Control\nOFF · Background Control vẫn khả dụng; chỉ checkbox local này mới cấp quyền desktop trực tiếp.";
            }
        }

        private static CheckBox CreatePermissionCheckBox(string tag, string content, Button styleSource)
        {
            return new CheckBox
            {
                Tag = tag,
                Content = content,
                Margin = new Thickness(0, 2, 0, 4),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = styleSource.FontSize,
                FontWeight = FontWeights.SemiBold,
                Foreground = styleSource.Foreground,
                IsThreeState = false
            };
        }

        private static TextBlock CreateSummaryText(string tag, Button styleSource)
        {
            return new TextBlock
            {
                Tag = tag,
                TextWrapping = TextWrapping.Wrap,
                Foreground = styleSource.Foreground,
                Margin = new Thickness(22, 0, 0, 8)
            };
        }

        private static void ToggleDesktopForegroundAccess()
        {
            try
            {
                ToggleDesktopForegroundAccessCore();
            }
            catch (Exception ex)
            {
                FailClosedForegroundAccess(ex);
            }
        }

        private static void ToggleDesktopForegroundAccessCore()
        {
            if (McpBackgroundHostRuntime.IsForegroundAvailable)
            {
                try
                {
                    McpBackgroundHostRuntime.DisableForegroundFromLocalUser();
                }
                finally
                {
                    McpDesktopControlSession.DisableForegroundAccessFromLocalUser(
                        "User đã tắt quyền dùng chuột / bàn phím / màn hình desktop; background CAD/API vẫn được phép chạy.");
                }
                McpAgentExperience.Info(
                    "desktop-control",
                    "Foreground Control đã TẮT; Background Control vẫn AVAILABLE và được ưu tiên mặc định.",
                    string.Empty,
                    "ChatGPT vẫn có thể dùng CAD/QS3D API, bounded command dispatch và same-process BricsCAD UI controls mà không chiếm chuột/bàn phím/màn hình user.");
                return;
            }

            McpDesktopControlSession.ResumeFromLocalUser();
            try
            {
                McpBackgroundHostRuntime.EnableForegroundFromLocalUser();
            }
            catch
            {
                try { McpBackgroundHostRuntime.DisableForegroundFromLocalUser(); } catch { }
                try
                {
                    McpDesktopControlSession.DisableForegroundAccessFromLocalUser(
                        "Foreground policy synchronization failed; fail-closed về desktop OFF.");
                }
                catch { }
                throw;
            }

            McpAgentExperience.Success(
                "desktop-control",
                "Foreground Control đã BẬT theo checkbox local của user; Background Control vẫn khả dụng.",
                "Esc x2, checkbox OFF, Pause/Emergency hoặc đóng BricsCAD sẽ khóa foreground lại.");
        }

        private static void FailClosedForegroundAccess(Exception error)
        {
            try { McpBackgroundHostRuntime.DisableForegroundFromLocalUser(); } catch { }
            try
            {
                McpDesktopControlSession.DisableForegroundAccessFromLocalUser(
                    "Foreground checkbox gặp lỗi nên QS3D đã fail-closed về desktop OFF.");
            }
            catch { }
            try
            {
                McpAgentExperience.Error(
                    "desktop-control",
                    "Không đổi được Foreground Control: " + (error == null ? "unknown error" : error.Message),
                    "QS3D đã fail-closed về foreground OFF; Background Control vẫn là đường mặc định. Thử lại từ checkbox Agent Center nếu vẫn cần foreground access.");
            }
            catch { }
        }

        private static CheckBox? FindTaggedCheckBox(Panel panel, string tag)
        {
            foreach (UIElement child in panel.Children)
            {
                var checkBox = child as CheckBox;
                if (checkBox != null && string.Equals(checkBox.Tag as string, tag, StringComparison.Ordinal)) return checkBox;
            }
            return null;
        }

        private static StackPanel? FindTaggedPanel(Panel panel, string tag)
        {
            foreach (UIElement child in panel.Children)
            {
                var stack = child as StackPanel;
                if (stack != null && string.Equals(stack.Tag as string, tag, StringComparison.Ordinal)) return stack;
            }
            return null;
        }

        private static TextBlock? FindTaggedText(Panel panel, string tag)
        {
            foreach (UIElement child in panel.Children)
            {
                var text = child as TextBlock;
                if (text != null && string.Equals(text.Tag as string, tag, StringComparison.Ordinal)) return text;
            }
            return null;
        }

        private static void InsertBefore(Panel panel, UIElement anchor, UIElement value)
        {
            var index = panel.Children.IndexOf(anchor);
            if (index < 0) panel.Children.Add(value);
            else panel.Children.Insert(index, value);
        }
    }
}
