using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public static class GeneratedHandleOwnershipPolicy
    {
        private const string GeneratedSolidOwnerKey = "GeneratedSolidHandle";
        private const string OpeningCutOwnerKey = "PhysicalOpeningCutSolidHandle";
        private static readonly IReadOnlyList<string> RebarSlots = Array.AsReadOnly(new[]
        {
            "GeneratedRebarHandles",
            "GeneratedShapeRebarHandles",
            "GeneratedTieRebarHandles",
            "GeneratedBeamStirrupHandles",
            "GeneratedSlabMeshHandles",
            "GeneratedWallMeshHandles",
            "GeneratedFoundationMeshHandles"
        });

        public static IReadOnlyList<string> RebarHandleKeys => RebarSlots;

        public static bool IsOwnerSlot(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            if (string.Equals(normalized, OpeningCutOwnerKey, StringComparison.OrdinalIgnoreCase)) return true;
            if (!normalized.StartsWith("Generated", StringComparison.OrdinalIgnoreCase)) return false;
            return normalized.EndsWith("Handle", StringComparison.OrdinalIgnoreCase) ||
                   normalized.EndsWith("Handles", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRebarOwnerSlot(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            foreach (var candidate in RebarSlots)
                if (string.Equals(candidate, normalized, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string CanonicalOwnerSlot(string key)
        {
            var normalized = (key ?? string.Empty).Trim();
            return IsHostSolidAlias(normalized) ? GeneratedSolidOwnerKey : normalized;
        }

        public static bool AreSameLogicalOwnerSlots(string left, string right) =>
            string.Equals(CanonicalOwnerSlot(left), CanonicalOwnerSlot(right), StringComparison.OrdinalIgnoreCase);

        public static IEnumerable<KeyValuePair<string, string>> EnumerateOwnerHandles(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            foreach (var property in element.Properties)
            {
                if (!IsOwnerSlot(property.Key) || string.IsNullOrWhiteSpace(property.Value)) continue;
                foreach (var handle in SplitHandles(property.Value))
                    yield return new KeyValuePair<string, string>(handle, property.Key);
            }
        }

        public static IEnumerable<KeyValuePair<string, string>> EnumerateLogicalOwnerHandles(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in EnumerateOwnerHandles(element))
            {
                var slot = CanonicalOwnerSlot(entry.Value);
                var token = entry.Key + "\n" + slot;
                if (!seen.Add(token)) continue;
                yield return new KeyValuePair<string, string>(entry.Key, slot);
            }
        }

        public static IReadOnlyList<string> CollectOwnerHandles(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return project.Elements
                .SelectMany(EnumerateOwnerHandles)
                .Select(x => x.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        public static bool TryFindOwner(ProjectState project, string handle, out ProjectElement? owner, out string propertyKey)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = (handle ?? string.Empty).Trim();
            owner = null;
            propertyKey = string.Empty;
            if (normalized.Length == 0) return false;

            foreach (var element in project.Elements)
            {
                foreach (var entry in EnumerateOwnerHandles(element))
                {
                    if (!string.Equals(entry.Key, normalized, StringComparison.OrdinalIgnoreCase)) continue;
                    if (owner != null)
                    {
                        var sameElement = string.Equals(owner.Id, element.Id, StringComparison.OrdinalIgnoreCase);
                        var sameLogicalSlot = AreSameLogicalOwnerSlots(propertyKey, entry.Value);
                        if (!sameElement || !sameLogicalSlot)
                            throw new InvalidOperationException("Generated CAD handle " + normalized + " is ambiguously claimed by " + owner.Id + "/" + propertyKey + " and " + element.Id + "/" + entry.Value + ".");
                        continue;
                    }
                    owner = element;
                    propertyKey = entry.Value;
                }
            }
            return owner != null;
        }

        private static bool IsHostSolidAlias(string key) =>
            string.Equals(key, GeneratedSolidOwnerKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, OpeningCutOwnerKey, StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<string> SplitHandles(string raw) =>
            (raw ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
