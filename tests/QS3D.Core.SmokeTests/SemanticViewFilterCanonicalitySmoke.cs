using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticViewFilterCanonicalitySmoke
    {
        public static void Run()
        {
            PaddedCaseVariedRelationsStillMatchCanonicalFilters();
        }

        private static void PaddedCaseVariedRelationsStillMatchCanonicalFilters()
        {
            var project = new ProjectState("P-VIEW-FILTER-CANONICAL", "Semantic view filter canonicality");
            project.Floors.Add(new FloorDefinition("F-01", "Floor 01", 0.0));
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone 01"));

            var element = new ProjectElement("E-01", ElementCategory.Beam)
            {
                FloorId = "  f-01  ",
                ZoneId = "  z-01  "
            };
            project.Elements.Add(element);

            var definition = new SemanticViewDefinition(
                "V-01",
                "Canonical relation filter",
                SemanticViewKind.Model,
                "F-01",
                "Z-01");

            var beforeVersion = project.ChangeVersion;
            var beforeFloorId = element.FloorId;
            var beforeZoneId = element.ZoneId;

            var plan = SemanticViewPlanner.Build(project, definition);

            Equal(1, plan.ElementIds.Count);
            Equal("E-01", plan.ElementIds[0]);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeFloorId, element.FloorId);
            Equal(beforeZoneId, element.ZoneId);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
