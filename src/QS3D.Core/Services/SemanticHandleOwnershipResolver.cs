using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class SemanticHandleOwnershipResolver
    {
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
                foreach (var entry in GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element))
                    Add(entry.Key, element, entry.Value, selected, owners, channels);
            }

            var matchedById = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in owners.Values)
            {
                if (matchedById.TryGetValue(element.Id, out var existing))
                {
                    if (!ReferenceEquals(existing, element))
                    {
                        throw new InvalidOperationException(
                            "Semantic element ID " + element.Id +
                            " is duplicated across multiple project instances selected by CAD handles. Repair duplicate semantic IDs before continuing.");
                    }
                    continue;
                }
                matchedById[element.Id] = element;
            }

            return matchedById.Values
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
                if (ReferenceEquals(existing, element)) return;
                var existingChannel = channels.TryGetValue(handle, out var value) ? value : "unknown";
                if (string.Equals(existing.Id, element.Id, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "CAD handle " + handle + " is claimed by multiple semantic instances sharing duplicate ID " + element.Id +
                        " (" + existingChannel + " / " + channel + "). Repair duplicate semantic IDs before continuing.");
                }
                throw new InvalidOperationException(
                    "CAD handle " + handle + " is ambiguously owned by semantic elements " + existing.Id + " (" + existingChannel + ") and " +
                    element.Id + " (" + channel + "). Resolve project semantic ownership before continuing.");
            }
            owners[handle] = element;
            channels[handle] = channel;
        }
    }
}
