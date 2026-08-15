using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.Templates
{
    public sealed class StandardFamilyTemplateApplyResult
    {
        public int FamiliesAdded { get; set; }
        public int FamiliesUpdated { get; set; }
        public int PropertiesApplied { get; set; }
    }

    public static class StandardFamilyTemplateCatalog
    {
        public const string VietnamStandard01Id = "QS3D_STD_VN_01";
        public const string VietnamStandard01Name = "QS3D Standard Việt Nam 01";

        public static TemplateProfile CreateVietnamStandard01()
        {
            var profile = new TemplateProfile(VietnamStandard01Id, VietnamStandard01Name);

            Add(profile, "qs3d-vn01-beam-200x400", "Dầm HCN 200x400", ElementCategory.Beam,
                P("WidthM", "0.200"), P("HeightM", "0.400"), P("Material", "Bê tông"),
                P("BQCode", "BEAM-RC-200X400"), P("Description", "Dầm bê tông cốt thép 200x400"));
            Add(profile, "qs3d-vn01-beam-200x500", "Dầm HCN 200x500", ElementCategory.Beam,
                P("WidthM", "0.200"), P("HeightM", "0.500"), P("Material", "Bê tông"),
                P("BQCode", "BEAM-RC-200X500"), P("Description", "Dầm bê tông cốt thép 200x500"));
            Add(profile, "qs3d-vn01-beam-300x500", "Dầm HCN 300x500", ElementCategory.Beam,
                P("WidthM", "0.300"), P("HeightM", "0.500"), P("Material", "Bê tông"),
                P("BQCode", "BEAM-RC-300X500"), P("Description", "Dầm bê tông cốt thép 300x500"));

            Add(profile, "qs3d-vn01-slab-100", "Sàn BTCT 100", ElementCategory.Slab,
                P("ThicknessM", "0.100"), P("Material", "Bê tông"), P("BQCode", "SLAB-RC-100"),
                P("Description", "Sàn bê tông cốt thép dày 100"));
            Add(profile, "qs3d-vn01-slab-120", "Sàn BTCT 120", ElementCategory.Slab,
                P("ThicknessM", "0.120"), P("Material", "Bê tông"), P("BQCode", "SLAB-RC-120"),
                P("Description", "Sàn bê tông cốt thép dày 120"));
            Add(profile, "qs3d-vn01-slab-150", "Sàn BTCT 150", ElementCategory.Slab,
                P("ThicknessM", "0.150"), P("Material", "Bê tông"), P("BQCode", "SLAB-RC-150"),
                P("Description", "Sàn bê tông cốt thép dày 150"));

            Add(profile, "qs3d-vn01-column-200x200", "Cột 200x200", ElementCategory.Column,
                P("WidthM", "0.200"), P("DepthM", "0.200"), P("Material", "Bê tông"),
                P("BQCode", "COLUMN-RC-200X200"), P("Description", "Cột bê tông cốt thép 200x200"));
            Add(profile, "qs3d-vn01-column-200x300", "Cột 200x300", ElementCategory.Column,
                P("WidthM", "0.200"), P("DepthM", "0.300"), P("Material", "Bê tông"),
                P("BQCode", "COLUMN-RC-200X300"), P("Description", "Cột bê tông cốt thép 200x300"));
            Add(profile, "qs3d-vn01-column-300x300", "Cột 300x300", ElementCategory.Column,
                P("WidthM", "0.300"), P("DepthM", "0.300"), P("Material", "Bê tông"),
                P("BQCode", "COLUMN-RC-300X300"), P("Description", "Cột bê tông cốt thép 300x300"));

            Add(profile, "qs3d-vn01-wall-brick-100", "Tường Gạch 100", ElementCategory.ArchitecturalWall,
                P("ThicknessM", "0.100"), P("Material", "Gạch"), P("BQCode", "WALL-BRICK-100"),
                P("Description", "Tường gạch dày 100"), P("FireRating", "0"), P("IsLoadBearing", "No"));
            Add(profile, "qs3d-vn01-wall-brick-200", "Tường Gạch 200", ElementCategory.ArchitecturalWall,
                P("ThicknessM", "0.200"), P("Material", "Gạch"), P("BQCode", "WALL-BRICK-200"),
                P("Description", "Tường gạch dày 200"), P("FireRating", "0"), P("IsLoadBearing", "Yes"));

            Add(profile, "qs3d-vn01-foundation-h300", "Móng H300", ElementCategory.Foundation,
                P("ThicknessM", "0.300"), P("Material", "Bê tông"), P("BQCode", "FOUNDATION-RC-H300"),
                P("Description", "Móng bê tông H300"));
            Add(profile, "qs3d-vn01-foundation-h500", "Móng H500", ElementCategory.Foundation,
                P("ThicknessM", "0.500"), P("Material", "Bê tông"), P("BQCode", "FOUNDATION-RC-H500"),
                P("Description", "Móng bê tông H500"));

            Add(profile, "qs3d-vn01-finish-floor", "Sàn Hoàn Thiện", ElementCategory.FloorFinish,
                P("ThicknessM", "0.050"), P("Material", "Hoàn thiện sàn"), P("BQCode", "FINISH-FLOOR"),
                P("Description", "Lớp hoàn thiện sàn"));
            Add(profile, "qs3d-vn01-finish-wall", "HT-TƯỜNG", ElementCategory.WallFinish,
                P("ThicknessM", "0.015"), P("Material", "Hoàn thiện tường"), P("BQCode", "FINISH-WALL"),
                P("Description", "Lớp hoàn thiện tường"));
            Add(profile, "qs3d-vn01-skirting", "Chân Tường", ElementCategory.Skirting,
                P("HeightM", "0.100"), P("ThicknessM", "0.015"), P("Material", "Chân tường"),
                P("BQCode", "FINISH-SKIRTING"), P("Description", "Chân tường hoàn thiện"));
            Add(profile, "qs3d-vn01-waterproofing", "Chống Thấm", ElementCategory.Waterproofing,
                P("ThicknessM", "0.002"), P("Material", "Chống thấm"), P("BQCode", "FINISH-WATERPROOFING"),
                P("Description", "Lớp chống thấm"));
            Add(profile, "qs3d-vn01-ceiling", "Trần Hoàn Thiện", ElementCategory.CeilingFinish,
                P("ThicknessM", "0.010"), P("Material", "Hoàn thiện trần"), P("BQCode", "FINISH-CEILING"),
                P("Description", "Lớp hoàn thiện trần"));

            return profile;
        }

        public static StandardFamilyTemplateApplyResult ApplyVietnamStandard01(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var profile = CreateVietnamStandard01();
            var rollback = ProjectStateSnapshot.Capture(project);

            try
            {
                var result = new StandardFamilyTemplateApplyResult();
                foreach (var source in profile.Families)
                {
                    var matches = project.Families
                        .Where(x => x.Category == source.Category && string.Equals(x.Name, source.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (matches.Count > 1)
                        throw new InvalidOperationException("Project contains multiple Families with the same Category + Name: " + source.Category + " / " + source.Name + ".");

                    var target = matches.SingleOrDefault();
                    var added = false;
                    if (target == null)
                    {
                        target = ProjectFamilyService.Create(project, NextAvailableId(project, source.Id), source.Name, source.Category);
                        result.FamiliesAdded++;
                        added = true;
                    }

                    var changed = false;
                    foreach (var property in source.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        if (target.Properties.TryGetValue(property.Key, out var current) && string.Equals(current ?? string.Empty, property.Value ?? string.Empty, StringComparison.Ordinal))
                            continue;
                        ProjectFamilyService.SetProperty(project, target.Id, property.Key, property.Value ?? string.Empty);
                        result.PropertiesApplied++;
                        changed = true;
                    }

                    if (!added && changed) result.FamiliesUpdated++;
                }

                if (result.FamiliesAdded > 0 || result.FamiliesUpdated > 0 || result.PropertiesApplied > 0)
                {
                    AuditTrail.ForProject(project).Record(
                        "template.apply",
                        string.Empty,
                        VietnamStandard01Id + " • families +" + result.FamiliesAdded.ToString(CultureInfo.InvariantCulture) +
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
                    throw new AggregateException("Standard Family template apply failed and rollback also failed.", applyError, rollbackError);
                }
                throw;
            }
        }

        private static void Add(TemplateProfile profile, string id, string name, ElementCategory category, params KeyValuePair<string, string>[] properties)
        {
            var family = new ProjectFamily(id, name, category);
            foreach (var property in properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                family.Properties[property.Key] = property.Value;
            profile.Families.Add(family);
        }

        private static KeyValuePair<string, string> P(string key, string value) => new KeyValuePair<string, string>(key, value);

        private static string NextAvailableId(ProjectState project, string preferred)
        {
            if (project.FindFamily(preferred) == null) return preferred;
            for (var suffix = 2; suffix <= 9999; suffix++)
            {
                var candidate = preferred + "-" + suffix.ToString(CultureInfo.InvariantCulture);
                if (project.FindFamily(candidate) == null) return candidate;
            }
            throw new InvalidOperationException("Cannot allocate a unique Family id for standard template Family: " + preferred + ".");
        }
    }
}
