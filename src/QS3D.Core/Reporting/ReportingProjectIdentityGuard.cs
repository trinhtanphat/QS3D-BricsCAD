using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    internal static class ReportingProjectIdentityGuard
    {
        internal static void RequireUniqueElementIds(ProjectState project, string reportName)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(reportName)) throw new ArgumentException("Report name is required.", nameof(reportName));

            RequireUniqueIds(project.Elements, x => x.Id, "element", reportName);
            RequireUniqueIds(project.Floors, x => x.Id, "floor", reportName);
            RequireUniqueIds(project.Zones, x => x.Id, "zone", reportName);
            RequireUniqueIds(project.Families, x => x.Id, "family", reportName);
            RequireCanonicalElementReferences(project.Elements, reportName);
            RequireExistingElementReferences(project, reportName);
        }

        internal static string NormalizeReferenceId(string? value) => (value ?? string.Empty).Trim();

        private static void RequireUniqueIds<T>(IEnumerable<T> items, Func<T, string> idSelector, string identityName, string reportName) where T : class
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var item in items)
            {
                if (item == null)
                    throw new InvalidOperationException(reportName + " cannot be built because project " + identityName + " index " + index + " is null.");

                var rawId = idSelector(item);
                if (string.IsNullOrWhiteSpace(rawId))
                    throw new InvalidOperationException(reportName + " cannot be built with a blank project " + identityName + " id.");

                var id = rawId.Trim();
                if (!string.Equals(rawId, id, StringComparison.Ordinal))
                    throw new InvalidOperationException(reportName + " cannot be built because project " + identityName + " id '" + rawId + "' is not canonical.");
                if (!seenIds.Add(id))
                    throw new InvalidOperationException(reportName + " cannot be built because project " + identityName + " id '" + id + "' is duplicated.");
                index++;
            }
        }

        private static void RequireCanonicalElementReferences(IEnumerable<ProjectElement> elements, string reportName)
        {
            var index = 0;
            foreach (var element in elements)
            {
                if (element == null)
                    throw new InvalidOperationException(reportName + " cannot be built because project element index " + index + " is null.");

                RequireCanonicalReference(element.FamilyId, "family", element.Id, reportName);
                RequireCanonicalReference(element.FloorId, "floor", element.Id, reportName);
                RequireCanonicalReference(element.ZoneId, "zone", element.Id, reportName);
                index++;
            }
        }

        private static void RequireExistingElementReferences(ProjectState project, string reportName)
        {
            var families = FamilyIndex(project.Families);
            var floorIds = IdentitySet(project.Floors, x => x.Id);
            var zoneIds = IdentitySet(project.Zones, x => x.Id);

            foreach (var element in project.Elements)
            {
                RequireExistingFamilyReference(element, families, reportName);
                RequireExistingReference(element.FloorId, floorIds, "floor", element.Id, reportName);
                RequireExistingReference(element.ZoneId, zoneIds, "zone", element.Id, reportName);
            }
        }

        private static Dictionary<string, ProjectFamily> FamilyIndex(IEnumerable<ProjectFamily> families)
        {
            var result = new Dictionary<string, ProjectFamily>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in families)
                result.Add(family.Id, family);
            return result;
        }

        private static HashSet<string> IdentitySet<T>(IEnumerable<T> items, Func<T, string> idSelector) where T : class
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
                result.Add(idSelector(item));
            return result;
        }

        private static void RequireExistingFamilyReference(
            ProjectElement element,
            IReadOnlyDictionary<string, ProjectFamily> families,
            string reportName)
        {
            var familyId = element.FamilyId;
            if (string.IsNullOrWhiteSpace(familyId)) return;
            if (!families.TryGetValue(familyId, out var family))
                throw new InvalidOperationException(reportName + " cannot be built because element '" + element.Id + "' references missing family id '" + familyId + "'.");
            if (family.Category != element.Category)
                throw new InvalidOperationException(reportName + " cannot be built because element '" + element.Id + "' category " + element.Category + " does not match family '" + family.Id + "' category " + family.Category + ".");
        }

        private static void RequireExistingReference(
            string? rawId,
            ISet<string> existingIds,
            string identityName,
            string elementId,
            string reportName)
        {
            if (rawId == null || string.IsNullOrWhiteSpace(rawId)) return;
            if (!existingIds.Contains(rawId))
                throw new InvalidOperationException(reportName + " cannot be built because element '" + elementId + "' references missing " + identityName + " id '" + rawId + "'.");
        }

        private static void RequireCanonicalReference(string? rawId, string identityName, string elementId, string reportName)
        {
            if (rawId == null || string.IsNullOrWhiteSpace(rawId)) return;
            var id = rawId.Trim();
            if (!string.Equals(rawId, id, StringComparison.Ordinal))
                throw new InvalidOperationException(reportName + " cannot be built because element '" + elementId + "' has a noncanonical " + identityName + " reference id '" + rawId + "'.");
        }
    }
}
