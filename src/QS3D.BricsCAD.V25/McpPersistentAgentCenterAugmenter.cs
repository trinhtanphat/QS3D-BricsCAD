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
    /// explicit background-vs-foreground desktop permission UI. Kept separate from transport
    /// diagnostics so tunnel/onboarding work can evolve without widening desktop authority.
    /// </summary>
    internal static class McpPersistentAgentCenterAugmenter
    {
        private const string AgentCenterTitle = "QS3D - ChatGPT MCP Agent Center";
        private const string ResumeDesktopLabel = "Resume desktop";
        private const string PermissionPanelTag = "QS3D_MCP_LOCAL_CONTROL_PERMISSION_PANEL";
        private const string BackgroundModeCheckBoxTag = "QS3D_MCP_BACKGROUND_MODE_CHECKBOX";
        private const string DesktopForegroundToggleTag = "QS3D_MCP_DESKTOP_FOREGROUND_TOGGLE";
        private const string PermissionStatusTag = "QS3D_MCP_LOCAL_CONTROL_PERMISSION_STATUS";
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
                {
                    RefreshDesktopPermissionPanel(panel, button);
                    return;
                }
            }
        }

        private static void RefreshDesktopPermissionPanel(Panel parent, Button resumeButton)
        {
            var permissionPanel = FindTaggedPanel(parent, PermissionPanelTag);
            if (permissionPanel == null)
            {
                permissionPanel = new StackPanel
                {
                    Tag = PermissionPanelTag,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                permissionPanel.Children.Add(new TextBlock
                {
                    Text = "Quyền điều khiển ChatGPT MCP",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 4),
                    TextWrapping = TextWrapping.Wrap
                });

                permissionPanel.Children.Add(new CheckBox
                {
                    Tag = BackgroundModeCheckBoxTag,
                    Content = "MCP chạy nền BricsCAD/API (không chiếm chuột/phím): BẬT",
                    IsChecked = true,
                    IsEnabled = false,
                    Margin = new Thickness(0, 2, 0, 2),
                    ToolTip = "Đây là đường mặc định: direct CAD/QS3D API và same-process BricsCAD background controls không di chuyển chuột hoặc gõ bàn phím của user."
                });

                var foreground = new CheckBox
                {
                    Tag = DesktopForegroundToggleTag,
                    Content = "Cho phép chuột / bàn phím / màn hình user",
                    Margin = new Thickness(0, 2, 0, 2),
                    ToolTip = "Bật fallback foreground cho desktop_* khi thật sự cần. Quyền này vẫn chịu confirmMutation/confirmSensitiveRead, local consent và Esc×2 Emergency Stop."
                };
                foreground.Click += (_, __) => ToggleDesktopForegroundAccess();
                permissionPanel.Children.Add(foreground);

                permissionPanel.Children.Add(new TextBlock
                {
                    Text = "Background là đường mặc định; foreground chỉ dùng khi thật sự cần thao tác desktop.",
                    Margin = new Thickness(20, 2, 0, 2),
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.78
                });

                permissionPanel.Children.Add(new TextBlock
                {
                    Tag = PermissionStatusTag,
                    Margin = new Thickness(20, 2, 0, 0),
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.78
                });

                InsertAfter(parent, resumeButton, permissionPanel);
            }

            var background = FindTaggedCheckBox(permissionPanel, BackgroundModeCheckBoxTag);
            if (background != null)
            {
                background.IsChecked = true;
                background.Content = "MCP chạy nền BricsCAD/API (không chiếm chuột/phím): BẬT";
            }

            var foregroundToggle = FindTaggedCheckBox(permissionPanel, DesktopForegroundToggleTag);
            var allowed = McpDesktopControlSession.IsEnabled && IsForegroundFallbackEnabled();
            if (foregroundToggle != null) foregroundToggle.IsChecked = allowed;

            var status = FindTaggedTextBlock(permissionPanel, PermissionStatusTag);
            if (status != null)
            {
                status.Text = allowed
                    ? "Foreground: BẬT · desktop consent " + McpDesktopControlSession.ConsentState + " · Esc×2 để dừng ngay."
                    : "Foreground: TẮT · ChatGPT vẫn dùng CAD/QS3D API và background BricsCAD controls.";
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
                    "Key mới không được dùng cho tunnel trong phiên này; kiểm tra Windows Credential Manager rồi thử lại.");
            }
        }

        private static bool IsForegroundFallbackEnabled()
        {
            try
            {
                var result = McpBackgroundHostRuntime.Call(
                    "bricscad_interaction_policy_get", "{}", null, _ => { });
                return result.IndexOf("\"mode\":\"foreground_fallback\"", StringComparison.Ordinal) >= 0;
            }
            catch { return false; }
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

            McpDesktopControlSession.ResumeFromLocalUser();
            TrySetInteractionPolicy("foreground_fallback");
            McpAgentExperience.Success(
                "desktop-control",
                "Foreground desktop access đã BẬT theo thao tác local của user.",
                "QS3D giữ consent ON trong phiên; Esc ×2, bỏ tick checkbox foreground hoặc đóng BricsCAD sẽ khóa lại.");
        }

        private static void FailClosedForegroundAccess(Exception error)
        {
            try { TrySetInteractionPolicy("background_only"); } catch { }
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
                    "Không đổi được foreground desktop access: " + (error == null ? "unknown error" : error.Message),
                    "QS3D đã fail-closed về desktop OFF/background_only; thử lại từ Agent Center nếu vẫn cần foreground access.");
            }
            catch { }
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

        private static StackPanel? FindTaggedPanel(Panel panel, string tag)
        {
            foreach (UIElement child in panel.Children)
            {
                var stack = child as StackPanel;
                if (stack != null && string.Equals(stack.Tag as string, tag, StringComparison.Ordinal)) return stack;
            }
            return null;
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

        private static TextBlock? FindTaggedTextBlock(Panel panel, string tag)
        {
            foreach (UIElement child in panel.Children)
            {
                var textBlock = child as TextBlock;
                if (textBlock != null && string.Equals(textBlock.Tag as string, tag, StringComparison.Ordinal)) return textBlock;
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
