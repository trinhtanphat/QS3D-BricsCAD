using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private const string QuantityGeneratedSolidHandleKey = "GeneratedSolidHandle";
        private const string QuarantinedGeometryHandle = "7FFFFFFFFFFFFFFE";

        private enum GeneratedSolidStatus
        {
            None,
            Valid,
            Invalid
        }

        private static ProjectState? PrepareQuantityGeometrySnapshot(
            Document document,
            ProjectState liveProject,
            IEnumerable<string> targetElementIds,
            out string error)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (liveProject == null) throw new ArgumentNullException(nameof(liveProject));
            error = string.Empty;

            var targets = new HashSet<string>(
                (targetElementIds ?? Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            var snapshot = ProjectStateSnapshot.CreateDetachedCopy(liveProject);

            foreach (var liveElement in liveProject.Elements)
            {
                var detachedElement = snapshot.FindElement(liveElement.Id);
                if (detachedElement == null) continue;

                // Geometry explanation is per canonical element. Do not let semantic dependency
                // traversal accidentally substitute a dependency's CAD object for this element.
                detachedElement.DependsOn.Clear();

                var status = ResolveConfiguredGeneratedSolid(
                    document,
                    liveProject,
                    liveElement,
                    out var generatedHandle,
                    out var failure);
                if (status == GeneratedSolidStatus.Valid)
                {
                    // SourceHandleResolver intentionally prefers source geometry. For this detached,
                    // read-only BREP view we instead route it to the already-owned generated Solid3d.
                    detachedElement.SourceHandles.Clear();
                    continue;
                }

                if (status == GeneratedSolidStatus.Invalid)
                {
                    if (targets.Contains(liveElement.Id))
                    {
                        error = "Solid3d generated của " + liveElement.Id + " đã stale/foreign: " + failure;
                        return null;
                    }

                    // A configured generated handle is authoritative. If it is stale/foreign, do not
                    // silently fall back to an unrelated 2D source while explaining exact BREP.
                    detachedElement.SourceHandles.Clear();
                    detachedElement.SourceHandles.Add(QuarantinedGeometryHandle);
                    continue;
                }

                if (!targets.Contains(liveElement.Id)) continue;

                // Legacy/recovered projects may have valid owner XData but no configured property.
                // Recover only exact owner-matched live solids for the selected target.
                var owned = GeneratedGeometryService.FindMatchingOwnedHandles(
                    document,
                    liveProject.ProjectId,
                    liveElement.Id,
                    liveElement.Category);
                var liveOwned = CadHandleService.GetLiveSolidHandles(document, owned)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (liveOwned.Length == 0) continue;
                detachedElement.SourceHandles.Clear();
                foreach (var handle in liveOwned) detachedElement.SourceHandles.Add(handle);
            }

            return snapshot;
        }

        private static IReadOnlyList<string> ResolveQuantityPreferredLiveHandles(
            Document document,
            ProjectState project,
            IEnumerable<string> elementIds,
            out string error)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            error = string.Empty;

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in (elementIds ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var element = project.FindElement(id);
                if (element == null)
                {
                    error = "Semantic element không còn tồn tại: " + id;
                    return Array.Empty<string>();
                }

                var status = ResolveConfiguredGeneratedSolid(document, project, element, out var generatedHandle, out var failure);
                if (status == GeneratedSolidStatus.Valid)
                {
                    if (seen.Add(generatedHandle)) result.Add(generatedHandle);
                    continue;
                }
                if (status == GeneratedSolidStatus.Invalid)
                {
                    error = "Generated Solid3d của " + id + " đã stale/foreign: " + failure;
                    return Array.Empty<string>();
                }

                var owned = GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, element.Id, element.Category);
                var liveOwned = CadHandleService.GetLiveSolidHandles(document, owned);
                if (liveOwned.Count > 0)
                {
                    foreach (var handle in liveOwned.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                        if (seen.Add(handle)) result.Add(handle);
                    continue;
                }

                foreach (var handle in SourceHandleResolver.Resolve(project, new[] { id }))
                    if (!string.IsNullOrWhiteSpace(handle) && seen.Add(handle.Trim())) result.Add(handle.Trim());
            }
            return result.AsReadOnly();
        }

        private static GeneratedSolidStatus ResolveConfiguredGeneratedSolid(
            Document document,
            ProjectState project,
            ProjectElement element,
            out string handle,
            out string failure)
        {
            handle = string.Empty;
            failure = string.Empty;
            if (!element.Properties.TryGetValue(QuantityGeneratedSolidHandleKey, out var configured) || string.IsNullOrWhiteSpace(configured))
                return GeneratedSolidStatus.None;

            var normalized = CadHandleService.NormalizeHexHandle(configured);
            if (normalized == null)
            {
                failure = "GeneratedSolidHandle không hợp lệ.";
                return GeneratedSolidStatus.Invalid;
            }

            var ids = CadHandleService.Resolve(document, new[] { normalized });
            if (ids.Count != 1)
            {
                failure = "handle " + normalized + " không còn live trong active DWG.";
                return GeneratedSolidStatus.Invalid;
            }

            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var solid = transaction.GetObject(ids[0], OpenMode.ForRead, false) as Solid3d;
                if (solid == null || solid.IsErased)
                {
                    failure = "handle " + normalized + " không trỏ tới Solid3d live.";
                    transaction.Commit();
                    return GeneratedSolidStatus.Invalid;
                }
                if (!GeneratedGeometryService.HasMatchingOwnership(solid, project, element))
                {
                    failure = "QS3D ownership marker không khớp project/element/category.";
                    transaction.Commit();
                    return GeneratedSolidStatus.Invalid;
                }
                transaction.Commit();
            }

            handle = normalized;
            return GeneratedSolidStatus.Valid;
        }
    }
}
