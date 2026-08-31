using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Same-process BricsCAD UI observation/control for MCP workflows that should not steal
    /// the user's foreground window, cursor or keyboard. This is deliberately not a generic
    /// desktop automation surface: every HWND must belong to the current BricsCAD process.
    /// </summary>
    internal static class McpBackgroundHostRuntime
    {
        private const int BackgroundOnly = 0;
        private const int ForegroundFallback = 1;
        private const int MaxSnapshotItems = 200;
        private const int MaxControlTextCharacters = 2048;
        private const int MaxSnapshotTextCharacters = 32768;
        private const int MaxClassCharacters = 256;
        private const int MaxSetTextCharacters = 4000;
        private const int MessageTimeoutMilliseconds = 2000;
        private const uint BM_CLICK = 0x00F5;
        private const uint WM_SETTEXT = 0x000C;
        private const uint SMTO_BLOCK = 0x0001;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        private static int _interactionPolicy = BackgroundOnly;

        internal static IEnumerable<string> ToolDescriptors()
        {
            return new[]
            {
                Tool("bricscad_interaction_policy_get",
                    "Read whether MCP BricsCAD interaction is background-only or permits the explicit foreground desktop fallback.", ""),
                Tool("bricscad_interaction_policy_set",
                    "Set BricsCAD MCP interaction policy. background_only is safe/default. foreground_fallback additionally requires current local desktop consent and still keeps every desktop mutation behind confirmMutation.",
                    "\"mode\":{\"type\":\"string\",\"enum\":[\"background_only\",\"foreground_fallback\"]}," + ConfirmMutationProperty(),
                    "mode", "confirmMutation"),
                Tool("bricscad_ui_text_snapshot",
                    "Read bounded visible title/control text only from windows owned by the current BricsCAD process. Useful for command-line/status/popup diagnostics without screen OCR. Captured text is returned to the caller and is not written to the MCP audit stream.",
                    "\"scope\":{\"type\":\"string\",\"enum\":[\"all\",\"commandline\",\"popup\"]},"
                    + "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":200},"
                    + ConfirmSensitiveReadProperty(), "scope", "confirmSensitiveRead"),
                Tool("bricscad_ui_invoke",
                    "Invoke one visible standard Button control owned by the current BricsCAD process using a bounded window message. Does not focus the window, move the cursor or inject keyboard/mouse input.",
                    ControlHandleProperty() + "," + ConfirmMutationProperty(), "controlHandle", "confirmMutation"),
                Tool("bricscad_ui_set_text",
                    "Set bounded text on one visible standard Edit/RichEdit control owned by the current BricsCAD process using WM_SETTEXT. Does not focus the window or inject global keyboard input.",
                    ControlHandleProperty() + ",\"text\":{\"type\":\"string\",\"maxLength\":4000}," + ConfirmMutationProperty(),
                    "controlHandle", "text", "confirmMutation")
            };
        }

        internal static string Call(string toolName, string arguments, Action? ensureMutationRunning, Action<string> audit)
        {
            var tool = toolName ?? string.Empty;
            var args = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            switch (tool)
            {
                case "bricscad_interaction_policy_get": return PolicyJson();
                case "bricscad_interaction_policy_set": return SetPolicy(args, RequireMutationCallback(ensureMutationRunning), audit);
                case "bricscad_ui_text_snapshot": return TextSnapshot(args, audit);
                case "bricscad_ui_invoke": return InvokeButton(args, RequireMutationCallback(ensureMutationRunning), audit);
                case "bricscad_ui_set_text": return SetControlText(args, RequireMutationCallback(ensureMutationRunning), audit);
                default: throw new InvalidOperationException("Unknown MCP BricsCAD background-host tool: " + tool + ".");
            }
        }

        internal static void EnsureGlobalInteractionAllowed(string toolName)
        {
            if (!UsesGlobalInteraction(toolName)) return;
            if (Volatile.Read(ref _interactionPolicy) == ForegroundFallback) return;
            throw new InvalidOperationException(
                "Global Windows input is disabled by the BricsCAD MCP background_only interaction policy. "
                + "Prefer direct CAD/QS3D/background-host tools. To use the explicit desktop fallback, locally enable QS3D desktop control and then set bricscad_interaction_policy_set mode=foreground_fallback with confirmMutation=true.");
        }

        private static bool UsesGlobalInteraction(string toolName)
        {
            switch (toolName ?? string.Empty)
            {
                case "desktop_screenshot":
                case "desktop_window_focus":
                case "desktop_mouse_move":
                case "desktop_mouse_click":
                case "desktop_mouse_scroll":
                case "desktop_mouse_drag":
                case "desktop_type":
                case "desktop_key":
                case "desktop_clipboard_write":
                case "desktop_sequence":
                    return true;
                default:
                    return false;
            }
        }

        private static string PolicyJson()
        {
            var foreground = Volatile.Read(ref _interactionPolicy) == ForegroundFallback;
            return "{\"mode\":\"" + (foreground ? "foreground_fallback" : "background_only")
                   + "\",\"globalInputAllowed\":" + (foreground ? "true" : "false")
                   + ",\"defaultMode\":\"background_only\",\"processScoped\":true}";
        }

        private static string SetPolicy(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var mode = McpTopLevelJson.ExtractString(body, "mode").Trim().ToLowerInvariant();
            if (mode != "background_only" && mode != "foreground_fallback")
                throw new InvalidOperationException("mode must be background_only or foreground_fallback.");
            ensureMutationRunning();
            if (mode == "foreground_fallback")
            {
                // Remote MCP cannot silently enable global mouse/keyboard takeover. The user must
                // first enable the existing non-persistent local desktop consent in Agent Center.
                McpDesktopControlSession.RequireLocalConsent("foreground-fallback-enable");
                ensureMutationRunning();
                Interlocked.Exchange(ref _interactionPolicy, ForegroundFallback);
            }
            else
            {
                Interlocked.Exchange(ref _interactionPolicy, BackgroundOnly);
            }
            if (audit != null) audit("interaction-policy=" + mode);
            return PolicyJson();
        }

        private static string TextSnapshot(string body, Action<string> audit)
        {
            RequireSensitiveRead(body);
            var scope = McpTopLevelJson.ExtractString(body, "scope").Trim().ToLowerInvariant();
            if (scope != "all" && scope != "commandline" && scope != "popup")
                throw new InvalidOperationException("scope must be all, commandline or popup.");
            var limit = Integer(body, "limit", 80, 1, MaxSnapshotItems);
            var items = CaptureTextItems(scope, limit);
            var builder = new StringBuilder("{\"scope\":\"").Append(Escape(scope)).Append("\",\"items\":[");
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0) builder.Append(',');
                var item = items[i];
                builder.Append("{\"handle\":\"").Append(HandleText(item.Handle))
                    .Append("\",\"class\":\"").Append(Escape(item.ClassName))
                    .Append("\",\"text\":\"").Append(Escape(item.Text))
                    .Append("\",\"topLevel\":").Append(item.TopLevel ? "true" : "false").Append('}');
            }
            builder.Append("],\"count\":").Append(items.Count.ToString(CultureInfo.InvariantCulture))
                .Append(",\"truncated\":").Append(items.Count >= limit ? "true" : "false").Append('}');
            if (audit != null) audit("background-ui-text-snapshot scope=" + scope + "; count=" + items.Count.ToString(CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static List<TextItem> CaptureTextItems(string scope, int limit)
        {
            var result = new List<TextItem>();
            var seen = new HashSet<long>();
            var textBudget = MaxSnapshotTextCharacters;
            IntPtr mainWindow;
            using (var process = Process.GetCurrentProcess()) mainWindow = process.MainWindowHandle;

            EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                if (result.Count >= limit || textBudget <= 0) return false;
                if (!BelongsToCurrentProcess(hwnd) || !IsWindowVisible(hwnd)) return true;
                var isPopupTop = hwnd != mainWindow;
                if (scope == "popup" && !isPopupTop) return true;

                AddTextItem(hwnd, true, scope, isPopupTop, result, seen, ref textBudget, limit);
                EnumChildWindows(hwnd, delegate(IntPtr child, IntPtr childParam)
                {
                    if (result.Count >= limit || textBudget <= 0) return false;
                    if (!BelongsToCurrentProcess(child) || !IsWindowVisible(child)) return true;
                    AddTextItem(child, false, scope, isPopupTop, result, seen, ref textBudget, limit);
                    return result.Count < limit && textBudget > 0;
                }, IntPtr.Zero);
                return result.Count < limit && textBudget > 0;
            }, IntPtr.Zero);
            return result;
        }

        private static void AddTextItem(
            IntPtr hwnd,
            bool topLevel,
            string scope,
            bool popupTree,
            List<TextItem> result,
            HashSet<long> seen,
            ref int textBudget,
            int limit)
        {
            if (result.Count >= limit || textBudget <= 0 || !seen.Add(hwnd.ToInt64())) return;
            var className = ClassName(hwnd);
            if (scope == "popup" && !popupTree) return;
            if (scope == "commandline" && !LooksLikeCommandLineClass(className)) return;
            var text = WindowText(hwnd);
            if (text.Length == 0) return;
            if (text.Length > textBudget) text = text.Substring(0, textBudget);
            textBudget -= text.Length;
            result.Add(new TextItem { Handle = hwnd, ClassName = className, Text = text, TopLevel = topLevel });
        }

        private static bool LooksLikeCommandLineClass(string className)
        {
            var value = (className ?? string.Empty).ToUpperInvariant();
            return value.Contains("EDIT") || value.Contains("RICH") || value.Contains("COMMAND")
                   || value.Contains("CMD") || value.Contains("PROMPT") || value.Contains("CONSOLE");
        }

        private static string InvokeButton(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredControl(body);
            var className = ClassName(hwnd);
            if (!string.Equals(className, "Button", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("bricscad_ui_invoke accepts only a standard Button control owned by the current BricsCAD process.");
            ensureMutationRunning();
            SendMessageBounded(hwnd, BM_CLICK, IntPtr.Zero, IntPtr.Zero, "Button invoke");
            ensureMutationRunning();
            if (audit != null) audit("background-ui-invoke handle=" + HandleText(hwnd) + "; class=Button");
            return "{\"invoked\":true,\"controlHandle\":\"" + HandleText(hwnd) + "\",\"class\":\"Button\",\"background\":true}";
        }

        private static string SetControlText(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredControl(body);
            var className = ClassName(hwnd);
            if (!IsTextControlClass(className))
                throw new InvalidOperationException("bricscad_ui_set_text accepts only standard Edit/RichEdit controls owned by the current BricsCAD process.");
            var text = McpTopLevelJson.ExtractString(body, "text") ?? string.Empty;
            if (text.Length > MaxSetTextCharacters)
                throw new InvalidOperationException("text exceeds " + MaxSetTextCharacters.ToString(CultureInfo.InvariantCulture) + " characters.");
            if (text.IndexOf('\0') >= 0) throw new InvalidOperationException("text contains a forbidden NUL character.");
            ensureMutationRunning();
            SendTextMessageBounded(hwnd, WM_SETTEXT, text, "Edit/RichEdit text set");
            ensureMutationRunning();
            if (audit != null) audit("background-ui-set-text handle=" + HandleText(hwnd) + "; class=" + className + "; chars=" + text.Length.ToString(CultureInfo.InvariantCulture));
            return "{\"updated\":true,\"controlHandle\":\"" + HandleText(hwnd) + "\",\"class\":\""
                   + Escape(className) + "\",\"characters\":" + text.Length.ToString(CultureInfo.InvariantCulture) + ",\"background\":true}";
        }

        private static bool IsTextControlClass(string className)
        {
            var value = (className ?? string.Empty).ToUpperInvariant();
            return value == "EDIT" || value.StartsWith("RICHEDIT", StringComparison.Ordinal);
        }

        private static IntPtr RequiredControl(string body)
        {
            var text = McpTopLevelJson.ExtractString(body, "controlHandle").Trim();
            if (text.Length == 0) throw new InvalidOperationException("controlHandle is required.");
            ulong value;
            if (text.Length > 16 || !ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || value == 0)
                throw new InvalidOperationException("controlHandle must be a non-zero hexadecimal window handle up to 16 characters.");
            var hwnd = new IntPtr(unchecked((long)value));
            if (!IsWindow(hwnd) || !IsWindowVisible(hwnd) || !BelongsToCurrentProcess(hwnd))
                throw new InvalidOperationException("controlHandle must identify a visible window/control owned by the current BricsCAD process.");
            return hwnd;
        }

        private static bool BelongsToCurrentProcess(IntPtr hwnd)
        {
            uint processId;
            if (hwnd == IntPtr.Zero || GetWindowThreadProcessId(hwnd, out processId) == 0 || processId == 0) return false;
            using (var current = Process.GetCurrentProcess()) return processId == unchecked((uint)current.Id);
        }

        private static string WindowText(IntPtr hwnd)
        {
            var length = Math.Max(0, Math.Min(MaxControlTextCharacters, GetWindowTextLength(hwnd)));
            if (length == 0) return string.Empty;
            var builder = new StringBuilder(length + 1);
            GetWindowText(hwnd, builder, builder.Capacity);
            var value = builder.ToString();
            return value.Length <= MaxControlTextCharacters ? value : value.Substring(0, MaxControlTextCharacters);
        }

        private static string ClassName(IntPtr hwnd)
        {
            var builder = new StringBuilder(MaxClassCharacters);
            var length = GetClassName(hwnd, builder, builder.Capacity);
            if (length <= 0) return string.Empty;
            var value = builder.ToString();
            return value.Length <= MaxClassCharacters ? value : value.Substring(0, MaxClassCharacters);
        }

        private static void SendMessageBounded(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam, string description)
        {
            if (!BelongsToCurrentProcess(hwnd)) throw new InvalidOperationException("Target control no longer belongs to the current BricsCAD process.");
            IntPtr messageResult;
            var sent = SendMessageTimeout(hwnd, message, wParam, lParam, SMTO_BLOCK | SMTO_ABORTIFHUNG,
                MessageTimeoutMilliseconds, out messageResult);
            if (sent == IntPtr.Zero) throw new TimeoutException(description + " failed or timed out.");
        }

        private static void SendTextMessageBounded(IntPtr hwnd, uint message, string value, string description)
        {
            if (!BelongsToCurrentProcess(hwnd)) throw new InvalidOperationException("Target control no longer belongs to the current BricsCAD process.");
            IntPtr messageResult;
            var sent = SendMessageTimeout(hwnd, message, IntPtr.Zero, value ?? string.Empty, SMTO_BLOCK | SMTO_ABORTIFHUNG,
                MessageTimeoutMilliseconds, out messageResult);
            if (sent == IntPtr.Zero) throw new TimeoutException(description + " failed or timed out.");
        }

        private static void RequireSensitiveRead(string body)
        {
            if (!McpTopLevelJson.ExtractBoolean(body, "confirmSensitiveRead"))
                throw new InvalidOperationException("confirmSensitiveRead=true is required for this BricsCAD UI text read.");
        }

        private static Action RequireMutationCallback(Action? ensureMutationRunning)
        {
            if (ensureMutationRunning == null)
                throw new InvalidOperationException("BricsCAD background-host mutation context is unavailable.");
            return ensureMutationRunning;
        }

        private static int Integer(string body, string property, int fallback, int min, int max)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return fallback;
            if (value < min || value > max)
                throw new InvalidOperationException(property + " must be between " + min.ToString(CultureInfo.InvariantCulture)
                    + " and " + max.ToString(CultureInfo.InvariantCulture) + ".");
            return value;
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

        private static string ControlHandleProperty()
        {
            return "\"controlHandle\":{\"type\":\"string\",\"pattern\":\"^[0-9A-Fa-f]{1,16}$\"}";
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

        private sealed class TextItem
        {
            public IntPtr Handle;
            public string ClassName = string.Empty;
            public string Text = string.Empty;
            public bool TopLevel;
        }

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hwnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam,
            uint flags, uint timeout, out IntPtr result);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr wParam, string lParam,
            uint flags, uint timeout, out IntPtr result);
    }
}
