using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class BulkEditService
    {
        private sealed class PendingPropertyUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string Value { get; set; } = string.Empty;
        }

        public IReadOnlyList<string> SetProperty(ProjectState project, IEnumerable<ProjectElement> elements, string propertyName, string value)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Property name is required.", nameof(propertyName));
            var changed = new List<string>();
            foreach (var element in OwnedDistinct(project, elements))
            {
                element.Properties.TryGetValue(propertyName, out var before);
                var next = value ?? string.Empty;
                if (string.Equals(before ?? string.Empty, next, StringComparison.Ordinal)) continue;
                element.Properties[propertyName] = next;
                element.MarkDirty(DirtyFlags(element, propertyName));
                changed.Add(element.Id);
            }
            if (changed.Count > 0) project.Touch();
            return changed.AsReadOnly();
        }

        public IReadOnlyList<string> MultiplyNumericProperty(ProjectState project, IEnumerable<ProjectElement> elements, string propertyName, double factor)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Property name is required.", nameof(propertyName));
            if (double.IsNaN(factor) || double.IsInfinity(factor)) throw new ArgumentOutOfRangeException(nameof(factor));

            var updates = new List<PendingPropertyUpdate>();
            foreach (var element in OwnedDistinct(project, elements))
            {
                if (!element.Properties.TryGetValue(propertyName, out var text)) continue;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var current) || double.IsNaN(current) || double.IsInfinity(current))
                    throw new FormatException("Invalid numeric property " + propertyName + " on " + element.Id + ": " + text);
                var next = current * factor;
                if (double.IsNaN(next) || double.IsInfinity(next)) throw new OverflowException("Bulk property multiplication overflow for " + element.Id + "/" + propertyName);
                var formatted = next.ToString("R", CultureInfo.InvariantCulture);
                if (string.Equals(text, formatted, StringComparison.Ordinal)) continue;
                updates.Add(new PendingPropertyUpdate { Element = element, Value = formatted });
            }

            if (updates.Count == 0) return Array.Empty<string>();
            var changed = new List<string>(updates.Count);
            foreach (var update in updates)
            {
                update.Element.Properties[propertyName] = update.Value;
                update.Element.MarkDirty(DirtyFlags(update.Element, propertyName));
                changed.Add(update.Element.Id);
            }
            project.Touch();
            return changed.AsReadOnly();
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
            var count = 0;
            foreach (var element in OwnedDistinctByIds(project, elementIds))
            {
                if (element.Category != family.Category) continue;
                if (string.Equals(element.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase)) continue;

                var previousFamily = project.FindFamily(element.FamilyId);
                var inheritedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (previousFamily != null)
                    foreach (var property in previousFamily.Properties)
                        if (element.Properties.TryGetValue(property.Key, out var current) && string.Equals(current, property.Value ?? string.Empty, StringComparison.Ordinal))
                            inheritedKeys.Add(property.Key);

                foreach (var key in inheritedKeys)
                    if (!family.Properties.ContainsKey(key)) element.Properties.Remove(key);
                foreach (var property in family.Properties)
                    if (inheritedKeys.Contains(property.Key) || !element.Properties.ContainsKey(property.Key))
                        element.Properties[property.Key] = property.Value ?? string.Empty;

                element.FamilyId = family.Id;
                var dirty = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
                if (ElementGeometryPolicy.RequiresGeneratedGeometry(element.Category)) dirty |= ElementDirtyFlags.Geometry;
                element.MarkDirty(dirty);
                count++;
            }
            if (count > 0) project.Touch();
            return count;
        }

        private static IReadOnlyList<ProjectElement> OwnedDistinctByIds(ProjectState project, IEnumerable<string> elementIds)
        {
            var resolved = new List<ProjectElement>();
            foreach (var id in elementIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                var normalized = id.Trim();
                ProjectElement? match = null;
                foreach (var candidate in project.Elements)
                {
                    if (candidate == null || !string.Equals(candidate.Id, normalized, StringComparison.OrdinalIgnoreCase)) continue;
                    match = candidate;
                    break;
                }
                if (match != null) resolved.Add(match);
            }
            return OwnedDistinct(project, resolved);
        }

        private static IReadOnlyList<ProjectElement> OwnedDistinct(ProjectState project, IEnumerable<ProjectElement> elements)
        {
            var projectElements = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var projectElement in project.Elements)
            {
                if (projectElement == null) continue;
                if (projectElements.ContainsKey(projectElement.Id))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + projectElement.Id);
                projectElements[projectElement.Id] = projectElement;
            }

            var unique = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (element == null) continue;
                if (!projectElements.TryGetValue(element.Id, out var owned) || !ReferenceEquals(owned, element))
                    throw new InvalidOperationException("Element does not belong to the project instance: " + element.Id);
                unique[owned.Id] = owned;
            }
            return new List<ProjectElement>(unique.Values).AsReadOnly();
        }

        private static ElementDirtyFlags DirtyFlags(ProjectElement element, string propertyName)
        {
            var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;
            if (ElementGeometryPolicy.AffectsGeneratedGeometry(element.Category, propertyName)) flags |= ElementDirtyFlags.Geometry;
            return flags;
        }
    }
}
