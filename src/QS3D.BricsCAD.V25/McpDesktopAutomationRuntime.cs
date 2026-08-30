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
    /// Desktop mutation/sensitive reads require local consent in addition to MCP confirmation.
    /// This runtime exposes explicit tools plus one bounded single-target sequence; it never launches a process, shell or script.
    /// </summary>
    internal static class McpDesktopAutomationRuntime
    {
        private const int MaxWindows = 100;
        private const int MaxWindowTitleLength = 512;
        private const int MaxWaitTitleLength = 160;
        private const int MaxTypedCharacters = 8000;
        private const int MaxClipboardCharacters = 65536;
        private const int MaxScreenshotWidth = 1280;
        private const int MaxScreenshotHeight = 900;
        private const int MaxScreenshotBytes = 3 * 1024 * 1024;
        private const int MaxWaitMilliseconds = 15000;
        private const int MinWaitPollMilliseconds = 50;
        private const int MaxWaitPollMilliseconds = 1000;
        private const int MinDragMilliseconds = 50;
        private const int MaxDragMilliseconds = 3000;
        private const int DragStepMilliseconds = 25;
        private const int ClipboardTimeoutMilliseconds = 5000;
        private const int MaxSequenceSteps = 12;
        private const int MaxSequenceMilliseconds = 30000;
        private const int MaxSequenceDelayMilliseconds = 2000;
        private const int MaxSequenceJsonCharacters = 32768;
        private const int MaxSequenceStepArgumentsCharacters = 8192;
        private const int SequenceDelaySliceMilliseconds = 50;
        private const int MaxSequenceScreenshots = 1;
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
        private const uint PW_RENDERFULLCONTENT = 0x00000002;

        private static readonly HashSet<string> Tools = new HashSet<string>(StringComparer.Ordinal)
        {
            "diagnostics_log_tail",
            "diagnostics_since",
            "diagnostics_snapshot",
            "diagnostics_wait",
            "theme_get",
            "theme_set",
            "bricscad_interaction_policy_get",
            "bricscad_interaction_policy_set",
            "bricscad_ui_text_snapshot",
            "bricscad_ui_invoke",
            "bricscad_ui_set_text",
            "desktop_cursor_position",
            "desktop_window_list",
            "desktop_foreground_window",
            "desktop_wait_for_window",
            "desktop_window_focus",
            "desktop_mouse_move",
            "desktop_mouse_click",
            "desktop_mouse_scroll",
            "desktop_mouse_drag",
            "desktop_type",
            "desktop_key",
            "desktop_clipboard_read",
            "desktop_clipboard_write",
            "desktop_screenshot",
            "desktop_sequence"
        };

        private static readonly HashSet<string> MutationTools = new HashSet<string>(StringComparer.Ordinal)
        {
            "theme_set",
            "bricscad_interaction_policy_set",
            "bricscad_ui_invoke",
            "bricscad_ui_set_text",
            "desktop_window_focus",
            "desktop_mouse_move",
            "desktop_mouse_click",
            "desktop_mouse_scroll",
            "desktop_mouse_drag",
            "desktop_type",
            "desktop_key",
            "desktop_clipboard_write",
            "desktop_sequence"
        };

        private static readonly HashSet<string> SensitiveTools = new HashSet<string>(StringComparer.Ordinal)
        {
            "desktop_clipboard_read",
            "desktop_screenshot"
        };

        private static readonly HashSet<string> SequenceAllowedTools = new HashSet<string>(StringComparer.Ordinal)
        {
            "desktop_window_focus",
            "desktop_mouse_move",
            "desktop_mouse_click",
            "desktop_mouse_scroll",
            "desktop_mouse_drag",
            "desktop_type",
            "desktop_key",
            "desktop_clipboard_write",
            "desktop_wait_for_window",
            "desktop_screenshot"
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
            var descriptors = new List<string>
            {
                Tool("desktop_cursor_position", "Read the current Windows desktop cursor position.", ""),
                Tool("desktop_window_list", "List a bounded set of visible top-level windows in the current interactive Windows session.",
                    "\"limit\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":100}"),
                Tool("desktop_foreground_window", "Read metadata for the current foreground window when it belongs to this interactive Windows session.", ""),
                Tool("desktop_wait_for_window", "Wait up to 15 seconds for a visible current-session top-level window matching an exact handle and/or bounded title substring. Read-only; never focuses or clicks.",
                    WindowHandleProperty() + ",\"titleContains\":{\"type\":\"string\",\"maxLength\":160},\"timeoutMs\":{\"type\":\"integer\",\"minimum\":0,\"maximum\":15000},\"pollIntervalMs\":{\"type\":\"integer\",\"minimum\":50,\"maximum\":1000}"),
                Tool("desktop_window_focus", "Restore and focus one visible current-session window. Requires local QS3D desktop consent and confirmMutation=true.",
                    WindowHandleProperty() + "," + ConfirmMutationProperty(), "windowHandle", "confirmMutation"),
                Tool("desktop_mouse_move", "Move the Windows cursor to absolute virtual-desktop coordinates. Requires local QS3D desktop consent and confirmMutation=true.",
                    PointProperties() + "," + ConfirmMutationProperty(), "x", "y", "confirmMutation"),
                Tool("desktop_mouse_click", "Focus one exact visible current-session window and click a point inside its current bounds. Requires local QS3D desktop consent and confirmMutation=true.",
                    WindowHandleProperty() + "," + PointProperties() + ",\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]},\"count\":{\"type\":\"integer\",\"minimum\":1,\"maximum\":3}," + ConfirmMutationProperty(),
                    "windowHandle", "x", "y", "button", "confirmMutation"),
                Tool("desktop_mouse_scroll", "Focus one exact visible current-session window and inject a bounded vertical wheel delta at a point inside its current bounds. Requires local QS3D desktop consent and confirmMutation=true.",
                    WindowHandleProperty() + "," + PointProperties() + ",\"delta\":{\"type\":\"integer\",\"minimum\":-1200,\"maximum\":1200}," + ConfirmMutationProperty(),
                    "windowHandle", "x", "y", "delta", "confirmMutation"),
                Tool("desktop_mouse_drag", "Drag inside one exact visible current-session window with bounded duration and continuous target/emergency-stop revalidation. Requires local QS3D desktop consent and confirmMutation=true.",
                    WindowHandleProperty() + ",\"startX\":{\"type\":\"integer\"},\"startY\":{\"type\":\"integer\"},\"endX\":{\"type\":\"integer\"},\"endY\":{\"type\":\"integer\"},\"button\":{\"type\":\"string\",\"enum\":[\"left\",\"right\",\"middle\"]},\"durationMs\":{\"type\":\"integer\",\"minimum\":50,\"maximum\":3000}," + ConfirmMutationProperty(),
                    "windowHandle", "startX", "startY", "endX", "endY", "button", "confirmMutation"),
                Tool("desktop_type", "Focus one visible current-session window and type bounded Unicode text. Requires local QS3D desktop consent and confirmMutation=true.",
                    WindowHandleProperty() + ",\"text\":{\"type\":\"string\",\"maxLength\":8000}," + ConfirmMutationProperty(),
                    "windowHandle", "text", "confirmMutation"),
                Tool("desktop_key", "Focus one visible current-session window and press an allowlisted named key with optional modifiers. Requires local QS3D desktop consent and confirmMutation=true.",
                    WindowHandleProperty() + ",\"key\":{\"type\":\"string\",\"maxLength\":24},\"ctrl\":{\"type\":\"boolean\"},\"alt\":{\"type\":\"boolean\"},\"shift\":{\"type\":\"boolean\"},\"win\":{\"type\":\"boolean\"}," + ConfirmMutationProperty(),
                    "windowHandle", "key", "confirmMutation"),
                Tool("desktop_clipboard_read", "Read bounded Unicode text from the Windows clipboard. Requires local QS3D desktop consent and confirmSensitiveRead=true.",
                    ConfirmSensitiveReadProperty(), "confirmSensitiveRead"),
                Tool("desktop_clipboard_write", "Replace Windows clipboard text with bounded Unicode text. Requires local QS3D desktop consent and confirmMutation=true.",
                    "\"text\":{\"type\":\"string\",\"maxLength\":65536}," + ConfirmMutationProperty(), "text", "confirmMutation"),
                Tool("desktop_screenshot", "Capture a bounded in-memory PNG of the virtual desktop or one visible current-session window, optionally cropped relative to the selected source. Requires local QS3D desktop consent and confirmSensitiveRead=true.",
                    "\"scope\":{\"type\":\"string\",\"enum\":[\"screen\",\"window\"]}," + WindowHandleProperty()
                    + ",\"cropX\":{\"type\":\"integer\"},\"cropY\":{\"type\":\"integer\"},\"cropWidth\":{\"type\":\"integer\",\"minimum\":1},\"cropHeight\":{\"type\":\"integer\",\"minimum\":1}"
                    + ",\"maxWidth\":{\"type\":\"integer\",\"minimum\":160,\"maximum\":1280},\"maxHeight\":{\"type\":\"integer\",\"minimum\":120,\"maximum\":900},"
                    + ConfirmSensitiveReadProperty(), "scope", "confirmSensitiveRead"),
                Tool("desktop_sequence", "Execute up to 12 fail-fast desktop UI steps against one exact visible current-session window for at most 30 seconds. Requires local consent and confirmMutation=true; target-window screenshots additionally require confirmSensitiveRead=true.",
                    WindowHandleProperty()
                    + ",\"stepsJson\":{\"type\":\"string\",\"maxLength\":32768}"
                    + ",\"maxDurationMs\":{\"type\":\"integer\",\"minimum\":1000,\"maximum\":30000},"
                    + ConfirmMutationProperty() + "," + ConfirmSensitiveReadProperty(),
                    "windowHandle", "stepsJson", "confirmMutation")
            };
            descriptors.AddRange(McpDirectDiagnosticsThemeRuntime.ToolDescriptors());
            descriptors.AddRange(McpBackgroundHostRuntime.ToolDescriptors());
            return descriptors;
        }

        internal static string Call(string toolName, string arguments, Action? ensureMutationRunning, Action<string> audit)
        {
            var tool = toolName ?? string.Empty;
            if (!IsTool(tool)) throw new InvalidOperationException("Unknown MCP desktop tool: " + tool);
            var args = string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments;
            McpBackgroundHostRuntime.EnsureGlobalInteractionAllowed(tool);

            McpDesktopControlSession.GuardedActionScope? guardedAction = null;
            var requiresDesktopConsent = tool.StartsWith("desktop_", StringComparison.Ordinal)
                                         && (MutationTools.Contains(tool) || SensitiveTools.Contains(tool));
            if (requiresDesktopConsent)
            {
                McpDesktopControlSession.RequireLocalConsent(tool);
                guardedAction = McpDesktopControlSession.BeginGuardedAction(tool);
            }

            try
            {
                string result;
                switch (tool)
                {
                    case "diagnostics_log_tail":
                    case "diagnostics_since":
                    case "diagnostics_snapshot":
                    case "diagnostics_wait":
                    case "theme_get":
                    case "theme_set":
                        result = McpDirectDiagnosticsThemeRuntime.Call(tool, args, ensureMutationRunning, audit); break;
                    case "bricscad_interaction_policy_get":
                    case "bricscad_interaction_policy_set":
                    case "bricscad_ui_text_snapshot":
                    case "bricscad_ui_invoke":
                    case "bricscad_ui_set_text":
                        result = McpBackgroundHostRuntime.Call(tool, args, ensureMutationRunning, audit); break;
                    case "desktop_cursor_position": result = CursorPositionJson(); break;
                    case "desktop_window_list": result = WindowListJson(Integer(args, "limit", 30, 1, MaxWindows)); break;
                    case "desktop_foreground_window": result = ForegroundWindowJson(); break;
                    case "desktop_wait_for_window": result = WaitForWindow(args); break;
                    case "desktop_window_focus": result = FocusWindow(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    case "desktop_mouse_move": result = MouseMove(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    case "desktop_mouse_click": result = MouseClick(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    case "desktop_mouse_scroll": result = MouseScroll(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    case "desktop_mouse_drag": result = MouseDrag(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    case "desktop_type": result = TypeText(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    case "desktop_key": result = PressKey(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    case "desktop_clipboard_read": result = ClipboardRead(args, audit); break;
                    case "desktop_clipboard_write": result = ClipboardWrite(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    case "desktop_screenshot": result = Screenshot(args, audit); break;
                    case "desktop_sequence": result = RunSequence(args, RequireMutationCallback(ensureMutationRunning), audit); break;
                    default: throw new InvalidOperationException("Unknown MCP desktop tool: " + tool);
                }
                if (guardedAction != null) guardedAction.MarkSuccess();
                return result;
            }
            catch (Exception ex)
            {
                if (guardedAction != null) guardedAction.MarkFailed(ex);
                throw;
            }
            finally
            {
                if (guardedAction != null) guardedAction.Dispose();
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
            var windows = EnumerateWindows(limit);
            var builder = new StringBuilder("{\"windows\":[");
            for (var i = 0; i < windows.Count; i++)
            {
                if (i > 0) builder.Append(',');
                builder.Append(WindowJson(windows[i]));
            }
            return builder.Append("]}").ToString();
        }

        private static List<WindowInfo> EnumerateWindows(int limit)
        {
            var windows = new List<WindowInfo>();
            EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                if (windows.Count >= limit) return false;
                WindowInfo info;
                if (TryGetWindowInfo(hwnd, true, out info)) windows.Add(info);
                return windows.Count < limit;
            }, IntPtr.Zero);
            return windows;
        }

        private static string ForegroundWindowJson()
        {
            EnsureInteractiveSession();
            var hwnd = GetForegroundWindow();
            WindowInfo info;
            if (hwnd == IntPtr.Zero || !TryGetWindowInfo(hwnd, false, out info)) return "{\"window\":null}";
            return "{\"window\":" + WindowJson(info) + "}";
        }

        private static string WaitForWindow(string body)
        {
            EnsureInteractiveSession();
            var titleContains = McpTopLevelJson.ExtractString(body, "titleContains").Trim();
            if (titleContains.Length > MaxWaitTitleLength)
                throw new InvalidOperationException("titleContains exceeds " + MaxWaitTitleLength.ToString(CultureInfo.InvariantCulture) + " characters.");
            var handleText = McpTopLevelJson.ExtractString(body, "windowHandle").Trim();
            var hasHandle = handleText.Length > 0;
            IntPtr expected = IntPtr.Zero;
            if (hasHandle) expected = ParseWindowHandle(handleText);
            if (!hasHandle && titleContains.Length == 0)
                throw new InvalidOperationException("desktop_wait_for_window requires windowHandle and/or titleContains.");

            var timeout = Integer(body, "timeoutMs", 5000, 0, MaxWaitMilliseconds);
            var poll = Integer(body, "pollIntervalMs", 100, MinWaitPollMilliseconds, MaxWaitPollMilliseconds);
            var started = Stopwatch.StartNew();
            while (true)
            {
                WindowInfo match;
                if (TryFindWindow(expected, hasHandle, titleContains, out match))
                    return "{\"found\":true,\"elapsedMs\":" + started.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture)
                           + ",\"window\":" + WindowJson(match) + "}";
                if (started.ElapsedMilliseconds >= timeout)
                    return "{\"found\":false,\"elapsedMs\":" + started.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture) + "}";
                Thread.Sleep(Math.Min(poll, Math.Max(1, timeout - (int)Math.Min(timeout, started.ElapsedMilliseconds))));
            }
        }

        private static bool TryFindWindow(IntPtr expected, bool hasHandle, string titleContains, out WindowInfo match)
        {
            match = new WindowInfo();
            if (hasHandle)
            {
                WindowInfo info;
                if (!TryGetWindowInfo(expected, false, out info)) return false;
                if (titleContains.Length != 0 && info.Title.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) < 0) return false;
                match = info;
                return true;
            }

            foreach (var info in EnumerateWindows(MaxWindows))
            {
                if (info.Title.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) < 0) continue;
                match = info;
                return true;
            }
            return false;
        }

        private static string RunSequence(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var steps = ParseSequenceSteps(body);
            if (steps.Count == 0) throw new InvalidOperationException("desktop_sequence requires at least one step.");
            var maxDuration = StrictOptionalInteger(body, "maxDurationMs", 15000, 1000, MaxSequenceMilliseconds);
            var sensitiveConfirmed = McpTopLevelJson.ExtractBoolean(body, "confirmSensitiveRead");
            var screenshotCount = 0;
            foreach (var step in steps)
            {
                if (string.Equals(step.Tool, "desktop_screenshot", StringComparison.Ordinal)) screenshotCount++;
            }
            if (screenshotCount > MaxSequenceScreenshots)
                throw new InvalidOperationException("desktop_sequence permits at most one screenshot step to keep output bounded.");
            if (screenshotCount > 0 && !sensitiveConfirmed)
                throw new InvalidOperationException("confirmSensitiveRead=true is required for desktop_sequence screenshot steps.");

            var started = Stopwatch.StartNew();
            EnsureSequenceRunning(hwnd, ensureMutationRunning, started, maxDuration);
            var results = new StringBuilder("{\"executed\":true,\"windowHandle\":\"")
                .Append(HandleText(hwnd)).Append("\",\"results\":[");
            var completed = 0;
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                try
                {
                    EnsureSequenceRunning(hwnd, ensureMutationRunning, started, maxDuration);
                    var result = ExecuteSequenceStep(step, hwnd, sensitiveConfirmed, ensureMutationRunning, audit, started, maxDuration);
                    EnsureSequenceRunning(hwnd, ensureMutationRunning, started, maxDuration);
                    if (completed > 0) results.Append(',');
                    results.Append("{\"index\":").Append(i + 1)
                        .Append(",\"tool\":\"").Append(Escape(step.Tool)).Append("\",\"result\":")
                        .Append(result).Append('}');
                    completed++;
                    Audit(audit, "sequence step=" + (i + 1).ToString(CultureInfo.InvariantCulture)
                                 + "; tool=" + step.Tool + "; status=success");
                    if (step.DelayAfterMilliseconds > 0)
                        DelaySequence(hwnd, step.DelayAfterMilliseconds, ensureMutationRunning, started, maxDuration);
                }
                catch (Exception ex)
                {
                    Audit(audit, "sequence step=" + (i + 1).ToString(CultureInfo.InvariantCulture)
                                 + "; tool=" + step.Tool + "; status=failed; completed="
                                 + completed.ToString(CultureInfo.InvariantCulture) + "; durationMs="
                                 + started.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));
                    throw new InvalidOperationException(
                        "desktop_sequence failed at step " + (i + 1).ToString(CultureInfo.InvariantCulture)
                        + " (" + step.Tool + ") after " + completed.ToString(CultureInfo.InvariantCulture)
                        + " completed step(s). Sequence execution is fail-fast. Sequence does not roll back completed steps. Cause: "
                        + ex.Message, ex);
                }
            }

            EnsureSequenceRunning(hwnd, ensureMutationRunning, started, maxDuration);
            var duration = started.ElapsedMilliseconds;
            Audit(audit, "sequence status=success; completed=" + completed.ToString(CultureInfo.InvariantCulture)
                         + "; durationMs=" + duration.ToString(CultureInfo.InvariantCulture));
            return results.Append("],\"stepsCompleted\":").Append(completed)
                .Append(",\"durationMs\":").Append(duration).Append('}').ToString();
        }

        private static string ExecuteSequenceStep(
            SequenceStep step,
            IntPtr hwnd,
            bool sensitiveConfirmed,
            Action ensureMutationRunning,
            Action<string> audit,
            Stopwatch sequenceStarted,
            int maxDuration)
        {
            Action sequenceGuard = delegate
            {
                EnsureSequenceStepRunning(hwnd, ensureMutationRunning, sequenceStarted, maxDuration);
            };
            sequenceGuard();
            var args = step.Arguments;
            switch (step.Tool)
            {
                case "desktop_window_focus":
                    return FocusWindow(WithSequenceWindow(args, hwnd), sequenceGuard, audit);
                case "desktop_mouse_move":
                {
                    var x = IntegerRequired(args, "x", -1000000, 1000000);
                    var y = IntegerRequired(args, "y", -1000000, 1000000);
                    RequirePointInsideWindow(hwnd, x, y);
                    sequenceGuard();
                    FocusAndVerify(hwnd);
                    EnsureTargetReady(hwnd, x, y, sequenceGuard);
                    if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows rejected the sequence cursor move.");
                    EnsureTargetReady(hwnd, x, y, sequenceGuard);
                    Audit(audit, "handle=" + HandleText(hwnd) + "; x=" + x.ToString(CultureInfo.InvariantCulture)
                                 + "; y=" + y.ToString(CultureInfo.InvariantCulture));
                    return "{\"moved\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\",\"x\":"
                           + x.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + y.ToString(CultureInfo.InvariantCulture) + "}";
                }
                case "desktop_mouse_click":
                    return MouseClick(WithSequenceWindow(args, hwnd), sequenceGuard, audit);
                case "desktop_mouse_scroll":
                    return MouseScroll(WithSequenceWindow(args, hwnd), sequenceGuard, audit);
                case "desktop_mouse_drag":
                    return MouseDrag(WithSequenceWindow(args, hwnd), sequenceGuard, audit);
                case "desktop_type":
                    return TypeText(WithSequenceWindow(args, hwnd), sequenceGuard, audit);
                case "desktop_key":
                    return PressKey(WithSequenceWindow(args, hwnd), sequenceGuard, audit);
                case "desktop_clipboard_write":
                    return ClipboardWrite(args, sequenceGuard, audit);
                case "desktop_wait_for_window":
                    return WaitForSequenceTarget(args, hwnd, ensureMutationRunning, sequenceStarted, maxDuration);
                case "desktop_screenshot":
                    if (!sensitiveConfirmed)
                        throw new InvalidOperationException("confirmSensitiveRead=true is required for desktop_sequence screenshot steps.");
                    return Screenshot(WithSequenceScreenshot(args, hwnd), audit);
                default:
                    throw new InvalidOperationException("Sequence step tool is not allowlisted: " + step.Tool + ".");
            }
        }

        private static List<SequenceStep> ParseSequenceSteps(string body)
        {
            var raw = McpTopLevelJson.ExtractString(body, "stepsJson");
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("stepsJson is required and must contain a JSON array of sequence steps.");
            if (raw.Length > MaxSequenceJsonCharacters)
                throw new InvalidOperationException("stepsJson exceeds " + MaxSequenceJsonCharacters.ToString(CultureInfo.InvariantCulture) + " characters.");

            var steps = new List<SequenceStep>();
            var index = 0;
            SkipSequenceWhitespace(raw, ref index);
            if (index >= raw.Length || raw[index] != '[')
                throw new InvalidOperationException("stepsJson must be a JSON array.");
            index++;
            while (true)
            {
                SkipSequenceWhitespace(raw, ref index);
                if (index >= raw.Length) throw new InvalidOperationException("stepsJson ended unexpectedly.");
                if (raw[index] == ']')
                {
                    index++;
                    SkipSequenceWhitespace(raw, ref index);
                    if (index != raw.Length) throw new InvalidOperationException("Unexpected content after stepsJson array.");
                    break;
                }
                if (steps.Count >= MaxSequenceSteps)
                    throw new InvalidOperationException("desktop_sequence exceeds the maximum of " + MaxSequenceSteps.ToString(CultureInfo.InvariantCulture) + " steps.");

                var stepJson = ReadSequenceStepObject(raw, ref index);
                var tool = McpTopLevelJson.ExtractString(stepJson, "tool").Trim();
                if (tool.Length == 0) throw new InvalidOperationException("Each sequence step requires tool.");
                if (string.Equals(tool, "desktop_clipboard_read", StringComparison.Ordinal))
                    throw new InvalidOperationException("Sequence cannot include desktop_clipboard_read.");
                if (!SequenceAllowedTools.Contains(tool))
                    throw new InvalidOperationException("Sequence step tool is not allowlisted: " + tool + ".");

                var hasArguments = McpTopLevelJson.HasProperty(stepJson, "arguments");
                var arguments = hasArguments ? McpTopLevelJson.ExtractString(stepJson, "arguments") : "{}";
                if (hasArguments && string.IsNullOrWhiteSpace(arguments))
                    throw new InvalidOperationException("Sequence step arguments must be a JSON object string.");
                arguments = ValidateSequenceArguments(arguments);
                if (McpTopLevelJson.HasProperty(arguments, "windowHandle"))
                    throw new InvalidOperationException("Sequence step arguments must not contain windowHandle; the sequence owns the exact target.");
                if (McpTopLevelJson.HasProperty(arguments, "confirmMutation"))
                    throw new InvalidOperationException("Sequence step arguments must not contain confirmMutation; the sequence owns mutation confirmation.");
                if (McpTopLevelJson.HasProperty(arguments, "confirmSensitiveRead"))
                    throw new InvalidOperationException("Sequence step arguments must not contain confirmSensitiveRead; the sequence owns sensitive-read confirmation.");
                if (string.Equals(tool, "desktop_screenshot", StringComparison.Ordinal)
                    && McpTopLevelJson.HasProperty(arguments, "scope"))
                    throw new InvalidOperationException("Sequence screenshot is forced to the bound target window; step scope must be omitted.");

                var delay = StrictOptionalInteger(stepJson, "delayAfterMs", 0, 0, MaxSequenceDelayMilliseconds);
                steps.Add(new SequenceStep { Tool = tool, Arguments = arguments, DelayAfterMilliseconds = delay });

                SkipSequenceWhitespace(raw, ref index);
                if (index >= raw.Length) throw new InvalidOperationException("stepsJson ended unexpectedly after a step.");
                if (raw[index] == ',')
                {
                    index++;
                    SkipSequenceWhitespace(raw, ref index);
                    if (index >= raw.Length || raw[index] == ']')
                        throw new InvalidOperationException("stepsJson cannot contain a trailing comma.");
                    continue;
                }
                if (raw[index] != ']')
                    throw new InvalidOperationException("stepsJson requires ',' or ']' after each step.");
            }
            return steps;
        }

        private static string ReadSequenceStepObject(string source, ref int index)
        {
            if (index >= source.Length || source[index] != '{')
                throw new InvalidOperationException("Each stepsJson element must be a flat JSON object.");
            var start = index;
            var depth = 0;
            var inString = false;
            var escaped = false;
            while (index < source.Length)
            {
                var ch = source[index++];
                if (inString)
                {
                    if (escaped) { escaped = false; continue; }
                    if (ch == '\\') { escaped = true; continue; }
                    if (ch == '"') inString = false;
                    continue;
                }
                if (ch == '"') { inString = true; continue; }
                if (ch == '[') throw new InvalidOperationException("Sequence step records must be flat JSON objects; nested arrays are not allowed.");
                if (ch == '{')
                {
                    depth++;
                    if (depth > 1) throw new InvalidOperationException("Sequence step records must be flat JSON objects; nested objects are not allowed.");
                    continue;
                }
                if (ch == '}')
                {
                    depth--;
                    if (depth < 0) throw new InvalidOperationException("Invalid stepsJson object boundary.");
                    if (depth == 0) return source.Substring(start, index - start);
                }
            }
            throw new InvalidOperationException("Unterminated sequence step object.");
        }

        private static string ValidateSequenceArguments(string arguments)
        {
            var value = (arguments ?? string.Empty).Trim();
            if (value.Length == 0) value = "{}";
            if (value.Length > MaxSequenceStepArgumentsCharacters)
                throw new InvalidOperationException("Sequence step arguments exceed " + MaxSequenceStepArgumentsCharacters.ToString(CultureInfo.InvariantCulture) + " characters.");
            if (value.Length < 2 || value[0] != '{' || value[value.Length - 1] != '}')
                throw new InvalidOperationException("Sequence step arguments must be a flat JSON object string.");

            var depth = 0;
            var inString = false;
            var escaped = false;
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (inString)
                {
                    if (escaped) { escaped = false; continue; }
                    if (ch == '\\') { escaped = true; continue; }
                    if (ch == '"') inString = false;
                    continue;
                }
                if (ch == '"') { inString = true; continue; }
                if (ch == '[' || ch == ']')
                    throw new InvalidOperationException("Sequence step arguments must be flat JSON scalars; arrays are not allowed.");
                if (ch == '{')
                {
                    depth++;
                    if (depth > 1) throw new InvalidOperationException("Sequence step arguments must be flat JSON scalars; nested objects are not allowed.");
                }
                else if (ch == '}')
                {
                    depth--;
                    if (depth < 0) throw new InvalidOperationException("Sequence step arguments contain an invalid object boundary.");
                    if (depth == 0 && i != value.Length - 1)
                        throw new InvalidOperationException("Unexpected content after sequence step arguments object.");
                }
            }
            if (inString || escaped || depth != 0)
                throw new InvalidOperationException("Sequence step arguments contain unterminated JSON content.");

            string ignoredRaw, scanError;
            bool ignoredFound;
            if (!McpTopLevelJson.TryFindPropertyValue(value, "__qs3d_sequence_validation__", out ignoredRaw, out ignoredFound, out scanError))
                throw new InvalidOperationException(scanError);
            return value;
        }

        private static string WithSequenceWindow(string arguments, IntPtr hwnd)
        {
            return AddSequenceProperty(arguments, "windowHandle", "\"" + HandleText(hwnd) + "\"");
        }

        private static string WithSequenceScreenshot(string arguments, IntPtr hwnd)
        {
            var value = AddSequenceProperty(arguments, "scope", "\"window\"");
            value = AddSequenceProperty(value, "windowHandle", "\"" + HandleText(hwnd) + "\"");
            value = AddSequenceProperty(value, "confirmSensitiveRead", "true");
            return value;
        }

        private static string AddSequenceProperty(string arguments, string name, string rawJsonValue)
        {
            var value = ValidateSequenceArguments(arguments);
            if (McpTopLevelJson.HasProperty(value, name))
                throw new InvalidOperationException("Sequence executor owns argument property: " + name + ".");
            if (value == "{}") return "{\"" + Escape(name) + "\":" + rawJsonValue + "}";
            return value.Substring(0, value.Length - 1) + ",\"" + Escape(name) + "\":" + rawJsonValue + "}";
        }

        private static string WaitForSequenceTarget(
            string arguments,
            IntPtr hwnd,
            Action ensureMutationRunning,
            Stopwatch sequenceStarted,
            int maxDuration)
        {
            var titleContains = McpTopLevelJson.ExtractString(arguments, "titleContains").Trim();
            if (titleContains.Length > MaxWaitTitleLength)
                throw new InvalidOperationException("titleContains exceeds " + MaxWaitTitleLength.ToString(CultureInfo.InvariantCulture) + " characters.");
            var timeout = StrictOptionalInteger(arguments, "timeoutMs", 5000, 0, MaxWaitMilliseconds);
            var poll = StrictOptionalInteger(arguments, "pollIntervalMs", 100, MinWaitPollMilliseconds, MaxWaitPollMilliseconds);
            var waitStarted = Stopwatch.StartNew();
            while (true)
            {
                EnsureSequenceRunning(hwnd, ensureMutationRunning, sequenceStarted, maxDuration);
                WindowInfo info;
                if (TryGetWindowInfo(hwnd, false, out info)
                    && (titleContains.Length == 0 || info.Title.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) >= 0))
                    return "{\"found\":true,\"elapsedMs\":" + waitStarted.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture)
                           + ",\"window\":" + WindowJson(info) + "}";
                if (waitStarted.ElapsedMilliseconds >= timeout)
                    throw new TimeoutException("Sequence wait-for-target timed out before the bound target matched.");
                var sleep = Math.Min(poll, SequenceDelaySliceMilliseconds);
                Thread.Sleep(Math.Max(1, sleep));
            }
        }

        private static void DelaySequence(
            IntPtr hwnd,
            int milliseconds,
            Action ensureMutationRunning,
            Stopwatch sequenceStarted,
            int maxDuration)
        {
            var remaining = milliseconds;
            while (remaining > 0)
            {
                EnsureSequenceRunning(hwnd, ensureMutationRunning, sequenceStarted, maxDuration);
                var slice = Math.Min(SequenceDelaySliceMilliseconds, remaining);
                Thread.Sleep(slice);
                remaining -= slice;
            }
            EnsureSequenceRunning(hwnd, ensureMutationRunning, sequenceStarted, maxDuration);
        }

        private static void EnsureSequenceRunning(IntPtr hwnd, Action ensureMutationRunning, Stopwatch started, int maxDuration)
        {
            ensureMutationRunning();
            McpDesktopControlSession.RequireLocalConsent("desktop_sequence");
            if (started.ElapsedMilliseconds > maxDuration)
                throw new TimeoutException("desktop_sequence exceeded its bounded maximum duration.");
            ValidateWindow(hwnd, true);
        }

        private static void EnsureSequenceStepRunning(IntPtr hwnd, Action ensureMutationRunning, Stopwatch started, int maxDuration)
        {
            EnsureSequenceRunning(hwnd, ensureMutationRunning, started, maxDuration);
        }

        private static void SkipSequenceWhitespace(string value, ref int index)
        {
            while (index < value.Length)
            {
                var ch = value[index];
                if (ch != ' ' && ch != '\t' && ch != '\r' && ch != '\n') break;
                index++;
            }
        }

        private static string FocusWindow(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            ensureMutationRunning();
            FocusAndVerify(hwnd);
            Audit(audit, "handle=" + HandleText(hwnd));
            return "{\"focused\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\"}";
        }

        private static string MouseMove(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var x = IntegerRequired(body, "x", -1000000, 1000000);
            var y = IntegerRequired(body, "y", -1000000, 1000000);
            RequireVirtualDesktopPoint(x, y);
            ensureMutationRunning();
            if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows rejected the cursor move.");
            Audit(audit, "x=" + x.ToString(CultureInfo.InvariantCulture) + "; y=" + y.ToString(CultureInfo.InvariantCulture));
            return "{\"moved\":true,\"x\":" + x.ToString(CultureInfo.InvariantCulture)
                   + ",\"y\":" + y.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string MouseClick(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var x = IntegerRequired(body, "x", -1000000, 1000000);
            var y = IntegerRequired(body, "y", -1000000, 1000000);
            var button = RequiredMouseButton(body, out var down, out var up);
            var count = Integer(body, "count", 1, 1, 3);
            RequirePointInsideWindow(hwnd, x, y);
            ensureMutationRunning();
            FocusAndVerify(hwnd);

            for (var i = 0; i < count; i++)
            {
                EnsureTargetReady(hwnd, x, y, ensureMutationRunning);
                if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows rejected the cursor move before click.");
                EnsureTargetReady(hwnd, x, y, ensureMutationRunning);
                SendMouse(down, 0);
                EnsureTargetReady(hwnd, x, y, ensureMutationRunning);
                SendMouse(up, 0);
                if (i + 1 < count) Thread.Sleep(40);
            }

            Audit(audit, "handle=" + HandleText(hwnd) + "; x=" + x.ToString(CultureInfo.InvariantCulture)
                         + "; y=" + y.ToString(CultureInfo.InvariantCulture) + "; button=" + button
                         + "; count=" + count.ToString(CultureInfo.InvariantCulture));
            return "{\"clicked\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\",\"x\":"
                   + x.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + y.ToString(CultureInfo.InvariantCulture)
                   + ",\"button\":\"" + Escape(button) + "\",\"count\":" + count.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string MouseScroll(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var x = IntegerRequired(body, "x", -1000000, 1000000);
            var y = IntegerRequired(body, "y", -1000000, 1000000);
            var delta = IntegerRequired(body, "delta", -1200, 1200);
            if (delta == 0) throw new InvalidOperationException("delta must be non-zero.");
            RequirePointInsideWindow(hwnd, x, y);
            ensureMutationRunning();
            FocusAndVerify(hwnd);
            EnsureTargetReady(hwnd, x, y, ensureMutationRunning);
            if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows rejected the cursor move before scroll.");
            EnsureTargetReady(hwnd, x, y, ensureMutationRunning);
            SendMouse(MOUSEEVENTF_WHEEL, unchecked((uint)delta));
            Audit(audit, "handle=" + HandleText(hwnd) + "; x=" + x.ToString(CultureInfo.InvariantCulture)
                         + "; y=" + y.ToString(CultureInfo.InvariantCulture) + "; delta=" + delta.ToString(CultureInfo.InvariantCulture));
            return "{\"scrolled\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\",\"x\":"
                   + x.ToString(CultureInfo.InvariantCulture) + ",\"y\":" + y.ToString(CultureInfo.InvariantCulture)
                   + ",\"delta\":" + delta.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string MouseDrag(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var startX = IntegerRequired(body, "startX", -1000000, 1000000);
            var startY = IntegerRequired(body, "startY", -1000000, 1000000);
            var endX = IntegerRequired(body, "endX", -1000000, 1000000);
            var endY = IntegerRequired(body, "endY", -1000000, 1000000);
            var duration = Integer(body, "durationMs", 350, MinDragMilliseconds, MaxDragMilliseconds);
            var button = RequiredMouseButton(body, out var down, out var up);
            RequirePointInsideWindow(hwnd, startX, startY);
            RequirePointInsideWindow(hwnd, endX, endY);
            ensureMutationRunning();
            FocusAndVerify(hwnd);
            EnsureTargetReady(hwnd, startX, startY, ensureMutationRunning);
            if (!SetCursorPos(startX, startY)) throw new InvalidOperationException("Windows rejected the drag start cursor move.");

            var buttonDown = false;
            try
            {
                EnsureTargetReady(hwnd, startX, startY, ensureMutationRunning);
                SendMouse(down, 0);
                buttonDown = true;
                var steps = Math.Max(2, Math.Min(120, (duration + DragStepMilliseconds - 1) / DragStepMilliseconds));
                var sleep = Math.Max(1, duration / steps);
                for (var i = 1; i <= steps; i++)
                {
                    var x = startX + (int)Math.Round((endX - startX) * (i / (double)steps));
                    var y = startY + (int)Math.Round((endY - startY) * (i / (double)steps));
                    EnsureTargetReady(hwnd, x, y, ensureMutationRunning);
                    if (!SetCursorPos(x, y)) throw new InvalidOperationException("Windows rejected an intermediate drag cursor move.");
                    if (i < steps) Thread.Sleep(sleep);
                }
                EnsureTargetReady(hwnd, endX, endY, ensureMutationRunning);
                SendMouse(up, 0);
                buttonDown = false;
            }
            finally
            {
                if (buttonDown)
                {
                    try { SendMouse(up, 0); } catch { }
                }
            }

            Audit(audit, "handle=" + HandleText(hwnd) + "; start=" + startX.ToString(CultureInfo.InvariantCulture) + ","
                         + startY.ToString(CultureInfo.InvariantCulture) + "; end=" + endX.ToString(CultureInfo.InvariantCulture) + ","
                         + endY.ToString(CultureInfo.InvariantCulture) + "; button=" + button + "; durationMs=" + duration.ToString(CultureInfo.InvariantCulture));
            return "{\"dragged\":true,\"windowHandle\":\"" + HandleText(hwnd) + "\",\"button\":\"" + Escape(button)
                   + "\",\"durationMs\":" + duration.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string TypeText(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var text = RequiredText(body, "text", MaxTypedCharacters);
            ensureMutationRunning();
            FocusAndVerify(hwnd);
            foreach (var ch in text)
            {
                ensureMutationRunning();
                RequireForegroundWindow(hwnd);
                SendInputs(new[]
                {
                    new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE } } },
                    new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } } }
                }, "Unicode keyboard input");
            }
            Audit(audit, "handle=" + HandleText(hwnd) + "; chars=" + text.Length.ToString(CultureInfo.InvariantCulture));
            return "{\"typed\":true,\"windowHandle\":\"" + HandleText(hwnd)
                   + "\",\"characters\":" + text.Length.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string PressKey(string body, Action ensureMutationRunning, Action<string> audit)
        {
            var hwnd = RequiredWindow(body);
            var keyName = McpTopLevelJson.ExtractString(body, "key").Trim().ToUpperInvariant();
            if (keyName.Length == 0 || keyName.Length > 24)
                throw new InvalidOperationException("key is required and must be <=24 characters.");
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
            var auditKey = IsCharacterKey(keyName) ? "CHARACTER" : keyName;
            Audit(audit, "handle=" + HandleText(hwnd) + "; key=" + auditKey + "; ctrl=" + ctrl
                         + "; alt=" + alt + "; shift=" + shift + "; win=" + win);
            return "{\"pressed\":true,\"windowHandle\":\"" + HandleText(hwnd)
                   + "\",\"keyType\":\"" + (IsCharacterKey(keyName) ? "character" : "named") + "\"}";
        }

        private static bool IsCharacterKey(string keyName)
        {
            return keyName.Length == 1 && ((keyName[0] >= 'A' && keyName[0] <= 'Z') || (keyName[0] >= '0' && keyName[0] <= '9'));
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
            if (scope != "screen" && scope != "window")
                throw new InvalidOperationException("scope must be screen or window.");
            var maxWidth = Integer(body, "maxWidth", MaxScreenshotWidth, 160, MaxScreenshotWidth);
            var maxHeight = Integer(body, "maxHeight", MaxScreenshotHeight, 120, MaxScreenshotHeight);

            BitmapSource source;
            var handle = string.Empty;
            if (scope == "window")
            {
                var hwnd = RequiredWindow(body);
                RECT sourceRect;
                if (!GetWindowRect(hwnd, out sourceRect)) throw new InvalidOperationException("Could not read the target window bounds.");
                var fullWidth = sourceRect.Right - sourceRect.Left;
                var fullHeight = sourceRect.Bottom - sourceRect.Top;
                if (fullWidth <= 0 || fullHeight <= 0) throw new InvalidOperationException("Target window bounds are empty.");
                var rect = ApplyScreenshotCrop(body, sourceRect);
                var cropX = rect.Left - sourceRect.Left;
                var cropY = rect.Top - sourceRect.Top;
                var cropWidth = rect.Right - rect.Left;
                var cropHeight = rect.Bottom - rect.Top;
                var fullWindow = CaptureWindowBitmap(hwnd, fullWidth, fullHeight);
                source = CropBitmap(fullWindow, cropX, cropY, cropWidth, cropHeight);
                handle = HandleText(hwnd);
            }
            else
            {
                var sourceRect = VirtualDesktopRect();
                var rect = ApplyScreenshotCrop(body, sourceRect);
                var width = rect.Right - rect.Left;
                var height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0) throw new InvalidOperationException("Screenshot bounds are empty.");
                source = CaptureBitmap(rect.Left, rect.Top, width, height);
            }

            source = ScaleBitmap(source, maxWidth, maxHeight);
            byte[] png;
            while (true)
            {
                png = EncodePng(source);
                if (png.Length <= MaxScreenshotBytes) break;
                if (source.PixelWidth <= 160 || source.PixelHeight <= 120)
                    throw new InvalidOperationException("Screenshot exceeds the bounded MCP output size.");
                source = ScaleBitmap(source,
                    Math.Max(160, source.PixelWidth * 3 / 4),
                    Math.Max(120, source.PixelHeight * 3 / 4));
            }

            Audit(audit, "screenshot scope=" + scope + "; width=" + source.PixelWidth.ToString(CultureInfo.InvariantCulture)
                         + "; height=" + source.PixelHeight.ToString(CultureInfo.InvariantCulture)
                         + (handle.Length == 0 ? string.Empty : "; handle=" + handle));
            return "{\"scope\":\"" + scope + "\",\"windowHandle\":\"" + handle
                   + "\",\"mimeType\":\"image/png\",\"width\":" + source.PixelWidth.ToString(CultureInfo.InvariantCulture)
                   + ",\"height\":" + source.PixelHeight.ToString(CultureInfo.InvariantCulture)
                   + ",\"bytes\":" + png.Length.ToString(CultureInfo.InvariantCulture)
                   + ",\"pngBase64\":\"" + Convert.ToBase64String(png) + "\"}";
        }

        private static RECT ApplyScreenshotCrop(string body, RECT source)
        {
            int cropX, cropY, cropWidth, cropHeight;
            bool hasX, hasY, hasWidth, hasHeight;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, "cropX", out cropX, out hasX, out error)) throw new InvalidOperationException(error);
            if (!McpTopLevelJson.TryExtractInteger(body, "cropY", out cropY, out hasY, out error)) throw new InvalidOperationException(error);
            if (!McpTopLevelJson.TryExtractInteger(body, "cropWidth", out cropWidth, out hasWidth, out error)) throw new InvalidOperationException(error);
            if (!McpTopLevelJson.TryExtractInteger(body, "cropHeight", out cropHeight, out hasHeight, out error)) throw new InvalidOperationException(error);
            var any = hasX || hasY || hasWidth || hasHeight;
            if (!any) return source;
            if (!(hasX && hasY && hasWidth && hasHeight) || cropWidth <= 0 || cropHeight <= 0)
                throw new InvalidOperationException("cropX, cropY, cropWidth and cropHeight must all be provided; crop dimensions must be > 0.");

            var left = Math.Max((long)source.Left, (long)source.Left + cropX);
            var top = Math.Max((long)source.Top, (long)source.Top + cropY);
            var right = Math.Min((long)source.Right, (long)source.Left + cropX + cropWidth);
            var bottom = Math.Min((long)source.Bottom, (long)source.Top + cropY + cropHeight);
            if (right <= left || bottom <= top)
                throw new InvalidOperationException("Screenshot crop does not intersect the selected source bounds.");
            return new RECT { Left = checked((int)left), Top = checked((int)top), Right = checked((int)right), Bottom = checked((int)bottom) };
        }

        private static BitmapSource CaptureWindowBitmap(IntPtr hwnd, int width, int height)
        {
            var screen = GetDC(IntPtr.Zero);
            if (screen == IntPtr.Zero) throw new InvalidOperationException("Could not acquire a compatible device context for window capture.");
            IntPtr memory = IntPtr.Zero;
            IntPtr bitmap = IntPtr.Zero;
            IntPtr previous = IntPtr.Zero;
            try
            {
                memory = CreateCompatibleDC(screen);
                if (memory == IntPtr.Zero) throw new InvalidOperationException("Could not create window screenshot memory context.");
                bitmap = CreateCompatibleBitmap(screen, width, height);
                if (bitmap == IntPtr.Zero) throw new InvalidOperationException("Could not create window screenshot bitmap.");
                previous = SelectObject(memory, bitmap);
                if (previous == IntPtr.Zero) throw new InvalidOperationException("Could not select window screenshot bitmap.");
                if (!PrintWindow(hwnd, memory, PW_RENDERFULLCONTENT))
                    throw new InvalidOperationException("Windows PrintWindow could not render the target window without foreground capture.");
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

        private static BitmapSource CropBitmap(BitmapSource source, int x, int y, int width, int height)
        {
            if (x == 0 && y == 0 && width == source.PixelWidth && height == source.PixelHeight) return source;
            if (x < 0 || y < 0 || width <= 0 || height <= 0 || x + width > source.PixelWidth || y + height > source.PixelHeight)
                throw new InvalidOperationException("Window screenshot crop is outside the rendered window bounds.");
            var cropped = new CroppedBitmap(source, new Int32Rect(x, y, width, height));
            cropped.Freeze();
            return cropped;
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

        private static void EnsureTargetReady(IntPtr hwnd, int x, int y, Action ensureMutationRunning)
        {
            ensureMutationRunning();
            RequireForegroundWindow(hwnd);
            RequirePointInsideWindow(hwnd, x, y);
        }

        private static void RequireForegroundWindow(IntPtr expected)
        {
            ValidateWindow(expected, true);
            if (GetForegroundWindow() != expected)
                throw new InvalidOperationException("Desktop foreground window changed; input stopped before injection.");
        }

        private static void RequirePointInsideWindow(IntPtr hwnd, int x, int y)
        {
            ValidateWindow(hwnd, true);
            RECT rect;
            if (!GetWindowRect(hwnd, out rect)) throw new InvalidOperationException("Could not revalidate target window bounds.");
            if (x < rect.Left || x >= rect.Right || y < rect.Top || y >= rect.Bottom)
                throw new InvalidOperationException("Desktop input point must stay inside the current target window bounds.");
            RequireVirtualDesktopPoint(x, y);
        }

        private static IntPtr RequiredWindow(string body)
        {
            var text = McpTopLevelJson.ExtractString(body, "windowHandle").Trim();
            if (text.Length == 0) throw new InvalidOperationException("windowHandle is required.");
            var hwnd = ParseWindowHandle(text);
            ValidateWindow(hwnd, true);
            return hwnd;
        }

        private static IntPtr ParseWindowHandle(string text)
        {
            ulong value;
            if (text.Length == 0 || text.Length > 16
                || !ulong.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) || value == 0)
                throw new InvalidOperationException("windowHandle must be a non-zero hexadecimal window handle up to 16 characters.");
            return new IntPtr(unchecked((long)value));
        }

        private static void ValidateWindow(IntPtr hwnd, bool requireVisible)
        {
            EnsureInteractiveSession();
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                throw new InvalidOperationException("Desktop window handle is no longer valid.");
            if (requireVisible && !IsWindowVisible(hwnd))
                throw new InvalidOperationException("Desktop window must be visible.");

            uint processId;
            if (GetWindowThreadProcessId(hwnd, out processId) == 0 || processId == 0)
                throw new InvalidOperationException("Could not identify the desktop window process.");
            Process process;
            try { process = Process.GetProcessById(checked((int)processId)); }
            catch (Exception ex) { throw new InvalidOperationException("Desktop window process is unavailable.", ex); }
            using (process)
            using (var current = Process.GetCurrentProcess())
            {
                if (process.SessionId != current.SessionId)
                    throw new InvalidOperationException("Desktop window belongs to a different Windows session.");
            }
        }

        private static bool TryGetWindowInfo(IntPtr hwnd, bool requireTitle, out WindowInfo info)
        {
            info = new WindowInfo();
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
                   + "\",\"bounds\":{\"x\":" + info.Left.ToString(CultureInfo.InvariantCulture)
                   + ",\"y\":" + info.Top.ToString(CultureInfo.InvariantCulture)
                   + ",\"width\":" + info.Width.ToString(CultureInfo.InvariantCulture)
                   + ",\"height\":" + info.Height.ToString(CultureInfo.InvariantCulture)
                   + "},\"foreground\":" + (info.Foreground ? "true" : "false") + "}";
        }

        private static RECT VirtualDesktopRect()
        {
            var x = GetSystemMetrics(SM_XVIRTUALSCREEN);
            var y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            var width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            var height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("Windows virtual desktop bounds are unavailable.");
            return new RECT { Left = x, Top = y, Right = x + width, Bottom = y + height };
        }

        private static void RequireVirtualDesktopPoint(int x, int y)
        {
            var rect = VirtualDesktopRect();
            if (x < rect.Left || x >= rect.Right || y < rect.Top || y >= rect.Bottom)
                throw new InvalidOperationException("Desktop coordinates must stay inside the Windows virtual desktop.");
        }

        private static string RequiredMouseButton(string body, out uint down, out uint up)
        {
            var button = McpTopLevelJson.ExtractString(body, "button").Trim().ToLowerInvariant();
            if (button == "left") { down = MOUSEEVENTF_LEFTDOWN; up = MOUSEEVENTF_LEFTUP; }
            else if (button == "right") { down = MOUSEEVENTF_RIGHTDOWN; up = MOUSEEVENTF_RIGHTUP; }
            else if (button == "middle") { down = MOUSEEVENTF_MIDDLEDOWN; up = MOUSEEVENTF_MIDDLEUP; }
            else throw new InvalidOperationException("button must be left, right or middle.");
            return button;
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
                case "ENTER": return 0x0D;
                case "ESC":
                case "ESCAPE": return 0x1B;
                case "TAB": return 0x09;
                case "BACKSPACE": return 0x08;
                case "DELETE": return 0x2E;
                case "INSERT": return 0x2D;
                case "SPACE": return 0x20;
                case "LEFT": return 0x25;
                case "UP": return 0x26;
                case "RIGHT": return 0x27;
                case "DOWN": return 0x28;
                case "HOME": return 0x24;
                case "END": return 0x23;
                case "PAGEUP": return 0x21;
                case "PAGEDOWN": return 0x22;
                case "CAPSLOCK": return 0x14;
                case "PRINTSCREEN": return 0x2C;
                case "PAUSE": return 0x13;
            }

            if (key.Length >= 2 && key[0] == 'F')
            {
                int function;
                if (int.TryParse(key.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out function)
                    && function >= 1 && function <= 24)
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
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion { ki = new KEYBDINPUT { wVk = key, dwFlags = up ? KEYEVENTF_KEYUP : 0u } }
            };
        }

        private static void SendMouse(uint flags, uint data)
        {
            SendInputs(new[]
            {
                new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { mouseData = data, dwFlags = flags } } }
            }, "mouse input");
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

        private static Action RequireMutationCallback(Action? ensureMutationRunning)
        {
            if (ensureMutationRunning == null)
                throw new InvalidOperationException("Desktop mutation execution context is unavailable.");
            return ensureMutationRunning;
        }

        private static string RequiredText(string body, string property, int maximum)
        {
            var value = McpTopLevelJson.ExtractString(body, property) ?? string.Empty;
            if (value.Length > maximum)
                throw new InvalidOperationException(property + " exceeds " + maximum.ToString(CultureInfo.InvariantCulture) + " characters.");
            foreach (var ch in value)
                if (ch == '\0') throw new InvalidOperationException(property + " contains a forbidden NUL character.");
            return value;
        }

        private static int Integer(string body, string property, int fallback, int min, int max)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        private static int StrictOptionalInteger(string body, string property, int fallback, int min, int max)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found) return fallback;
            if (value < min || value > max)
                throw new InvalidOperationException(property + " must be an integer between "
                    + min.ToString(CultureInfo.InvariantCulture) + " and " + max.ToString(CultureInfo.InvariantCulture) + ".");
            return value;
        }

        private static int IntegerRequired(string body, string property, int min, int max)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body, property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found || value < min || value > max)
                throw new InvalidOperationException(property + " must be an integer between "
                    + min.ToString(CultureInfo.InvariantCulture) + " and " + max.ToString(CultureInfo.InvariantCulture) + ".");
            return value;
        }

        private static T RunSta<T>(Func<T> action)
        {
            T result = default!;
            Exception? error = null;
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
            if (error != null)
                throw new InvalidOperationException("Windows clipboard operation failed: " + error.Message, error);
            return result;
        }

        private static void EnsureInteractiveSession()
        {
            if (!Environment.UserInteractive)
                throw new InvalidOperationException("Windows desktop automation requires an interactive user session.");
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

        private sealed class SequenceStep
        {
            public string Tool = string.Empty;
            public string Arguments = "{}";
            public int DelayAfterMilliseconds;
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
        [DllImport("user32.dll", SetLastError = true)] private static extern bool PrintWindow(IntPtr hwnd, IntPtr destination, uint flags);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint rasterOperation);
    }
}
