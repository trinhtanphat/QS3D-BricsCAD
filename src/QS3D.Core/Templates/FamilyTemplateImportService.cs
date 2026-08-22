using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Templates
{
    public sealed class FamilyTemplateImportResult
    {
        public int FamiliesAdded { get; set; }
        public int FamiliesUpdated { get; set; }
        public int PropertiesApplied { get; set; }
    }

    public static class FamilyTemplateImportService
    {
        public static FamilyTemplateImportResult Apply(ProjectState project, TemplateProfile profile)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            ValidateSourceFamilies(profile);
            ValidateTargetFamilies(project, profile);

            var rollback = ProjectStateSnapshot.Capture(project);
            try
            {
                var result = new FamilyTemplateImportResult();
                foreach (var source in profile.Families
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var matches = project.Families
                        .Where(x => x.Category == source.Category &&
                                    string.Equals(x.Name, source.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (matches.Count > 1)
                        throw new InvalidOperationException(
                            "Project contains multiple Families with the same Category + Name: " +
                            source.Category + " / " + source.Name + ".");

                    var target = matches.SingleOrDefault();
                    var added = false;
                    if (target == null)
                    {
                        target = ProjectFamilyService.Create(
                            project,
                            NextLocalId(project),
                            source.Name,
                            source.Category);
                        result.FamiliesAdded++;
                        added = true;
                    }

                    var changed = false;
                    foreach (var property in source.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        var next = property.Value ?? string.Empty;
                        if (target.Properties.TryGetValue(property.Key, out var current) &&
                            string.Equals(current ?? string.Empty, next, StringComparison.Ordinal))
                            continue;

                        ProjectFamilyService.SetProperty(project, target.Id, property.Key, next);
                        result.PropertiesApplied++;
                        changed = true;
                    }

                    if (!added && changed) result.FamiliesUpdated++;
                }

                if (result.FamiliesAdded > 0 || result.FamiliesUpdated > 0 || result.PropertiesApplied > 0)
                {
                    AuditTrail.ForProject(project).Record(
                        "family.template.import",
                        string.Empty,
                        profile.Id + " • families +" + result.FamiliesAdded.ToString(CultureInfo.InvariantCulture) +
                        "/~" + result.FamiliesUpdated.ToString(CultureInfo.InvariantCulture) +
                        " • properties " + result.PropertiesApplied.ToString(CultureInfo.InvariantCulture));
                }

                return result;
            }
            catch (Exception applyError)
            {
                try { rollback.Restore(project); }
                catch (Exception rollbackError)
                {
                    throw new AggregateException(
                        "Family-only template import failed and project rollback also failed.",
                        applyError,
                        rollbackError);
                }
                throw;
            }
        }

        private static void ValidateSourceFamilies(TemplateProfile profile)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var source in profile.Families)
            {
                if (source == null)
                    throw new InvalidOperationException("Family template contains a null Family entry.");
                var key = FamilyKey(source.Category, source.Name);
                if (!keys.Add(key))
                    throw new InvalidOperationException(
                        "Family template contains duplicate Category + Name: " + source.Category + " / " + source.Name + ".");
            }
        }

        private static void ValidateTargetFamilies(ProjectState project, TemplateProfile profile)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sourceKeys = new HashSet<string>(
                profile.Families.Select(x => FamilyKey(x.Category, x.Name)),
                StringComparer.OrdinalIgnoreCase);
            var relevantKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var family in project.Families)
            {
                if (family == null)
                    throw new InvalidOperationException("Project family collection contains a null Family entry.");
                if (!ids.Add(family.Id))
                    throw new InvalidOperationException("Project contains duplicate Family id: " + family.Id + ".");

                var key = FamilyKey(family.Category, family.Name);
                if (!sourceKeys.Contains(key))
                    continue;
                if (!relevantKeys.Add(key))
                    throw new InvalidOperationException(
                        "Project contains multiple Families with the same Category + Name required by this template: " +
                        family.Category + " / " + family.Name + ".");
            }
        }

        private static string FamilyKey(ElementCategory category, string name) =>
            category + "\u001f" + (name ?? string.Empty).Trim();

        private static string NextLocalId(ProjectState project)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                var candidate = "family-" + Guid.NewGuid().ToString("N");
                if (project.FindFamily(candidate) == null) return candidate;
            }
            throw new InvalidOperationException("Cannot allocate a fresh project-local Family id for template import.");
        }
    }
}
