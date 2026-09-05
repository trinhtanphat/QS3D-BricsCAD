using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Automation;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Passively observes visible modal/owned BricsCAD notification dialogs so MCP can
    /// retrieve their bounded text through the existing cad_audit_tail diagnostics path.
    /// Only the current BricsCAD process is observed; editable-control contents are never read.
    /// </summary>
    internal static class McpPopupObserver
    {
        private const uint EventSystemDialogStart = 0x0010;
        private const uint EventSystemDialogEnd = 0x0011;
        private const uint EventObjectShow = 0x8002;
        private const uint WineventOutOfContext = 0x0000;
        private const int ObjIdWindow = 0;
        private const uint GaRoot = 2;
        private const int MaxChildWindows = 64;
        private const int MaxAutomationNodes = 96;
        private const int MaxAutomationDepth = 6;
        private const int MaxTitleCharacters = 512;
        private const int MaxMessageCharacters = 1400;
        private const int MaxButtonCharacters = 320;
        private static readonly object Gate = new object();
        private static readonly Dictionary<long, PopupSignature> LastSeen = new Dictionary<long, PopupSignature>();
        private static readonly WinEventDelegate Callback = OnWinEvent;
        private static IntPtr _dialogHook;
        private static IntPtr _showHook;
        private static bool _started;
        private static uint _processId;

        private sealed class PopupSignature
        {
            public string Signature = string.Empty;
            public DateTime Utc;
        }

        internal static void Start()
        {
            lock (Gate)
            {
                if (_started) return;
                _processId = (uint)Process.GetCurrentProcess().Id;
                _dialogHook = SetWinEventHook(
                    EventSystemDialogStart,
                    EventSystemDialogEnd,
                    IntPtr.Zero,
                    Callback,
                    _processId,
                    0,
                    WineventOutOfContext);
                _showHook = SetWinEventHook(
                    EventObjectShow,
                    EventObjectShow,
                    IntPtr.Zero,
                    Callback,
                    _processId,
                    0,
                    WineventOutOfContext);
                if (_dialogHook == IntPtr.Zero && _showHook == IntPtr.Zero)
                    throw new InvalidOperationException("Windows popup notification hook could not be installed.");
                _started = true;
            }

            QueueExistingDialogs();
            McpDiagnosticHub.Record("bricscad", "info", "popup-observer-start", "BricsCAD popup notification observer started.");
        }

        internal static void Stop()
        {
            IntPtr dialogHook;
            IntPtr showHook;
            lock (Gate)
            {
                if (!_started) return;
                _started = false;
                dialogHook = _dialogHook;
                showHook = _showHook;
                _dialogHook = IntPtr.Zero;
                _showHook = IntPtr.Zero;
                LastSeen.Clear();
            }

            if (dialogHook != IntPtr.Zero) try { UnhookWinEvent(dialogHook); } catch { }
            if (showHook != IntPtr.Zero) try { UnhookWinEvent(showHook); } catch { }
            McpDiagnosticHub.Record("bricscad", "info", "popup-observer-stop", "BricsCAD popup notification observer stopped.");
        }

        private static void QueueExistingDialogs()
        {
            try
            {
                EnumWindows((hwnd, _) =>
                {
                    if (IsCandidateRoot(hwnd)) QueueCapture(hwnd);
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }

        private static void OnWinEvent(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint eventThread, uint eventTime)
        {
            if (hwnd == IntPtr.Zero) return;
            if (eventType == EventObjectShow && (idObject != ObjIdWindow || idChild != 0)) return;
            var root = GetAncestor(hwnd, GaRoot);
            if (root == IntPtr.Zero) root = hwnd;
            if (!IsCandidateRoot(root)) return;
            QueueCapture(root);
        }

        private static void QueueCapture(IntPtr hwnd)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    Thread.Sleep(80);
                    Capture(hwnd);
                }
                catch { }
            });
        }

        private static bool IsCandidateRoot(IntPtr hwnd)
        {
            return McpPopupWindowClassifier.IsPopupRoot(hwnd, CurrentMainWindowHandle());
        }

        private static IntPtr CurrentMainWindowHandle()
        {
            try
            {
                using (var process = Process.GetCurrentProcess()) return process.MainWindowHandle;
            }
            catch { return IntPtr.Zero; }
        }

        private static void Capture(IntPtr hwnd)
        {
            if (!IsCandidateRoot(hwnd)) return;

            var title = WindowText(hwnd, MaxTitleCharacters);
            var className = WindowClass(hwnd);
            var messages = new List<string>();
            var buttons = new List<string>();
            var childCount = 0;

            EnumChildWindows(hwnd, (child, _) =>
            {
                if (childCount++ >= MaxChildWindows) return false;
                var childClass = WindowClass(child);
                if (childClass.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                var text = WindowText(child, 512).Trim();
                if (text.Length == 0) return true;
                if (childClass.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AppendDistinct(buttons, text, MaxButtonCharacters);
                    return true;
                }
                if (childClass.IndexOf("Static", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AppendDistinct(messages, text, MaxMessageCharacters);
                }
                return true;
            }, IntPtr.Zero);

            CaptureAutomationText(hwnd, title, messages, buttons);

            if (messages.Count == 0 && buttons.Count == 0 && title.Length == 0) return;
            var message = JoinBounded(messages, " | ", MaxMessageCharacters);
            var buttonText = JoinBounded(buttons, " | ", MaxButtonCharacters);
            var signature = title + "\n" + className + "\n" + message + "\n" + buttonText;
            if (!ShouldRecord(hwnd, signature)) return;

            var detail = "handle=0x" + hwnd.ToInt64().ToString("X", CultureInfo.InvariantCulture)
                         + "; title=" + title
                         + "; class=" + className
                         + "; message=" + message
                         + "; buttons=" + buttonText;
            Document? document = null;
            try { document = Application.DocumentManager.MdiActiveDocument; } catch { }
            McpDiagnosticHub.Record("bricscad", "warning", "popup-notification", detail, document);
        }

        private static void CaptureAutomationText(
            IntPtr hwnd,
            string title,
            List<string> messages,
            List<string> buttons)
        {
            AutomationElement root;
            try { root = AutomationElement.FromHandle(hwnd); }
            catch { return; }
            if (root == null) return;

            try
            {
                if (root.Current.ProcessId != unchecked((int)_processId)) return;
            }
            catch { return; }

            var visited = 0;
            WalkAutomation(root, title, 0, messages, buttons, ref visited);
        }

        private static void WalkAutomation(
            AutomationElement element,
            string title,
            int depth,
            List<string> messages,
            List<string> buttons,
            ref int visited)
        {
            if (element == null || visited >= MaxAutomationNodes || depth > MaxAutomationDepth) return;
            visited++;

            var skipChildren = false;
            if (depth > 0)
            {
                try
                {
                    var current = element.Current;
                    var controlType = current.ControlType;
                    if (controlType == ControlType.Edit)
                    {
                        skipChildren = true;
                    }
                    else if (!current.IsOffscreen)
                    {
                        var name = NormalizeText(current.Name);
                        if (name.Length > 0)
                        {
                            if (controlType == ControlType.Button)
                            {
                                AppendDistinct(buttons, name, MaxButtonCharacters);
                            }
                            else if (controlType == ControlType.Text
                                     && !string.Equals(name, title, StringComparison.Ordinal))
                            {
                                AppendDistinct(messages, name, MaxMessageCharacters);
                            }
                        }
                    }
                }
                catch { }
            }

            if (skipChildren || depth >= MaxAutomationDepth || visited >= MaxAutomationNodes) return;

            AutomationElement? child;
            try { child = TreeWalker.ControlViewWalker.GetFirstChild(element); }
            catch { return; }

            while (child != null && visited < MaxAutomationNodes)
            {
                var currentChild = child;
                WalkAutomation(currentChild, title, depth + 1, messages, buttons, ref visited);
                try { child = TreeWalker.ControlViewWalker.GetNextSibling(currentChild); }
                catch { return; }
            }
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var text = value.Trim();
            if (text.Length > 512) text = text.Substring(0, 512);
            return text;
        }

        private static bool ShouldRecord(IntPtr hwnd, string signature)
        {
            var key = hwnd.ToInt64();
            var now = DateTime.UtcNow;
            lock (Gate)
            {
                if (!_started) return false;
                PopupSignature previous;
                if (LastSeen.TryGetValue(key, out previous)
                    && string.Equals(previous.Signature, signature, StringComparison.Ordinal)
                    && (now - previous.Utc).TotalSeconds < 10)
                    return false;

                LastSeen[key] = new PopupSignature { Signature = signature, Utc = now };
                if (LastSeen.Count > 64)
                {
                    var stale = new List<long>();
                    foreach (var item in LastSeen)
                        if ((now - item.Value.Utc).TotalMinutes > 2) stale.Add(item.Key);
                    foreach (var item in stale) LastSeen.Remove(item);
                }
                return true;
            }
        }

        private static void AppendDistinct(List<string> target, string value, int maxCharacters)
        {
            if (target.Count >= 16) return;
            foreach (var existing in target)
                if (string.Equals(existing, value, StringComparison.Ordinal)) return;
            var remaining = maxCharacters - JoinBounded(target, " | ", maxCharacters).Length;
            if (remaining <= 0) return;
            target.Add(value.Length <= remaining ? value : value.Substring(0, remaining));
        }

        private static string JoinBounded(List<string> values, string separator, int maxCharacters)
        {
            if (values.Count == 0) return string.Empty;
            var text = string.Join(separator, values);
            return text.Length <= maxCharacters ? text : text.Substring(0, maxCharacters);
        }

        private static string WindowText(IntPtr hwnd, int maxCharacters)
        {
            try
            {
                var length = Math.Min(Math.Max(GetWindowTextLength(hwnd), 0), maxCharacters);
                var capacity = Math.Max(2, Math.Min(maxCharacters + 1, length + 1));
                var builder = new StringBuilder(capacity);
                GetWindowText(hwnd, builder, builder.Capacity);
                var text = builder.ToString();
                return text.Length <= maxCharacters ? text : text.Substring(0, maxCharacters);
            }
            catch { return string.Empty; }
        }

        private static string WindowClass(IntPtr hwnd)
        {
            try
            {
                var builder = new StringBuilder(256);
                return GetClassName(hwnd, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
            }
            catch { return string.Empty; }
        }

        private delegate void WinEventDelegate(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint eventThread, uint eventTime);
        private delegate bool EnumWindowsDelegate(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr eventHookAssembly, WinEventDelegate callback, uint processId, uint threadId, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWinEvent(IntPtr hook);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsDelegate callback, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsDelegate callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);
    }
}
