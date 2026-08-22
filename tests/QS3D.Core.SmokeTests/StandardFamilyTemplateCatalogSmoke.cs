using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class StandardFamilyTemplateCatalogSmoke
    {
        public static void Run()
        {
            CatalogMatchesApprovedLibrary();
            ApplyUsesCategoryAndNameAndIsIdempotent();
        }

        private static void CatalogMatchesApprovedLibrary()
        {
            var profile = StandardFamilyTemplateCatalog.CreateVietnamStandard01();
            Equal(StandardFamilyTemplateCatalog.VietnamStandard01Id, profile.Id, "Unexpected standard Family template id.");
            Equal(18, profile.Families.Count, "Standard Family template must contain the approved 18 Family rows.");

            Equal(3, profile.Families.Count(x => x.Category == ElementCategory.Beam), "Beam catalog mismatch.");
            Equal(3, profile.Families.Count(x => x.Category == ElementCategory.Slab), "Slab catalog mismatch.");
            Equal(3, profile.Families.Count(x => x.Category == ElementCategory.Column), "Column catalog mismatch.");
            Equal(2, profile.Families.Count(x => x.Category == ElementCategory.ArchitecturalWall), "Wall catalog mismatch.");
            Equal(2, profile.Families.Count(x => x.Category == ElementCategory.Foundation), "Foundation catalog mismatch.");
            Equal(5, profile.Families.Count(x =>
                x.Category == ElementCategory.FloorFinish ||
                x.Category == ElementCategory.WallFinish ||
                x.Category == ElementCategory.Skirting ||
                x.Category == ElementCategory.Waterproofing ||
                x.Category == ElementCategory.CeilingFinish), "Finish catalog mismatch.");

            var wall = profile.Families.Single(x => x.Category == ElementCategory.ArchitecturalWall && x.Name == "Tường Gạch 200");
            Property(wall, "ThicknessM", "0.200");
            Property(wall, "Material", "Gạch");
            Property(wall, "BQCode", "WALL-BRICK-200");
            Property(wall, "Description", "Tường gạch dày 200");
            Property(wall, "FireRating", "0");
            Property(wall, "IsLoadBearing", "Yes");
        }

        private static void ApplyUsesCategoryAndNameAndIsIdempotent()
        {
            var project = new ProjectState("P-STD-FAMILY", "Standard Family template smoke");
            var manualWall = new ProjectFamily("manual-wall-200", "Tường Gạch 200", ElementCategory.ArchitecturalWall);
            manualWall.Properties["ThicknessM"] = "0.150";
            manualWall.Properties["CustomKeep"] = "YES";
            project.Families.Add(manualWall);

            var first = StandardFamilyTemplateCatalog.ApplyVietnamStandard01(project);
            Equal(17, first.FamiliesAdded, "Apply must reuse the pre-existing same Category + Name Family instead of duplicating it.");
            Equal(1, first.FamiliesUpdated, "Existing wall defaults should be brought to the standard template.");
            Equal(18, project.Families.Count, "Apply produced an unexpected Family count.");

            var wallMatches = project.Families
                .Where(x => x.Category == ElementCategory.ArchitecturalWall && string.Equals(x.Name, "Tường Gạch 200", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Equal(1, wallMatches.Count, "Apply duplicated Tường Gạch 200.");
            var wall = wallMatches[0];
            Equal("manual-wall-200", wall.Id, "Apply must preserve the existing project-local Family id.");
            Property(wall, "ThicknessM", "0.200");
            Property(wall, "Material", "Gạch");
            Property(wall, "BQCode", "WALL-BRICK-200");
            Property(wall, "Description", "Tường gạch dày 200");
            Property(wall, "FireRating", "0");
            Property(wall, "IsLoadBearing", "Yes");
            Property(wall, "CustomKeep", "YES");

            var versionBeforeSecondApply = project.ChangeVersion;
            var auditsBeforeSecondApply = project.AuditEvents.Count;
            var updatedBeforeSecondApply = project.UpdatedUtc;
            var second = StandardFamilyTemplateCatalog.ApplyVietnamStandard01(project);
            Equal(0, second.FamiliesAdded, "Second apply must not add duplicate Families.");
            Equal(0, second.FamiliesUpdated, "Second apply must not rewrite already-standard Families.");
            Equal(0, second.PropertiesApplied, "Second apply must not rewrite already-standard properties.");
            Equal(18, project.Families.Count, "Second apply changed Family count.");
            Equal(versionBeforeSecondApply, project.ChangeVersion, "Second apply must not bump project ChangeVersion.");
            Equal(auditsBeforeSecondApply, project.AuditEvents.Count, "Second apply must not append a no-op audit event.");
            Equal(updatedBeforeSecondApply, project.UpdatedUtc, "Second apply must not change project UpdatedUtc.");
        }

        private static void Property(ProjectFamily family, string key, string expected)
        {
            if (!family.Properties.TryGetValue(key, out var actual))
                throw new Exception("Missing Family property " + family.Name + "/" + key + ".");
            Equal(expected, actual, "Unexpected Family property " + family.Name + "/" + key + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
