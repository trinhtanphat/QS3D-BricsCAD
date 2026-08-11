using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class GeneratedHandleOwnershipIndex
    {
        private sealed class Entry
        {
            public Entry(ProjectElement owner, string propertyKey)
            {
                Owner = owner;
                PropertyKey = propertyKey;
            }

            public ProjectElement Owner { get; }
            public string PropertyKey { get; }
            public string? Ambiguity { get; set; }
        }

        private readonly Dictionary<string, Entry> _entries;

        private GeneratedHandleOwnershipIndex(Dictionary<string, Entry> entries)
        {
            _entries = entries;
        }

        public int Count => _entries.Count;

        public static GeneratedHandleOwnershipIndex Build(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            EnsureValidUniqueElementIds(project);
            var entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in project.Elements)
            {
                foreach (var ownerHandle in GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element))
                {
                    var handle = (ownerHandle.Key ?? string.Empty).Trim();
                    if (handle.Length == 0) continue;

                    if (!entries.TryGetValue(handle, out var existing))
                    {
                        entries.Add(handle, new Entry(element, ownerHandle.Value));
                        continue;
                    }

                    var sameElement = ReferenceEquals(existing.Owner, element);
                    var sameLogicalSlot = GeneratedHandleOwnershipPolicy.AreSameLogicalOwnerSlots(existing.PropertyKey, ownerHandle.Value);
                    if (sameElement && sameLogicalSlot) continue;

                    if (existing.Ambiguity == null)
                    {
                        existing.Ambiguity = "Generated CAD handle " + handle + " is ambiguously claimed by " +
                            existing.Owner.Id + "/" + existing.PropertyKey + " and " + element.Id + "/" + ownerHandle.Value + ".";
                    }
                }
            }

            return new GeneratedHandleOwnershipIndex(entries);
        }

        public bool TryFindOwner(string handle, out ProjectElement? owner, out string propertyKey)
        {
            var normalized = (handle ?? string.Empty).Trim();
            owner = null;
            propertyKey = string.Empty;
            if (normalized.Length == 0) return false;
            if (!_entries.TryGetValue(normalized, out var entry)) return false;
            if (entry.Ambiguity != null) throw new InvalidOperationException(entry.Ambiguity);

            owner = entry.Owner;
            propertyKey = entry.PropertyKey;
            return true;
        }

        private static void EnsureValidUniqueElementIds(ProjectState project)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Generated handle ownership index cannot inspect a null project element.");
                if (string.IsNullOrWhiteSpace(element.Id))
                    throw new InvalidOperationException("Generated handle ownership index requires non-empty semantic element IDs.");

                var id = element.Id.Trim();
                if (!seen.Add(id))
                    throw new InvalidOperationException("Project contains duplicate element id: " + id);
            }
        }
    }
}
