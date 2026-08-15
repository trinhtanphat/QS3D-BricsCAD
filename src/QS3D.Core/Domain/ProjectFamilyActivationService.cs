using System;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    public static class ProjectFamilyActivationService
    {
        public static ProjectFamily? GetActive(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidateUniqueFamilyIds(project);
            if (!project.Metadata.TryGetValue("ActiveFamilyId", out var id) || string.IsNullOrWhiteSpace(id)) return null;
            return project.FindFamily(id.Trim());
        }

        public static void SetActive(ProjectState project, string familyId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidateUniqueFamilyIds(project);
            var normalized = (familyId ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Family id is required.", nameof(familyId));
            var family = project.FindFamily(normalized) ?? throw new InvalidOperationException("Family not found: " + normalized);
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var current) &&
                !string.IsNullOrWhiteSpace(current) &&
                ReferenceEquals(project.FindFamily(current.Trim()), family)) return;
            project.Metadata["ActiveFamilyId"] = family.Id;
        }

        public static void ClearIfMissing(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            ValidateUniqueFamilyIds(project);
            if (!project.Metadata.TryGetValue("ActiveFamilyId", out var current)) return;
            if (!string.IsNullOrWhiteSpace(current) && project.FindFamily(current.Trim()) != null) return;
            project.Metadata.Remove("ActiveFamilyId");
        }

        private static void ValidateUniqueFamilyIds(ProjectState project)
        {
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null)
                    throw new InvalidOperationException("Project family collection contains a null family.");
                if (!seenIds.Add(family.Id))
                    throw new InvalidOperationException("Project contains duplicate family id: " + family.Id + ".");
            }
        }
    }
}
