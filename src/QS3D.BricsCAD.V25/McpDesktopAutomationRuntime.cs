using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Bounded Windows desktop automation used by the embedded MCP full-agent surface.
    /// This runtime never launches a process or shell. Mutating callers must enter through
    /// McpCadAgentRuntime.Mutation so confirmMutation and the emergency-stop epoch remain
    /// canonical; the callback is rechecked immediately before every injected input.
    /// </summary>
    internal static class McpDesktopAutomationRuntime
    {
        private const int MaxWindows = 100;
        private const int MaxWindowTitleLength = 512;
        private const int MaxTypedCharacters = 8000;
        private const int MaxClipboardCharacters = 65536;
        private const int MaxScreenshotWidth = 1280;
        private const int MaxScreenshotHeight = 900;
        private const int MaxScreenshotBytes = 3 * 1024 * 1024;
        private const int ClipboardTimeoutMilliseconds = 5000;
        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const int SW_RESTORE = 9;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const uint SRCCOPY = 0x00CC0020;

        private static readonly HashSet<string> Tools = new HashSet<string>(StringComparer.Ordinal)
        {
            "desktop_cursor_position",
            "desktop_window_list",
            "desktop_foreground_window",
            "desktop_window_focus",
            "desktop_mouse_move",
            "desktop_mouse_click",
            "desktop_mouse_scroll",
            "desktop_type",
            "desktop_key",
            "desktop_clipboard_read",
            "desktop_clipboard_write",
            "desktop_screenshot"
        };

        private static readonly HashSet<string> MutationTools = new HashSet<string>(StringComparer.Ordinal)
        {
            "desktop_window_focus",
            "desktop_mouse_move",
            "desktop_mouse_click",
            "desktop_mouse_scroll",
            "desktop_type",
            "desktop_key",
            "desktop_clipboard_write"
        };

        internal static bool IsTool(string tool)
        {
            return Tools.Contains(tool ?? string.Empty);
        }

        internal static bool RequiresMutation(string tool)
        {
            return MutationTools.Contains(tool ?? string.Empty);
        }

        internal static IEnumerable<string> ToolDescriptors()
        {
            return new[]
            {
                Tool("desktop_cursor_position", "Read the current Windows desktop cursor position.", ""),
                Tool("desktop_window_list", "List a bounded set of visible top-level windows in the current interactive Windows session.",
                    "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}"),
                Tool("desktop_foreground_window", "Read metadata for the current foreground window when it belongs to this interactive Windows session.", ""),
                Tool("desktop_window_focus", "Restore and focus one visible current-session window by hexadecimal handle.",
                    WindowHandleProperty() + "," + ConfirmMutationProperty(), "windowHandle", "confirmMutation"),
                Tool("desktop_mouse_move", "Move the Windows cursor to absolute virtual-desktop coordinates.",
                    PointProperties() + "," + ConfirmMutationProperty(), "x", "y", "confirmMutation"),
                Tool("desktop_mouse_click", "Move and click at absolute virtual-desktop coordinates.",
                    PointProperties() + ",\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]},\"count\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":3}," + ConfirmMutationProperty(),
                    "x", "y", "button", "confirmMutation"),
                Tool("desktop_mouse_scroll", "Move the cursor and inject a bounded vertical mouse-wheel delta.",
                    PointProperties() + ",\"delta\":{\"type\":\"integer\",\"minimum\":-1200,\"maximum\":1200}," + ConfirmMutationProperty(),
                    "x", "y", "delta", "confirmMutation"),
                Tool("desktop_type", "Focus one visible current-session window and type bounded Unicode text.",
                    WindowHandleProperty() + ",\"text\":{\"type\":\"string\",\"maxLength\":8000}," + ConfirmMutationProperty(),
                    "windowHandle", "text", "confirmMutation"),
                Tool("desktop_key", "Focus one visible current-session window and press an allowlisted named key with optional modifiers.",
                    WindowHandleProperty() + ",\"key\":{\"type\":\"string\",\"maxLength\":24},\"ctrl\":{\"type\":\"boolean\"},\"alt\":{\"type\":\"boolean\"},\"shift\":{\"type\":\"boolean\"},\"win\":{\"type\":\"boolean\"}," + ConfirmMutationProperty(),
                    "windowHandle", "key", "confirmMutation"),
                Tool("desktop_clipboard_read", "Read bounded Unicode text from the Windows clipboard after explicit sensitive-read acknowledgement.",
                    ConfirmSensitiveReadProperty(), "confirmSensitiveRead"),
                Tool("desktop_clipboard_write", "Replace Windows clipboard text with bounded Unicode text.",
                    "\"text\":{\"type\":\"string\",\"maxLength\":65536}," + ConfirmMutationProperty(), "text", "confirmMutation"),
                Tool("desktop_screenshot", "Capture a bounded in-memory PNG of the virtual desktop or one visible current-session window after explicit sensitive-read acknowledgement.",
                    "\"scope\":{\"type\":\"string\",\"enum\":[\"screen\",\"window\"]}," + WindowHandleProperty()
                    + ",\"maxWidth\":{\"type\":\"integer\",\"minimum\":160,\"maximum\":1280},\"maxHeight\":{\"type\":\"integer\",\"minimum\":120,\"maximum\":900},"
                    + ConfirmSensitiveReadProperty(), "scope", "confirmSensitiveRead")
            };
        }

        internal static string Call(string toolName, string arguments, Action ensureMutationRunning, Action<string> audit)
        {
            var tool = toolName ?? string.Empty;
            var args = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            switch (tool)
            {
                case "desktop_cursor_position": return CursorPositionJson();
                case "desktop_window_list": return WindowListJson(Integer(args, "limit", 30, 1, MaxWindows));
                case "desktop_foreground_window": return ForegroundWindowJson();
                case "desktop_window_focus": return FocusWindow(args, ensureMutationRunning, audit);
                case "desktop_mouse_move": return MouseMove(args, ensureMutationRunning, audit);
                case "desktop_mouse_click": return MouseClick(args, ensureMutationRunning, audit);
                case "desktop_mouse_scroll": return MouseScroll(args, ensureMutationRunning, audit);
                case "desktop_type": return TypeText(args, ensureMutationRunning, audit);
                case "desktop_key": return PressKey(args, ensureMutationRunning, audit);
                case "desktop_clipboard_read": return ClipboardRead(args, audit);
                case "desktop_clipboard_write": return ClipboardWrite(args, ensureMutationRunning, audit);
                case "desktop_screenshot": return Screenshot(args, audit);
                default: throw new InvalidOperationException("Unknown MCP desktop tool: " + tool);
            }
        }

        private static string CursorPositionJson()
        {
            EnsureInteractiveSession();
            POINT point;
            if (!GetCursorPos(out point)) throw new InvalidOperationException("Could not read the Windows cursor position.");
            return "{\"x\":" + point.X.ToString(CultureInfo.InvariantCulture)
                   + ",\"y\":" + point.Y.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string WindowListJson(int limit)
        {
            EnsureInteractiveSession();
            var windows = new List<WindowInfo>();
            EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                if (windows.Count >= limit) return false;
                WindowInfo info;
                if (TryGetWindowInfo(hwnd, true, out info)) windows.Add(info);
                return true;
            }, IntPtr.Zero);

            var builder = new StringBuilder("{\"windows\":[");
            for (var i = 0; i < windows.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(WindowJson(windows[i]));
            }
            return builder.Append("]}").ToString();
        }

        private static string ForegroundWindowJson()
        {
            EnsureInteractiveSession();
            var hwnd = GetForegroundWindow();
            WindowInfo info;
            if (hwnd == IntPtr.Zero || !TryGetWindowInfo(hwnd, false, out info)) return "{\"window\":null}";
            return "{\"window\":" + WindowJson(info) + "}";
        }

        private static string FocusWindow(string body, Action ensureMutationRunning, Action<string> audit)
        {
            RequireMutationCallback(ensureMutationRunning);
            var hwnd = RequiredWindow(body);
            ensureMutationRunning();
            FocusAndVerify(hwnd);
            Audit(audit, "handle=" + HandleText(hwnd));
            return "{\"focused\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\"}";
        }

        private static string MouseMove(string body, Action ensureMutationRunning, Action<string> audit)
        {
            RequireMutationCallback(ensureMutationRunning);
            var x = IntegerRequired(body, "x", -1000000, 1000000);
            var y = IntegerRequired(body, "y", -1000000, 1000000);
            RequireVirtualDesktopPoint(x, y);
            ensureMutationRunning();
            if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows rejected the cursor move.");
            Audit(audit, "x=" + x.ToString(CultureInfo.InvariantCulture) + "; y=" + y.ToString(CultureInfo.InvariantCulture));
            return "{\"moved\":true,\"x\":" + x.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + y.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string MouseClick(string body, Action ensureMutationRunning, Action<string> audit)
        {
            RequireMutationCallback(ensureMutationRunning);
            var x = IntegerRequired(body, "x", -1000000, 1000000);
            var y = IntegerRequired(body, "y", -1000000, 1000000);
            var button = McpTopLevelJson.ExtractString(body, "button").Trim().ToLowerInvariant();
            var count = Integer(body, "count", 1, 1, 3);
            RequireVirtualDesktopPoint(x, y);
            uint down;
            uint up;
            if (button == "left") { down = MOUSEEVENTF_LEFTDOWN; up = MOUSEEVENTF_LEFTUP; }
            else if (button == "right") { down = MOUSEEVENTF_RIGHTDOWN; up = MOUSEEVENTF_RIGHTUP; }
            else if (button == "middle") { down = MOUSEEVENTF_MIDDLEDOWN; up = MOUSEEVENTF_MIDDLEUP; }
            else throw new InvalidOperationException("button must be left, right or middle.");

            ensureMutationRunning();
            if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows rejected the cursor move before click.");
            for (var i = 0; i < count; i++)
            {
                ensureMutationRunning();
                SendMouse(down, 0);
                ensureMutationRunning();
                SendMouse(up, 0);
                if (i + 1 < count) Thread.Sleep(40);
            }
            Audit(audit, "x=" + x.ToString(CultureInfo.InvariantCulture) + "; y=" + y.ToString(CultureInfo.InvariantCulture)
                         + "; button=" + button + "; count=" + count.ToString(CultureInfo.InvariantCulture));
            return "{\"clicked\":true,\"x\":" + x.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + y.ToString(CultureInfo.InvariantCulture)
                   + ",\"button\":\"" + Escape(button) + "\",\"count\":" + count.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string MouseScroll(string body, Action ensureMutationRunning, Action<string> audit)
        {
            RequireMutationCallback(ensureMutationRunning);
            var x = IntegerRequired(body, "x", -1000000, 1000000);
            var y = IntegerRequired(body, "y", -1000000, 1000000);
            var delta = IntegerRequired(body, "delta", -1200, 1200);
            if (delta == 0) throw new InvalidOperationException("delta must be non-zero.");
            RequireVirtualDesktopPoint(x, y);
            ensureMutationRunning();
            if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows rejected the cursor move before scroll.");
            ensureMutationRunning();
            SendMouse(MOUSEEVENTF_WHEEL, unchecked((uint)delta));
            Audit(audit, "x=" + x.ToString(CultureInfo.InvariantCulture) + "; y=" + y.ToString(CultureInfo.InvariantCulture)
                         + "; delta=" + delta.ToString(CultureInfo.InvariantCulture));
            return "{\"scrolled\":true,\"x\":" + x.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + y.ToString(CultureInfo.InvariantCulture)
                   + ",\"delta\":" + delta.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string TypeText(string body, Action ensureMutationRunning, Action<string> audit)
        {
            RequireMutationCallback(ensureMutationRunning);
            var hwnd = RequiredWindow(body);
            var text = RequiredText(body, "text", MaxTypedCharacters);
            ensureMutationRunning();
            FocusAndVerify(hwnd);
            foreach (var ch in text)
            {
                ensureMutationRunning();
                RequireForegroundWindow(hwnd);
                var input = new[]
                {
                    new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE } } },
                    new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } } }
                };
                SendInputs(input, "Unicode keyboard input");
            }
            Audit(audit, "handle=" + HandleText(hwnd) + "; chars=" + text.Length.ToString(CultureInfo.InvariantCulture));
            return "{\"typed\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\",\"characters\":" + text.Length.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string PressKey(string body, Action ensureMutationRunning, Action<string> audit)
        {
            RequireMutationCallback(ensureMutationRunning);
            var hwnd = RequiredWindow(body);
            var keyName = McpTopLevelJson.ExtractString(body, "key").Trim().ToUpperInvariant();
            if (keyName.Length == 0 || keyName.Length > 24) throw new InvalidOperationException("key is required and must be <=24 characters.");
            var ctrl = McpTopLevelJson.ExtractBoolean(body, "ctrl");
            var alt = McpTopLevelJson.ExtractBoolean(body, "alt");
            var shift = McpTopLevelJson.ExtractBoolean(body, "shift");
            var win = McpTopLevelJson.ExtractBoolean(body, "win");
            var key = VirtualKey(keyName);
            ensureMutationRunning();
            FocusAndVerify(hwnd);
            ensureMutationRunning();
            RequireForegroundWindow(hwnd);
            SendVirtualKey(key, ctrl, alt, shift, win);
            Audit(audit, "handle=" + HandleText(hwnd) + "; key=" + keyName + "; ctrl=" + ctrl + "; alt=" + alt + "; shift=" + shift + "; win=" + win);
            return "{\"pressed\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\",\"key\":\"" + Escape(keyName) + "\"}";
        }

        private static string ClipboardRead(string body, Action<string> audit)
        {
            RequireSensitiveRead(body);
            var text = RunSta(delegate
            {
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        if (!Clipboard.ContainsText(TextDataFormat.UnicodeText)) return string.Empty;
                        var value = Clipboard.GetText(TextDataFormat.UnicodeText) ?? string.Empty;
                        return value.Length <= MaxClipboardCharacters ? value : value.Substring(0, MaxClipboardCharacters);
                    }
                    catch (COMException)
                    {
                        if (attempt == 4) throw;
                        Thread.Sleep(50);
                    }
                }
                return string.Empty;
            });
            Audit(audit, "clipboard-read chars=" + text.Length.ToString(CultureInfo.InvariantCulture));
            return "{\"text\":\"" + Escape(text) + "\",\"characters\":" + text.Length.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string ClipboardWrite(string body, Action ensureMutationRunning, Action<string> audit)
        {
            RequireMutationCallback(ensureMutationRunning);
            var text = RequiredText(body, "text", MaxClipboardCharacters);
            ensureMutationRunning();
            RunSta(delegate
            {
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        ensureMutationRunning();
                        Clipboard.SetText(text, TextDataFormat.UnicodeText);
                        return true;
                    }
                    catch (COMException)
                    {
                        if (attempt == 4) throw;
                        Thread.Sleep(50);
                    }
                }
                return false;
            });
            Audit(audit, "clipboard-write chars=" + text.Length.ToString(CultureInfo.InvariantCulture));
            return "{\"written\":true,\"characters\":" + text.Length.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string Screenshot(string body, Action<string> audit)
        {
            RequireSensitiveRead(body);
            var scope = McpTopLevelJson.ExtractString(body, "scope").Trim().ToLowerInvariant();
            if (scope != "screen" && scope != "window") throw new InvalidOperationException("scope must be screen or window.");
            var maxWidth = Integer(body, "maxWidth", MaxScreenshotWidth, 160, MaxScreenshotWidth);
            var maxHeight = Integer(body, "maxHeight", MaxScreenshotHeight, 120, MaxScreenshotHeight);
            RECT rect;
            string handle = string.Empty;
            if (scope == "window")
            {
                var hwnd = RequiredWindow(body);
                if (!GetWindowRect(hwnd, out rect)) throw new InvalidOperationException("Could not read the target window bounds.");
                handle = HandleText(hwnd);
            }
            else
            {
                rect = VirtualDesktopRect();
            }
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0) throw new InvalidOperationException("Screenshot bounds are empty.");

            var source = CaptureBitmap(rect.Left, rect.Top, width, height);
            source = ScaleBitmap(source, maxWidth, maxHeight);
            byte[] png;
            while (true)
            {
                png = EncodePng(source);
                if (png.Length <= MaxScreenshotBytes) break;
                if (source.PixelWidth <= 160 || source.PixelHeight <= 120)
                    throw new InvalidOperationException("Screenshot exceeds the bounded MCP output size.");
                source = ScaleBitmap(source, Math.Max(160, source.PixelWidth * 3 / 4), Math.Max(120, source.PixelHeight * 3 / 4));
            }
            Audit(audit, "screenshot scope=" + scope + "; width=" + source.PixelWidth.ToString(CultureInfo.InvariantCulture)
                         + "; height=" + source.PixelHeight.ToString(CultureInfo.InvariantCulture) + (handle.Length == 0 ? string.Empty : "; handle=" + handle));
            return "{\"scope\":\"" + scope + "\",\"windowHandle\":\"" + handle + "\",\"mimeType\":\"image/png\",\"width\":"
                   + source.PixelWidth.ToString(CultureInfo.InvariantCulture) + ",\"height\":" + source.PixelHeight.ToString(CultureInfo.InvariantCulture)
                   + ",\"bytes\":" + png.Length.ToString(CultureInfo.InvariantCulture) + ",\"pngBase64\":\"" + Convert.ToBase64String(png) + "\"}";
        }

        private static BitmapSource CaptureBitmap(int x, int y, int width, int height)
        {
            var screen = GetDC(IntPtr.Zero);
            if (screen == IntPtr.Zero) throw new InvalidOperationException("Could not acquire the desktop device context.");
            IntPtr memory = IntPtr.Zero;
            IntPtr bitmap = IntPtr.Zero;
            IntPtr previous = IntPtr.Zero;
            try
            {
                memory = CreateCompatibleDC(screen);
                if (memory == IntPtr.Zero) throw new InvalidOperationException("Could not create screenshot memory context.");
                bitmap = CreateCompatibleBitmap(screen, width, height);
                if (bitmap == IntPtr.Zero) throw new InvalidOperationException("Could not create screenshot bitmap.");
                previous = SelectObject(memory, bitmap);
                if (previous == IntPtr.Zero) throw new InvalidOperationException("Could not select screenshot bitmap.");
                if (!BitBlt(memory, 0, 0, width, height, screen, x, y, SRCCOPY))
                    throw new InvalidOperationException("Windows BitBlt rejected screenshot capture.");
                var source = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                if (memory != IntPtr.Zero && previous != IntPtr.Zero) SelectObject(memory, previous);
                if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                ReleaseDC(IntPtr.Zero, screen);
            }
        }

        private static BitmapSource ScaleBitmap(BitmapSource source, int maxWidth, int maxHeight)
        {
            if (source.PixelWidth <= maxWidth && source.PixelHeight <= maxHeight) return source;
            var scale = Math.Min((double)maxWidth / source.PixelWidth, (double)maxHeight / source.PixelHeight);
            var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
            transformed.Freeze();
            return transformed;
        }

        private static byte[] EncodePng(BitmapSource source)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        private static void FocusAndVerify(IntPtr hwnd)
        {
            ValidateWindow(hwnd, true);
            ShowWindowAsync(hwnd, SW_RESTORE);
            if (GetForegroundWindow() != hwnd && !SetForegroundWindow(hwnd))
                throw new InvalidOperationException("Could not focus the requested desktop window; input was not sent.");
            for (var i = 0; i < 20; i++)
            {
                if (GetForegroundWindow() == hwnd)
                {
                    ValidateWindow(hwnd, true);
                    return;
                }
                Thread.Sleep(25);
            }
            throw new InvalidOperationException("Requested desktop window did not become foreground; input was not sent.");
        }

        private static void RequireForegroundWindow(IntPtr expected)
        {
            ValidateWindow(expected, true);
            if (GetForegroundWindow() != expected)
                throw new InvalidOperationException("Desktop foreground window changed; input stopped before injection.");
        }

        private static IntPtr RequiredWindow(string body)
        {
            var text = McpTopLevelJson.ExtractString(body, "windowHandle").Trim();
            ulong value;
            if (text.Length == 0 || text.Length > 16 || !ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || value == 0)
                throw new InvalidOperationException("windowHandle must be a non-zero hexadecimal window handle up to 16 characters.");
            var hwnd = new IntPtr(unchecked((long)value));
            ValidateWindow(hwnd, true);
            return hwnd;
        }

        private static void ValidateWindow(IntPtr hwnd, bool requireVisible)
        {
            EnsureInteractiveSession();
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) throw new InvalidOperationException("Desktop window handle is no longer valid.");
            if (requireVisible && !IsWindowVisible(hwnd)) throw new InvalidOperationException("Desktop window must be visible.");
            uint processId;
            if (GetWindowThreadProcessId(hwnd, out processId) == 0 || processId == 0)
                throw new InvalidOperationException("Could not identify the desktop window process.");
            Process process;
            try { process = Process.GetProcessById(checked((int)processId)); }
            catch (Exception ex) { throw new InvalidOperationException("Desktop window process is unavailable.", ex); }
            using (process)
            {
                if (process.SessionId != Process.GetCurrentProcess().SessionId)
                    throw new InvalidOperationException("Desktop window belongs to a different Windows session.");
            }
        }

        private static bool TryGetWindowInfo(IntPtr hwnd, bool requireTitle, out WindowInfo info)
        {
            info = null;
            try
            {
                ValidateWindow(hwnd, true);
                var title = WindowTitle(hwnd);
                if (requireTitle && title.Length == 0) return false;
                RECT rect;
                if (!GetWindowRect(hwnd, out rect)) return false;
                info = new WindowInfo
                {
                    Handle = HandleText(hwnd),
                    Title = title,
                    Left = rect.Left,
                    Top = rect.Top,
                    Width = Math.Max(0, rect.Right - rect.Left),
                    Height = Math.Max(0, rect.Bottom - rect.Top),
                    Foreground = GetForegroundWindow() == hwnd
                };
                return true;
            }
            catch { return false; }
        }

        private static string WindowTitle(IntPtr hwnd)
        {
            var length = Math.Max(0, Math.Min(MaxWindowTitleLength, GetWindowTextLength(hwnd)));
            var builder = new StringBuilder(length + 1);
            GetWindowText(hwnd, builder, builder.Capacity);
            var text = builder.ToString();
            return text.Length <= MaxWindowTitleLength ? text : text.Substring(0, MaxWindowTitleLength);
        }

        private static string WindowJson(WindowInfo info)
        {
            return "{\"windowHandle\":\"" + Escape(info.Handle) + "\",\"title\":\"" + Escape(info.Title)
                   + "\",\"bounds\":{\"x\":" + info.Left.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + info.Top.ToString(CultureInfo.InvariantCulture)
                   + ",\"width\":" + info.Width.ToString(CultureInfo.InvariantCulture) + ",\"height\":" + info.Height.ToString(CultureInfo.InvariantCulture)
                   + "},\"foreground\":" + (info.Foreground ? "true" : "false") + "}";
        }

        private static RECT VirtualDesktopRect()
        {
            var x = GetSystemMetrics(SM_XVIRTUALSCREEN);
            var y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            var width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            var height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            if (width <= 0 || height <= 0) throw new InvalidOperationException("Windows virtual desktop bounds are unavailable.");
            return new RECT { Left = x, Top = y, Right = x + width, Bottom = y + height };
        }

        private static void RequireVirtualDesktopPoint(int x, int y)
        {
            var rect = VirtualDesktopRect();
            if (x < rect.Left || x >= rect.Right || y < rect.Top || y >= rect.Bottom)
                throw new InvalidOperationException("Desktop coordinates must stay inside the Windows virtual desktop.");
        }

        private static void SendVirtualKey(ushort key, bool ctrl, bool alt, bool shift, bool win)
        {
            var input = new List<INPUT>();
            if (ctrl) input.Add(KeyInput(0x11, false));
            if (alt) input.Add(KeyInput(0x12, false));
            if (shift) input.Add(KeyInput(0x10, false));
            if (win) input.Add(KeyInput(0x5B, false));
            input.Add(KeyInput(key, false));
            input.Add(KeyInput(key, true));
            if (win) input.Add(KeyInput(0x5B, true));
            if (shift) input.Add(KeyInput(0x10, true));
            if (alt) input.Add(KeyInput(0x12, true));
            if (ctrl) input.Add(KeyInput(0x11, true));
            SendInputs(input.ToArray(), "keyboard input");
        }

        private static ushort VirtualKey(string key)
        {
            switch (key)
            {
                case "ENTER": return 0x0D; case "ESC": case "ESCAPE": return 0x1B; case "TAB": return 0x09;
                case "BACKSPACE": return 0x08; case "DELETE": return 0x2E; case "INSERT": return 0x2D; case "SPACE": return 0x20;
                case "LEFT": return 0x25; case "UP": return 0x26; case "RIGHT": return 0x27; case "DOWN": return 0x28;
                case "HOME": return 0x24; case "END": return 0x23; case "PAGEUP": return 0x21; case "PAGEDOWN": return 0x22;
                case "CAPSLOCK": return 0x14; case "PRINTSCREEN": return 0x2C; case "PAUSE": return 0x13;
            }
            if (key.Length >= 2 && key[0] == 'F')
            {
                int function;
                if (int.TryParse(key.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out function) && function >= 1 && function <= 24)
                    return checked((ushort)(0x6F + function));
            }
            if (key.Length == 1)
            {
                var ch = char.ToUpperInvariant(key[0]);
                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')) return ch;
            }
            throw new InvalidOperationException("Unsupported desktop key name.");
        }

        private static INPUT KeyInput(ushort key, bool up)
        {
            return new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = up ? KEYEVENTF_KEYUP : 0u } } };
        }

        private static void SendMouse(uint flags, uint data)
        {
            SendInputs(new[] { new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { mouseData = data, dwFlags = flags } } } }, "mouse input");
        }

        private static void SendInputs(INPUT[] inputs, string description)
        {
            if (inputs == null || inputs.Length == 0) return;
            if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) != (uint)inputs.Length)
                throw new InvalidOperationException("Windows SendInput rejected " + description + ".");
        }

        private static void RequireSensitiveRead(string body)
        {
            if (!McpTopLevelJson.ExtractBoolean(body, "confirmSensitiveRead"))
                throw new InvalidOperationException("confirmSensitiveRead=true is required for this desktop read.");
        }

        private static void RequireMutationCallback(Action ensureMutationRunning)
        {
            if (ensureMutationRunning == null)
                throw new InvalidOperationException("Desktop mutation execution context is unavailable.");
        }

        private static string RequiredText(string body, string property, int maximum)
        {
            var value = McpTopLevelJson.ExtractString(body, property);
            if (value == null) value = string.Empty;
            if (value.Length > maximum) throw new InvalidOperationException(property + " exceeds " + maximum.ToString(CultureInfo.InvariantCulture) + " characters.");
            foreach (var ch in value) if (ch == '\0') throw new InvalidOperationException(property + " contains a forbidden NUL character.");
            return value;
        }

        private static int Integer(string body, string property, int fallback, int min, int max)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error)) throw new InvalidOperationException(error);
            if (!found) return fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        private static int IntegerRequired(string body, string property, int min, int max)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error)) throw new InvalidOperationException(error);
            if (!found || value < min || value > max)
                throw new InvalidOperationException(property + " must be an integer between " + min.ToString(CultureInfo.InvariantCulture) + " and " + max.ToString(CultureInfo.InvariantCulture) + ".");
            return value;
        }

        private static T RunSta<T>(Func<T> action)
        {
            T result = default(T);
            Exception error = null;
            var done = new ManualResetEventSlim(false);
            var thread = new Thread(delegate()
            {
                try { result = action(); }
                catch (Exception ex) { error = ex; }
                finally { done.Set(); }
            }) { IsBackground = true, Name = "QS3D MCP clipboard STA" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!done.Wait(ClipboardTimeoutMilliseconds))
                throw new TimeoutException("Timed out waiting for Windows clipboard operation.");
            done.Dispose();
            if (error != null) throw new InvalidOperationException("Windows clipboard operation failed: " + error.Message, error);
            return result;
        }

        private static void EnsureInteractiveSession()
        {
            if (!Environment.UserInteractive) throw new InvalidOperationException("Windows desktop automation requires an interactive user session.");
        }

        private static void Audit(Action<string> audit, string detail)
        {
            if (audit != null) audit(detail ?? string.Empty);
        }

        private static string Tool(string name, string description, string properties, params string[] required)
        {
            var requiredJson = required == null || required.Length == 0
                ? string.Empty
                : ",\"required\":[\"" + string.Join("\",\"", required) + "\"]";
            return "{\"name\":\"" + Escape(name) + "\",\"description\":\"" + Escape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + (properties ?? string.Empty)
                   + "},\"additionalProperties\":false" + requiredJson + "}}";
        }

        private static string WindowHandleProperty()
        {
            return "\"windowHandle\":{\"type\":\"string\",\"pattern\":\"^[0-9A-Fa-f]{1,16}$\"}";
        }

        private static string PointProperties()
        {
            return "\"x\":{\"type\":\"integer\"},\"y\":{\"type\":\"integer\"}";
        }

        private static string ConfirmMutationProperty()
        {
            return "\"confirmMutation\":{\"type\":\"boolean\"}";
        }

        private static string ConfirmSensitiveReadProperty()
        {
            return "\"confirmSensitiveRead\":{\"type\":\"boolean\"}";
        }

        private static string HandleText(IntPtr hwnd)
        {
            return unchecked((ulong)hwnd.ToInt64()).ToString("X", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            return McpEmbeddedServer.JsonEscape(value ?? string.Empty);
        }

        private sealed class WindowInfo
        {
            public string Handle = string.Empty;
            public string Title = string.Empty;
            public int Left;
            public int Top;
            public int Width;
            public int Height;
            public bool Foreground;
        }

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
        [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
        [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr hwnd, int command);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT point);
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, INPUT[] inputs, int size);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint rasterOperation);
    }
}
