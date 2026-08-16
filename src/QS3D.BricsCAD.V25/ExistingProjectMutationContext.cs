using System;
using System.IO;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Resolves an already-existing QS3D project for mutation without allowing a
    /// cold-cache read to leak a detached ProjectState into a write path.
    /// </summary>
    internal static class ExistingProjectMutationContext
    {
        public static bool TryGet(Document document, out ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            // Probe with the intentionally non-creating/read-only path first. When
            // the cache is cold this may return a detached disk snapshot; never
            // return that instance to a mutation caller.
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var observed))
            {
                project = null!;
                return false;
            }

            var expectedProjectId = observed.ProjectId;
            var canonical = ProjectContextCoordinator.GetOrCreate(document);

            // A sidecar can disappear or be replaced between the read-only probe
            // and canonical bind. GetOrCreate is allowed to create a default project,
            // so detect that race before any caller receives a mutable reference.
            if (!string.Equals(canonical.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
            {
                ProjectContextCoordinator.Forget(document);
                project = null!;
                throw new InvalidOperationException(
                    "QS3D project changed while binding the existing project for mutation. " +
                    "No mutation was applied; reload the intended project and retry.");
            }

            ProjectContextCoordinator.RequireBackingStoreUnchanged(document, canonical, "QS3D existing-project mutation");
            project = canonical;
            return true;
        }

        public static ProjectState Require(Document document, string operation)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(operation)) operation = "QS3D mutation";

            // Home/Start Center intentionally owns first-save creation for a new,
            // still-unnamed DWG. Keep every other mutation fail-closed: native
            // QS3DSAVE and arbitrary mutation callers still cannot create a project.
            if (IsMouseFirstUnsavedProjectSave(document, operation))
                return ProjectContextCoordinator.GetOrCreate(document);

            // ProjectContextCoordinator.Save is the one existing-project write
            // boundary that must tolerate a verified DWG path transition. This is
            // reached after BricsCAD Save/SaveAs has already moved the active DWG.
            // HasPendingChanges performs the non-mutating, allow-path-transition
            // freshness/target-sidecar check; Save repeats it under the project lock
            // before committing, so this does not weaken collision protection.
            if (string.Equals(operation, "Save Project", StringComparison.Ordinal) &&
                ProjectContextCoordinator.TryGetCached(document, out var cached))
            {
                _ = ProjectContextCoordinator.HasPendingChanges(document);
                return cached;
            }

            if (!TryGet(document, out var project))
                throw new InvalidOperationException(
                    operation + " cần một QS3D project hiện hữu; thao tác này không tạo project mới.");
            return project;
        }

        private static bool IsMouseFirstUnsavedProjectSave(Document document, string operation)
        {
            if (!string.Equals(operation, "Lưu dự án", StringComparison.Ordinal) &&
                !string.Equals(operation, "Lưu thành", StringComparison.Ordinal))
                return false;

            var drawing = document.Name ?? string.Empty;
            return string.IsNullOrWhiteSpace(drawing) || !Path.IsPathRooted(drawing);
        }
    }
}
