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
                element.FamilyId = family.Id;
                element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
                count++;
            }
            if (count > 0) project.Touch();
            return count;
        }
    }
}
