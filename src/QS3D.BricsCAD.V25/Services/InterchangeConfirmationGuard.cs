using System;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class InterchangeConfirmationGuard
    {
        public static ProjectState RequireFresh(
            Document document,
            ProjectState reviewedProject,
            long reviewedChangeVersion,
            string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (reviewedProject == null) throw new ArgumentNullException(nameof(reviewedProject));
            if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("Operation label is required.", nameof(operation));

            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " requires the DWG that was reviewed to remain active.");

            var currentProject = QS3D.BricsCAD.V25.ProjectContextCoordinator.GetOrCreate(document);
            if (!ReferenceEquals(currentProject, reviewedProject) || currentProject.ChangeVersion != reviewedChangeVersion)
                throw new InvalidOperationException(
                    operation + " target semantic project changed after preview. Run the command again to review a fresh plan.");

            return currentProject;
        }
    }
}
