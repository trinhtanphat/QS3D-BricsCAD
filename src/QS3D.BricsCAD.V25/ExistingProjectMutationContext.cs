using System;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25
{
    internal static class ExistingProjectMutationContext
    {
        public static bool TryGet(Document document, out ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            project = null!;

            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var existing))
                return false;

            var canonical = ProjectContextCoordinator.GetOrCreate(document);
            if (!string.Equals(existing.ProjectId, canonical.ProjectId, StringComparison.OrdinalIgnoreCase))
            {
                ProjectContextCoordinator.Forget(document);
                throw new InvalidOperationException(
                    "QS3D project changed while resolving canonical mutation state. Re-open/reload the project and retry.");
            }

            project = canonical;
            return true;
        }

        public static ProjectState Require(Document document, string operation)
        {
            if (TryGet(document, out var project)) return project;
            throw new InvalidOperationException(
                (operation ?? "QS3D mutation") + " cần một QS3D project hiện hữu; thao tác này không tạo project mới.");
        }
    }
}
