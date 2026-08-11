if (-not ("Qs3dBricsCadRunnerWindowInterop" -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;

public static class Qs3dBricsCadRunnerWindowInterop
{
    private const uint WmClose = 0x0010;

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr state);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr state);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int capacity);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, StringBuilder text, int capacity);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    public static int CloseProxyInformationDialogs(int processId)
    {
        if (processId <= 0) throw new ArgumentOutOfRangeException("processId");
        var closed = 0;
        EnumWindows((window, state) =>
        {
            uint ownerProcessId;
            GetWindowThreadProcessId(window, out ownerProcessId);
            if (ownerProcessId != (uint)processId || !IsWindowVisible(window)) return true;

            var title = new StringBuilder(256);
            var className = new StringBuilder(64);
            GetWindowText(window, title, title.Capacity);
            GetClassName(window, className, className.Capacity);
            if (string.Equals(title.ToString(), "Proxy Information", StringComparison.Ordinal) &&
                string.Equals(className.ToString(), "#32770", StringComparison.Ordinal) &&
                PostMessage(window, WmClose, IntPtr.Zero, IntPtr.Zero))
            {
                closed++;
            }
            return true;
        }, IntPtr.Zero);
        return closed;
    }
}
"@
}

function Close-Qs3dProxyInformationDialog {
    param([AllowNull()][Diagnostics.Process]$Process)
    if ($null -eq $Process) { return 0 }
    try {
        $Process.Refresh()
        if ($Process.HasExited) { return 0 }
        return [Qs3dBricsCadRunnerWindowInterop]::CloseProxyInformationDialogs($Process.Id)
    }
    catch [InvalidOperationException] {
        return 0
    }
}
