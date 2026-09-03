using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Preferred host-show wrapper for a fresh Model Health snapshot.
    /// ModelHealthWindow itself owns process-wide pending/published arbitration so
    /// legacy direct callers and migrated callers share one lifecycle invariant.
    /// This wrapper additionally closes an unpublished candidate when host show fails.
    /// </summary>
    internal static class ModelHealthWindowPresenter
    {
        public static void Show(
            Document document,
            IReadOnlyList<ModelHealthIssue> issues,
            Action<ModelHealthIssue>? locate = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (issues == null) throw new ArgumentNullException(nameof(issues));

            ModelHealthWindow? candidate = null;
            try
            {
                candidate = new ModelHealthWindow(document, issues, locate);
                Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                if (!candidate.IsLoaded)
                    throw new InvalidOperationException("Model Health window did not remain loaded after host publication.");

                candidate = null;
            }
            finally
            {
                if (candidate != null)
                {
                    try { candidate.Close(); } catch { }
                }
            }
        }
    }
}
