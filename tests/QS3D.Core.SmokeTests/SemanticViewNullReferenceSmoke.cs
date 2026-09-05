using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewNullReferenceSmoke
    {
        public static void Run()
        {
            NullFloorReferenceRejectsAtCatalogBoundary();
            NullZoneReferenceRejectsAtCatalogBoundary();
        }

        private static void NullFloorReferenceRejectsAtCatalogBoundary()
        {
            var project = BuildProject();
            var beforeCount = project.Floors.Count;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            MustFailArgumentNull(
                () => project.Floors.Add(null!),
                "Null Floor admission must fail closed at the persisted catalog boundary.");

            if (project.Floors.Count != beforeCount ||
                project.ChangeVersion != beforeVersion ||
                project.UpdatedUtc != beforeUpdatedUtc)
                throw new Exception("Rejected null Floor admission mutated persisted project state.");

            _ = SemanticViewPlanner.Build(
                project,
                new SemanticViewDefinition("VIEW-FLOOR", "Null floor guard", floorId: "F-02"));
        }

        private static void NullZoneReferenceRejectsAtCatalogBoundary()
        {
            var project = BuildProject();
            var beforeCount = project.Zones.Count;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            MustFailArgumentNull(
                () => project.Zones.Add(null!),
                "Null Zone admission must fail closed at the persisted catalog boundary.");

            if (project.Zones.Count != beforeCount ||
                project.ChangeVersion != beforeVersion ||
                project.UpdatedUtc != beforeUpdatedUtc)
                throw new Exception("Rejected null Zone admission mutated persisted project state.");

            _ = SemanticViewPlanner.Build(
                project,
                new SemanticViewDefinition("VIEW-ZONE", "Null zone guard", zoneId: "Z-A"));
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-VIEW-NULL", "Semantic View Null Reference");
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, "", "F-02", "Z-A"));
            return project;
        }

        private static void MustFailArgumentNull(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentNullException ex)
            {
                if (string.Equals(ex.ParamName, "item", StringComparison.Ordinal)) return;
                throw new Exception(message + " Unexpected parameter '" + ex.ParamName + "'.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception(message + " Expected ArgumentNullException but received " + ex.GetType().Name + ".", ex);
            }

            throw new Exception(message);
        }
    }
}
