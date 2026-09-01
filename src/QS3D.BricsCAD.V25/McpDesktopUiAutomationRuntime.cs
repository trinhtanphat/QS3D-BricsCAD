using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Exact-target desktop window layout and privacy-bounded UI Automation observation.
    /// This runtime never launches processes, reads Value/TextPattern content, or walks outside
    /// the supplied visible current-session top-level window.
    /// </summary>
    internal static class McpDesktopUiAutomationRuntime
    {
        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;
        private const int DefaultDepth = 4;
        private const int DefaultNodes = 80;
        private const int MaxDepth = 8;
        private const int MaxNodes = 200;
        private const int MaxSafeName = 160;

        private static readonly HashSet<string> Tools = new HashSet<string>(StringComparer.Ordinal)
        {
            "desktop_window_set_state",
            "desktop_window_move_resize",
            "desktop_ui_tree"
        };

        internal static bool IsTool(string tool)
        {
            return Tools.Contains(tool ?? string.Empty);
        }

        internal static IEnumerable<string> ToolDescriptors()
        {
            yield return Tool(
                "desktop_window_set_state",
                "Maximize or restore one exact visible current-session top-level window. Requires local desktop consent and confirmMutation=true.",
                WindowHandleProperty() + ",\"state\":{\"type\":\"string\",\"enum\":[\"maximize\",\"restore\"]}," + ConfirmMutationProperty(),
                "windowHandle", "state", "confirmMutation");
            yield return Tool(
                "desktop_window_move_resize",
                "Move and resize one exact visible current-session top-level window wholly inside the virtual desktop. Requires local desktop consent and confirmMutation=true.",
                WindowHandleProperty() + ",\"x\":{\"type\":\"integer\"},\"y\":{\"type\":\"integer\"},\"width\":{\"type\":\"integer\",\"minimum\":160},\"height\":{\"type\":\"integer\",\"minimum\":120}," + ConfirmMutationProperty(),
                "windowHandle", "x", "y", "width", "height", "confirmMutation");
            yield return Tool(
                "desktop_ui_tree",
                "Read a bounded semantic UI Automation tree for one exact visible current-session window. Returns control type, privacy-safe action name, bounds and enabled state only; Edit, Document, password and generic Text names are redacted. Requires local desktop consent and confirmSensitiveRead=true.",
                WindowHandleProperty() + ",\"maxDepth\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":8},\"maxNodes\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":200}," + ConfirmSensitiveReadProperty(),
                "windowHandle", "confirmSensitiveRead");
        }

        internal static string Call(string tool, string body, Action? ensureMutationRunning, Action<string> audit)
        {
            if (!IsTool(tool)) throw new InvalidOperationException("Unknown semantic desktop UI tool: " + tool);
            switch (tool)
            {
                case "desktop_window_set_state":
                    return SetWindowState(body, RequireMutationCallback(ensureMutationRunning), audit);
                case "desktop_window_move_resize":
                    return MoveResize(body, RequireMutationCallback(ensureMutationRunning), audit);
                case "desktop_ui_tree":
                    RequireSensitiveRead(body);
                    return UiTree(body, audit);
                default:
                    throw new InvalidOperationException("Unknown semantic desktop UI tool: " + tool);
            }
        }

        private static string SetWindowState(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var state = McpTopLevelJson.ExtractString(body, "state").Trim().ToLowerInvariant();
            if (state != "maximize" && state != "restore")
                throw new InvalidOperationException("state must be maximize or restore.");
            ensureMutationRunning();
            ShowWindow(hwnd, state == "maximize" ? SW_MAXIMIZE : SW_RESTORE);
            ensureMutationRunning();
            RequireWindow(hwnd);
            var maximized = IsZoomed(hwnd);
            if (state == "maximize" && !maximized)
                throw new InvalidOperationException("Windows did not confirm the requested maximized state.");
            if (state == "restore" && maximized)
                throw new InvalidOperationException("Windows did not confirm the requested restored state.");
            Audit(audit, "window-state handle=" + HandleText(hwnd) + "; state=" + state);
            return "{\"updated\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\",\"state\":\"" + state + "\",\"maximized\":" + (maximized ? "true" : "false") + "}";
        }

        private static string MoveResize(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var x = RequiredInteger(body, "x");
            var y = RequiredInteger(body, "y");
            var width = RequiredInteger(body, "width");
            var height = RequiredInteger(body, "height");
            if (width < 160 || height < 120)
                throw new InvalidOperationException("width must be >=160 and height must be >=120.");

            var vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
            var vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
            var vw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            var vh = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            if (vw <= 0 || vh <= 0)
                throw new InvalidOperationException("Virtual desktop bounds are unavailable.");
            if ((long)x < vx || (long)y < vy || (long)x + width > (long)vx + vw || (long)y + height > (long)vy + vh)
                throw new InvalidOperationException("Requested window rectangle must stay wholly inside the current virtual desktop.");

            ensureMutationRunning();
            if (!SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE))
                throw new InvalidOperationException("Windows rejected the requested move/resize.");
            ensureMutationRunning();
            RequireWindow(hwnd);
            RECT rect;
            if (!GetWindowRect(hwnd, out rect)) throw new InvalidOperationException("Could not verify the moved window bounds.");
            var actualWidth = rect.Right - rect.Left;
            var actualHeight = rect.Bottom - rect.Top;
            if (Math.Abs(rect.Left - x) > 2 || Math.Abs(rect.Top - y) > 2 || Math.Abs(actualWidth - width) > 2 || Math.Abs(actualHeight - height) > 2)
                throw new InvalidOperationException("Windows did not confirm the requested move/resize bounds.");
            Audit(audit, "window-move-resize handle=" + HandleText(hwnd) + "; x=" + x + "; y=" + y + "; width=" + width + "; height=" + height);
            return "{\"updated\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\",\"bounds\":{" +
                   "\"x\":" + rect.Left.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + rect.Top.ToString(CultureInfo.InvariantCulture) +
                   ",\"width\":" + actualWidth.ToString(CultureInfo.InvariantCulture) + ",\"height\":" + actualHeight.ToString(CultureInfo.InvariantCulture) + "}}";
        }

        private static string UiTree(string body, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var maxDepth = OptionalInteger(body, "maxDepth", DefaultDepth, 1, MaxDepth);
            var maxNodes = OptionalInteger(body, "maxNodes", DefaultNodes, 1, MaxNodes);
            AutomationElement root;
            try { root = AutomationElement.FromHandle(hwnd); }
            catch (Exception ex) { throw new InvalidOperationException("Windows UI Automation could not attach to the target window.", ex); }
            if (root == null) throw new InvalidOperationException("Windows UI Automation did not expose the target window.");

            var builder = new StringBuilder("{\"windowHandle\":\"").Append(HandleText(hwnd))
                .Append("\",\"maxDepth\":").Append(maxDepth.ToString(CultureInfo.InvariantCulture))
                .Append(",\"maxNodes\":").Append(maxNodes.ToString(CultureInfo.InvariantCulture))
                .Append(",\"nodes\":[");
            var count = 0;
            var truncated = false;
            Walk(root, 0, maxDepth, maxNodes, builder, ref count, ref truncated);
            builder.Append("],\"count\":").Append(count.ToString(CultureInfo.InvariantCulture))
                .Append(",\"truncated\":").Append(truncated ? "true" : "false").Append('}');
            Audit(audit, "ui-tree handle=" + HandleText(hwnd) + "; nodes=" + count + "; maxDepth=" + maxDepth + "; truncated=" + truncated);
            return builder.ToString();
        }

        private static void Walk(AutomationElement element, int depth, int maxDepth, int maxNodes, StringBuilder builder, ref int count, ref bool truncated)
        {
            if (count >= maxNodes) { truncated = true; return; }
            AppendNode(element, depth, builder, count > 0);
            count++;
            if (depth >= maxDepth) return;

            AutomationElement child;
            try { child = TreeWalker.ControlViewWalker.GetFirstChild(element); }
            catch { return; }
            while (child != null)
            {
                if (count >= maxNodes) { truncated = true; return; }
                Walk(child, depth + 1, maxDepth, maxNodes, builder, ref count, ref truncated);
                if (count >= maxNodes) { truncated = true; return; }
                try { child = TreeWalker.ControlViewWalker.GetNextSibling(child); }
                catch { return; }
            }
        }

        private static void AppendNode(AutomationElement element, int depth, StringBuilder builder, bool comma)
        {
            string controlType = "Unknown";
            string name = string.Empty;
            bool enabled = false;
            bool offscreen = false;
            bool password = false;
            System.Windows.Rect bounds = System.Windows.Rect.Empty;
            try
            {
                var current = element.Current;
                controlType = SafeControlType(current.ControlType);
                enabled = current.IsEnabled;
                offscreen = current.IsOffscreen;
                password = current.IsPassword;
                bounds = current.BoundingRectangle;
                if (!password && SafeNameControl(current.ControlType)) name = Bound(current.Name, MaxSafeName);
            }
            catch { }
            if (comma) builder.Append(',');
            builder.Append("{\"depth\":").Append(depth.ToString(CultureInfo.InvariantCulture))
                .Append(",\"controlType\":\"").Append(Escape(controlType)).Append("\"")
                .Append(",\"name\":\"").Append(Escape(name)).Append("\"")
                .Append(",\"enabled\":").Append(enabled ? "true" : "false")
                .Append(",\"offscreen\":").Append(offscreen ? "true" : "false")
                .Append(",\"redacted\":").Append(password || name.Length == 0 ? "true" : "false")
                .Append(",\"bounds\":");
            if (bounds.IsEmpty || double.IsNaN(bounds.X) || double.IsInfinity(bounds.X)) builder.Append("null");
            else
            {
                builder.Append("{\"x\":").Append(JsonNumber(bounds.X)).Append(",\"y\":").Append(JsonNumber(bounds.Y))
                    .Append(",\"width\":").Append(JsonNumber(bounds.Width)).Append(",\"height\":").Append(JsonNumber(bounds.Height)).Append('}');
            }
            builder.Append('}');
        }

        private static bool SafeNameControl(ControlType type)
        {
            return type == ControlType.Button || type == ControlType.TabItem || type == ControlType.MenuItem
                   || type == ControlType.CheckBox || type == ControlType.RadioButton || type == ControlType.ComboBox
                   || type == ControlType.ListItem || type == ControlType.TreeItem || type == ControlType.Hyperlink
                   || type == ControlType.Window || type == ControlType.ToolBar;
        }

        private static string SafeControlType(ControlType type)
        {
            if (type == null) return "Unknown";
            var value = type.ProgrammaticName ?? string.Empty;
            const string prefix = "ControlType.";
            return value.StartsWith(prefix, StringComparison.Ordinal) ? value.Substring(prefix.Length) : Bound(value, 64);
        }

        private static IntPtr RequiredWindow(string body)
        {
            var raw = McpTopLevelJson.ExtractString(body ?? "{}", "windowHandle").Trim();
            ulong value;
            if (raw.Length == 0 || raw.Length > 16
                || !ulong.TryParse(raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || value == 0)
                throw new InvalidOperationException("windowHandle must be a non-zero hexadecimal window handle up to 16 characters.");
            var hwnd = new IntPtr(unchecked((long)value));
            RequireWindow(hwnd);
            return hwnd;
        }

        private static void RequireWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd) || !IsWindowVisible(hwnd))
                throw new InvalidOperationException("Target must be an exact visible top-level window.");
            if (GetAncestor(hwnd, 2) != hwnd)
                throw new InvalidOperationException("Target must be a top-level window.");
            uint processId;
            if (GetWindowThreadProcessId(hwnd, out processId) == 0 || processId == 0)
                throw new InvalidOperationException("Could not resolve target window process.");
            try
            {
                using (var process = Process.GetProcessById((int)processId))
                using (var current = Process.GetCurrentProcess())
                    if (process.SessionId != current.SessionId)
                        throw new InvalidOperationException("Target window is outside the current interactive session.");
            }
            catch (ArgumentException) { throw new InvalidOperationException("Target window process no longer exists."); }
        }

        private static void RequireSensitiveRead(string body)
        {
            if (!McpTopLevelJson.ExtractBoolean(body ?? "{}", "confirmSensitiveRead"))
                throw new InvalidOperationException("confirmSensitiveRead=true is required for desktop_ui_tree.");
        }

        private static Action RequireMutationCallback(Action? callback)
        {
            if (callback == null) throw new InvalidOperationException("Mutation execution context is unavailable.");
            return callback;
        }

        private static int RequiredInteger(string body, string property)
        {
            int value; bool found; string error;
            if (!McpTopLevelJson.TryExtractInteger(body ?? "{}", property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) throw new InvalidOperationException(property + " is required.");
            return value;
        }

        private static int OptionalInteger(string body, string property, int fallback, int minimum, int maximum)
        {
            int value; bool found; string error;
            if (!McpTopLevelJson.TryExtractInteger(body ?? "{}", property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return fallback;
            if (value < minimum || value > maximum)
                throw new InvalidOperationException(property + " must be between " + minimum + " and " + maximum + ".");
            return value;
        }

        private static string Tool(string name, string description, string properties, params string[] required)
        {
            var requiredJson = required == null || required.Length == 0 ? string.Empty : ",\"required\":[\"" + string.Join("\",\"", required) + "\"]";
            return "{\"name\":\"" + Escape(name) + "\",\"description\":\"" + Escape(description)
                   + "\",\"inputSchema\":{\"type\":\"object\",\"properties\":{" + properties
                   + "},\"additionalProperties\":false" + requiredJson + "}}";
        }

        private static string WindowHandleProperty() { return "\"windowHandle\":{\"type\":\"string\",\"pattern\":\"^[0-9A-Fa-f]{1,16}$\"}"; }
        private static string ConfirmMutationProperty() { return "\"confirmMutation\":{\"type\":\"boolean\",\"const\":true}"; }
        private static string ConfirmSensitiveReadProperty() { return "\"confirmSensitiveRead\":{\"type\":\"boolean\",\"const\":true}"; }
        private static string HandleText(IntPtr hwnd) { return unchecked((ulong)hwnd.ToInt64()).ToString("X", CultureInfo.InvariantCulture); }
        private static string Bound(string value, int maximum) { var text = value ?? string.Empty; return text.Length <= maximum ? text : text.Substring(0, maximum); }
        private static string Escape(string value) { return McpEmbeddedServer.JsonEscape(value ?? string.Empty); }
        private static string JsonNumber(double value) { return double.IsNaN(value) || double.IsInfinity(value) ? "null" : value.ToString("R", CultureInfo.InvariantCulture); }
        private static void Audit(Action<string> audit, string detail) { if (audit != null) audit(detail); }

        [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool IsZoomed(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
        [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
    }
}
