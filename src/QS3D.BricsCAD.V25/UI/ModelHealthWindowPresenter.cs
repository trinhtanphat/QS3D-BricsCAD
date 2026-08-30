using System;
using System.Collections.Generic;
using Bricscad.ApplicationServices;
using QS3D.Core.Diagnostics;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Owns the one live Model Health review surface for the hosted V25 process.
    /// Health commands still recompute their snapshots on every invocation; this
    /// presenter only serializes terminal replacement/publication so stale
    /// document-bound windows cannot accumulate.
    /// </summary>
    internal static class ModelHealthWindowPresenter
    {
        private static ModelHealthWindow? _published;

        public static void Show(
            Document document,
            IReadOnlyList<ModelHealthIssue> issues,
            Action<ModelHealthIssue>? locate = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (issues == null) throw new ArgumentNullException(nameof(issues));

            ClosePublishedBeforeReplacement();

            ModelHealthWindow? candidate = null;
            try
            {
                candidate = new ModelHealthWindow(document, issues, locate);
                var publishedCandidate = candidate;
                candidate.Closed += (_, __) =>
                {
                    if (ReferenceEquals(_published, publishedCandidate))
                        _published = null;
                };

                Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                if (!candidate.IsLoaded)
                    throw new InvalidOperationException("Model Health window did not remain loaded after host publication.");

                _published = candidate;
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

        private static void ClosePublishedBeforeReplacement()
        {
            var previous = _published;
            if (previous == null) return;

            if (!previous.IsLoaded)
            {
                if (ReferenceEquals(_published, previous))
                    _published = null;
                return;
            }

            previous.Close();

            if (previous.IsLoaded || ReferenceEquals(_published, previous))
                throw new InvalidOperationException("The existing Model Health window did not reach terminal close; replacement was refused.");
        }
    }
}
