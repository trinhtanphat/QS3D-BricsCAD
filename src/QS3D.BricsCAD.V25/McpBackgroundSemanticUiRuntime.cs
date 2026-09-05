using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using Bricscad.ApplicationServices;

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
        private const int CadApplicationDispatchTimeoutMilliseconds = 5000;
        private const uint GA_ROOT = 2;

        private static readonly object DiscoverySync = new object();
        private static long _semanticDiscoveryGeneration;
        private static SemanticDiscoverySnapshot? _semanticDiscovery;
        private static bool _providerAttemptInFlight;

        internal static string SemanticTree(string body, Action<string> audit)
        {
            RequireSensitiveRead(body);
            var hwnd = RequiredCurrentBricsCadWindow(body);
            var targetThreadId = TargetWindowThreadId(hwnd);
            var document = ActiveDocumentSnapshot.Capture();
            var maxDepth = OptionalInteger(body, "maxDepth", DefaultDepth, 1, MaxDepth);
            var maxNodes = OptionalInteger(body, "maxNodes", DefaultNodes, 1, MaxNodes);
            var root = RootElement(hwnd);

            var nodes = new StringBuilder();
            var count = 0;
            var truncated = false;
            Walk(root, "root", 0, maxDepth, maxNodes, nodes, ref count, ref truncated);

            // Bind the tree only after both the BricsCAD active document and the target window
            // still match what was observed while traversing the UIA ControlView.
            RequireSameActiveDocument(document);
            RequiredCurrentBricsCadWindow(hwnd);
            if (TargetWindowThreadId(hwnd) != targetThreadId)
                throw new InvalidOperationException("Semantic target UI thread changed during discovery; request a fresh semantic discovery.");
            var discoveryGeneration = RecordSemanticDiscovery(hwnd, targetThreadId, document);

            var builder = new StringBuilder("{\"mode\":\"semantic\",\"windowHandle\":\"")
                .Append(HandleText(hwnd))
                .Append("\",\"discoveryGeneration\":").Append(discoveryGeneration.ToString(CultureInfo.InvariantCulture))
                .Append(",\"maxDepth\":").Append(maxDepth.ToString(CultureInfo.InvariantCulture))
                .Append(",\"maxNodes\":").Append(maxNodes.ToString(CultureInfo.InvariantCulture))
                .Append(",\"nodes\":[").Append(nodes)
                .Append("],\"count\":").Append(count.ToString(CultureInfo.InvariantCulture))
                .Append(",\"truncated\":").Append(truncated ? "true" : "false")
                .Append(",\"background\":true}");
            Audit(audit, "background-semantic-tree handle=" + HandleText(hwnd)
                + "; generation=" + discoveryGeneration.ToString(CultureInfo.InvariantCulture)
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
            var expectedDiscoveryGeneration = RequiredPositiveInteger(body, "expectedDiscoveryGeneration");
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

            var discovery = RequireFreshSemanticDiscovery(expectedDiscoveryGeneration, hwnd);
            RequireSameActiveDocument(discovery.Document);
            RequireSameTargetUiThread(discovery, hwnd);

            var root = RootElement(hwnd);
            var element = ResolvePath(root, path);
            VerifyElementIdentity(element, expectedControlType, expectedAutomationId ?? string.Empty);
            VerifyActionAvailable(element, action);

            // UI Automation provider methods can self-deadlock when called from the same target UI
            // thread. Background semantic mutation is therefore rejected before the provider boundary.
            RequireDifferentTargetUiThread(hwnd);
            ensureMutationRunning();
            RequireSameActiveDocument(discovery.Document);
            RequireSameTargetUiThread(discovery, hwnd);
            RequireFreshSemanticDiscovery(expectedDiscoveryGeneration, hwnd);

            // This compare-and-invalidate operation is the provider-attempt boundary. It prevents a
            // concurrent discovery from silently replacing the generation between validation and UIA.
            var invalidatedGeneration = InvalidateSemanticDiscovery(expectedDiscoveryGeneration, hwnd);
            try
            {
                try
                {
                    ExecuteAction(element, action);
                }
                catch (Exception)
                {
                    throw BuildUncertainOutcome(
                        "provider-error", hwnd, path, action, expectedControlType, invalidatedGeneration, audit);
                }

                try
                {
                    ensureMutationRunning();
                    RequiredCurrentBricsCadWindow(hwnd);
                    RequireSameActiveDocument(discovery.Document);
                    RequireSameTargetUiThread(discovery, hwnd);
                }
                catch (Exception)
                {
                    throw BuildUncertainOutcome(
                        "postcondition-diverged", hwnd, path, action, expectedControlType, invalidatedGeneration, audit);
                }

                Audit(audit, "background-semantic-action outcome=provider-completed; handle=" + HandleText(hwnd)
                    + "; path=" + path + "; action=" + action + "; controlType=" + expectedControlType
                    + "; invalidatedGeneration=" + invalidatedGeneration.ToString(CultureInfo.InvariantCulture));
                return "{\"invoked\":true,\"semantic\":true,\"background\":true,\"windowHandle\":\""
                    + HandleText(hwnd) + "\",\"elementPath\":\"" + Escape(path) + "\",\"action\":\""
                    + Escape(action) + "\",\"controlType\":\"" + Escape(expectedControlType)
                    + "\",\"applicationStatus\":\"provider-completed\",\"providerCallStarted\":true"
                    + ",\"cadStateVerified\":false,\"retryAllowed\":false,\"requiresRediscovery\":true"
                    + ",\"discoveryGeneration\":" + invalidatedGeneration.ToString(CultureInfo.InvariantCulture) + "}";
            }
            finally
            {
                FinishProviderAttempt();
            }
        }

        private static InvalidOperationException BuildUncertainOutcome(
            string reason,
            IntPtr hwnd,
            string path,
            string action,
            string controlType,
            long invalidatedGeneration,
            Action<string> audit)
        {
            var result = "{\"invoked\":false,\"semantic\":true,\"background\":true,\"windowHandle\":\""
                + HandleText(hwnd) + "\",\"elementPath\":\"" + Escape(path) + "\",\"action\":\""
                + Escape(action) + "\",\"controlType\":\"" + Escape(controlType)
                + "\",\"applicationStatus\":\"uncertain\",\"reason\":\"" + Escape(reason)
                + "\",\"providerCallStarted\":true,\"cadStateVerified\":false,\"retryAllowed\":false"
                + ",\"requiresRediscovery\":true,\"discoveryGeneration\":"
                + invalidatedGeneration.ToString(CultureInfo.InvariantCulture) + "}";

            // Reuse the canonical actionId acknowledgement ledger. Keeping the record Accepted with
            // a bounded uncertain result blocks automatic replay without inventing a semantic ledger.
            try
            {
                var actionId = McpMutationAckLedger.CurrentActionId;
                if (actionId.Length > 0) McpMutationAckLedger.MarkAcceptedResult(actionId, result);
            }
            catch { }

            Audit(audit, "background-semantic-action outcome=uncertain; reason=" + reason
                + "; handle=" + HandleText(hwnd) + "; path=" + path + "; action=" + action
                + "; invalidatedGeneration=" + invalidatedGeneration.ToString(CultureInfo.InvariantCulture));
            return new InvalidOperationException(
                "background semantic UI outcome is uncertain; reason=" + reason
                + "; cadStateVerified=false; retryAllowed=false; requiresRediscovery=true; no automatic retry. "
                + "Inspect the actionId acknowledgement before recovery.");
        }

        private static long RecordSemanticDiscovery(IntPtr hwnd, uint targetThreadId, ActiveDocumentSnapshot document)
        {
            lock (DiscoverySync)
            {
                if (_providerAttemptInFlight)
                    throw new InvalidOperationException("A semantic provider attempt is in progress; request a fresh semantic discovery after it settles.");
                var generation = Interlocked.Increment(ref _semanticDiscoveryGeneration);
                if (generation <= 0)
                {
                    _semanticDiscovery = null;
                    throw new InvalidOperationException("Semantic discovery generation is unavailable; restart the MCP host before semantic UI mutation.");
                }
                _semanticDiscovery = new SemanticDiscoverySnapshot(generation, hwnd, targetThreadId, document);
                return generation;
            }
        }

        private static SemanticDiscoverySnapshot RequireFreshSemanticDiscovery(long expectedDiscoveryGeneration, IntPtr hwnd)
        {
            lock (DiscoverySync)
            {
                var discovery = _semanticDiscovery;
                if (_providerAttemptInFlight || discovery == null
                    || discovery.Generation != expectedDiscoveryGeneration
                    || Interlocked.Read(ref _semanticDiscoveryGeneration) != expectedDiscoveryGeneration
                    || discovery.WindowHandle != hwnd)
                    throw new InvalidOperationException(
                        "expectedDiscoveryGeneration is stale; a fresh semantic discovery is required before semantic mutation.");
                return discovery;
            }
        }

        private static long InvalidateSemanticDiscovery(long expectedDiscoveryGeneration, IntPtr hwnd)
        {
            lock (DiscoverySync)
            {
                var discovery = _semanticDiscovery;
                if (_providerAttemptInFlight || discovery == null
                    || discovery.Generation != expectedDiscoveryGeneration
                    || Interlocked.Read(ref _semanticDiscoveryGeneration) != expectedDiscoveryGeneration
                    || discovery.WindowHandle != hwnd)
                    throw new InvalidOperationException(
                        "expectedDiscoveryGeneration became stale before the provider attempt; request a fresh semantic discovery.");

                _semanticDiscovery = null;
                _providerAttemptInFlight = true;
                var generation = Interlocked.Increment(ref _semanticDiscoveryGeneration);
                if (generation <= 0)
                    throw new InvalidOperationException("Semantic discovery generation is unavailable after provider invalidation.");
                return generation;
            }
        }

        private static void FinishProviderAttempt()
        {
            lock (DiscoverySync) _providerAttemptInFlight = false;
        }

        private static void RequireSameTargetUiThread(SemanticDiscoverySnapshot discovery, IntPtr hwnd)
        {
            if (discovery == null || discovery.WindowHandle != hwnd
                || TargetWindowThreadId(hwnd) != discovery.TargetThreadId)
                throw new InvalidOperationException("Semantic target UI thread changed; request a fresh semantic discovery.");
        }

        private static void RequireDifferentTargetUiThread(IntPtr hwnd)
        {
            if (TargetWindowThreadId(hwnd) == GetCurrentThreadId())
                throw new InvalidOperationException(
                    "Background semantic provider mutation is rejected on the same target UI thread; use a background MCP call with fresh semantic discovery.");
        }

        private static uint TargetWindowThreadId(IntPtr hwnd)
        {
            uint processId;
            var threadId = GetWindowThreadProcessId(hwnd, out processId);
            if (threadId == 0 || processId == 0)
                throw new InvalidOperationException("Could not resolve semantic target UI thread.");
            using (var current = Process.GetCurrentProcess())
            {
                if (processId != unchecked((uint)current.Id))
                    throw new InvalidOperationException("Semantic target window must belong to the current BricsCAD process.");
            }
            return threadId;
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

            AutomationElement? child;
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

                AutomationElement? child;
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
                    throw new InvalidOperationException("elementPath is stale or no longer resolves; request a fresh semantic discovery.");
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
            catch (Exception)
            {
                throw new InvalidOperationException("Semantic target is stale or unavailable; request a fresh semantic discovery.");
            }

            if (!string.Equals(controlType, expectedControlType, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic target control type changed; refusing stale/mismatched action and requiring fresh semantic discovery.");
            if (!string.Equals(automationId, expectedAutomationId ?? string.Empty, StringComparison.Ordinal))
                throw new InvalidOperationException("Semantic target automationId changed; refusing stale/mismatched action and requiring fresh semantic discovery.");
            if (!enabled || offscreen)
                throw new InvalidOperationException("Semantic target must be enabled and onscreen at action time; request a fresh semantic discovery.");
        }

        private static AutomationElement RootElement(IntPtr hwnd)
        {
            AutomationElement root;
            try { root = AutomationElement.FromHandle(hwnd); }
            catch (Exception)
            {
                throw new InvalidOperationException("Windows UI Automation could not attach to the BricsCAD target window.");
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
            catch (ElementNotAvailableException)
            {
                throw new InvalidOperationException("BricsCAD UI Automation root became unavailable.");
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
            TargetWindowThreadId(hwnd);
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

        private static int RequiredPositiveInteger(string body, string property)
        {
            int value;
            bool found;
            string error;
            if (!McpTopLevelJson.TryExtractInteger(body ?? "{}", property, out value, out found, out error))
                throw new InvalidOperationException(error);
            if (!found || value <= 0)
                throw new InvalidOperationException(property + " is required and must be a positive integer from fresh semantic discovery.");
            return value;
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

        private static bool IsCadApplicationThread()
        {
            IntPtr mainWindow;
            uint processId;
            using (var process = Process.GetCurrentProcess())
            {
                mainWindow = process.MainWindowHandle;
                if (mainWindow == IntPtr.Zero) return false;
                var threadId = GetWindowThreadProcessId(mainWindow, out processId);
                return threadId != 0
                    && processId == unchecked((uint)process.Id)
                    && threadId == GetCurrentThreadId();
            }
        }

        private static T InvokeOnCadApplicationContext<T>(Func<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (IsCadApplicationThread()) return action();

            var work = new CadApplicationWork<T>(action);
            try
            {
                Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadApplicationWork<T>, work);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Could not dispatch bounded semantic UI affinity work to the BricsCAD application context.");
            }

            if (!work.Done.Task.Wait(CadApplicationDispatchTimeoutMilliseconds))
                throw new TimeoutException("Timed out waiting for bounded BricsCAD application-context affinity work.");
            if (work.Failed)
                throw new InvalidOperationException("Bounded BricsCAD application-context affinity work failed closed.");
            return work.Result;
        }

        private static void ExecuteCadApplicationWork<T>(object state)
        {
            ((CadApplicationWork<T>)state).Run();
        }

        private static void RequireSameActiveDocument(ActiveDocumentSnapshot expected)
        {
            if (expected == null)
                throw new InvalidOperationException("Semantic active document affinity is unavailable; request a fresh semantic discovery.");
            var current = ActiveDocumentSnapshot.Capture();
            if (!ReferenceEquals(expected.Document, current.Document))
                throw new InvalidOperationException("The active BricsCAD document changed; request a fresh semantic discovery before mutation.");
        }

        private sealed class SemanticDiscoverySnapshot
        {
            internal SemanticDiscoverySnapshot(long generation, IntPtr windowHandle, uint targetThreadId, ActiveDocumentSnapshot document)
            {
                Generation = generation;
                WindowHandle = windowHandle;
                TargetThreadId = targetThreadId;
                Document = document;
            }

            internal long Generation { get; }
            internal IntPtr WindowHandle { get; }
            internal uint TargetThreadId { get; }
            internal ActiveDocumentSnapshot Document { get; }
        }

        private sealed class ActiveDocumentSnapshot
        {
            private ActiveDocumentSnapshot(Document document, uint cadUiThreadId)
            {
                Document = document;
                CadUiThreadId = cadUiThreadId;
            }

            internal Document Document { get; }
            internal uint CadUiThreadId { get; }

            internal static ActiveDocumentSnapshot Capture()
            {
                return InvokeOnCadApplicationContext(() =>
                {
                    var document = Application.DocumentManager.MdiActiveDocument;
                    if (document == null)
                        throw new InvalidOperationException("No active BricsCAD document is available for semantic UI affinity.");
                    return new ActiveDocumentSnapshot(document, GetCurrentThreadId());
                });
            }
        }

        private sealed class CadApplicationWork<T>
        {
            private readonly Func<T> _action;

            internal CadApplicationWork(Func<T> action)
            {
                _action = action;
            }

            internal readonly TaskCompletionSource<bool> Done =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            internal T Result = default!;
            internal bool Failed;

            internal void Run()
            {
                try { Result = _action(); }
                catch { Failed = true; }
                finally { Done.TrySetResult(true); }
            }
        }

        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}
