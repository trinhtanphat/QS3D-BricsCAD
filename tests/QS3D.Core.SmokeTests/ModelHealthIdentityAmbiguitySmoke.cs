using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ModelHealthIdentityAmbiguitySmoke
    {
        public static void Run()
        {
            CatalogNullsRejectAtAdmission();
            ModelHealthReportsAmbiguityWithoutThrowing();
            ComprehensiveHealthPreservesReportAcrossProviderFailures();
            DependencyHealthRejectsAmbiguousTargets();
            LevelHealthReportsDuplicateLevelReferencesWithoutPendingQualification();
        }

        private static void CatalogNullsRejectAtAdmission()
        {
            var project = new ProjectState("health-null-admission", "Health null admission");
            var beforeVersion = project.ChangeVersion;

            ThrowsArgumentNullItem(() => project.Floors.Add(null!));
            ThrowsArgumentNullItem(() => project.Zones.Add(null!));
            ThrowsArgumentNullItem(() => project.Families.Add(null!));

            if (project.Floors.Count != 0 || project.Zones.Count != 0 || project.Families.Count != 0)
                throw new Exception("Rejected null catalog entries must not mutate persisted catalogs.");
            if (project.ChangeVersion != beforeVersion)
                throw new Exception("Rejected null catalog entries must not advance project revision.");
        }

        private static void ModelHealthReportsAmbiguityWithoutThrowing()
        {
            var project = CorruptProject();
            var issues = new ModelHealthService().Inspect(project);

            Has(issues, "NULL_ELEMENT");
            Has(issues, "DUPLICATE_ID");
            Has(issues, "DUPLICATE_FAMILY_ID");
            Has(issues, "DUPLICATE_FLOOR_ID");
            Has(issues, "DUPLICATE_ZONE_ID");
            Has(issues, "AMBIGUOUS_ACTIVE_FLOOR");
            Has(issues, "AMBIGUOUS_ACTIVE_ZONE");
            HasFor(issues, "AMBIGUOUS_FAMILY", "D");
            HasFor(issues, "AMBIGUOUS_FLOOR", "D");
            HasFor(issues, "AMBIGUOUS_ZONE", "D");
            HasFor(issues, "AMBIGUOUS_HOST", "D");
            HasFor(issues, "AMBIGUOUS_DEPENDENCY", "D");
        }

        private static void ComprehensiveHealthPreservesReportAcrossProviderFailures()
        {
            var issues = new ComprehensiveModelHealthService().Inspect(CorruptProject());
            Has(issues, "NULL_ELEMENT");
            Has(issues, "DUPLICATE_FAMILY_ID");
            HasFor(issues, "AMBIGUOUS_HOST", "D");
            HasFor(issues, "AMBIGUOUS_DEPENDENCY", "D");
            Has(issues, "HEALTH_PROVIDER_FAILED");
        }

        private static void DependencyHealthRejectsAmbiguousTargets()
        {
            var project = CorruptProject();
            project.Elements.Remove(null!);
            var issues = new DependencyHealthService().Inspect(project);
            HasFor(issues, "DEPENDENCY_TARGET_AMBIGUOUS", "D");
        }

        private static void LevelHealthReportsDuplicateLevelReferencesWithoutPendingQualification()
        {
            var project = CorruptProject();
            project.Elements.Remove(null!);
            var issues = new LevelReferenceHealthService().Inspect(project);

            Has(issues, "DUPLICATE_LEVEL_ID");
            HasFor(issues, "BOTTOM_LEVEL_REFERENCE_AMBIGUOUS", "D");
            HasFor(issues, "TOP_LEVEL_REFERENCE_AMBIGUOUS", "D");
            if (issues.Any(x => x.ElementId == "D" && x.Code == "LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING"))
                throw new Exception("Ambiguous Level references must not be reported as semantically valid/native-pending.");
        }

        private static ProjectState CorruptProject()
        {
            var project = new ProjectState("health-ambiguity", "Health ambiguity")
            {
                ActiveFloorId = "L",
                ActiveZoneId = "Z"
            };

            project.Floors.Add(new FloorDefinition("L", "Level A", 0d));
            project.Floors.Add(new FloorDefinition("l", "Level B", 3d));
            project.Zones.Add(new ZoneDefinition("Z", "Zone A"));
            project.Zones.Add(new ZoneDefinition("z", "Zone B"));

            project.Families.Add(new ProjectFamily("F", "Door A", ElementCategory.Door));
            project.Families.Add(new ProjectFamily("f", "Door B", ElementCategory.Door));

            var firstHost = new ProjectElement("H", ElementCategory.ArchitecturalWall, string.Empty, "L", "Z");
            firstHost.Properties["LengthM"] = "3";
            firstHost.Properties["HeightM"] = "3";
            firstHost.Properties["ThicknessM"] = "0.2";
            firstHost.Properties["Material"] = "Concrete";
            var secondHost = new ProjectElement("h", ElementCategory.ArchitecturalWall, string.Empty, "L", "Z");
            secondHost.Properties["LengthM"] = "4";
            secondHost.Properties["HeightM"] = "3";
            secondHost.Properties["ThicknessM"] = "0.2";
            secondHost.Properties["Material"] = "Concrete";

            var door = new ProjectElement("D", ElementCategory.Door, "F", "L", "Z");
            door.Properties["WidthM"] = "0.9";
            door.Properties["HeightM"] = "2.1";
            door.Properties["Material"] = "Wood";
            door.Properties["HostWallId"] = "H";
            door.Properties[ProjectFloorService.BottomLevelIdKey] = "L";
            door.Properties[ProjectFloorService.TopLevelIdKey] = "L";
            door.DependsOn.Add("H");

            project.Elements.Add(firstHost);
            project.Elements.Add(secondHost);
            project.Elements.Add(door);
            project.Elements.Add(null!);
            return project;
        }

        private static void ThrowsArgumentNullItem(Action action)
        {
            try { action(); }
            catch (ArgumentNullException ex)
            {
                if (string.Equals(ex.ParamName, "item", StringComparison.Ordinal)) return;
                throw new Exception("Expected ArgumentNullException parameter 'item', actual='" + ex.ParamName + "'.");
            }
            throw new Exception("Expected ArgumentNullException for null persisted catalog admission.");
        }

        private static void Has(System.Collections.Generic.IEnumerable<ModelHealthIssue> issues, string code)
        {
            if (!issues.Any(x => x.Code == code)) throw new Exception("Missing health issue " + code + ".");
        }

        private static void HasFor(System.Collections.Generic.IEnumerable<ModelHealthIssue> issues, string code, string elementId)
        {
            if (!issues.Any(x => x.Code == code && string.Equals(x.ElementId, elementId, StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Missing health issue " + code + " for " + elementId + ".");
        }
    }
}
