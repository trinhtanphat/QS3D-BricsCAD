using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class BulkEditService
    {
        private const int MaxTargetInputCount = 10000;

        private sealed class PendingPropertyUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string Value { get; set; } = string.Empty;
        }

        private sealed class PendingFamilyAssignment
        {
            public ProjectElement Element { get; set; } = null!;
            public HashSet<string> InheritedKeys { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<string> SetProperty(ProjectState project, IEnumerable<ProjectElement> elements, string propertyName, string value)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var key = SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName);
            var beforeTargetEnumeration = project.ChangeVersion;
            var targets = OwnedDistinct(project, elements);
            RequireTargetEnumerationFreshness(project, beforeTargetEnumeration, "Bulk edit object target enumeration");
            RequireCurrentElementOwnership(project, targets, "Bulk edit object target enumeration");
            var updates = new List<PendingPropertyUpdate>();
            var next = value ?? string.Empty;
            foreach (var element in targets)
            {
                var hadBefore = element.Properties.TryGetValue(key, out var before);
                if (hadBefore && string.Equals(before ?? string.Empty, next, StringComparison.Ordinal)) continue;
                updates.Add(new PendingPropertyUpdate { Element = element, Value = next });
            }

            if (updates.Count == 0) return Array.Empty<string>();
            ValidatePendingPropertyMapsForMutation(updates, "bulk setting a property");
            return ProjectSemanticMutationExecutor.Execute(project, "bulk.set-property", () =>
            {
                var changed = new List<string>(updates.Count);
                foreach (var update in updates)
                {
                    update.Element.SetProperty(key, update.Value);
                    changed.Add(update.Element.Id);
                }
                project.Touch();
                return changed.AsReadOnly();
            });
        }

        public IReadOnlyList<string> MultiplyNumericProperty(ProjectState project, IEnumerable<ProjectElement> elements, string propertyName, double factor)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (double.IsNaN(factor) || double.IsInfinity(factor)) throw new ArgumentOutOfRangeException(nameof(factor));
            var key = SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName);

            var beforeTargetEnumeration = project.ChangeVersion;
            var targets = OwnedDistinct(project, elements);
            RequireTargetEnumerationFreshness(project, beforeTargetEnumeration, "Bulk numeric object target enumeration");
            RequireCurrentElementOwnership(project, targets, "Bulk numeric object target enumeration");
            var updates = new List<PendingPropertyUpdate>();
            foreach (var element in targets)
            {
                if (!element.Properties.TryGetValue(key, out var text)) continue;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var current) || double.IsNaN(current) || double.IsInfinity(current))
                    throw new FormatException("Invalid numeric property " + key + " on " + element.Id + ": " + text);
                if (current == 0d && HasNonZeroSignificand(text))
                    throw new InvalidOperationException("Bulk numeric property underflow for " + element.Id + "/" + key + ": " + text);
                var next = current * factor;
                if (double.IsNaN(next) || double.IsInfinity(next)) throw new OverflowException("Bulk property multiplication overflow for " + element.Id + "/" + key);
                if (next == 0d && current != 0d && factor != 0d)
                    throw new InvalidOperationException("Bulk property multiplication underflow for " + element.Id + "/" + key);
                if (next.Equals(current) && current != 0d && factor != 1d)
                    throw new InvalidOperationException("Bulk property multiplication lost a non-unit factor at floating-point precision for " + element.Id + "/" + key);
                if (next.Equals(current)) continue;
                var formatted = next.ToString("R", CultureInfo.InvariantCulture);
                updates.Add(new PendingPropertyUpdate { Element = element, Value = formatted });
            }

            if (updates.Count == 0) return Array.Empty<string>();
            ValidatePendingPropertyMapsForMutation(updates, "bulk multiplying a numeric property");
            return ProjectSemanticMutationExecutor.Execute(project, "bulk.multiply-numeric-property", () =>
            {
                var changed = new List<string>(updates.Count);
                foreach (var update in updates)
                {
                    update.Element.SetProperty(key, update.Value);
                    changed.Add(update.Element.Id);
                }
                project.Touch();
                return changed.AsReadOnly();
            });
        }

        public int SetProperty(ProjectState project, IEnumerable<string> elementIds, string propertyName, string value)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            var beforeTargetEnumeration = project.ChangeVersion;
            var targets = OwnedDistinctByIds(project, elementIds);
            RequireTargetEnumerationFreshness(project, beforeTargetEnumeration, "Bulk edit target-id enumeration");
            return SetProperty(project, targets, propertyName, value).Count;
        }

        public int AssignFamily(ProjectState project, IEnumerable<string> elementIds, string familyId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            ValidateUniqueFamilyIds(project);
            var family = project.FindFamily(familyId) ?? throw new KeyNotFoundException("Unknown family: " + familyId);

            var familyOwnership = SnapshotFamilyOwnership(project);
            var beforeTargetEnumeration = project.ChangeVersion;
            var targets = OwnedDistinctByIds(project, elementIds);
            RequireTargetEnumerationFreshness(project, beforeTargetEnumeration, "Bulk Family target-id enumeration");
            RequireCurrentFamilyAssignmentOwnership(project, family, targets);
            RequireFamilyOwnershipUnchanged(project, familyOwnership);

            var targetProperties = ProjectFamilyService.SnapshotProperties(family, "Target", "bulk assignment");
            var targetPropertyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in targetProperties) targetPropertyKeys.Add(property.Key);

            foreach (var element in targets)
                if (element.Category != family.Category)
                    throw new InvalidOperationException("Cannot assign family " + family.Id + " (" + family.Category + ") to element " + element.Id + " (" + element.Category + "). Bulk family assignment is all-or-nothing.");

            var pending = new List<PendingFamilyAssignment>();
            var previousSnapshots = new Dictionary<string, IReadOnlyList<KeyValuePair<string, string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in targets)
            {
                var previousFamilyId = (element.FamilyId ?? string.Empty).Trim();
                if (string.Equals(previousFamilyId, family.Id, StringComparison.OrdinalIgnoreCase)) continue;

                IReadOnlyList<KeyValuePair<string, string>> previousProperties = Array.Empty<KeyValuePair<string, string>>();
                if (previousFamilyId.Length > 0)
                {
                    var previousFamily = project.FindFamily(previousFamilyId) ??
                        throw new InvalidOperationException("Element " + element.Id + " references missing family id: " + previousFamilyId + ". Repair the relation before bulk reassignment.");
                    if (previousFamily.Category != element.Category)
                        throw new InvalidOperationException("Element " + element.Id + " references previous Family '" + previousFamily.Id + "' category " + previousFamily.Category + " while the element category is " + element.Category + ". Repair the relation before bulk reassignment.");
                    if (!previousSnapshots.TryGetValue(previousFamily.Id, out previousProperties))
                    {
                        previousProperties = ProjectFamilyService.SnapshotProperties(previousFamily, "Previous", "bulk assignment");
                        previousSnapshots.Add(previousFamily.Id, previousProperties);
                    }
                }

                var inheritedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in previousProperties)
                    if (element.Properties.TryGetValue(property.Key, out var current) && string.Equals(current, property.Value, StringComparison.Ordinal))
                        inheritedKeys.Add(property.Key);
                pending.Add(new PendingFamilyAssignment { Element = element, InheritedKeys = inheritedKeys });
            }

            if (pending.Count == 0) return 0;
            var pendingElements = new List<ProjectElement>(pending.Count);
            foreach (var item in pending) pendingElements.Add(item.Element);
            ProjectFamilyService.ValidateMemberPropertyKeysForMutation(pendingElements, "bulk assigning a Family");
            return ProjectSemanticMutationExecutor.Execute(project, "bulk.assign-family", () =>
            {
                foreach (var item in pending)
                {
                    var element = item.Element;
                    foreach (var inheritedKey in item.InheritedKeys)
                        if (!targetPropertyKeys.Contains(inheritedKey)) element.Properties.Remove(inheritedKey);
                    foreach (var property in targetProperties)
                        if (item.InheritedKeys.Contains(property.Key) || !element.Properties.ContainsKey(property.Key))
                            element.Properties[property.Key] = property.Value;

                    element.FamilyId = family.Id;
                    var dirty = ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity;
                    if (ElementGeometryPolicy.RequiresGeneratedGeometry(element.Category)) dirty |= ElementDirtyFlags.Geometry;
                    element.MarkDirty(dirty);
                }
                project.Touch();
                return pending.Count;
            });
        }

        private static IReadOnlyList<ProjectElement> OwnedDistinctByIds(ProjectState project, IEnumerable<string> elementIds)
        {
            var sourceElements = new List<ProjectElement>(project.Elements);
            var rawIds = MaterializeBounded(elementIds, "Bulk edit target list");
            var sourceIndex = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceElement in sourceElements)
            {
                if (sourceElement == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                var sourceElementId = (sourceElement.Id ?? string.Empty).Trim();
                if (sourceElementId.Length == 0)
                    throw new InvalidOperationException("Project contains a semantic element with a blank id.");
                if (sourceIndex.ContainsKey(sourceElementId))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + sourceElementId);
                sourceIndex.Add(sourceElementId, sourceElement);
            }

            var resolved = new List<ProjectElement>();
            var requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in rawIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Bulk edit target id is required.", nameof(elementIds));
                var normalized = id.Trim();
                if (!string.Equals(normalized, id, StringComparison.Ordinal))
                    throw new ArgumentException("Bulk edit target id must use canonical surrounding whitespace: " + id, nameof(elementIds));
                if (!requested.Add(id))
                    throw new InvalidOperationException("Bulk edit target list contains duplicate semantic element id: " + id);
                if (!sourceIndex.TryGetValue(id, out var match))
                    throw new KeyNotFoundException("Unknown semantic element: " + id);
                resolved.Add(match);
            }
            return resolved.AsReadOnly();
        }

        private static IReadOnlyList<ProjectElement> OwnedDistinct(ProjectState project, IEnumerable<ProjectElement> elements)
        {
            EnsureKnownCountWithinBound(elements, "Bulk edit target collection");

            var projectElements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var projectElement in project.Elements)
            {
                if (projectElement == null)
                    throw new InvalidOperationException("Project contains a null semantic element entry.");
                var projectElementId = (projectElement.Id ?? string.Empty).Trim();
                if (projectElementId.Length == 0)
                    throw new InvalidOperationException("Project contains an element with a blank semantic id.");
                if (projectElements.ContainsKey(projectElementId))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + projectElementId);
                projectElements.Add(projectElementId, projectElement);
            }

            var unique = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var inputCount = 0;
            foreach (var element in elements)
            {
                if (inputCount >= MaxTargetInputCount)
                    throw new InvalidOperationException("Bulk edit target collection cannot exceed " + MaxTargetInputCount + " input entries.");
                inputCount++;
                if (element == null)
                    throw new InvalidOperationException("Bulk edit target collection contains a null semantic element entry.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("Bulk edit target contains an element with a blank semantic id.");
                if (!projectElements.TryGetValue(elementId, out var owned) || !ReferenceEquals(owned, element))
                    throw new InvalidOperationException("Element does not belong to the project instance: " + elementId);
                unique[elementId] = owned;
            }
            return new List<ProjectElement>(unique.Values).AsReadOnly();
        }

        private static void RequireCurrentElementOwnership(ProjectState project, IReadOnlyList<ProjectElement> elements, string label)
        {
            foreach (var element in elements)
            {
                var current = project.FindElement(element.Id);
                if (!ReferenceEquals(current, element))
                    throw new InvalidOperationException(label + " target no longer belongs to the project after enumeration: " + element.Id + ".");
            }
        }

        private static IReadOnlyDictionary<string, ProjectFamily> SnapshotFamilyOwnership(ProjectState project)
        {
            var result = new Dictionary<string, ProjectFamily>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null)
                    throw new InvalidOperationException("Project family collection contains a null family.");
                var id = family.Id ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id) || !string.Equals(id, id.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Project family collection contains a blank or non-canonical family id.");
                if (result.ContainsKey(id))
                    throw new InvalidOperationException("Project contains duplicate family id: " + id + ".");
                result.Add(id, family);
            }
            return result;
        }

        private static void RequireFamilyOwnershipUnchanged(
            ProjectState project,
            IReadOnlyDictionary<string, ProjectFamily> expected)
        {
            if (project.Families.Count != expected.Count)
                throw new InvalidOperationException("Project Family ownership changed while materializing bulk assignment targets. Retry against the current project state.");

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null ||
                    !seen.Add(family.Id) ||
                    !expected.TryGetValue(family.Id, out var original) ||
                    !ReferenceEquals(original, family))
                    throw new InvalidOperationException("Project Family ownership changed while materializing bulk assignment targets. Retry against the current project state.");
            }
        }

        private static void RequireCurrentFamilyAssignmentOwnership(ProjectState project, ProjectFamily family, IReadOnlyList<ProjectElement> elements)
        {
            var currentFamily = project.FindFamily(family.Id);
            if (!ReferenceEquals(currentFamily, family))
                throw new InvalidOperationException("Target Family no longer belongs to the project after bulk assignment target enumeration: " + family.Id + ".");

            foreach (var element in elements)
            {
                var current = project.FindElement(element.Id);
                if (!ReferenceEquals(current, element))
                    throw new InvalidOperationException("Element no longer belongs to the project after bulk Family assignment target enumeration: " + element.Id + ".");
            }
        }

        private static void ValidateUniqueFamilyIds(ProjectState project)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null)
                    throw new InvalidOperationException("Project family collection contains a null family.");
                var id = family.Id ?? string.Empty;
                if (string.IsNullOrWhiteSpace(id) || !string.Equals(id, id.Trim(), StringComparison.Ordinal))
                    throw new InvalidOperationException("Project family collection contains a blank or non-canonical family id.");
                if (!seen.Add(id))
                    throw new InvalidOperationException("Project contains duplicate family id: " + id + ".");
            }
        }

        private static void ValidatePendingPropertyMapsForMutation(IReadOnlyList<PendingPropertyUpdate> updates, string repairOperation)
        {
            foreach (var update in updates)
            {
                var element = update.Element;
                var canonicalKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in element.Properties)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                        throw new InvalidOperationException("Bulk edit target '" + element.Id + "' contains an empty property key. Repair the element before " + repairOperation + ".");
                    var normalizedKey = pair.Key.Trim();
                    if (!string.Equals(normalizedKey, pair.Key, StringComparison.Ordinal))
                        throw new InvalidOperationException("Bulk edit target '" + element.Id + "' contains a non-canonical property key: '" + pair.Key + "'. Repair the element before " + repairOperation + ".");
                    if (!canonicalKeys.Add(normalizedKey))
                        throw new InvalidOperationException("Bulk edit target '" + element.Id + "' contains duplicate canonical property key: " + normalizedKey + ".");
                }
            }
        }

        private static IReadOnlyList<string> MaterializeBounded(IEnumerable<string> values, string label)
        {
            EnsureKnownCountWithinBound(values, label);
            var result = new List<string>();
            var inputCount = 0;
            foreach (var value in values)
            {
                if (inputCount >= MaxTargetInputCount)
                    throw new InvalidOperationException(label + " cannot exceed " + MaxTargetInputCount + " input entries.");
                inputCount++;
                result.Add(value);
            }
            return result.AsReadOnly();
        }

        private static void EnsureKnownCountWithinBound<T>(IEnumerable<T> values, string label)
        {
            if (values is ICollection<T> collection && collection.Count > MaxTargetInputCount)
                throw new InvalidOperationException(label + " cannot exceed " + MaxTargetInputCount + " input entries.");
            if (values is IReadOnlyCollection<T> readOnlyCollection && readOnlyCollection.Count > MaxTargetInputCount)
                throw new InvalidOperationException(label + " cannot exceed " + MaxTargetInputCount + " input entries.");
        }

        private static bool HasNonZeroSignificand(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (character == 'e' || character == 'E') break;
                if (character >= '1' && character <= '9') return true;
            }
            return false;
        }

        private static void RequireTargetEnumerationFreshness(ProjectState project, long beforeVersion, string label)
        {
            if (project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException(label + " changed the project while targets were being enumerated. Retry the bulk edit against the current project state.");
        }
    }
}
