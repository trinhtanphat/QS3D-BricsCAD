using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeValidationSmoke
    {
        public static void Run()
        {
            ExportedValidSnapshotPasses();
            WrongUnitsFailClosed();
            GeneratedOwnershipSmugglingFailsClosed();
            BrokenDependencyFailsClosed();
            DependencyCycleFailsClosed();
        }

        private static void ExportedValidSnapshotPasses()
        {
            var project = BuildFixture();
            var json = ProjectInterchangeJsonExporter.Build(project);
            var result = ProjectInterchangeJsonValidator.Validate(json);
            if (!result.IsValid)
                throw new Exception("Exporter-produced valid semantic snapshot must validate: " + string.Join(" | ", result.Issues.Select(x => x.Code + ":" + x.Message)));
            if (result.ElementCount != 2 || result.FamilyCount != 1 || result.FloorCount != 1 || result.ZoneCount != 1)
                throw new Exception("Interchange validator summary counts are incorrect.");
            if (!string.Equals(result.Format, ProjectInterchangeJsonExporter.FormatName, StringComparison.Ordinal) || result.FormatVersion != ProjectInterchangeJsonExporter.FormatVersion)
                throw new Exception("Interchange validator did not preserve format identity.");
        }

        private static void WrongUnitsFailClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildFixture())
                .Replace("\"length\":\"m\"", "\"length\":\"mm\"");
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "UNIT_LENGTH");
        }

        private static void GeneratedOwnershipSmugglingFailsClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildFixture())
                .Replace("\"Mark\":\"B-01\"", "\"GeneratedSolidHandle\":\"DEAD\",\"Mark\":\"B-01\"");
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "GENERATED_RUNTIME_PROPERTY");
        }

        private static void BrokenDependencyFailsClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildFixture())
                .Replace("\"dependencies\":[\"E-ROOT\"]", "\"dependencies\":[\"E-MISSING\"]");
            RequireError(ProjectInterchangeJsonValidator.Validate(json), "DEPENDENCY_REF_MISSING");
        }

        private static void DependencyCycleFailsClosed()
        {
            var project = BuildFixture();
            project.FindElement("E-ROOT")!.DependsOn.Add("E-001");
            RequireError(ProjectInterchangeJsonValidator.Validate(ProjectInterchangeJsonExporter.Build(project)), "DEPENDENCY_CYCLE");
        }

        private static ProjectState BuildFixture()
        {
            var project = new ProjectState("P-VALIDATE", "Interchange Validate Smoke")
            {
                DrawingFingerprint = "DWG-FP",
                UpdatedUtc = new DateTime(2026, 8, 10, 11, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("FL-1", "L01", 0d));
            project.Families.Add(new ProjectFamily("FAM-1", "B300x500", ElementCategory.Beam));

            var root = new ProjectElement("E-ROOT", ElementCategory.Beam, "FAM-1", "FL-1", "Z-1")
            {
                DrawingFingerprint = "DWG-FP"
            };
            root.SourceHandles.Add("100");
            root.SetProperty("Mark", "B-00");
            root.SetQuantity("LengthM", 3d);
            project.Elements.Add(root);

            var child = new ProjectElement("E-001", ElementCategory.Beam, "FAM-1", "FL-1", "Z-1")
            {
                DrawingFingerprint = "DWG-FP"
            };
            child.SourceHandles.Add("101");
            child.DependsOn.Add("E-ROOT");
            child.SetProperty("Mark", "B-01");
            child.SetQuantity("LengthM", 5d);
            project.Elements.Add(child);
            return project;
        }

        private static void RequireError(ProjectInterchangeValidationResult result, string code)
        {
            if (result.IsValid) throw new Exception("Expected interchange validation failure: " + code);
            if (!result.Issues.Any(x => x.Severity == InterchangeValidationSeverity.Error && string.Equals(x.Code, code, StringComparison.Ordinal)))
                throw new Exception("Expected interchange validation issue: " + code + "; got " + string.Join(",", result.Issues.Select(x => x.Code)));
        }
    }
}
