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
    /// Privacy-bounded same-process BricsCAD semantic UI Automation for Background Control.
    /// This runtime never focuses windows, moves the cursor, injects keyboard/mouse input,
    /// reads Value/TextPattern content, captures pixels, or starts processes.
    /// </summary>
    internal static class McpBackgroundSemanticUiRuntime
    {
        private const int DefaultDepth = 4;
        private const int DefaultNodes = 80;
        private const int MaxDepth = 8;
        private const int MaxNodes = 200;
        private const int MaxAutomationId = 256;
        private const int MaxControlType = 64;
        private const int MaxPathCharacters = 96;
        private const uint GA_ROOT = 2;

        internal static string SemanticTree(string body, Action<string> audit)
        {
            RequireSensitiveRead(body);
            var hwnd = RequiredCurrentBricsCadWindow(body);
            var maxDepth = OptionalInteger(body, "maxDepth", DefaultDepth, 1, MaxDepth);
            var maxNodes = OptionalInteger(body, "maxNodes", DefaultNodes, 1, MaxNodes);
            var root = RootElement(hwnd);

            var builder = new StringBuilder("{\"mode\":\"semantic\",\"windowHandle\":\"")
                .Append(HandleText(hwnd))
                .Append("\",\"maxDepth\":").Append(maxDepth.ToString(CultureInfo.InvariantCulture))
                .Append(",\"maxNodes\":").Append(maxNodes.ToString(CultureInfo.InvariantCulture))
                .Append(",\"nodes\":[");
            var count = 0;
            var truncated = false;
            Walk(root, "root", 0, maxDepth, maxNodes, builder, ref count, ref truncated);
            builder.Append("],\"count\":").Append(count.ToString(CultureInfo.InvariantCulture))
                .Append(",\"truncated\":").Append(truncated ? "true" : "false")
                .Append(",\"background\":true}");
            Audit(audit, "background-semantic-tree handle=" + HandleText(hwnd)
                + "; nodes=" + count.ToString(CultureInfo.InvariantCulture)
                + "; maxDepth=" + maxDepth.ToString(CultureInfo.InvariantCulture)
                + "; truncated=" + (truncated ? "true" : "false"));
            return builder.ToString();
        }

        internal static string SemanticAction(string body, Action ensureMutationRunning, Action<string> audit)
        {
            if (ensureMutationRunning == null)
                throw new InvalidOperationException("BricsCAD background semantic mutation context is unavailable.");
            RequireConfirmMutation(body);
            var hwnd = RequiredCurrentBricsCadWindow(body);
            var path = RequiredPath(body);
            var action = McpTopLevelJson.ExtractString(body ?? "{}", "action").Trim().ToLowerInvariant();
            if (action != "invoke" && action != "toggle" && action != "select"
                && action != "expand" && action != "collapse")
                throw new InvalidOperationException("action must be invoke, toggle, select, expand or collapse.");

            var expectedControlType = McpTopLevelJson.ExtractString(body ?? "{}", "expectedControlType").Trim();
            if (expectedControlType.Length == 0)
                throw new InvalidOperationException("expectedControlType is required for semantic actions.");
            if (expectedControlType.Length > MaxControlType)
                throw new InvalidOperationException("expectedControlType exceeds the bounded metadata limit.");
            var expectedAutomationId = McpTopLevelJson.ExtractString(body ?? "{}", "expectedAutomationId");
            if (expectedAutomationId != null && expectedAutomationId.Length > MaxAutomationId)
                throw new InvalidOperationException("expectedAutomationId exceeds the bounded metadata limit.");

            var root = RootElement(hwnd);
            var element = ResolvePath(root, path);
            VerifyElementIdentity(element, expectedControlType, expectedAutomationId ?? string.Empty);
            VerifyActionAvailable(element, action);

            ensureMutationRunning();
            ExecuteAction(element, action);
            ensureMutationRunning();

            // Revalidate process/window ownership after the provider action. The exact element may
            // disappear after a successful invoke, so do not require the old UIA element to survive.
            RequiredCurrentBricsCadWindow(hwnd);
            Audit(audit, "background-semantic-action handle=" + HandleText(hwnd)
                + "; path=" + path + "; action=" + action + "; controlType=" + expectedControlType);
            return "{\"invoked\":true,\"semantic\":true,\"background\":true,\"windowHandle\":\""
                + HandleText(hwnd) + "\",\"elementPath\":\"" + Escape(path) + "\",\"action\":\""
                + Escape(action) + "\",\"controlType\":\"" + Escape(expectedControlType) + "\"}";
        }

        private static void Walk(
            AutomationElement element,
            string path,
            int depth,
            int maxDepth,
            int maxNodes,
            StringBuilder builder,
            ref int count,
            ref bool truncated)
        {
            if (count >= maxNodes)
            {
                truncated = true;
                return;
            }

            AppendNode(element, path, depth, builder, count > 0);
            count++;
            if (depth >= maxDepth) return;

            AutomationElement child;
            try { child = TreeWalker.ControlViewWalker.GetFirstChild(element); }
            catch { return; }

            var childIndex = 0;
            while (child != null)
            {
                if (count >= maxNodes)
                {
                    truncated = true;
                    return;
                }

                var childPath = path == "root"
                    ? childIndex.ToString(CultureInfo.InvariantCulture)
                    : path + "/" + childIndex.ToString(CultureInfo.InvariantCulture);
                Walk(child, childPath, depth + 1, maxDepth, maxNodes, builder, ref count, ref truncated);
                childIndex++;
                try { child = TreeWalker.ControlViewWalker.GetNextSibling(child); }
                catch { return; }
            }
        }

        private static void AppendNode(AutomationElement element, string path, int depth, StringBuilder builder, bool comma)
        {
            string controlType;
            string automationId;
            bool enabled;
            bool offscreen;
            try
            {
                var current = element.Current;
                controlType = SafeControlType(current.ControlType);
                automationId = Bound(current.AutomationId, MaxAutomationId);
                enabled = current.IsEnabled;
                offscreen = current.IsOffscreen;
            }
            catch
            {
                controlType = "Unknown";
                automationId = string.Empty;
                enabled = false;
                offscreen = true;
            }

            var actions = SupportedActions(element);
            if (comma) builder.Append(',');
            builder.Append("{\"elementPath\":\"").Append(Escape(path))
                .Append("\",\"depth\":").Append(depth.ToString(CultureInfo.InvariantCulture))
                .Append(",\"controlType\":\"").Append(Escape(controlType))
                .Append("\",\"automationId\":\"").Append(Escape(automationId))
                .Append("\",\"enabled\":").Append(enabled ? "true" : "false")
                .Append(",\"offscreen\":").Append(offscreen ? "true" : "false")
                .Append(",\"actions\":[");
            for (var i = 0; i < actions.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append('"').Append(Escape(actions[i])).Append('"');
            }
            builder.Append("]}");
        }

        private static List<string> SupportedActions(AutomationElement element)
        {
            var actions = new List<string>();
            object pattern;

            if (TryPattern(element, InvokePattern.Pattern, out pattern))
                actions.Add("invoke");
            if (TryPattern(element, TogglePattern.Pattern, out pattern))
                actions.Add("toggle");
            if (TryPattern(element, SelectionItemPattern.Pattern, out pattern))
                actions.Add("select");
            if (TryPattern(element, ExpandCollapsePattern.Pattern, out pattern))
            {
                try
                {
                    var state = ((ExpandCollapsePattern)pattern).Current.ExpandCollapseState;
                    if (state == ExpandCollapseState.Expanded || state == ExpandCollapseState.PartiallyExpanded)
                        actions.Add("collapse");
                    if (state == ExpandCollapseState.Collapsed || state == ExpandCollapseState.PartiallyExpanded)
                        actions.Add("expand");
                }
                catch { }
            }

            return actions;
        }

        private static void VerifyActionAvailable(AutomationElement element, string action)
        {
            var actions = SupportedActions(element);
            if (!actions.Contains(action))
                throw new InvalidOperationException("Requested semantic action is not currently supported by the exact UI element.");
        }

        private static void ExecuteAction(AutomationElement element, string action)
        {
            object pattern;
            switch (action)
            {
                case "invoke":
                    if (!TryPattern(element, InvokePattern.Pattern, out pattern))
                        throw new InvalidOperationException("InvokePattern is unavailable.");
                    ((InvokePattern)pattern).Invoke();
                    return;
                case "toggle":
                    if (!TryPattern(element, TogglePattern.Pattern, out pattern))
                        throw new InvalidOperationException("TogglePattern is unavailable.");
                    ((TogglePattern)pattern).Toggle();
                    return;
                case "select":
                    if (!TryPattern(element, SelectionItemPattern.Pattern, out pattern))
                        throw new InvalidOperationException("SelectionItemPattern is unavailable.");
                    ((SelectionItemPattern)pattern).Select();
                    return;
                case "expand":
                    if (!TryPattern(element, ExpandCollapsePattern.Pattern, out pattern))
                        throw new InvalidOperationException("ExpandCollapsePattern is unavailable.");
                    ((ExpandCollapsePattern)pattern).Expand();
                    return;
                case "collapse":
                    if (!TryPattern(element, ExpandCollapsePattern.Pattern, out pattern))
                        throw new InvalidOperationException("ExpandCollapsePattern is unavailable.");
                    ((ExpandCollapsePattern)pattern).Collapse();
                    return;
                default:
                    throw new InvalidOperationException("Unsupported semantic action.");
            }
        }

        private static bool TryPattern(AutomationElement element, AutomationPattern pattern, out object value)
        {
            value = null!;
            try { return element.TryGetCurrentPattern(pattern, out value); }
            catch { return false; }
        }

        private static AutomationElement ResolvePath(AutomationElement root, string path)
        {
            if (path == "root") return root;
            var segments = path.Split('/');
            if (segments.Length == 0 || segments.Length > MaxDepth)
                throw new InvalidOperationException("elementPath exceeds the bounded semantic depth.");

            var current = root;
            foreach (var segment in segments)
            {
                int index;
                if (segment.Length == 0 || !int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out index)
                    || index < 0 || index >= MaxNodes)
                    throw new InvalidOperationException("elementPath contains an invalid bounded child index.");

                AutomationElement child;
                try { child = TreeWalker.ControlViewWalker.GetFirstChild(current); }
                catch { child = null; }
                var cursor = 0;
                while (child != null && cursor < index)
                {
                    try { child = TreeWalker.ControlViewWalker.GetNextSibling(child); }
                    catch { child = null; }
                    cursor++;
                }

                if (child == null)
                    throw new InvalidOperationException("elementPath is stale or no longer resolves.");
                current = child;
            }
            return current;
        }

        private static void VerifyElementIdentity(AutomationElement element, string expectedControlType, string expectedAutomationId)
        {
            string controlType;
            string automationId;
            bool enabled;
            bool offscreen;
            try
            {
                var current = element.Current;
                controlType = SafeControlType(current.ControlType);
                automationId = Bound(current.AutomationId, MaxAutomationId);
                enabled = current.IsEnabled;
                offscreen = current.IsOffscreen;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Semantic target is stale or unavailable.", ex);
            }

            if (!string.Equals(controlType, expectedControlType, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic target control type changed; refusing stale/mismatched action.");
            if (!string.Equals(automationId, expectedAutomationId ?? string.Empty, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic target automationId changed; refusing stale/mismatched action.");
            if (!enabled || offscreen)
                throw new InvalidOperationException("Semantic target must be enabled and onscreen at action time.");
        }

        private static AutomationElement RootElement(IntPtr hwnd)
        {
            AutomationElement root;
            try { root = AutomationElement.FromHandle(hwnd); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Windows UI Automation could not attach to the BricsCAD target window.", ex);
            }
            if (root == null)
                throw new InvalidOperationException("Windows UI Automation did not expose the BricsCAD target window.");

            try
            {
                using (var current = Process.GetCurrentProcess())
                {
                    if (root.Current.ProcessId != current.Id)
                        throw new InvalidOperationException("UI Automation root does not belong to the current BricsCAD process.");
                }
            }
            catch (ElementNotAvailableException ex)
            {
                throw new InvalidOperationException("BricsCAD UI Automation root became unavailable.", ex);
            }
            return root;
        }

        private static IntPtr RequiredCurrentBricsCadWindow(string body)
        {
            var raw = McpTopLevelJson.ExtractString(body ?? "{}", "windowHandle").Trim();
            ulong value;
            if (raw.Length == 0 || raw.Length > 16
                || !ulong.TryParse(raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || value == 0)
                throw new InvalidOperationException("windowHandle must be a non-zero hexadecimal window handle up to 16 characters.");
            var hwnd = new IntPtr(unchecked((long)value));
            RequiredCurrentBricsCadWindow(hwnd);
            return hwnd;
        }

        private static void RequiredCurrentBricsCadWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd) || !IsWindowVisible(hwnd) || GetAncestor(hwnd, GA_ROOT) != hwnd)
                throw new InvalidOperationException("windowHandle must identify an exact visible top-level BricsCAD window.");
            uint processId;
            if (GetWindowThreadProcessId(hwnd, out processId) == 0 || processId == 0)
                throw new InvalidOperationException("Could not resolve semantic target window process.");
            using (var current = Process.GetCurrentProcess())
            {
                if (processId != unchecked((uint)current.Id))
                    throw new InvalidOperationException("Semantic target window must belong to the current BricsCAD process.");
            }
        }

        private static string RequiredPath(string body)
        {
            var path = McpTopLevelJson.ExtractString(body ?? "{}", "elementPath").Trim();
            if (path.Length == 0 || path.Length > MaxPathCharacters)
                throw new InvalidOperationException("elementPath is required and must stay within the bounded path length.");
            if (path == "root") return path;
            foreach (var ch in path)
                if (!(ch >= '0' && ch <= '9') && ch != '/')
                    throw new InvalidOperationException("elementPath may contain only bounded numeric child indexes separated by '/'.");
            return path;
        }

        private static void RequireSensitiveRead(string body)
        {
            if (!McpTopLevelJson.ExtractBoolean(body ?? "{}", "confirmSensitiveRead"))
                throw new InvalidOperationException("confirmSensitiveRead=true is required for semantic BricsCAD UI discovery.");
        }

        private static void RequireConfirmMutation(string body)
        {
            if (!McpTopLevelJson.ExtractBoolean(body ?? "{}", "confirmMutation"))
                throw new InvalidOperationException("confirmMutation=true is required for semantic BricsCAD UI actions.");
        }

        private static int OptionalInteger(string body, string property, int fallback, int minimum, int maximum)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body ?? "{}", property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return fallback;
            if (value < minimum || value > maximum)
                throw new InvalidOperationException(property + " must be between "
                    + minimum.ToString(CultureInfo.InvariantCulture) + " and "
                    + maximum.ToString(CultureInfo.InvariantCulture) + ".");
            return value;
        }

        private static string SafeControlType(ControlType type)
        {
            if (type == null) return "Unknown";
            var value = type.ProgrammaticName ?? string.Empty;
            const string prefix = "ControlType.";
            return value.StartsWith(prefix, StringComparison.Ordinal)
                ? Bound(value.Substring(prefix.Length), MaxControlType)
                : Bound(value, MaxControlType);
        }

        private static string Bound(string value, int maximum)
        {
            var text = value ?? string.Empty;
            return text.Length <= maximum ? text : text.Substring(0, maximum);
        }

        private static string HandleText(IntPtr hwnd)
        {
            return unchecked((ulong)hwnd.ToInt64()).ToString("X", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            return McpEmbeddedServer.JsonEscape(value ?? string.Empty);
        }

        private static void Audit(Action<string> audit, string detail)
        {
            if (audit != null) audit(detail ?? string.Empty);
        }

        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}
