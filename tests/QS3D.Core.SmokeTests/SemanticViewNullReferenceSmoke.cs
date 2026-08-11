using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewNullReferenceSmoke
    {
        public static void Run()
        {
            NullFloorReferenceFailsClosed();
            NullZoneReferenceFailsClosed();
        }

        private static void NullFloorReferenceFailsClosed()
        {
            var project = BuildProject();
            project.Floors.Add(null!);

            MustFailInvalidOperation(
                () => SemanticViewPlanner.Build(
                    project,
                    new SemanticViewDefinition("VIEW-FLOOR", "Null floor guard", floorId: "F-02")),
                "Semantic view planning must fail closed when the project floor collection contains a null entry.");
        }

        private static void NullZoneReferenceFailsClosed()
        {
            var project = BuildProject();
            project.Zones.Add(null!);

            MustFailInvalidOperation(
                () => SemanticViewPlanner.Build(
                    project,
                    new SemanticViewDefinition("VIEW-ZONE", "Null zone guard", zoneId: "Z-A")),
                "Semantic view planning must fail closed when the project zone collection contains a null entry.");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-VIEW-NULL", "Semantic View Null Reference");
            project.Floors.Add(new FloorDefinition("F-02", "L02", 3.6d));
            project.Zones.Add(new ZoneDefinition("Z-A", "Zone A"));
            project.Elements.Add(new ProjectElement("B-001", ElementCategory.Beam, "", "F-02", "Z-A"));
            return project;
        }

        private static void MustFailInvalidOperation(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new Exception(message + " Expected InvalidOperationException but received " + ex.GetType().Name + ".", ex);
            }

            throw new Exception(message);
        }
    }
}
