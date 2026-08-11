using System;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Services
{
    /// <summary>
    /// Keeps an already-authorized Interchange mutation bound to the exact live
    /// project instance that was reviewed. This guard is intentionally
    /// non-creating; losing or replacing the cached target requires a new review.
    /// </summary>
    internal static class InterchangeMutationTargetGuard
    {
        public static ProjectState RequireExact(Document document, ProjectState authorizedProject, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (authorizedProject == null) throw new ArgumentNullException(nameof(authorizedProject));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation label is required.", nameof(operation));

            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the reviewed DWG to remain active.");

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject) ||
                !ReferenceEquals(currentProject, authorizedProject))
                throw new InvalidOperationException(
                    operation + " target project is no longer the exact reviewed project. Run the command again.");

            // Exact in-memory identity is not enough: an external process can replace/remove the
            // authoritative .qsdb while the reviewed ProjectState remains cached. Every Interchange
            // mutation using this helper must fail before mutation when that backing-store revision
            // is no longer the one bound to the reviewed canonical project.
            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, currentProject, operation + " / exact target bind");
            return currentProject;
        }
    }
}
