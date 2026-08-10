using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public sealed class BulkEditService
    {
        public int SetProperty(ProjectState project, IEnumerable<string> elementIds, string propertyName, string value)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            var count = 0;
            foreach (var id in elementIds)
            {
                var element = project.FindElement(id);
                if (element == null) continue;
                element.SetProperty(propertyName, value);
                count++;
            }
            if (count > 0) project.Touch();
            return count;
        }

        public int AssignFamily(ProjectState project, IEnumerable<string> elementIds, string familyId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elementIds == null) throw new ArgumentNullException(nameof(elementIds));
            var family = project.FindFamily(familyId) ?? throw new KeyNotFoundException("Unknown family: " + familyId);
            var count = 0;
            foreach (var id in elementIds)
            {
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
            }
            if (count > 0) project.Touch();
            return count;
        }
    }
}
