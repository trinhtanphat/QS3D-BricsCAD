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
        private const string DesktopForegroundToggleTag = "QS3D_MCP_DESKTOP_FOREGROUND_TOGGLE";
        private const string DualControlSummaryTag = "QS3D_MCP_DUAL_CONTROL_SUMMARY";
        private const string BackgroundSummaryTag = "QS3D_MCP_BACKGROUND_CONTROL_SUMMARY";
        private const string ForegroundSummaryTag = "QS3D_MCP_FOREGROUND_CONTROL_SUMMARY";
        private const string RuntimeKeyCaptureTag = "QS3D_MCP_RUNTIME_KEY_CAPTURE";
        private const string RuntimeKeyLabelPrefix = "Runtime API key · chỉ giữ trong RAM";
        private const string ForegroundOnLabel = "Foreground Control · chuột / bàn phím / màn hình user: BẬT";
        private const string ForegroundOffLabel = "Foreground Control · chuột / bàn phím / màn hình user: TẮT";
        private static readonly object Sync = new object();
        private static readonly HashSet<Button> ResumeSyncButtons = new HashSet<Button>();
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
                ResumeSyncButtons.Clear();
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
                    WireResumeForegroundSync(button);
                }
                else if (string.Equals(text, PauseDesktopLabel, StringComparison.Ordinal)
                         || string.Equals(text, EmergencyStopLabel, StringComparison.Ordinal))
                {
                    WireDisableForegroundSync(button);
                }
            }

            if (resumeButton == null) return;
            RefreshDualControlSummary(panel, resumeButton);
            RefreshDesktopForegroundToggle(panel, resumeButton);
        }

        private static void WireResumeForegroundSync(Button button)
        {
            lock (Sync)
            {
                if (!ResumeSyncButtons.Add(button)) return;
            }

            // The canonical Agent Center handler was attached when the button was created, before
            // this augmenter discovers it. Therefore local desktop consent is resumed first, then
            // this handler synchronizes the explicit Foreground Control policy gate.
            button.Click += (_, __) =>
            {
                try
                {
                    McpBackgroundHostRuntime.EnableForegroundFromLocalUser();
                }
                catch (Exception ex)
                {
                    FailClosedForegroundAccess(ex);
                }
            };
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

        private static void RefreshDualControlSummary(Panel panel, Button resumeButton)
        {
            var summary = FindTaggedPanel(panel, DualControlSummaryTag);
            if (summary == null)
            {
                summary = new StackPanel
                {
                    Tag = DualControlSummaryTag,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                summary.Children.Add(CreateSummaryText(BackgroundSummaryTag, resumeButton));
                summary.Children.Add(CreateSummaryText(ForegroundSummaryTag, resumeButton));
                InsertBefore(panel, resumeButton, summary);
            }

            var backgroundText = FindTaggedText(summary, BackgroundSummaryTag);
            if (backgroundText != null)
            {
                backgroundText.Text =
                    "Thao tác nền · Background Control\n"
                    + "AVAILABLE · ưu tiên mặc định · cad_*/qs3d_*/bounded command/same-process UI; không chiếm global mouse/keyboard/focus và không tự chuyển sang thao tác trực tiếp.";
            }

            var foregroundText = FindTaggedText(summary, ForegroundSummaryTag);
            if (foregroundText != null)
            {
                foregroundText.Text = McpBackgroundHostRuntime.IsForegroundAvailable
                    ? "Thao tác trực tiếp · Foreground Control\nON · explicit desktop input; có thể dùng chuột/bàn phím/focus của user theo local consent. Background Control vẫn khả dụng."
                    : "Thao tác trực tiếp · Foreground Control\nOFF · Background Control vẫn khả dụng; chỉ user local mới bật quyền desktop trực tiếp.";
            }
        }

        private static TextBlock CreateSummaryText(string tag, Button styleSource)
        {
            return new TextBlock
            {
                Tag = tag,
                TextWrapping = TextWrapping.Wrap,
                Foreground = styleSource.Foreground,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            };
        }

        private static void RefreshDesktopForegroundToggle(Panel panel, Button resumeButton)
        {
            if (!McpDesktopControlSession.IsEnabled && McpBackgroundHostRuntime.IsForegroundPolicyEnabled)
            {
                // Consent may have been revoked by Esc ×2 or another local safety path. Never
                // leave a stale foreground policy armed after local consent disappears.
                McpBackgroundHostRuntime.DisableForegroundFromLocalUser();
            }

            var toggle = FindTaggedButton(panel, DesktopForegroundToggleTag);
            if (toggle == null)
            {
                toggle = CloneActionButton(resumeButton, string.Empty, DesktopForegroundToggleTag);
                toggle.Click += (_, __) => ToggleDesktopForegroundAccess();
                InsertAfter(panel, resumeButton, toggle);
            }

            toggle.Content = McpBackgroundHostRuntime.IsForegroundAvailable
                ? ForegroundOnLabel
                : ForegroundOffLabel;
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
                    "ChatGPT vẫn có thể dùng CAD/QS3D API, bounded command dispatch và same-process BricsCAD UI controls mà không chiếm chuột/bàn phím.");
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
                "Foreground Control đã BẬT theo thao tác local của user; Background Control vẫn khả dụng.",
                "QS3D giữ consent theo policy hiện tại; Esc ×2, toggle OFF, Pause/Emergency hoặc đóng BricsCAD sẽ khóa foreground lại.");
        }

        private static void FailClosedForegroundAccess(Exception error)
        {
            try { McpBackgroundHostRuntime.DisableForegroundFromLocalUser(); } catch { }
            try
            {
                McpDesktopControlSession.DisableForegroundAccessFromLocalUser(
                    "Foreground toggle gặp lỗi nên QS3D đã fail-closed về desktop OFF.");
            }
            catch { }
            try
            {
                McpAgentExperience.Error(
                    "desktop-control",
                    "Không đổi được Foreground Control: " + (error == null ? "unknown error" : error.Message),
                    "QS3D đã fail-closed về foreground OFF; Background Control vẫn là đường mặc định. Thử lại từ Agent Center nếu vẫn cần foreground access.");
            }
            catch { }
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

        private static void InsertBefore(Panel panel, UIElement anchor, UIElement value)
        {
            var index = panel.Children.IndexOf(anchor);
            if (index < 0) panel.Children.Add(value);
            else panel.Children.Insert(index, value);
        }

        private static void InsertAfter(Panel panel, UIElement anchor, UIElement value)
        {
            var index = panel.Children.IndexOf(anchor);
            if (index < 0 || index + 1 >= panel.Children.Count) panel.Children.Add(value);
            else panel.Children.Insert(index + 1, value);
        }
    }
}
