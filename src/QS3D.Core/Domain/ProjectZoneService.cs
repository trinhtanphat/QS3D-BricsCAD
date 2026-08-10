using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Domain
{
    public static class ProjectZoneService
    {
        private const int MaxZones = 2000;
        private const int MaxNameLength = 120;

        public static ZoneDefinition Create(ProjectState project, string id, string name)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedId = Required(id, nameof(id), 64);
            var normalizedName = Required(name, nameof(name), MaxNameLength);
            if (project.Zones.Count >= MaxZones) throw new InvalidOperationException("Project supports at most " + MaxZones + " zones.");
            if (project.Zones.Any(x => string.Equals(x.Id, normalizedId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Zone id already exists: " + normalizedId);
            EnsureUniqueName(project, normalizedName, string.Empty);
            var zone = new ZoneDefinition(normalizedId, normalizedName);
            project.Zones.Add(zone);
            if (string.IsNullOrWhiteSpace(project.ActiveZoneId)) project.ActiveZoneId = zone.Id;
            project.Touch();
            return zone;
        }

        public static ZoneDefinition Update(ProjectState project, string id, string name)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var zone = FindRequired(project, id);
            var normalizedName = Required(name, nameof(name), MaxNameLength);
            EnsureUniqueName(project, normalizedName, zone.Id);
            if (string.Equals(zone.Name, normalizedName, StringComparison.Ordinal)) return zone;
            zone.Name = normalizedName;
            foreach (var element in project.Elements.Where(x => string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)))
                element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            project.Touch();
            return zone;
        }

        public static void SetActive(ProjectState project, string zoneId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var zone = FindRequired(project, zoneId);
            if (string.Equals(project.ActiveZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)) return;
            project.ActiveZoneId = zone.Id;
            project.Touch();
        }

        public static int Assign(ProjectState project, string zoneId, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var zone = FindRequired(project, zoneId);

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
                unique[element.Id] = owned;
            }

            var changed = 0;
            foreach (var element in unique.Values)
            {
                if (string.Equals(element.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase)) continue;
                element.ZoneId = zone.Id;
                element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
                changed++;
            }
            if (changed > 0) project.Touch();
            return changed;
        }

        public static bool Delete(ProjectState project, string zoneId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var zone = FindRequired(project, zoneId);
            if (string.Equals(project.ActiveZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot delete the active zone. Activate another zone first.");
            var references = project.Elements.Count(x => string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase));
            if (references > 0)
                throw new InvalidOperationException("Zone '" + zone.Name + "' is referenced by " + references + " semantic element(s). Reassign them before deletion.");
            var removed = project.Zones.Remove(zone);
            if (removed) project.Touch();
            return removed;
        }

        public static int ReferenceCount(ProjectState project, string zoneId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var zone = FindRequired(project, zoneId);
            return project.Elements.Count(x => string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase));
        }

        private static ZoneDefinition FindRequired(ProjectState project, string id)
        {
            var normalized = Required(id, nameof(id), 64);
            return project.FindZone(normalized) ?? throw new InvalidOperationException("Zone not found: " + normalized);
        }

        private static void EnsureUniqueName(ProjectState project, string name, string exceptId)
        {
            if (project.Zones.Any(x => !string.Equals(x.Id, exceptId, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Another zone already uses the name '" + name + "'.");
        }

        private static string Required(string value, string parameterName, int maxLength)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0 || text.Length > maxLength)
                throw new ArgumentException(parameterName + " must contain 1.." + maxLength + " characters.", parameterName);
            return text;
        }
    }
}
