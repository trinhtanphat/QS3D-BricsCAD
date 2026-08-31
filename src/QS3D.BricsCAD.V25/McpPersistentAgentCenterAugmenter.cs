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
    /// explicit dual BricsCAD control model. Background Control stays preferred/default; Foreground
    /// Control remains a local-user opt-in and never replaces the background path.
    /// </summary>
    internal static class McpPersistentAgentCenterAugmenter
    {
        private const string AgentCenterTitle = "QS3D - ChatGPT MCP Agent Center";
        private const string ResumeDesktopLabel = "Resume desktop";
        private const string PauseDesktopLabel = "Pause desktop";
        private const string EmergencyStopLabel = "EMERGENCY STOP AGENT";
        private const string DesktopForegroundToggleTag = "QS3D_MCP_DESKTOP_FOREGROUND_TOGGLE";
        private const string BackgroundCapabilityCardTag = "QS3D_MCP_BACKGROUND_CAPABILITY_CARD";
        private const string ForegroundCapabilityCardTag = "QS3D_MCP_FOREGROUND_CAPABILITY_CARD";
        private const string BackgroundStatusTextTag = "QS3D_MCP_BACKGROUND_STATUS_TEXT";
        private const string ForegroundStatusTextTag = "QS3D_MCP_FOREGROUND_STATUS_TEXT";
        private const string ResumeSyncHookTag = "QS3D_MCP_FOREGROUND_RESUME_SYNC";
        private const string PauseSyncHookTag = "QS3D_MCP_FOREGROUND_PAUSE_SYNC";
        private const string EmergencySyncHookTag = "QS3D_MCP_FOREGROUND_EMERGENCY_SYNC";
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

            Button? resumeButton = null;
            Button? pauseButton = null;
            Button? emergencyButton = null;
            foreach (var child in snapshot)
            {
                var button = child as Button;
                if (button == null) continue;
                var text = button.Content as string ?? string.Empty;
                if (string.Equals(text, ResumeDesktopLabel, StringComparison.Ordinal)) resumeButton = button;
                else if (string.Equals(text, PauseDesktopLabel, StringComparison.Ordinal)) pauseButton = button;
                else if (string.Equals(text, EmergencyStopLabel, StringComparison.Ordinal)) emergencyButton = button;
            }

            if (resumeButton == null) return;

            RefreshDualControlCards(panel, resumeButton);
            RefreshDesktopForegroundToggle(panel, resumeButton);
            AttachForegroundSynchronization(resumeButton, pauseButton, emergencyButton);
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

        private static void RefreshDualControlCards(Panel panel, Button resumeButton)
        {
            var backgroundCard = FindTaggedElement<Border>(panel, BackgroundCapabilityCardTag);
            if (backgroundCard == null)
            {
                backgroundCard = CreateCapabilityCard(
                    "Thao tác nền · Background Control",
                    "Background control",
                    "AVAILABLE · ưu tiên mặc định",
                    "CAD/QS3D API, bounded command dispatch và same-process BricsCAD UI. Không dùng global mouse/keyboard/focus; UI không hỗ trợ sẽ fail và không tự chuyển sang thao tác trực tiếp.",
                    BackgroundCapabilityCardTag,
                    BackgroundStatusTextTag);
                InsertBefore(panel, resumeButton, backgroundCard);
            }

            var foregroundCard = FindTaggedElement<Border>(panel, ForegroundCapabilityCardTag);
            if (foregroundCard == null)
            {
                foregroundCard = CreateCapabilityCard(
                    "Thao tác trực tiếp · Foreground Control",
                    "Foreground control",
                    "OFF · local Resume required",
                    "Chỉ dùng explicit desktop_* khi thật sự cần thao tác cửa sổ/chuột/bàn phím. Bật Foreground không tắt Background; background vẫn là default route.",
                    ForegroundCapabilityCardTag,
                    ForegroundStatusTextTag);
                InsertBefore(panel, resumeButton, foregroundCard);
            }

            var backgroundStatus = FindTaggedDescendant<TextBlock>(backgroundCard, BackgroundStatusTextTag);
            if (backgroundStatus != null)
            {
                backgroundStatus.Text = "AVAILABLE · ưu tiên mặc định · defaultRoute=background · fallback=explicit_only";
                backgroundStatus.Foreground = Brushes.ForestGreen;
            }

            var foregroundStatus = FindTaggedDescendant<TextBlock>(foregroundCard, ForegroundStatusTextTag);
            if (foregroundStatus != null)
            {
                var policyEnabled = IsForegroundFallbackEnabled();
                var localConsent = McpDesktopControlSession.IsEnabled;
                var available = policyEnabled && localConsent;
                foregroundStatus.Text = available
                    ? "ON · local consent + explicit policy · background vẫn AVAILABLE"
                    : localConsent
                        ? "CONSENT ON · policy OFF · foreground chưa available"
                        : McpDesktopControlSession.ConsentState + " · local Resume required";
                foregroundStatus.Foreground = available ? Brushes.ForestGreen : Brushes.DarkGoldenrod;
            }
        }

        private static Border CreateCapabilityCard(
            string title,
            string rowLabel,
            string rowValue,
            string description,
            string cardTag,
            string statusTag)
        {
            var body = new StackPanel();
            body.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13.5,
                TextWrapping = TextWrapping.Wrap
            });
            var statusRow = new Grid { Margin = new Thickness(0, 7, 0, 5) };
            statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(122) });
            statusRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusRow.Children.Add(new TextBlock
            {
                Text = rowLabel,
                Foreground = Brushes.DimGray,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Top
            });
            var value = new TextBlock
            {
                Text = rowValue,
                Tag = statusTag,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.ForestGreen,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(value, 1);
            statusRow.Children.Add(value);
            body.Children.Add(statusRow);
            body.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = Brushes.DimGray,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap
            });
            return new Border
            {
                Tag = cardTag,
                BorderBrush = SystemColors.ControlDarkBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8),
                Child = body
            };
        }

        private static void AttachForegroundSynchronization(Button resumeButton, Button? pauseButton, Button? emergencyButton)
        {
            if (!string.Equals(resumeButton.Tag as string, ResumeSyncHookTag, StringComparison.Ordinal))
            {
                resumeButton.Tag = ResumeSyncHookTag;
                resumeButton.Click += (_, __) => SynchronizeForegroundEnableFromLocalUser();
            }
            if (pauseButton != null && !string.Equals(pauseButton.Tag as string, PauseSyncHookTag, StringComparison.Ordinal))
            {
                pauseButton.Tag = PauseSyncHookTag;
                pauseButton.Click += (_, __) => SynchronizeForegroundDisableFromLocalUser("Pause desktop");
            }
            if (emergencyButton != null && !string.Equals(emergencyButton.Tag as string, EmergencySyncHookTag, StringComparison.Ordinal))
            {
                emergencyButton.Tag = EmergencySyncHookTag;
                emergencyButton.Click += (_, __) => SynchronizeForegroundDisableFromLocalUser("Emergency Stop");
            }
        }

        private static void SynchronizeForegroundEnableFromLocalUser()
        {
            try
            {
                // The canonical Resume handler runs first and grants local consent. This helper then
                // synchronizes the compatibility policy without changing the preferred background route.
                McpBackgroundHostRuntime.EnableForegroundFromLocalUser();
                McpAgentExperience.Success(
                    "desktop-control",
                    "Foreground Control đã ON; Background Control vẫn AVAILABLE và là route ưu tiên.",
                    "Dùng desktop_* explicit-only khi background CAD/QS3D/same-process UI không đủ.");
            }
            catch (Exception ex)
            {
                FailClosedForegroundAccess(ex);
            }
        }

        private static void SynchronizeForegroundDisableFromLocalUser(string source)
        {
            try
            {
                McpBackgroundHostRuntime.DisableForegroundFromLocalUser();
                McpAgentExperience.Info(
                    "desktop-control",
                    source + " đã tắt Foreground Control; Background Control vẫn AVAILABLE.",
                    string.Empty,
                    "Tiếp tục ưu tiên CAD/QS3D API, command dispatch và same-process UI.");
            }
            catch (Exception ex)
            {
                FailClosedForegroundAccess(ex);
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
                    McpBackgroundHostRuntime.DisableForegroundFromLocalUser();
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
            McpBackgroundHostRuntime.EnableForegroundFromLocalUser();
            McpAgentExperience.Success(
                "desktop-control",
                "Foreground desktop access đã BẬT theo thao tác local của user.",
                "QS3D giữ consent ON trong phiên; Esc ×2, nút toggle OFF hoặc đóng BricsCAD sẽ khóa lại.");
        }

        private static void FailClosedForegroundAccess(Exception error)
        {
            try { TrySetInteractionPolicy("background_only"); } catch { }
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
                    "Không đổi được foreground desktop access: " + (error == null ? "unknown error" : error.Message),
                    "QS3D đã fail-closed về desktop OFF/background_only; Background Control vẫn available.");
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

        private static T? FindTaggedElement<T>(Panel panel, string tag) where T : FrameworkElement
        {
            foreach (UIElement child in panel.Children)
            {
                var element = child as T;
                if (element != null && string.Equals(element.Tag as string, tag, StringComparison.Ordinal)) return element;
            }
            return null;
        }

        private static T? FindTaggedDescendant<T>(DependencyObject root, string tag) where T : FrameworkElement
        {
            var element = root as T;
            if (element != null && string.Equals(element.Tag as string, tag, StringComparison.Ordinal)) return element;
            try
            {
                var count = VisualTreeHelper.GetChildrenCount(root);
                for (var i = 0; i < count; i++)
                {
                    var found = FindTaggedDescendant<T>(VisualTreeHelper.GetChild(root, i), tag);
                    if (found != null) return found;
                }
            }
            catch { }
            return null;
        }

        private static Button? FindTaggedButton(Panel panel, string tag)
        {
            return FindTaggedElement<Button>(panel, tag);
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
