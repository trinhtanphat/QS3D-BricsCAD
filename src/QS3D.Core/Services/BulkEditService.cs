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
            var updates = new List<PendingPropertyUpdate>();
            var next = value ?? string.Empty;
            foreach (var element in OwnedDistinct(project, elements))
            {
                element.Properties.TryGetValue(key, out var before);
                if (string.Equals(before ?? string.Empty, next, StringComparison.Ordinal)) continue;
                updates.Add(new PendingPropertyUpdate { Element = element, Value = next });
            }

            if (updates.Count == 0) return Array.Empty<string>();
            return ProjectSemanticMutationExecutor.Execute(project, "bulk.set-property", () =>
            {
                var changed = new List<string>(updates.Count);
                foreach (var update in updates)
                {
                    update.Element.Properties[key] = update.Value;
                    update.Element.MarkDirty(DirtyFlags(update.Element, key));
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

            var updates = new List<PendingPropertyUpdate>();
            foreach (var element in OwnedDistinct(project, elements))
            {
                if (!element.Properties.TryGetValue(key, out var text)) continue;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var current) || double.IsNaN(current) || double.IsInfinity(current))
                    throw new FormatException("Invalid numeric property " + key + " on " + element.Id + ": " + text);
                var next = current * factor;
                if (double.IsNaN(next) || double.IsInfinity(next)) throw new OverflowException("Bulk property multiplication overflow for " + element.Id + "/" + key);
                var formatted = next.ToString("R", CultureInfo.InvariantCulture);
                if (string.Equals(text, formatted, StringComparison.Ordinal)) continue;
                updates.Add(new PendingPropertyUpdate { Element = element, Value = formatted });
            }

            if (updates.Count == 0) return Array.Empty<string>();
            return ProjectSemanticMutationExecutor.Execute(project, "bulk.multiply-numeric-property", () =>
            {
                var changed = new List<string>(updates.Count);
                foreach (var update in updates)
                {
                    update.Element.Properties[key] = update.Value;
                    update.Element.MarkDirty(DirtyFlags(update.Element, key));
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
            return SetProperty(project, OwnedDistinctByIds(project, elementIds), propertyName, value).Count;
        }

        public int AssignFamily(ProjectState project, IEnumerable<string> elementIds, string familyId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            var family = project.FindFamily(familyId) ?? throw new KeyNotFoundException("Unknown family: " + familyId);
            var targets = OwnedDistinctByIds(project, elementIds);
            foreach (var element in targets)
                if (element.Category != family.Category)
                    throw new InvalidOperationException("Cannot assign family " + family.Id + " (" + family.Category + ") to element " + element.Id + " (" + element.Category + "). Bulk family assignment is all-or-nothing.");

            var pending = new List<PendingFamilyAssignment>();
            foreach (var element in targets)
            {
                var previousFamilyId = (element.FamilyId ?? string.Empty).Trim();
                if (string.Equals(previousFamilyId, family.Id, StringComparison.OrdinalIgnoreCase)) continue;

                ProjectFamily? previousFamily = null;
                if (previousFamilyId.Length > 0)
                {
                    previousFamily = project.FindFamily(previousFamilyId) ??
                        throw new InvalidOperationException("Element " + element.Id + " references missing family id: " + previousFamilyId + ". Repair the relation before bulk reassignment.");
                }
                var inheritedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (previousFamily != null)
                    foreach (var property in previousFamily.Properties)
                        if (element.Properties.TryGetValue(property.Key, out var current) && string.Equals(current, property.Value ?? string.Empty, StringComparison.Ordinal))
                            inheritedKeys.Add(property.Key);
                pending.Add(new PendingFamilyAssignment { Element = element, InheritedKeys = inheritedKeys });
            }

            if (pending.Count == 0) return 0;
            return ProjectSemanticMutationExecutor.Execute(project, "bulk.assign-family", () =>
            {
                foreach (var item in pending)
                {
                    var element = item.Element;
                    foreach (var inheritedKey in item.InheritedKeys)
                        if (!family.Properties.ContainsKey(inheritedKey)) element.Properties.Remove(inheritedKey);
                    foreach (var property in family.Properties)
                        if (item.InheritedKeys.Contains(property.Key) || !element.Properties.ContainsKey(property.Key))
                            element.Properties[property.Key] = property.Value ?? string.Empty;

                    element.FamilyId = family.Id;
                    var dirty = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
                    if (ElementGeometryPolicy.RequiresGeneratedGeometry(element.Category)) dirty |= ElementDirtyFlags.Geometry;
                    element.MarkDirty(dirty);
                }
                project.Touch();
                return pending.Count;
            });
        }

        private static IReadOnlyList<ProjectElement> OwnedDistinctByIds(ProjectState project, IEnumerable<string> elementIds)
        {
            var rawIds = MaterializeBounded(elementIds, "Bulk edit target list");
            var resolved = new List<ProjectElement>();
            var requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in rawIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    throw new ArgumentException("Bulk edit target id is required.", nameof(elementIds));
                var normalized = id.Trim();
                if (!requested.Add(normalized))
                    throw new InvalidOperationException("Bulk edit target list contains duplicate semantic element id: " + normalized);
                var match = project.FindElement(normalized) ?? throw new KeyNotFoundException("Unknown semantic element: " + normalized);
                resolved.Add(match);
            }
            return OwnedDistinct(project, resolved);
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
                    throw new InvalidOperationException("Project contains a semantic element with a blank id.");
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

        private static ElementDirtyFlags DirtyFlags(ProjectElement element, string propertyName)
        {
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (ElementGeometryPolicy.AffectsGeneratedGeometry(element.Category, propertyName)) flags |= ElementDirtyFlags.Geometry;
            return flags;
        }
    }
}
