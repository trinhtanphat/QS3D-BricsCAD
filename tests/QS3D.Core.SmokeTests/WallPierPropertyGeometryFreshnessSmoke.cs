using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPierPropertyGeometryFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InstanceProfileModeEditStalesGeneratedSolid();
            InstanceChamferEditStalesGeneratedSolid();
            FamilyProfileEditStalesInheritedGeneratedSolid();
            WallPierKeyDoesNotDirtyUnrelatedGeneratedCategory();
        }

        private static void InstanceProfileModeEditStalesGeneratedSolid()
        {
            RequireInstanceEditStales("WallPierProfileMode", "Chamfered");
        }

        private static void InstanceChamferEditStalesGeneratedSolid()
        {
            RequireInstanceEditStales("WallPierChamferM", "0.03");
        }

        private static void RequireInstanceEditStales(string key, string value)
        {
            var wall = NewCleanGeneratedWallPier("WP-" + key);

            wall.SetProperty(key, value);

            if ((wall.Dirty & ElementDirtyFlags.Geometry) == 0)
                throw new InvalidOperationException(key + " must dirty WallPier geometry.");
            if (!wall.IsGeneratedSolidStale())
                throw new InvalidOperationException(key + " must stale existing WallPier generated solid output.");
        }

        private static void FamilyProfileEditStalesInheritedGeneratedSolid()
        {
            var project = new ProjectState("P-WP-FRESH", "WallPier freshness");
            var family = ProjectFamilyService.Create(project, "F-WP", "WallPier Family", ElementCategory.WallPier);
            var wall = new ProjectElement("WP-FAMILY", ElementCategory.WallPier, family.Id, string.Empty, string.Empty);
            wall.Properties["GeneratedSolidHandle"] = "S-FAMILY";
            wall.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall);

            var result = ProjectFamilyService.SetProperty(project, family.Id, "WallPierProfileMode", "Chamfered");

            if (result.InheritedInstancesUpdated != 1)
                throw new InvalidOperationException("Family WallPier profile edit did not propagate to the inherited instance.");
            if ((wall.Dirty & ElementDirtyFlags.Geometry) == 0)
                throw new InvalidOperationException("Inherited WallPier profile edit must dirty Geometry.");
            if (!wall.IsGeneratedSolidStale())
                throw new InvalidOperationException("Inherited WallPier profile edit must stale existing generated solid output.");
        }

        private static void WallPierKeyDoesNotDirtyUnrelatedGeneratedCategory()
        {
            var beam = new ProjectElement("B-WP-KEY", ElementCategory.Beam);
            beam.Properties["GeneratedSolidHandle"] = "S-BEAM";
            beam.MarkClean(ElementDirtyFlags.All);

            beam.SetProperty("WallPierProfileMode", "Chamfered");

            if ((beam.Dirty & ElementDirtyFlags.Geometry) != 0)
                throw new InvalidOperationException("WallPier profile keys must not dirty unrelated generated categories.");
            if (beam.IsGeneratedSolidStale())
                throw new InvalidOperationException("WallPier profile keys must not stale unrelated generated solid output.");
        }

        private static ProjectElement NewCleanGeneratedWallPier(string id)
        {
            var wall = new ProjectElement(id, ElementCategory.WallPier);
            wall.Properties["GeneratedSolidHandle"] = "S-" + id;
            wall.MarkClean(ElementDirtyFlags.All);
            return wall;
        }
    }
}
