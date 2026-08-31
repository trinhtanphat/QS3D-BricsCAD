using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Local-only approval boundary for desktop-wide MCP input/sensitive reads.
    /// Consent cannot be enabled through MCP. The local Agent Center host may renew an enabled
    /// consent lease so long-running sessions do not surprise-expire; Esc x2, explicit OFF and
    /// process shutdown remain authoritative revocation boundaries.
    /// </summary>
    internal static class McpDesktopControlSession
    {
        internal static readonly TimeSpan DoubleEscapeWindow = TimeSpan.FromMilliseconds(1200);
        internal static readonly TimeSpan ConsentIdleTimeout = TimeSpan.FromMinutes(10);
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_ESCAPE = 0x1B;
        private const uint LLKHF_INJECTED = 0x10;

        private static readonly object Sync = new object();
        private static readonly LowLevelKeyboardProc KeyboardCallback = KeyboardHookCallback;
        private static bool _enabled;
        private static string _consentState = "OFF";
        private static long _consentGeneration;
        private static DateTime _idleDeadlineUtc = DateTime.MinValue;
        private static IntPtr _keyboardHook;
        private static DateTime _lastPhysicalEscapeUtc = DateTime.MinValue;
        private static int _activeScopes;
        private static string _activeTool = string.Empty;
        private static string _activeActionId = string.Empty;
        private static Dispatcher? _dispatcher;
        private static McpDesktopControlOverlayWindow? _overlay;

        public static bool IsEnabled
        {
            get
            {
                ExpireConsentIfIdle();
                lock (Sync) return _enabled;
            }
        }

        public static string ConsentState
        {
            get
            {
                ExpireConsentIfIdle();
                lock (Sync) return _consentState;
            }
        }

        public static TimeSpan IdleRemaining
        {
            get
            {
                ExpireConsentIfIdle();
                lock (Sync)
                {
                    if (!_enabled || _idleDeadlineUtc == DateTime.MinValue) return TimeSpan.Zero;
                    var remaining = _idleDeadlineUtc - DateTime.UtcNow;
                    return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                }
            }
        }

        public static string ActiveTool { get { lock (Sync) return _activeTool; } }
        public static string ActiveActionId { get { lock (Sync) return _activeActionId; } }

        public static void EnableFromLocalUser()
        {
            ResumeFromLocalUser();
        }

        public static void ResumeFromLocalUser()
        {
            ExpireConsentIfIdle();
            lock (Sync)
            {
                if (_enabled)
                {
                    _idleDeadlineUtc = DateTime.UtcNow + ConsentIdleTimeout;
                    return;
                }
            }

            // The emergency hook must exist before consent becomes usable.
            EnsureKeyboardHook();
            lock (Sync)
            {
                _enabled = true;
                _consentState = "ON";
                unchecked { _consentGeneration++; }
                _idleDeadlineUtc = DateTime.UtcNow + ConsentIdleTimeout;
                _lastPhysicalEscapeUtc = DateTime.MinValue;
            }

            if (McpCadAgentRuntime.AutomationStopped && McpEmbeddedServer.IsRunning)
            {
                try
                {
                    McpLocalAgentClient.CallOne(
                        McpEmbeddedServer.Endpoint,
                        6000,
                        "cad_agent_resume",
                        "{\"confirmMutation\":true}");
                }
                catch (Exception ex)
                {
                    lock (Sync)
                    {
                        _enabled = false;
                        _consentState = "PAUSED";
                        _idleDeadlineUtc = DateTime.MinValue;
                        unchecked { _consentGeneration++; }
                    }
                    ReleaseKeyboardHook();
                    throw new InvalidOperationException("Không resume được MCP Agent khi bật quyền desktop: " + ex.Message, ex);
                }
            }

            McpAgentExperience.Success(
                "desktop-control",
                "User đã Resume desktop control cho phiên BricsCAD hiện tại.",
                "QS3D local host sẽ tự renew consent trong phiên; Esc ×2, toggle OFF hoặc đóng BricsCAD để dừng ngay.");
        }

        /// <summary>
        /// Local-process keepalive only. This is deliberately not exposed through MCP, so a remote
        /// caller cannot revive a permission that the user turned off. The short lease remains a
        /// fail-safe if the local UI augmenter/watchdog itself stops running.
        /// </summary>
        public static void RenewConsentLeaseFromLocalHost()
        {
            lock (Sync)
            {
                if (_enabled) _idleDeadlineUtc = DateTime.UtcNow + ConsentIdleTimeout;
            }
        }

        public static void PauseFromLocalUser(string reason)
        {
            StopSession(reason, true, false, "PAUSED");
        }

        public static void DisableFromLocalUser(string reason)
        {
            StopSession(reason, true, false, "OFF");
        }

        /// <summary>
        /// Revokes desktop-wide reads/input while leaving the API-first CAD agent mutation runtime
        /// alive. This is the normal OFF path for the foreground-access toggle.
        /// </summary>
        public static void DisableForegroundAccessFromLocalUser(string reason)
        {
            StopSession(reason, false, false, "OFF");
        }

        public static void RequireLocalConsent(string tool)
        {
            ExpireConsentIfIdle();
            lock (Sync)
            {
                if (!_enabled)
                    throw new InvalidOperationException(
                        "Local desktop-control consent is " + _consentState + ". Open QS3D MCP Agent Center > Agent and enable foreground desktop access locally before using "
                        + (tool ?? "this desktop tool") + ".");
            }
        }

        public static GuardedActionScope BeginGuardedAction(string tool)
        {
            ExpireConsentIfIdle();
            RequireLocalConsent(tool);
            var safeTool = string.IsNullOrWhiteSpace(tool) ? "desktop action" : tool.Trim();
            long consentGeneration;
            lock (Sync)
            {
                if (!_enabled)
                    throw new InvalidOperationException("Local desktop-control consent was disabled before the action started.");
                consentGeneration = _consentGeneration;
                _idleDeadlineUtc = DateTime.UtcNow + ConsentIdleTimeout;
                _activeScopes++;
                _activeTool = safeTool;
            }

            var action = McpAgentExperience.StartDesktopAction(
                "desktop-control",
                safeTool,
                "Esc ×2 để dừng ngay; sau khi dừng hãy kiểm tra drawing/backup trước khi Resume.");
            lock (Sync) _activeActionId = action.ActionId;
            ShowOverlay(safeTool, action.ActionId);
            return new GuardedActionScope(safeTool, consentGeneration, IsSensitiveReadTool(safeTool), action);
        }

        public static void ExpireConsentIfIdle()
        {
            var expired = false;
            lock (Sync)
            {
                if (_enabled && _idleDeadlineUtc != DateTime.MinValue && DateTime.UtcNow >= _idleDeadlineUtc)
                {
                    _enabled = false;
                    _consentState = "EXPIRED";
                    _idleDeadlineUtc = DateTime.MinValue;
                    unchecked { _consentGeneration++; }
                    _activeScopes = 0;
                    _activeTool = string.Empty;
                    _activeActionId = string.Empty;
                    _lastPhysicalEscapeUtc = DateTime.MinValue;
                    expired = true;
                }
            }
            if (!expired) return;

            try { McpCadAgentRuntime.StopAutomation(); } catch { }
            HideOverlay();
            ReleaseKeyboardHook();
            McpAgentExperience.Warning(
                "desktop-control",
                "Desktop consent đã EXPIRED vì local-host lease không còn được renew.",
                "Kiểm tra trạng thái QS3D/Agent Center, sau đó bật lại foreground access local nếu muốn tiếp tục.");
        }

        public static void Shutdown()
        {
            StopSession("BricsCAD/QS3D đang đóng; desktop consent đã được xóa khỏi bộ nhớ.", false, false, "OFF");
            ReleaseKeyboardHook();
            HideOverlay();
        }

        private static bool IsSensitiveReadTool(string tool)
        {
            return string.Equals(tool, "desktop_clipboard_read", StringComparison.Ordinal)
                   || string.Equals(tool, "desktop_screenshot", StringComparison.Ordinal)
                   || string.Equals(tool, "desktop_sequence", StringComparison.Ordinal);
        }

        private static void CompleteGuardedAction(GuardedActionScope scope, string terminalState, string failureMessage)
        {
            var hide = false;
            lock (Sync)
            {
                if (_activeScopes > 0) _activeScopes--;
                if (_activeScopes == 0)
                {
                    _activeTool = string.Empty;
                    _activeActionId = string.Empty;
                    hide = true;
                }
            }
            if (hide) HideOverlay();

            var next = IsEnabled
                ? "Desktop consent vẫn ON; local host đang auto-renew. Esc ×2 luôn có thể dừng."
                : "Desktop consent đang " + ConsentState + ". Kiểm tra drawing/backup rồi bật lại local nếu muốn tiếp tục.";
            var message = terminalState == "failed" && !string.IsNullOrWhiteSpace(failureMessage)
                ? "Desktop action thất bại: " + scope.Tool + " · " + BoundMessage(failureMessage)
                : "Desktop action " + terminalState + ": " + scope.Tool;
            McpAgentExperience.CompleteDesktopAction(scope.Action, message, next, terminalState);
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "0:00";
            return ((int)remaining.TotalMinutes).ToString() + ":" + remaining.Seconds.ToString("00");
        }

        private static string BoundMessage(string value)
        {
            value = (value ?? string.Empty).Replace("\0", string.Empty).Trim();
            return value.Length <= 300 ? value : value.Substring(0, 300);
        }

        private static void StopSession(string reason, bool stopAutomation, bool cancelCadCommand, string state)
        {
            bool hadSession;
            lock (Sync)
            {
                hadSession = _enabled || _activeScopes > 0;
                _enabled = false;
                _consentState = string.IsNullOrWhiteSpace(state) ? "OFF" : state;
                _idleDeadlineUtc = DateTime.MinValue;
                unchecked { _consentGeneration++; }
                _activeScopes = 0;
                _activeTool = string.Empty;
                _activeActionId = string.Empty;
                _lastPhysicalEscapeUtc = DateTime.MinValue;
            }

            if (stopAutomation)
            {
                try { McpCadAgentRuntime.StopAutomation(); } catch { }
            }
            HideOverlay();
            ReleaseKeyboardHook();

            if (hadSession || stopAutomation)
            {
                McpAgentExperience.Warning(
                    "desktop-control",
                    string.IsNullOrWhiteSpace(reason) ? "Desktop control đã dừng." : reason,
                    "Background CAD/API có thể tiếp tục nếu Agent mutation chưa bị emergency-stop; foreground desktop muốn dùng lại phải được user bật local.");
            }

            if (cancelCadCommand && McpEmbeddedServer.IsRunning)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { McpLocalAgentClient.CallOne(McpEmbeddedServer.Endpoint, 5000, "cad_cancel_command", "{}"); }
                    catch (Exception ex)
                    {
                        McpAgentExperience.Error(
                            "desktop-control",
                            "Emergency Stop thành công nhưng không gửi được CAD cancel: " + BoundMessage(ex.Message),
                            "Nếu BricsCAD command vẫn chờ input, nhấn Esc trực tiếp trong BricsCAD.");
                    }
                });
            }
        }

        private static void EnsureKeyboardHook()
        {
            lock (Sync)
            {
                if (_keyboardHook != IntPtr.Zero) return;
                var module = IntPtr.Zero;
                try
                {
                    using (var process = Process.GetCurrentProcess())
                    using (var mainModule = process.MainModule)
                    {
                        if (mainModule != null) module = GetModuleHandle(mainModule.ModuleName);
                    }
                }
                catch { module = GetModuleHandle(null); }

                var hook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardCallback, module, 0);
                if (hook == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "Windows không cho QS3D cài keyboard emergency hook; desktop control không được bật để tránh mất nút dừng Esc×2.");
                _keyboardHook = hook;
            }
        }

        private static void ReleaseKeyboardHook()
        {
            IntPtr hook;
            lock (Sync)
            {
                hook = _keyboardHook;
                _keyboardHook = IntPtr.Zero;
            }
            if (hook != IntPtr.Zero)
            {
                try { UnhookWindowsHookEx(hook); } catch { }
            }
        }

        private static IntPtr KeyboardHookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && (wParam == new IntPtr(WM_KEYDOWN) || wParam == new IntPtr(WM_SYSKEYDOWN)))
            {
                try
                {
                    var data = (KbdLlHookStruct)Marshal.PtrToStructure(lParam, typeof(KbdLlHookStruct));
                    if (data.VirtualKey == VK_ESCAPE && (data.Flags & LLKHF_INJECTED) == 0)
                    {
                        var now = DateTime.UtcNow;
                        var trigger = false;
                        lock (Sync)
                        {
                            if (_enabled && _lastPhysicalEscapeUtc != DateTime.MinValue
                                && now - _lastPhysicalEscapeUtc <= DoubleEscapeWindow)
                            {
                                trigger = true;
                                _lastPhysicalEscapeUtc = DateTime.MinValue;
                                _enabled = false;
                                _consentState = "PAUSED";
                                _idleDeadlineUtc = DateTime.MinValue;
                                unchecked { _consentGeneration++; }
                                _activeScopes = 0;
                                _activeTool = string.Empty;
                                _activeActionId = string.Empty;
                            }
                            else
                            {
                                _lastPhysicalEscapeUtc = now;
                            }
                        }

                        if (trigger)
                        {
                            try { McpCadAgentRuntime.StopAutomation(); } catch { }
                            HideOverlay();
                            McpAgentExperience.Warning(
                                "desktop-control",
                                "Esc×2: MCP desktop control + Agent mutation đã Emergency Stop và chuyển sang PAUSED.",
                                "Kiểm tra drawing/backup; Resume desktop local khi muốn tiếp tục.");
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                try
                                {
                                    if (McpEmbeddedServer.IsRunning)
                                        McpLocalAgentClient.CallOne(McpEmbeddedServer.Endpoint, 5000, "cad_cancel_command", "{}");
                                }
                                catch { }
                                finally { ReleaseKeyboardHook(); }
                            });
                        }
                    }
                }
                catch { }
            }
            return CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private static void ShowOverlay(string tool, string actionId)
        {
            var dispatcher = ResolveDispatcher();
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var overlay = _overlay;
                    if (overlay == null)
                    {
                        overlay = new McpDesktopControlOverlayWindow();
                        overlay.Closed += (_, __) =>
                        {
                            if (ReferenceEquals(_overlay, overlay)) _overlay = null;
                        };
                        _overlay = overlay;
                    }
                    overlay.SetTool(tool, actionId);
                    if (!overlay.IsVisible) overlay.Show();
                    overlay.Topmost = true;
                }
                catch { }
            }), DispatcherPriority.Send);
        }

        private static void HideOverlay()
        {
            var dispatcher = ResolveDispatcher();
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var overlay = _overlay;
                    if (overlay != null && overlay.IsVisible) overlay.Hide();
                }
                catch { }
            }), DispatcherPriority.Send);
        }

        private static Dispatcher ResolveDispatcher()
        {
            lock (Sync)
            {
                var existing = _dispatcher;
                if (existing != null && !existing.HasShutdownStarted) return existing;
                Dispatcher dispatcher;
                try
                {
                    dispatcher = System.Windows.Application.Current == null
                        ? Dispatcher.CurrentDispatcher
                        : System.Windows.Application.Current.Dispatcher;
                }
                catch { dispatcher = Dispatcher.CurrentDispatcher; }
                _dispatcher = dispatcher;
                return dispatcher;
            }
        }

        internal sealed class GuardedActionScope : IDisposable
        {
            private readonly long _consentGenerationAtStart;
            private readonly bool _failClosedOnConsentChange;
            private string _terminalState = "cancelled";
            private string _failureMessage = string.Empty;
            private int _disposed;

            internal GuardedActionScope(string tool, long consentGenerationAtStart, bool failClosedOnConsentChange, McpDesktopActionContext action)
            {
                Tool = tool ?? string.Empty;
                _consentGenerationAtStart = consentGenerationAtStart;
                _failClosedOnConsentChange = failClosedOnConsentChange;
                Action = action;
            }

            internal string Tool { get; private set; }
            internal McpDesktopActionContext Action { get; private set; }
            internal string ActionId { get { return Action == null ? string.Empty : Action.ActionId; } }

            internal void MarkSuccess()
            {
                _terminalState = "success";
                _failureMessage = string.Empty;
            }

            internal void MarkFailed(Exception ex)
            {
                _terminalState = "failed";
                _failureMessage = ex == null ? string.Empty : ex.Message;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

                bool consentRevoked;
                lock (Sync)
                {
                    consentRevoked = !_enabled || _consentGeneration != _consentGenerationAtStart;
                }
                if (consentRevoked) _terminalState = "cancelled";

                CompleteGuardedAction(this, _terminalState, _failureMessage);
                if (_failClosedOnConsentChange && consentRevoked)
                    throw new InvalidOperationException(
                        "Local desktop-control consent changed while the sensitive read was in progress; payload was discarded.");
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KbdLlHookStruct
        {
            public int VirtualKey;
            public int ScanCode;
            public uint Flags;
            public int Time;
            public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int hookId, LowLevelKeyboardProc callback, IntPtr module, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? moduleName);
    }

    /// <summary>
    /// Click-through, non-activating blue safety frame displayed while an MCP desktop tool is active.
    /// </summary>
    internal sealed class McpDesktopControlOverlayWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TRANSPARENT = 0x00000020L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const long WS_EX_NOACTIVATE = 0x08000000L;
        private readonly TextBlock _toolText;

        public McpDesktopControlOverlayWindow()
        {
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = Math.Max(1, SystemParameters.VirtualScreenWidth);
            Height = Math.Max(1, SystemParameters.VirtualScreenHeight);
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            IsHitTestVisible = false;
            Focusable = false;

            var grid = new Grid();
            grid.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x7F, 0xFF)),
                BorderThickness = new Thickness(5),
                Background = Brushes.Transparent
            });

            var banner = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(238, 9, 32, 68)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x9A, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0, 0, 10, 10),
                Padding = new Thickness(18, 9, 18, 9),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock
            {
                Text = "● QS3D MCP đang thao tác  •  ",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13
            });
            _toolText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xCD, 0xFF)),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13
            };
            row.Children.Add(_toolText);
            row.Children.Add(new TextBlock
            {
                Text = "  •  Esc ×2 để dừng ngay",
                Foreground = Brushes.White,
                FontSize = 13
            });
            banner.Child = row;
            grid.Children.Add(banner);
            Content = grid;
            SourceInitialized += (_, __) => MakeClickThrough();
        }

        public void SetTool(string tool, string actionId)
        {
            var safeTool = string.IsNullOrWhiteSpace(tool) ? "desktop" : tool.Trim();
            var safeId = string.IsNullOrWhiteSpace(actionId) ? string.Empty : " · " + actionId.Trim();
            _toolText.Text = safeTool + safeId;
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            try { Topmost = true; } catch { }
        }

        private void MakeClickThrough()
        {
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                if (handle == IntPtr.Zero) return;
                var style = GetWindowLongPtr(handle, GWL_EXSTYLE).ToInt64();
                SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr(style | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
            }
            catch { }
        }

        private static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index) : new IntPtr(GetWindowLong32(hwnd, index));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hwnd, index, value) : new IntPtr(SetWindowLong32(hwnd, index, value.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);
    }
}
