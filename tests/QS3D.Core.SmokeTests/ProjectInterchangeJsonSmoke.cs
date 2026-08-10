using System;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeJsonSmoke
    {
        public static void Run()
        {
            SnapshotIsDeterministicAndUsesStableIds();
            GeneratedOwnershipIsExcluded();
            NumericContractFailsClosed();
        }

        private static void SnapshotIsDeterministicAndUsesStableIds()
        {
            var project = BuildFixture();
            var first = ProjectInterchangeJsonExporter.Build(project);
            var second = ProjectInterchangeJsonExporter.Build(project);
            if (!string.Equals(first, second, StringComparison.Ordinal)) throw new Exception("Semantic interchange output must be deterministic for unchanged project state.");
            Require(first, "\"format\":\"QS3D.SemanticSnapshot\"");
            Require(first, "\"formatVersion\":1");
            Require(first, "\"length\":\"m\"");
            Require(first, "\"area\":\"m2\"");
            Require(first, "\"volume\":\"m3\"");
            Require(first, "\"mass\":\"kg\"");
            Require(first, "\"id\":\"E-001\"");
            Require(first, "\"familyId\":\"FAM-1\"");
            Require(first, "\"sourceRefScope\":\"drawing-local\"");
            Require(first, "\"sourceHandles\":[\"1A2B\"]");
            Require(first, "\"dependencies\":[\"E-ROOT\"]");
            Require(first, "\"VolumeM3\":1.25");
        }

        private static void GeneratedOwnershipIsExcluded()
        {
            var project = BuildFixture();
            var element = project.FindElement("E-001")!;
            element.Properties["GeneratedSolidHandle"] = "DEAD";
            element.Properties["GeneratedCurtainFrameHandles"] = "BEEF;CAFE";
            element.Properties[ProjectElement.GeneratedGeometryStateKey] = "stale";
            element.Properties["PhysicalOpeningCutSolidHandle"] = "AAAA";
            element.Properties["Mark"] = "B-01";

            var json = ProjectInterchangeJsonExporter.Build(project);
            if (json.Contains("DEAD") || json.Contains("BEEF") || json.Contains("CAFE") || json.Contains("AAAA"))
                throw new Exception("Interchange snapshot leaked generated/native CAD ownership handles.");
            if (json.Contains(ProjectElement.GeneratedGeometryStateKey))
                throw new Exception("Interchange snapshot leaked generated runtime state.");
            Require(json, "\"Mark\":\"B-01\"");
        }

        private static void NumericContractFailsClosed()
        {
            var project = BuildFixture();
            project.FindElement("E-001")!.Quantities["Broken"] = double.NaN;
            var failed = false;
            try { ProjectInterchangeJsonExporter.Build(project); }
            catch (System.IO.InvalidDataException) { failed = true; }
            if (!failed) throw new Exception("Non-finite interchange quantities must fail closed.");
        }

        private static ProjectState BuildFixture()
        {
            var project = new ProjectState("P-001", "Interchange Smoke")
            {
                DrawingFingerprint = "DWG-FP",
                UpdatedUtc = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("FL-1", "L01", 0d));
            project.Families.Add(new ProjectFamily("FAM-1", "B300x500", ElementCategory.Beam));
            var element = new ProjectElement("E-001", ElementCategory.Beam, "FAM-1", "FL-1", "Z-1")
            {
                DrawingFingerprint = "DWG-FP"
            };
            element.SourceHandles.Add("1A2B");
            element.DependsOn.Add("E-ROOT");
            element.SetProperty("Mark", "B-01");
            element.SetQuantity("VolumeM3", 1.25d);
            project.Elements.Add(element);
            return project;
        }

        private static void Require(string text, string token)
        {
            if (!text.Contains(token)) throw new Exception("Expected interchange token: " + token);
        }
    }
}
