using System;

namespace QS3D.Core.Domain
{
    public static class ProjectFamilyActivationService
    {
        public static ProjectFamily? GetActive(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue("ActiveFamilyId", out var id) || string.IsNullOrWhiteSpace(id)) return null;
            return project.FindFamily(id.Trim());
        }

        public static void SetActive(ProjectState project, string familyId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = (familyId ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Family id is required.", nameof(familyId));
            var family = project.FindFamily(normalized) ?? throw new InvalidOperationException("Family not found: " + normalized);
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var current) && string.Equals(current, family.Id, StringComparison.OrdinalIgnoreCase)) return;
            project.Touch();
            project.Metadata["ActiveFamilyId"] = family.Id;
        }

        public static void ClearIfMissing(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!project.Metadata.TryGetValue("ActiveFamilyId", out var current)) return;
            if (!string.IsNullOrWhiteSpace(current) && project.FindFamily(current.Trim()) != null) return;
            project.Touch();
            project.Metadata.Remove("ActiveFamilyId");
        }
    }
}
