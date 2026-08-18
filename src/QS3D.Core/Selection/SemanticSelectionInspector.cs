using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Selection
{
    public sealed class SemanticSelectionTextValue
    {
        internal SemanticSelectionTextValue(string name, int presentCount, bool isMixed, string? value)
        {
            Name = name;
            PresentCount = presentCount;
            IsMixed = isMixed;
            Value = value;
        }

        public string Name { get; }
        public int PresentCount { get; }
        public bool IsMixed { get; }
        public string? Value { get; }
    }

    public sealed class SemanticSelectionQuantityValue
    {
        internal SemanticSelectionQuantityValue(string name, int presentCount, bool isMixed, double? value)
        {
            Name = name;
            PresentCount = presentCount;
            IsMixed = isMixed;
            Value = value;
        }

        public string Name { get; }
        public int PresentCount { get; }
        public bool IsMixed { get; }
        public double? Value { get; }
    }

    public sealed class SemanticSelectionInspection
    {
        internal SemanticSelectionInspection(
            IReadOnlyList<string> elementIds,
            IReadOnlyList<ElementCategory> categories,
            SemanticSelectionTextValue family,
            SemanticSelectionTextValue floor,
            SemanticSelectionTextValue zone,
            IReadOnlyList<SemanticSelectionTextValue> properties,
            IReadOnlyList<SemanticSelectionQuantityValue> quantities)
        {
            ElementIds = new List<string>(elementIds).AsReadOnly();
            Categories = new List<ElementCategory>(categories).AsReadOnly();
            Family = family;
            Floor = floor;
            Zone = zone;
            Properties = new List<SemanticSelectionTextValue>(properties).AsReadOnly();
            Quantities = new List<SemanticSelectionQuantityValue>(quantities).AsReadOnly();
        }

        public int Count => ElementIds.Count;
        public IReadOnlyList<string> ElementIds { get; }
        public IReadOnlyList<ElementCategory> Categories { get; }
        public bool HasMixedCategories => Categories.Count > 1;
        public SemanticSelectionTextValue Family { get; }
        public SemanticSelectionTextValue Floor { get; }
        public SemanticSelectionTextValue Zone { get; }
        public IReadOnlyList<SemanticSelectionTextValue> Properties { get; }
        public IReadOnlyList<SemanticSelectionQuantityValue> Quantities { get; }
    }

    public static class SemanticSelectionInspector
    {
        private const int MaxSelection = 100000;

        public static SemanticSelectionInspection Inspect(ProjectState project, IEnumerable<string> elementIds)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));

            var inspectionVersion = project.ChangeVersion;
            RequireSelectionKnownCountWithinLimit(elementIds);
            var projectIndex = BuildUniqueProjectIndex(project);
            var familyIndex = BuildUniqueFamilyIndex(project);
            var requested = new List<string>();
            var requestedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawId in elementIds)
            {
                if (requested.Count >= MaxSelection) throw new InvalidOperationException("Semantic property inspector supports at most " + MaxSelection + " selected elements.");
                if (string.IsNullOrWhiteSpace(rawId)) throw new ArgumentException("Selected semantic element id is required.", nameof(elementIds));
                var canonicalId = rawId.Trim();
                if (!string.Equals(rawId, canonicalId, StringComparison.Ordinal))
                    throw new ArgumentException("Selected semantic element id must not contain leading or trailing whitespace.", nameof(elementIds));
                var id = rawId;
                if (!requestedSet.Add(id)) throw new InvalidOperationException("Semantic property inspector received duplicate selected element id: " + id + ".");
                if (!projectIndex.ContainsKey(id)) throw new InvalidOperationException("Semantic property inspector references missing element id: " + id + ".");
                requested.Add(id);
            }
            RequireProjectFresh(project, inspectionVersion, projectIndex, familyIndex);

            var selected = requested
                .Select(id => projectIndex[id])
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Id, StringComparer.Ordinal)
                .ToArray();
            ValidateSemanticReferences(project, selected, familyIndex);

            var categories = selected
                .Select(x => x.Category)
                .Distinct()
                .OrderBy(x => x.ToString(), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var inspection = new SemanticSelectionInspection(
                selected.Select(x => x.Id).ToArray(),
                categories,
                InspectReference("FamilyId", selected.Select(x => x.FamilyId).ToArray()),
                InspectReference("FloorId", selected.Select(x => x.FloorId).ToArray()),
                InspectReference("ZoneId", selected.Select(x => x.ZoneId).ToArray()),
                InspectProperties(selected, familyIndex),
                InspectQuantities(selected));
            RequireProjectFresh(project, inspectionVersion, projectIndex, familyIndex);
            return inspection;
        }

        private static void RequireSelectionKnownCountWithinLimit(IEnumerable<string> elementIds)
        {
            if (elementIds is ICollection<string> collection)
                RequireValidSelectionKnownCount(collection.Count);
            if (elementIds is IReadOnlyCollection<string> readOnlyCollection)
                RequireValidSelectionKnownCount(readOnlyCollection.Count);
            if (elementIds is System.Collections.ICollection nonGenericCollection)
                RequireValidSelectionKnownCount(nonGenericCollection.Count);
        }

        private static void RequireValidSelectionKnownCount(int count)
        {
            if (count < 0)
                throw new InvalidOperationException("Semantic property inspector selection source exposes an invalid negative known count.");
            if (count > MaxSelection)
                throw new InvalidOperationException("Semantic property inspector supports at most " + MaxSelection + " selected elements.");
        }

        private static Dictionary<string, ProjectElement> BuildUniqueProjectIndex(ProjectState project)
        {
            var result = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null) throw new InvalidOperationException("Project contains a null semantic element.");
                if (string.IsNullOrWhiteSpace(element.Id)) throw new InvalidOperationException("Project contains an empty semantic element id.");
                if (!Enum.IsDefined(typeof(ElementCategory), element.Category)) throw new InvalidOperationException("Project contains an undefined semantic element category: " + element.Id + ".");
                var id = element.Id.Trim();
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project contains duplicate semantic element id: " + id + ".");
                result.Add(id, element);
            }
            return result;
        }

        private static Dictionary<string, ProjectFamily> BuildUniqueFamilyIndex(ProjectState project)
        {
            var result = new Dictionary<string, ProjectFamily>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null) throw new InvalidOperationException("Project contains a null semantic family.");
                if (string.IsNullOrWhiteSpace(family.Id)) throw new InvalidOperationException("Project contains an empty semantic family id.");
                if (!Enum.IsDefined(typeof(ElementCategory), family.Category)) throw new InvalidOperationException("Project contains an undefined semantic family category: " + family.Id + ".");
                var id = family.Id.Trim();
                if (result.ContainsKey(id)) throw new InvalidOperationException("Project contains duplicate semantic family id: " + id + ".");
                result.Add(id, family);
            }
            return result;
        }

        private static void RequireProjectFresh(
            ProjectState project,
            long expectedChangeVersion,
            IReadOnlyDictionary<string, ProjectElement> expectedElements,
            IReadOnlyDictionary<string, ProjectFamily> expectedFamilies)
        {
            if (project.ChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException("Project state changed while materializing semantic selection ids.");
            if (project.Elements.Count != expectedElements.Count)
                throw StructuralFreshnessError();

            var seenElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null || string.IsNullOrWhiteSpace(element.Id)) throw StructuralFreshnessError();
                var id = element.Id.Trim();
                if (!seenElements.Add(id) ||
                    !expectedElements.TryGetValue(id, out var original) ||
                    !ReferenceEquals(original, element))
                    throw StructuralFreshnessError();
            }

            if (project.Families.Count != expectedFamilies.Count)
                throw StructuralFreshnessError();
            var seenFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null || string.IsNullOrWhiteSpace(family.Id)) throw StructuralFreshnessError();
                var id = family.Id.Trim();
                if (!seenFamilies.Add(id) ||
                    !expectedFamilies.TryGetValue(id, out var original) ||
                    !ReferenceEquals(original, family))
                    throw StructuralFreshnessError();
            }
        }

        private static InvalidOperationException StructuralFreshnessError() =>
            new InvalidOperationException("Project semantic ownership changed while inspecting semantic selection; retry the inspection.");

        private static void ValidateSemanticReferences(
            ProjectState project,
            IEnumerable<ProjectElement> selected,
            IReadOnlyDictionary<string, ProjectFamily> familyIndex)
        {
            foreach (var element in selected)
            {
                var familyId = CanonicalOptionalReference(element.FamilyId, element.Id, "family");
                var floorId = CanonicalOptionalReference(element.FloorId, element.Id, "floor");
                var zoneId = CanonicalOptionalReference(element.ZoneId, element.Id, "zone");
                if (familyId.Length > 0)
                {
                    if (!familyIndex.TryGetValue(familyId, out var family) || family == null)
                        throw new InvalidOperationException("Selected element references missing family id: " + element.Id + "/" + familyId + ".");
                    if (family.Category != element.Category)
                        throw new InvalidOperationException("Selected element/family category mismatch: " + element.Id + "/" + family.Id + ".");
                }
                if (floorId.Length > 0 && project.FindFloor(floorId) == null)
                    throw new InvalidOperationException("Selected element references missing floor id: " + element.Id + "/" + floorId + ".");
                if (zoneId.Length > 0 && project.FindZone(zoneId) == null)
                    throw new InvalidOperationException("Selected element references missing zone id: " + element.Id + "/" + zoneId + ".");
            }
        }

        private static string CanonicalOptionalReference(string? value, string elementId, string label)
        {
            if (value == null || string.IsNullOrWhiteSpace(value)) return string.Empty;
            var normalized = value.Trim();
            if (!string.Equals(value, normalized, StringComparison.Ordinal))
                throw new InvalidOperationException("Selected element contains a non-canonical " + label + " id: " + elementId + "/" + value + ".");
            return normalized;
        }

        private static SemanticSelectionTextValue InspectReference(string name, IReadOnlyList<string> values)
        {
            if (values.Count == 0) return new SemanticSelectionTextValue(name, 0, false, null);
            var normalized = values.Select(x => (x ?? string.Empty).Trim()).ToArray();
            var present = normalized.Count(x => x.Length > 0);
            var first = normalized[0];
            var mixed = normalized.Skip(1).Any(x => !string.Equals(x, first, StringComparison.OrdinalIgnoreCase));
            return new SemanticSelectionTextValue(name, present, mixed, mixed ? null : first);
        }

        private static IReadOnlyList<SemanticSelectionTextValue> InspectProperties(
            IReadOnlyList<ProjectElement> selected,
            IReadOnlyDictionary<string, ProjectFamily> familyIndex)
        {
            var effective = selected.Select(x => BuildEffectiveProperties(x, familyIndex)).ToArray();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var properties in effective)
                foreach (var key in properties.Keys) keys.Add(key);

            var result = new List<SemanticSelectionTextValue>(keys.Count);
            foreach (var key in keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal))
            {
                var present = 0;
                string? first = null;
                var firstSet = false;
                var mixed = false;
                foreach (var properties in effective)
                {
                    if (!properties.TryGetValue(key, out var value))
                    {
                        mixed = true;
                        continue;
                    }
                    present++;
                    if (!firstSet)
                    {
                        first = value;
                        firstSet = true;
                    }
                    else if (!string.Equals(first, value, StringComparison.Ordinal))
                    {
                        mixed = true;
                    }
                }
                if (present != selected.Count) mixed = true;
                result.Add(new SemanticSelectionTextValue(key, present, mixed, mixed ? null : first));
            }
            return result;
        }

        private static IReadOnlyDictionary<string, string> BuildEffectiveProperties(
            ProjectElement element,
            IReadOnlyDictionary<string, ProjectFamily> familyIndex)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var familyId = (element.FamilyId ?? string.Empty).Trim();
            if (familyId.Length > 0 && familyIndex.TryGetValue(familyId, out var family))
            {
                foreach (var property in family.Properties)
                {
                    RequireCanonicalPropertyKey(property.Key, "Family " + family.Id);
                    if (!IsInternalOwnershipProperty(property.Key)) result[property.Key] = property.Value ?? string.Empty;
                }
            }

            foreach (var property in element.Properties)
            {
                RequireCanonicalPropertyKey(property.Key, "element " + element.Id);
                if (!IsInternalOwnershipProperty(property.Key)) result[property.Key] = property.Value ?? string.Empty;
            }
            return result;
        }

        private static void RequireCanonicalPropertyKey(string key, string owner)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Selected " + owner + " contains an empty property key.");
            var canonicalKey = key.Trim();
            if (!string.Equals(key, canonicalKey, StringComparison.Ordinal))
                throw new InvalidOperationException("Selected " + owner + " contains a non-canonical property key: " + key + ".");
        }

        private static IReadOnlyList<SemanticSelectionQuantityValue> InspectQuantities(IReadOnlyList<ProjectElement> selected)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in selected)
            {
                foreach (var key in element.Quantities.Keys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                        throw new InvalidOperationException("Selected element contains an empty quantity name: " + element.Id + ".");
                    var canonicalKey = key.Trim();
                    if (!string.Equals(key, canonicalKey, StringComparison.Ordinal))
                        throw new InvalidOperationException("Selected element contains a non-canonical quantity name: " + element.Id + "/" + key + ".");
                    keys.Add(key);
                }
            }

            var result = new List<SemanticSelectionQuantityValue>(keys.Count);
            foreach (var key in keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal))
            {
                var present = 0;
                double? first = null;
                var mixed = false;
                foreach (var element in selected)
                {
                    if (!element.Quantities.TryGetValue(key, out var value))
                    {
                        mixed = true;
                        continue;
                    }
                    if (double.IsNaN(value) || double.IsInfinity(value))
                        throw new InvalidOperationException("Selected element contains a non-finite quantity: " + element.Id + "/" + key + ".");
                    if (value < 0d)
                        throw new InvalidOperationException("Selected element contains a negative quantity: " + element.Id + "/" + key + ".");
                    value = value == 0d ? 0d : value;
                    present++;
                    if (!first.HasValue) first = value;
                    else if (first.Value != value) mixed = true;
                }
                if (present != selected.Count) mixed = true;
                result.Add(new SemanticSelectionQuantityValue(key, present, mixed, mixed ? null : first));
            }
            return result;
        }

        private static bool IsInternalOwnershipProperty(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return true;
            var normalized = key.Trim();
            if (normalized.IndexOf("Handle", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (normalized.StartsWith("QS3D.Generated", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.StartsWith("QS3D.PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return true;
            if (normalized.StartsWith("PhysicalOpeningCut", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
