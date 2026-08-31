using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public static class GeneratedHandleOwnershipPolicy
    {
        private const string GeneratedSolidOwnerKey = "GeneratedSolidHandle";
        private const string OpeningCutOwnerKey = "PhysicalOpeningCutSolidHandle";
        private const int MaxDestructiveHandleCount = 10000;
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

        public static string NormalizeHandleIdentity(string? handle) => GeneratedHandleIdentity.Normalize(handle);

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
            var seen = new HashSet<KeyValuePair<string, string>>(LogicalOwnerPairComparer.Instance);
            foreach (var entry in EnumerateOwnerHandles(element))
            {
                var logical = new KeyValuePair<string, string>(entry.Key, CanonicalOwnerSlot(entry.Value));
                if (!seen.Add(logical)) continue;
                yield return logical;
            }
        }

        public static IReadOnlyList<string> CollectOwnerHandles(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            EnsureValidElementSet(project);
            return project.Elements
                .SelectMany(x => EnumerateOwnerHandles(x))
                .Select(x => x.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        public static bool TryFindOwner(ProjectState project, string handle, out ProjectElement? owner, out string propertyKey)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = NormalizeHandleIdentity(handle);
            owner = null;
            propertyKey = string.Empty;
            if (normalized.Length == 0) return false;
            EnsureValidElementSet(project);

            foreach (var element in project.Elements)
            {
                foreach (var entry in EnumerateOwnerHandles(element))
                {
                    if (!string.Equals(entry.Key, normalized, StringComparison.OrdinalIgnoreCase)) continue;
                    if (owner != null)
                    {
                        var sameElement = ReferenceEquals(owner, element);
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

        public static IReadOnlyList<string> ValidateAllBeforeErase(
            ProjectState project,
            ProjectElement expectedOwner,
            string expectedPropertyKey,
            IEnumerable<string> handles,
            Action<string> nativeOwnershipValidator)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (expectedOwner == null) throw new ArgumentNullException(nameof(expectedOwner));
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            if (nativeOwnershipValidator == null) throw new ArgumentNullException(nameof(nativeOwnershipValidator));
            if (string.IsNullOrWhiteSpace(expectedPropertyKey)) throw new ArgumentException("Generated owner slot is required.", nameof(expectedPropertyKey));

            var knownCount = ResolveKnownDestructiveHandleCount(handles);
            var normalized = new List<string>(knownCount ?? 0);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var observedCount = 0;
            using (var enumerator = handles.GetEnumerator())
            {
                while (true)
                {
                    RequireStableKnownDestructiveHandleCount(handles, knownCount);
                    var moved = enumerator.MoveNext();
                    RequireStableKnownDestructiveHandleCount(handles, knownCount);
                    if (!moved) break;
                    if (knownCount.HasValue && observedCount >= knownCount.Value)
                        throw DestructiveHandleCountMismatch(knownCount.Value, observedCount + 1);
                    if (observedCount >= MaxDestructiveHandleCount)
                        throw new InvalidOperationException("Generated handle set cannot exceed " + MaxDestructiveHandleCount + " input entries.");

                    var rawHandle = enumerator.Current;
                    RequireStableKnownDestructiveHandleCount(handles, knownCount);
                    observedCount++;
                    var handle = NormalizeHandleIdentity(rawHandle);
                    if (handle.Length == 0) throw new InvalidOperationException("Generated handle set contains a blank handle.");
                    if (!seen.Add(handle)) throw new InvalidOperationException("Generated handle set contains duplicate handle " + handle + ".");
                    normalized.Add(handle);
                }
            }

            RequireStableKnownDestructiveHandleCount(handles, knownCount);
            if (knownCount.HasValue && observedCount != knownCount.Value)
                throw DestructiveHandleCountMismatch(knownCount.Value, observedCount);

            normalized.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var handle in normalized)
            {
                ProjectElement? actualOwner;
                string actualPropertyKey;
                if (!TryFindOwner(project, handle, out actualOwner, out actualPropertyKey) || actualOwner == null)
                    throw new InvalidOperationException("Refusing destructive replacement because generated CAD handle " + handle + " has no semantic owner.");
                if (!ReferenceEquals(actualOwner, expectedOwner) || !AreSameLogicalOwnerSlots(actualPropertyKey, expectedPropertyKey))
                    throw new InvalidOperationException("Refusing destructive replacement because generated CAD handle " + handle + " is owned by " + actualOwner.Id + "/" + actualPropertyKey + " instead of " + expectedOwner.Id + "/" + expectedPropertyKey + ".");
                nativeOwnershipValidator(handle);
            }
            return normalized.AsReadOnly();
        }

        private static int? ResolveKnownDestructiveHandleCount(IEnumerable<string> handles)
        {
            int? knownCount = null;
            if (handles is ICollection<string> genericCollection)
                AcceptKnownDestructiveHandleCount(genericCollection.Count, ref knownCount);
            if (handles is IReadOnlyCollection<string> readOnlyCollection)
                AcceptKnownDestructiveHandleCount(readOnlyCollection.Count, ref knownCount);
            if (handles is ICollection nonGenericCollection)
                AcceptKnownDestructiveHandleCount(nonGenericCollection.Count, ref knownCount);
            return knownCount;
        }

        private static void AcceptKnownDestructiveHandleCount(int candidate, ref int? knownCount)
        {
            if (candidate < 0)
                throw new InvalidOperationException("Generated handle set known Count cannot be negative.");
            if (candidate > MaxDestructiveHandleCount)
                throw new InvalidOperationException("Generated handle set cannot exceed " + MaxDestructiveHandleCount + " input entries.");
            if (knownCount.HasValue && knownCount.Value != candidate)
                throw new InvalidOperationException(
                    "Generated handle set exposes conflicting known Counts: " + knownCount.Value + " and " + candidate + ".");
            knownCount = candidate;
        }

        private static void RequireStableKnownDestructiveHandleCount(IEnumerable<string> handles, int? expectedCount)
        {
            if (!expectedCount.HasValue) return;
            var observedCount = ResolveKnownDestructiveHandleCount(handles);
            if (!observedCount.HasValue || observedCount.Value != expectedCount.Value)
                throw new InvalidOperationException(
                    "Generated handle set known Count changed during traversal from " + expectedCount.Value + " to " +
                    (observedCount.HasValue ? observedCount.Value.ToString() : "<none>") + ".");
        }

        private static InvalidOperationException DestructiveHandleCountMismatch(int reportedCount, int observedCount)
        {
            return new InvalidOperationException(
                "Generated handle set changed during traversal; Count reported " + reportedCount +
                " entries but traversal produced " + observedCount + ".");
        }

        private static void EnsureValidElementSet(ProjectState project)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry; generated CAD ownership cannot be resolved safely.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("Project contains a blank semantic element id; generated CAD ownership cannot be resolved safely.");
                if (!seen.Add(elementId))
                    throw new InvalidOperationException("Project contains duplicate element id: " + elementId);
            }
        }

        private sealed class LogicalOwnerPairComparer : IEqualityComparer<KeyValuePair<string, string>>
        {
            public static readonly LogicalOwnerPairComparer Instance = new LogicalOwnerPairComparer();

            public bool Equals(KeyValuePair<string, string> left, KeyValuePair<string, string> right) =>
                string.Equals(left.Key, right.Key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(left.Value, right.Value, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(KeyValuePair<string, string> value)
            {
                unchecked
                {
                    return (StringComparer.OrdinalIgnoreCase.GetHashCode(value.Key ?? string.Empty) * 397) ^
                           StringComparer.OrdinalIgnoreCase.GetHashCode(value.Value ?? string.Empty);
                }
            }
        }

        private static bool IsHostSolidAlias(string key) =>
            string.Equals(key, GeneratedSolidOwnerKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(key, OpeningCutOwnerKey, StringComparison.OrdinalIgnoreCase);

        private static IReadOnlyList<string> SplitHandles(string raw)
        {
            var tokens = (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.None);
            var handles = new List<string>(tokens.Length);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tokens)
            {
                var normalized = NormalizeHandleIdentity(token);
                if (normalized.Length == 0)
                    throw new InvalidOperationException("Generated owner handle property contains an empty handle token; persisted ownership provenance is malformed.");
                if (!string.Equals(token, normalized, StringComparison.Ordinal))
                    throw new InvalidOperationException("Generated owner handle property contains non-canonical handle token '" + token + "'; expected '" + normalized + "'.");
                if (!seen.Add(normalized))
                    throw new InvalidOperationException("Generated owner handle property contains duplicate handle token " + normalized + ".");
                handles.Add(normalized);
            }
            return handles.AsReadOnly();
        }
    }
}
