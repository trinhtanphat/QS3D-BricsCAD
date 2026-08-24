using System;
using System.Threading;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Owns the BricsCAD application-quit reactor exactly once for all QS3D modeless windows.
    /// The native callbacks are deliberately state-only: WPF/document cleanup remains outside
    /// the native quit stack so BricsCAD retains sole ownership of final HWND teardown.
    /// </summary>
    internal static class ModelessHostQuiescenceCoordinator
    {
        private static int _initialized;
        private static int _isQuiescing;

        internal static bool IsQuiescing => Volatile.Read(ref _isQuiescing) != 0;

        internal static event EventHandler? QuiescenceAborted;

        internal static void EnsureInitialized()
        {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0) return;

            try
            {
                BcadApplication.QuitWillStart += OnQuitWillStart;
                try
                {
                    BcadApplication.QuitAborted += OnQuitAborted;
                }
                catch
                {
                    try { BcadApplication.QuitWillStart -= OnQuitWillStart; }
                    catch { }
                    throw;
                }
            }
            catch
            {
                Volatile.Write(ref _initialized, 0);
                throw;
            }
        }

        internal static void Stop()
        {
            // IExtensionApplication.Terminate may run while BricsCAD is already destroying native
            // reactors. At that point leave the native subscriptions to host/process teardown.
            if (IsQuiescing) return;
            if (Interlocked.Exchange(ref _initialized, 0) == 0) return;

            try { BcadApplication.QuitWillStart -= OnQuitWillStart; }
            catch { }
            try { BcadApplication.QuitAborted -= OnQuitAborted; }
            catch { }
            Volatile.Write(ref _isQuiescing, 0);
        }

        private static void OnQuitWillStart(object? sender, EventArgs e)
        {
            Volatile.Write(ref _isQuiescing, 1);
        }

        private static void OnQuitAborted(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _isQuiescing, 0) == 0) return;
            QuiescenceAborted?.Invoke(null, EventArgs.Empty);
        }
    }
}
