using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class SemanticHandleOwnershipResolver
    {
        private const int MaxSelectedHandleInputCount = 10000;

        public static ProjectElement? ResolveUniqueSourceOwner(ProjectState project, string sourceHandle)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(sourceHandle);
            if (normalized.Length == 0) throw new ArgumentException("Source handle is required.", nameof(sourceHandle));
            EnsureUniqueElementIds(project);

            ProjectElement? owner = null;
            foreach (var element in project.Elements)
            {
                var ownsSource = false;
                foreach (var storedHandle in GetCanonicalUniqueStoredSourceHandles(element))
                {
                    if (string.Equals(storedHandle, normalized, StringComparison.OrdinalIgnoreCase))
                        ownsSource = true;
                }
                if (!ownsSource) continue;
                if (owner != null && !ReferenceEquals(owner, element))
                    throw new InvalidOperationException(
                        "CAD source handle " + normalized + " is claimed by multiple semantic elements " + owner.Id + " and " + element.Id +
                        ". Repair source ownership before capture.");
                owner = element;
            }
            return owner;
        }

        public static ProjectElement? ResolveCaptureTarget(ProjectState project, string sourceHandle, ElementCategory category, string canonicalId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedHandle = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(sourceHandle);
            if (normalizedHandle.Length == 0) throw new ArgumentException("Source handle is required.", nameof(sourceHandle));
            var rawId = canonicalId ?? string.Empty;
            var normalizedId = rawId.Trim();
            if (normalizedId.Length == 0) throw new ArgumentException("Canonical element ID is required.", nameof(canonicalId));
            if (!string.Equals(rawId, normalizedId, StringComparison.Ordinal))
                throw new ArgumentException("Canonical element ID must not contain leading or trailing whitespace.", nameof(canonicalId));

            var sourceOwner = ResolveUniqueSourceOwner(project, normalizedHandle);
            if (sourceOwner != null && sourceOwner.Category != category)
                throw new InvalidOperationException(
                    "CAD source handle " + normalizedHandle + " is already tracked as " + sourceOwner.Category +
                    ". Untrack it before changing semantic category.");

            var canonicalMatches = project.Elements
                .Where(x => string.Equals(x.Id, normalizedId, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (canonicalMatches.Count > 1)
                throw new InvalidOperationException("Canonical semantic element ID " + normalizedId + " is duplicated. Repair the project before capture.");
            var canonical = canonicalMatches.Count == 0 ? null : canonicalMatches[0];

            if (sourceOwner != null)
            {
                if (canonical != null && !ReferenceEquals(sourceOwner, canonical))
                    throw new InvalidOperationException(
                        "CAD source handle " + normalizedHandle + " belongs to " + sourceOwner.Id +
                        " but canonical ID " + normalizedId + " belongs to another semantic element.");
                return sourceOwner;
            }

            if (canonical == null) return null;
            if (canonical.Category != category)
                throw new InvalidOperationException("Canonical semantic element " + normalizedId + " has category " + canonical.Category + ".");
            if (canonical.SourceHandles.Any(x => !string.Equals(
                    GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(x),
                    normalizedHandle,
                    StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    "Canonical semantic element " + normalizedId + " is already bound to another CAD source handle.");
            return canonical;
        }

        public static IReadOnlyList<ProjectElement> Resolve(ProjectState project, IEnumerable<string> selectedHandles)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (selectedHandles == null) throw new ArgumentNullException(nameof(selectedHandles));
            var elementOwnership = SnapshotElementOwnership(project);

            var inputVersion = project.ChangeVersion;
            var selected = MaterializeSelectedHandles(selectedHandles);
            if (project.ChangeVersion != inputVersion)
                throw new InvalidOperationException("Project state changed while materializing semantic handle selection. Retry against the current project state.");
            RequireElementOwnershipUnchanged(project, elementOwnership);
            if (selected.Count == 0) return Array.Empty<ProjectElement>();

            var owners = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var channels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                foreach (var handle in GetCanonicalUniqueStoredSourceHandles(element))
                    Add(handle, element, "SourceHandles", selected, owners, channels);
                foreach (var entry in GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element))
                    Add(entry.Key, element, entry.Value, selected, owners, channels);

                if (element.SourceHandles.Count == 0 &&
                    AutoRoomLifecycle.IsAutoRoom(element) &&
                    element.Properties.TryGetValue(AutoRoomLifecycle.BoundarySourceHandlesKey, out var boundaryHandles) &&
                    !string.IsNullOrWhiteSpace(boundaryHandles))
                {
                    foreach (var handle in boundaryHandles.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        Add(handle, element, AutoRoomLifecycle.BoundarySourceHandlesKey, selected, owners, channels);
                }
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

        private static HashSet<string> MaterializeSelectedHandles(IEnumerable<string> selectedHandles)
        {
            if (selectedHandles is ICollection<string> collection && collection.Count > MaxSelectedHandleInputCount)
                throw new InvalidOperationException("Semantic handle selection cannot exceed " + MaxSelectedHandleInputCount + " input entries.");
            if (selectedHandles is IReadOnlyCollection<string> readOnlyCollection && readOnlyCollection.Count > MaxSelectedHandleInputCount)
                throw new InvalidOperationException("Semantic handle selection cannot exceed " + MaxSelectedHandleInputCount + " input entries.");

            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inputCount = 0;
            foreach (var rawHandle in selectedHandles)
            {
                if (inputCount >= MaxSelectedHandleInputCount)
                    throw new InvalidOperationException("Semantic handle selection cannot exceed " + MaxSelectedHandleInputCount + " input entries.");
                inputCount++;
                if (string.IsNullOrWhiteSpace(rawHandle)) continue;
                selected.Add(GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(rawHandle));
            }
            return selected;
        }

        private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null element entry.");
                if (result.ContainsKey(element.Id))
                    throw new InvalidOperationException("Project contains duplicate element id: " + element.Id);
                result.Add(element.Id, element);
            }
            return result;
        }

        private static void RequireElementOwnershipUnchanged(
            ProjectState project,
            IReadOnlyDictionary<string, ProjectElement> expected)
        {
            if (project.Elements.Count != expected.Count)
                throw new InvalidOperationException("Project element ownership changed while materializing semantic handle selection. Retry against the current project state.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || !seen.Add(element.Id) ||
                    !expected.TryGetValue(element.Id, out var original) ||
                    !ReferenceEquals(original, element))
                    throw new InvalidOperationException("Project element ownership changed while materializing semantic handle selection. Retry against the current project state.");
            }
        }

        private static void EnsureUniqueElementIds(ProjectState project)
        {
            SnapshotElementOwnership(project);
        }

        private static IReadOnlyList<string> GetCanonicalUniqueStoredSourceHandles(ProjectElement element)
        {
            var result = new List<string>(element.SourceHandles.Count);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < element.SourceHandles.Count; index++)
            {
                var handle = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(
                    RequireCanonicalStoredSourceHandle(element, element.SourceHandles[index], index));
                if (!seen.Add(handle))
                    throw new InvalidOperationException(
                        "Semantic element " + element.Id + " contains duplicate SourceHandles identity at index " + index + ": " + handle + ". Repair source ownership before continuing.");
                result.Add(handle);
            }
            return result.AsReadOnly();
        }

        private static string RequireCanonicalStoredSourceHandle(ProjectElement element, string? rawHandle, int index)
        {
            var raw = rawHandle ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException(
                    "Semantic element " + element.Id + " contains an empty SourceHandles entry at index " + index + ". Repair source ownership before continuing.");
            if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Semantic element " + element.Id + " contains a non-canonical SourceHandles entry at index " + index + ". Repair source ownership before continuing.");
            return raw;
        }

        private static void Add(
            string? rawHandle,
            ProjectElement element,
            string channel,
            ISet<string> selected,
            IDictionary<string, ProjectElement> owners,
            IDictionary<string, string> channels)
        {
            var handle = GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(rawHandle);
            if (handle.Length == 0 || !selected.Contains(handle)) return;
            if (owners.TryGetValue(handle, out var existing))
            {
                var existingChannel = channels.TryGetValue(handle, out var value) ? value : "unknown";
                if (ReferenceEquals(existing, element))
                {
                    if (GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots(existingChannel, channel)) return;
                    throw new InvalidOperationException(
                        "CAD handle " + handle + " has conflicting ownership channels on semantic element " + element.Id +
                        " (" + existingChannel + " / " + channel + "). Resolve project semantic ownership before continuing.");
                }
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
