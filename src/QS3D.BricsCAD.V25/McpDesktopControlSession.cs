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
    /// Consent is deliberately process-memory-only and cannot be enabled through MCP.
    /// </summary>
    internal static class McpDesktopControlSession
    {
        internal static readonly TimeSpan DoubleEscapeWindow = TimeSpan.FromMilliseconds(1200);
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int VK_ESCAPE = 0x1B;
        private const uint LLKHF_INJECTED = 0x10;

        private static readonly object Sync = new object();
        private static readonly LowLevelKeyboardProc KeyboardCallback = KeyboardHookCallback;
        private static bool _enabled;
        private static long _consentGeneration;
        private static IntPtr _keyboardHook;
        private static DateTime _lastPhysicalEscapeUtc = DateTime.MinValue;
        private static int _activeScopes;
        private static string _activeTool = string.Empty;
        private static Dispatcher _dispatcher;
        private static McpDesktopControlOverlayWindow _overlay;

        public static bool IsEnabled { get { lock (Sync) return _enabled; } }
        public static string ActiveTool { get { lock (Sync) return _activeTool; } }

        public static void EnableFromLocalUser()
        {
            lock (Sync)
            {
                if (_enabled) return;
                _enabled = true;
                unchecked { _consentGeneration++; }
                _lastPhysicalEscapeUtc = DateTime.MinValue;
            }

            EnsureKeyboardHook();
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
                        unchecked { _consentGeneration++; }
                    }
                    ReleaseKeyboardHook();
                    throw new InvalidOperationException("Không resume được MCP Agent khi bật quyền desktop: " + ex.Message, ex);
                }
            }

            McpAgentExperience.Success(
                "desktop-control",
                "User đã bật quyền desktop cho phiên BricsCAD hiện tại.",
                "Khi MCP thao tác desktop sẽ có viền xanh; nhấn Esc 2 lần để dừng ngay.");
        }

        public static void DisableFromLocalUser(string reason)
        {
            StopSession(reason, true, false);
        }

        public static void RequireLocalConsent(string tool)
        {
            lock (Sync)
            {
                if (!_enabled)
                    throw new InvalidOperationException(
                        "Local desktop-control consent is OFF. Open QS3D MCP Agent Center > Agent and click 'Bật quyền desktop' before using "
                        + (tool ?? "this desktop tool") + ".");
            }
        }

        public static IDisposable BeginGuardedAction(string tool)
        {
            RequireLocalConsent(tool);
            var safeTool = string.IsNullOrWhiteSpace(tool) ? "desktop action" : tool.Trim();
            long consentGeneration;
            lock (Sync)
            {
                if (!_enabled)
                    throw new InvalidOperationException("Local desktop-control consent was disabled before the action started.");
                consentGeneration = _consentGeneration;
                _activeScopes++;
                _activeTool = safeTool;
            }

            McpAgentExperience.ActionStarted(
                "desktop-control",
                "MCP đang thao tác desktop: " + safeTool,
                "Esc 2 lần trong 1.2 giây để Emergency Stop.");
            ShowOverlay(safeTool);
            return new GuardedActionScope(safeTool, consentGeneration, IsSensitiveReadTool(safeTool));
        }

        public static void Shutdown()
        {
            StopSession("BricsCAD/QS3D đang đóng; desktop consent đã được xóa khỏi bộ nhớ.", false, false);
            ReleaseKeyboardHook();
            HideOverlay();
        }

        private static bool IsSensitiveReadTool(string tool)
        {
            return string.Equals(tool, "desktop_clipboard_read", StringComparison.Ordinal)
                   || string.Equals(tool, "desktop_screenshot", StringComparison.Ordinal);
        }

        private static void CompleteGuardedAction(string tool)
        {
            var hide = false;
            lock (Sync)
            {
                if (_activeScopes > 0) _activeScopes--;
                if (_activeScopes == 0)
                {
                    _activeTool = string.Empty;
                    hide = true;
                }
            }
            if (hide) HideOverlay();
            McpAgentExperience.ActionFinished(
                "desktop-control",
                "MCP desktop action kết thúc: " + (tool ?? string.Empty),
                IsEnabled ? "Desktop consent vẫn bật cho phiên này; Esc×2 luôn có thể dừng." : "Desktop consent đang OFF.");
        }

        private static void StopSession(string reason, bool stopAutomation, bool cancelCadCommand)
        {
            bool wasEnabled;
            lock (Sync)
            {
                wasEnabled = _enabled;
                _enabled = false;
                unchecked { _consentGeneration++; }
                _activeScopes = 0;
                _activeTool = string.Empty;
                _lastPhysicalEscapeUtc = DateTime.MinValue;
            }

            if (stopAutomation)
            {
                try { McpCadAgentRuntime.StopAutomation(); } catch { }
            }

            HideOverlay();
            ReleaseKeyboardHook();

            if (wasEnabled || stopAutomation)
            {
                McpAgentExperience.Warning(
                    "desktop-control",
                    string.IsNullOrWhiteSpace(reason) ? "Desktop control đã dừng." : reason,
                    "Muốn tiếp tục, user phải bật lại quyền desktop từ QS3D Agent Center.");
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
                            "Esc×2 đã Emergency Stop nhưng không gửi được CAD cancel: " + ex.Message,
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

                _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardCallback, module, 0);
                if (_keyboardHook == IntPtr.Zero)
                {
                    _enabled = false;
                    unchecked { _consentGeneration++; }
                    throw new InvalidOperationException(
                        "Windows không cho QS3D cài keyboard emergency hook; desktop control không được bật để tránh mất nút dừng Esc×2.");
                }
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
                                // Fail closed immediately inside the hook before any async UI work.
                                _enabled = false;
                                unchecked { _consentGeneration++; }
                                _activeScopes = 0;
                                _activeTool = string.Empty;
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
                                "Esc×2: MCP desktop control + Agent mutation đã Emergency Stop.",
                                "QS3D sẽ gửi thêm CAD cancel; user phải bật lại desktop consent để tiếp tục.");
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

        private static void ShowOverlay(string tool)
        {
            var dispatcher = ResolveDispatcher();
            if (dispatcher == null) return;
            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (_overlay == null)
                    {
                        _overlay = new McpDesktopControlOverlayWindow();
                        _overlay.Closed += (_, __) => _overlay = null;
                    }
                    _overlay.SetTool(tool);
                    if (!_overlay.IsVisible) _overlay.Show();
                    _overlay.Topmost = true;
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
                try { if (_overlay != null && _overlay.IsVisible) _overlay.Hide(); } catch { }
            }), DispatcherPriority.Send);
        }

        private static Dispatcher ResolveDispatcher()
        {
            lock (Sync)
            {
                if (_dispatcher != null && !_dispatcher.HasShutdownStarted) return _dispatcher;
                try
                {
                    _dispatcher = System.Windows.Application.Current == null
                        ? Dispatcher.CurrentDispatcher
                        : System.Windows.Application.Current.Dispatcher;
                }
                catch { _dispatcher = Dispatcher.CurrentDispatcher; }
                return _dispatcher;
            }
        }

        private sealed class GuardedActionScope : IDisposable
        {
            private readonly string _tool;
            private readonly long _consentGenerationAtStart;
            private readonly bool _failClosedOnConsentChange;
            private int _disposed;

            public GuardedActionScope(string tool, long consentGenerationAtStart, bool failClosedOnConsentChange)
            {
                _tool = tool ?? string.Empty;
                _consentGenerationAtStart = consentGenerationAtStart;
                _failClosedOnConsentChange = failClosedOnConsentChange;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

                var consentRevoked = false;
                if (_failClosedOnConsentChange)
                {
                    lock (Sync)
                    {
                        consentRevoked = !_enabled || _consentGeneration != _consentGenerationAtStart;
                    }
                }

                CompleteGuardedAction(_tool);
                if (consentRevoked)
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
        private static extern IntPtr GetModuleHandle(string moduleName);
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

        public void SetTool(string tool)
        {
            _toolText.Text = string.IsNullOrWhiteSpace(tool) ? "desktop" : tool.Trim();
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