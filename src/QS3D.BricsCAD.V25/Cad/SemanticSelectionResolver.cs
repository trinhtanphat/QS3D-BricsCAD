using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class SemanticSelectionResolver
    {
        private static readonly string[] SingleHandleKeys =
        {
            "GeneratedSolidHandle",
            "PhysicalOpeningCutSolidHandle"
        };

        private static readonly string[] MultiHandleKeys =
        {
            "GeneratedRebarHandles",
            "GeneratedShapeRebarHandles",
            "GeneratedTieRebarHandles",
            "GeneratedBeamStirrupHandles",
            "GeneratedSlabMeshHandles",
            "GeneratedWallMeshHandles",
            "GeneratedCurtainFrameHandles"
        };

        public static IReadOnlyList<ProjectElement> ResolveImplied(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return Array.Empty<ProjectElement>();
            var objectIds = selection.Value.GetObjectIds();
            if (objectIds.Length == 0) return Array.Empty<ProjectElement>();

            var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                foreach (var objectId in objectIds)
                {
                    var entity = transaction.GetObject(objectId, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased) continue;
                    var handle = entity.Handle.ToString();
                    if (!string.IsNullOrWhiteSpace(handle)) selectedHandles.Add(handle.Trim());
                }
            }
            if (selectedHandles.Count == 0) return Array.Empty<ProjectElement>();

            var owners = BuildOwnershipIndex(project);
            var resolved = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in selectedHandles)
            {
                if (!owners.TryGetValue(handle, out var owner)) continue;
                resolved[owner.Id] = owner;
            }
            return resolved.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static Dictionary<string, ProjectElement> BuildOwnershipIndex(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var source = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles) Add(handle, element, "SourceHandles", result, source);
                foreach (var key in SingleHandleKeys)
                    if (element.Properties.TryGetValue(key, out var raw)) Add(raw, element, key, result, source);
                foreach (var key in MultiHandleKeys)
                {
                    if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    foreach (var handle in Split(raw)) Add(handle, element, key, result, source);
                }
            }
            return result;
        }

        private static void Add(string? rawHandle, ProjectElement element, string channel, IDictionary<string, ProjectElement> owners, IDictionary<string, string> sources)
        {
            var handle = (rawHandle ?? string.Empty).Trim();
            if (handle.Length == 0) return;
            if (owners.TryGetValue(handle, out var existing))
            {
                if (string.Equals(existing.Id, element.Id, StringComparison.OrdinalIgnoreCase)) return;
                var existingChannel = sources.TryGetValue(handle, out var value) ? value : "unknown";
                throw new InvalidOperationException("CAD handle " + handle + " is ambiguously owned by semantic elements " + existing.Id + " (" + existingChannel + ") and " + element.Id + " (" + channel + "). Resolve project ownership before bulk property edits.");
            }
            owners[handle] = element;
            sources[handle] = channel;
        }

        private static IEnumerable<string> Split(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
