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
    /// foreground desktop fallback toggle. Kept separate from transport diagnostics so concurrent
    /// transport/tunnel UI hardening can evolve without overwriting these local permission controls.
    /// </summary>
    internal static class McpPersistentAgentCenterAugmenter
    {
        private const string AgentCenterTitle = "QS3D - ChatGPT MCP Agent Center";
        private const string ResumeDesktopLabel = "Resume desktop";
        private const string DesktopForegroundToggleTag = "QS3D_MCP_DESKTOP_FOREGROUND_TOGGLE";
        private const string RuntimeKeyCaptureTag = "QS3D_MCP_RUNTIME_KEY_CAPTURE";
        private const string RuntimeKeyLabelPrefix = "Runtime API key · chỉ giữ trong RAM";
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
            foreach (var child in snapshot)
            {
                var button = child as Button;
                if (button == null) continue;
                var text = button.Content as string ?? string.Empty;
                if (string.Equals(text, ResumeDesktopLabel, StringComparison.Ordinal))
                    RefreshDesktopForegroundToggle(panel, button);
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
                    "QS3D giữ consent ON trong phiên; Esc ×2, nút toggle OFF hoặc đóng BricsCAD sẽ khóa lại.");
            }
            catch (Exception ex)
            {
                try { TrySetInteractionPolicy("background_only"); } catch { }
                try
                {
                    McpDesktopControlSession.DisableForegroundAccessFromLocalUser(
                        "Không bật hoàn chỉnh được foreground fallback nên QS3D đã fail-closed về desktop OFF.");
                }
                catch { }
                try
                {
                    McpAgentExperience.Error(
                        "desktop-control",
                        "Không bật được foreground desktop access: " + ex.Message,
                        "QS3D đã fail-closed về desktop OFF/background_only; thử lại từ Agent Center nếu vẫn cần foreground access.");
                }
                catch { }
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

        private static Button? FindTaggedButton(Panel panel, string tag)
        {
            foreach (UIElement child in panel.Children)
            {
                var button = child as Button;
                if (button != null && string.Equals(button.Tag as string, tag, StringComparison.Ordinal)) return button;
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

        private static void InsertAfter(Panel panel, UIElement anchor, UIElement value)
        {
            var index = panel.Children.IndexOf(anchor);
            if (index < 0 || index + 1 >= panel.Children.Count) panel.Children.Add(value);
            else panel.Children.Insert(index + 1, value);
        }
    }
}
