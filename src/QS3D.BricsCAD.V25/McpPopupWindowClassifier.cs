using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Shared, fail-closed classification for BricsCAD top-level popup/dialog roots.
    /// Keeps passive popup diagnostics and bricscad_ui_text_snapshot(scope=popup)
    /// on the same definition instead of treating every auxiliary top-level HWND as a dialog.
    /// </summary>
    internal static class McpPopupWindowClassifier
    {
        private const uint GaRoot = 2;
        private const uint GwOwner = 4;

        internal static bool IsPopupRoot(IntPtr hwnd, IntPtr mainWindow)
        {
            if (hwnd == IntPtr.Zero || hwnd == mainWindow || !IsWindowVisible(hwnd)) return false;

            uint processId;
            if (GetWindowThreadProcessId(hwnd, out processId) == 0 || processId == 0) return false;
            using (var current = Process.GetCurrentProcess())
            {
                if (processId != unchecked((uint)current.Id)) return false;
            }

            var root = GetAncestor(hwnd, GaRoot);
            if (root != IntPtr.Zero && root != hwnd) return false;

            var className = WindowClass(hwnd);
            var title = WindowText(hwnd);
            if (IsBenignBricsCadChrome(className, title)) return false;

            // Standard Win32 modal/message dialogs do not always expose a useful owner.
            if (string.Equals(className, "#32770", StringComparison.Ordinal)) return true;

            var owner = GetWindow(hwnd, GwOwner);
            if (!BelongsToCurrentProcess(owner)) return false;

            // BricsCAD/QS3D WPF and wxWidgets dialogs are owned auxiliary windows. Require
            // dialog/window-like classes so arbitrary owned panes are not promoted to popups.
            if (Contains(className, "HwndWrapper")) return true;
            if (className.StartsWith("wx", StringComparison.OrdinalIgnoreCase)) return true;
            if (Contains(className, "Dialog") || Contains(className, "Afx")) return true;
            if (title.Length > 0 && Contains(className, "Window")) return true;

            return false;
        }

        private static bool IsBenignBricsCadChrome(string className, string title)
        {
            return Contains(className, "LookFrom")
                   || Contains(title, "LookFrom")
                   || Contains(className, "LookFromToolTip")
                   || Contains(title, "LookFromToolTip")
                   || Contains(className, "mini-command-line-frame")
                   || Contains(title, "mini-command-line-frame")
                   || Contains(className, "command-line-edit");
        }

        private static bool BelongsToCurrentProcess(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            uint processId;
            if (GetWindowThreadProcessId(hwnd, out processId) == 0 || processId == 0) return false;
            using (var current = Process.GetCurrentProcess())
                return processId == unchecked((uint)current.Id);
        }

        private static bool Contains(string value, string fragment)
        {
            return (value ?? string.Empty).IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
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

        private static string WindowText(IntPtr hwnd)
        {
            try
            {
                var length = Math.Min(Math.Max(GetWindowTextLength(hwnd), 0), 512);
                var builder = new StringBuilder(Math.Max(2, length + 1));
                GetWindowText(hwnd, builder, builder.Capacity);
                return builder.ToString();
            }
            catch { return string.Empty; }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

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
