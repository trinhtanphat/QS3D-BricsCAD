using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class InterchangeFamilySemanticApplier
    {
        public static ProjectFamily Add(
            ProjectState project,
            string id,
            string name,
            ElementCategory category,
            IEnumerable<KeyValuePair<string, string>> properties)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var family = ProjectFamilyService.Create(project, id, name, category);
            foreach (var property in Ordered(properties))
                ProjectFamilyService.SetProperty(project, family.Id, property.Key, property.Value ?? string.Empty);
            return family;
        }

        public static ProjectFamily Replace(
            ProjectState project,
            string id,
            string name,
            ElementCategory category,
            IEnumerable<KeyValuePair<string, string>> properties)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var family = project.FindFamily(id) ?? throw new InvalidOperationException("Replacement Family disappeared during mutation: " + id + ".");
            if (family.Category != category)
                throw new InvalidOperationException("Replacement Family category changed after planning for " + id + ".");

            ProjectFamilyService.Rename(project, family.Id, name);

            var incoming = Ordered(properties).ToList();
            var incomingKeys = new HashSet<string>(incoming.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var removedKeys = family.Properties.Keys
                .Where(x => !incomingKeys.Contains(x))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Remove first while the old family value is still available. The domain service removes
            // only instance values that were inherited from that old value and preserves true overrides.
            foreach (var key in removedKeys)
                ProjectFamilyService.RemoveProperty(project, family.Id, key);

            // SetProperty propagates changed/default values only to members that still inherit the old
            // Family value; element-level overrides stay untouched. All caller-side generated-output
            // invalidation is prepared before this method runs.
            foreach (var property in incoming)
                ProjectFamilyService.SetProperty(project, family.Id, property.Key, property.Value ?? string.Empty);

            return family;
        }

        private static IEnumerable<KeyValuePair<string, string>> Ordered(IEnumerable<KeyValuePair<string, string>> properties) =>
            (properties ?? Enumerable.Empty<KeyValuePair<string, string>>())
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);
    }
}
