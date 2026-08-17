using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace QS3D.Core.Domain
{
    public static class ProjectZoneService
    {
        private const int MaxZones = 2000;
        private const int MaxNameLength = 120;
        private const int MaxAssignmentTargetEntries = 10000;

        public static ZoneDefinition Create(ProjectState project, string id, string name)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalizedId = Required(id, nameof(id), 64);
            var normalizedName = Required(name, nameof(name), MaxNameLength);
            if (project.Zones.Any(x => x == null))
                throw new InvalidOperationException("Project zone collection contains a null zone.");
            ValidateUniqueZoneIds(project);
            if (project.Zones.Count >= MaxZones) throw new InvalidOperationException("Project supports at most " + MaxZones + " zones.");
            if (project.Zones.Any(x => string.Equals(x.Id, normalizedId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Zone id already exists: " + normalizedId);
            EnsureUniqueName(project, normalizedName, string.Empty);
            var zone = new ZoneDefinition(normalizedId, normalizedName);
            var activate = string.IsNullOrWhiteSpace(project.ActiveZoneId);
            if (activate) project.ActiveZoneId = zone.Id;
            else project.Touch();
            project.Zones.Add(zone);
            return zone;
        }

        public static ZoneDefinition Update(ProjectState project, string id, string name)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var zone = FindRequired(project, id);
            var normalizedName = Required(name, nameof(name), MaxNameLength);
            EnsureUniqueName(project, normalizedName, zone.Id);
            if (string.Equals(zone.Name, normalizedName, StringComparison.Ordinal)) return zone;

            var referencedElements = ResolveProjectElements(project)
                .Where(x => ReferencesZone(x, zone.Id))
                .ToList();

            project.Touch();
            zone.Name = normalizedName;
            foreach (var element in referencedElements)
                element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            return zone;
        }

        public static void SetActive(ProjectState project, string zoneId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var zone = FindRequired(project, zoneId);
            if (string.Equals(project.ActiveZoneId, zone.Id, StringComparison.Ordinal)) return;
            project.ActiveZoneId = zone.Id;
        }

        public static int Assign(ProjectState project, string zoneId, IEnumerable<ProjectElement> elements)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var zone = FindRequired(project, zoneId);

            var projectElements = ResolveProjectElements(project)
                .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            var targetEnumerationVersion = project.ChangeVersion;
            RequireAssignmentTargetCountWithinLimit(elements);
            if (project.ChangeVersion != targetEnumerationVersion)
                throw new InvalidOperationException("Project changed while Zone assignment targets were being counted. Retry assignment against the current project state.");

            var unique = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            var observedEntries = 0;
            foreach (var element in elements)
            {
                observedEntries++;
                if (observedEntries > MaxAssignmentTargetEntries)
                    throw AssignmentTargetLimitExceeded();
                if (element == null)
                    throw new InvalidOperationException("Zone assignment target collection contains a null element.");
                if (!projectElements.TryGetValue(element.Id, out var owned) || !ReferenceEquals(owned, element))
                    throw new InvalidOperationException("Element does not belong to the project instance: " + element.Id);
                unique[element.Id] = owned;
            }
            if (project.ChangeVersion != targetEnumerationVersion)
                throw new InvalidOperationException("Project changed while Zone assignment targets were being enumerated. Retry assignment against the current project state.");
            RequireCurrentAssignmentOwnership(project, zone, unique.Values);

            var changed = unique.Values
                .Where(x => !string.Equals((x.ZoneId ?? string.Empty).Trim(), zone.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (changed.Count == 0) return 0;

            project.Touch();
            foreach (var element in changed)
            {
                element.ZoneId = zone.Id;
                element.MarkDirty(ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            }
            return changed.Count;
        }

        public static bool Delete(ProjectState project, string zoneId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var zone = FindRequired(project, zoneId);
            if (string.Equals((project.ActiveZoneId ?? string.Empty).Trim(), zone.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cannot delete the active zone. Activate another zone first.");
            var references = ResolveProjectElements(project).Count(x => ReferencesZone(x, zone.Id));
            if (references > 0)
                throw new InvalidOperationException("Zone '" + zone.Name + "' is referenced by " + references + " semantic element(s). Reassign them before deletion.");
            project.Touch();
            return project.Zones.Remove(zone);
        }

        public static int ReferenceCount(ProjectState project, string zoneId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var zone = FindRequired(project, zoneId);
            return ResolveProjectElements(project).Count(x => ReferencesZone(x, zone.Id));
        }

        private static bool ReferencesZone(ProjectElement element, string zoneId)
        {
            return string.Equals((element.ZoneId ?? string.Empty).Trim(), zoneId, StringComparison.OrdinalIgnoreCase);
        }

        private static void RequireAssignmentTargetCountWithinLimit(IEnumerable<ProjectElement> elements)
        {
            if (elements is ICollection<ProjectElement> collection && collection.Count > MaxAssignmentTargetEntries)
                throw AssignmentTargetLimitExceeded();
            if (elements is IReadOnlyCollection<ProjectElement> readOnlyCollection && readOnlyCollection.Count > MaxAssignmentTargetEntries)
                throw AssignmentTargetLimitExceeded();
            if (elements is System.Collections.ICollection nonGenericCollection && nonGenericCollection.Count > MaxAssignmentTargetEntries)
                throw AssignmentTargetLimitExceeded();
        }

        private static InvalidOperationException AssignmentTargetLimitExceeded()
        {
            return new InvalidOperationException(
                "Zone assignment supports at most " + MaxAssignmentTargetEntries + " target entries per operation.");
        }

        private static void RequireCurrentAssignmentOwnership(ProjectState project, ZoneDefinition zone, IEnumerable<ProjectElement> elements)
        {
            ValidateUniqueZoneIds(project);
            var currentElements = ResolveProjectElements(project)
                .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            var currentZone = project.FindZone(zone.Id);
            if (!ReferenceEquals(currentZone, zone))
                throw new InvalidOperationException("Target Zone no longer belongs to the project after assignment target enumeration: " + zone.Id + ".");

            foreach (var element in elements)
            {
                if (!currentElements.TryGetValue(element.Id, out var current) || !ReferenceEquals(current, element))
                    throw new InvalidOperationException("Element no longer belongs to the project after Zone assignment target enumeration: " + element.Id + ".");
            }
        }

        private static ZoneDefinition FindRequired(ProjectState project, string id)
        {
            var normalized = Required(id, nameof(id), 64);
            ValidateUniqueZoneIds(project);
            return project.FindZone(normalized) ?? throw new InvalidOperationException("Zone not found: " + normalized);
        }

        private static void ValidateUniqueZoneIds(ProjectState project)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var zone in project.Zones)
            {
                if (zone == null)
                    throw new InvalidOperationException("Project zone collection contains a null zone.");
                if (!seenIds.Add(zone.Id))
                    throw new InvalidOperationException("Project contains duplicate zone id: " + zone.Id + ".");
            }
        }

        private static IReadOnlyList<ProjectElement> ResolveProjectElements(ProjectState project)
        {
            var resolved = new List<ProjectElement>(project.Elements.Count);
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project element collection contains a null entry.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("Project element collection contains an element with a blank semantic id.");
                if (!seenIds.Add(elementId))
                    throw new InvalidOperationException("Project contains duplicate semantic element id: " + elementId);
                resolved.Add(element);
            }
            return resolved;
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
            if (text.Any(char.IsControl))
                throw new ArgumentException(parameterName + " cannot contain control characters.", parameterName);
            try
            {
                XmlConvert.VerifyXmlChars(text);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(parameterName + " contains characters that are invalid in XML.", parameterName, ex);
            }
            return text;
        }
    }
}
