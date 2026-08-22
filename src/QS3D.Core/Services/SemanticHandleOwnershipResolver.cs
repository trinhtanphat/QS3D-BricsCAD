using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class SemanticHandleOwnershipResolver
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

        public static IReadOnlyList<ProjectElement> Resolve(ProjectState project, IEnumerable<string> selectedHandles)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (selectedHandles == null) throw new ArgumentNullException(nameof(selectedHandles));

            var selected = new HashSet<string>(
                selectedHandles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0) return Array.Empty<ProjectElement>();

            var owners = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var channels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var handle in element.SourceHandles)
                    Add(handle, element, "SourceHandles", selected, owners, channels);
                foreach (var key in SingleHandleKeys)
                    if (element.Properties.TryGetValue(key, out var raw)) Add(raw, element, key, selected, owners, channels);
                foreach (var key in MultiHandleKeys)
                {
                    if (!element.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    foreach (var handle in Split(raw)) Add(handle, element, key, selected, owners, channels);
                }
            }

            return owners.Values
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        private static void Add(
            string? rawHandle,
            ProjectElement element,
            string channel,
            ISet<string> selected,
            IDictionary<string, ProjectElement> owners,
            IDictionary<string, string> channels)
        {
            var handle = (rawHandle ?? string.Empty).Trim();
            if (handle.Length == 0 || !selected.Contains(handle)) return;
            if (owners.TryGetValue(handle, out var existing))
            {
                if (string.Equals(existing.Id, element.Id, StringComparison.OrdinalIgnoreCase)) return;
                var existingChannel = channels.TryGetValue(handle, out var value) ? value : "unknown";
                throw new InvalidOperationException("CAD handle " + handle + " is ambiguously owned by semantic elements " + existing.Id + " (" + existingChannel + ") and " + element.Id + " (" + channel + "). Resolve project ownership before bulk property edits.");
            }
            owners[handle] = element;
            channels[handle] = channel;
        }

        private static IEnumerable<string> Split(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
