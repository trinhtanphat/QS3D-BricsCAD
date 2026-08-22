using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class BulkEditService
    {
        public IReadOnlyList<string> SetProperty(ProjectState project, IEnumerable<ProjectElement> elements, string propertyName, string value)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Property name is required.", nameof(propertyName));
            var changed = new List<string>();
            foreach (var element in elements)
            {
                if (element == null) continue;
                element.Properties.TryGetValue(propertyName, out var before);
                var next = value ?? string.Empty;
                if (string.Equals(before ?? string.Empty, next, StringComparison.Ordinal)) continue;
                element.Properties[propertyName] = next;
                element.MarkGeneratedGeometryStale("Bulk property changed: " + propertyName);
                element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
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
            var changed = new List<string>();
            foreach (var element in elements)
            {
<<<<<<< Updated upstream
                if (element == null || !element.Properties.TryGetValue(propertyName, out var text)) continue;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var current) || double.IsNaN(current) || double.IsInfinity(current))
                    throw new FormatException("Invalid numeric property " + propertyName + " on " + element.Id + ": " + text);
                var next = current * factor;
                if (double.IsNaN(next) || double.IsInfinity(next)) throw new OverflowException("Bulk property multiplication overflow for " + element.Id + "/" + propertyName);
                var formatted = next.ToString("R", CultureInfo.InvariantCulture);
                if (string.Equals(text, formatted, StringComparison.Ordinal)) continue;
                element.Properties[propertyName] = formatted;
                element.MarkGeneratedGeometryStale("Bulk numeric property changed: " + propertyName);
                element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
                changed.Add(element.Id);
=======
                var element = project.FindElement(id);
                if (element == null || element.Category != family.Category) continue;
                if (string.Equals(element.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase)) continue;

                var previousFamily = project.FindFamily(element.FamilyId);
                var inheritedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (previousFamily != null)
                {
                    foreach (var property in previousFamily.Properties)
                    {
                        if (element.Properties.TryGetValue(property.Key, out var current) && string.Equals(current, property.Value ?? string.Empty, StringComparison.Ordinal))
                            inheritedKeys.Add(property.Key);
                    }
                }

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
>>>>>>> Stashed changes
            }
            if (changed.Count > 0) project.Touch();
            return changed.AsReadOnly();
        }
    }
}
