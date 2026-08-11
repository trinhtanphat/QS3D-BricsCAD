using System;
using QS3D.Core.Documentation;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticScheduleFilterCanonicalitySmoke
    {
        public static void Run()
        {
            PaddedCaseVariedRelationsStillMatchCanonicalFilters();
        }

        private static void PaddedCaseVariedRelationsStillMatchCanonicalFilters()
        {
            var project = new ProjectState("P-SCHEDULE-FILTER-CANONICAL", "Semantic schedule filter canonicality");
            project.Floors.Add(new FloorDefinition("F-01", "Floor 01", 0.0));
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone 01"));

            var element = new ProjectElement("E-01", ElementCategory.Beam)
            {
                FloorId = "  f-01  ",
                ZoneId = "  z-01  "
            };
            project.Elements.Add(element);

            var definition = new SemanticScheduleDefinition(
                "S-01",
                "Canonical relation filter",
                "Canonical relation filter",
                Array.Empty<ElementCategory>(),
                "F-01",
                "Z-01",
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { new SemanticDocumentationColumn("Id", "{Id}") });

            var beforeVersion = project.ChangeVersion;
            var beforeFloorId = element.FloorId;
            var beforeZoneId = element.ZoneId;

            var table = SemanticScheduleCatalog.Build(project, definition);

            Equal(1, table.Rows.Count);
            Equal("E-01", table.Rows[0].ElementId);
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
