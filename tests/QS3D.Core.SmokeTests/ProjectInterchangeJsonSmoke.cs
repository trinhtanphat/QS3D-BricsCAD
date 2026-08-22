using System;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeJsonSmoke
    {
        public static void Run()
        {
            SnapshotIsDeterministicAndUsesStableIds();
            GeneratedOwnershipIsExcluded();
            NumericContractFailsClosed();
            SourceReferencesFailClosedWithoutNormalization();
            DetachedCopyDoesNotMutateLiveProject();
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
            Require(first, "\"sourceHandles\": [\"1A2B\"]");
            Require(first, "\"dependencies\": [\"E-ROOT\"]");
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
            ThrowsInvalidData(() => ProjectInterchangeJsonExporter.Build(project), "Non-finite interchange quantities must fail closed.");
        }

        private static void SourceReferencesFailClosedWithoutNormalization()
        {
            var paddedHandle = BuildFixture();
            paddedHandle.FindElement("E-001")!.SourceHandles[0] = " 1A2B ";
            ThrowsInvalidData(() => ProjectInterchangeJsonExporter.Build(paddedHandle), "Padded source handles must not be silently trimmed during export.");

            var duplicateHandle = BuildFixture();
            duplicateHandle.FindElement("E-001")!.SourceHandles.Add("1a2b");
            ThrowsInvalidData(() => ProjectInterchangeJsonExporter.Build(duplicateHandle), "Case-insensitive duplicate source handles must not be silently deduplicated during export.");

            var blankDependency = BuildFixture();
            blankDependency.FindElement("E-001")!.DependsOn.Add(" ");
            ThrowsInvalidData(() => ProjectInterchangeJsonExporter.Build(blankDependency), "Blank dependencies must not be silently dropped during export.");

            var paddedDependency = BuildFixture();
            paddedDependency.FindElement("E-001")!.DependsOn[0] = " E-ROOT ";
            ThrowsInvalidData(() => ProjectInterchangeJsonExporter.Build(paddedDependency), "Padded dependencies must not be silently trimmed during export.");

            var duplicateDependency = BuildFixture();
            duplicateDependency.FindElement("E-001")!.DependsOn.Add("e-root");
            ThrowsInvalidData(() => ProjectInterchangeJsonExporter.Build(duplicateDependency), "Case-insensitive duplicate dependencies must not be silently deduplicated during export.");
        }

        private static void DetachedCopyDoesNotMutateLiveProject()
        {
            var live = BuildFixture();
            live.Metadata["Contract"] = "live";
            var liveElement = live.FindElement("E-001")!;
            var originalUpdatedUtc = live.UpdatedUtc;
            var originalElementUpdatedUtc = liveElement.UpdatedUtc;

            var detached = ProjectStateSnapshot.CreateDetachedCopy(live);
            var detachedElement = detached.FindElement("E-001")!;
            if (ReferenceEquals(live, detached) || ReferenceEquals(liveElement, detachedElement))
                throw new Exception("Detached interchange state must not share mutable project/element instances with the live project.");

            detached.Name = "Detached";
            detached.Metadata["Contract"] = "detached";
            detachedElement.SetProperty("Mark", "DETACHED");
            detachedElement.SetQuantity("VolumeM3", 9.5d);

            if (!string.Equals(live.Name, "Interchange Smoke", StringComparison.Ordinal)) throw new Exception("Detached project mutation leaked into live project name.");
            if (!string.Equals(live.Metadata["Contract"], "live", StringComparison.Ordinal)) throw new Exception("Detached project mutation leaked into live metadata.");
            if (!string.Equals(liveElement.Properties["Mark"], "B-01", StringComparison.Ordinal)) throw new Exception("Detached element property mutation leaked into live project.");
            if (Math.Abs(liveElement.Quantities["VolumeM3"] - 1.25d) > 1e-12) throw new Exception("Detached quantity mutation leaked into live project.");
            if (live.UpdatedUtc != originalUpdatedUtc || liveElement.UpdatedUtc != originalElementUpdatedUtc)
                throw new Exception("Detached mutation changed live project timestamps.");
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
            project.Elements.Add(new ProjectElement("E-ROOT", ElementCategory.Beam, "FAM-1", "FL-1", "Z-1"));
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

        private static void ThrowsInvalidData(Action action, string message)
        {
            try { action(); }
            catch (System.IO.InvalidDataException) { return; }
            throw new Exception(message);
        }

        private static void Require(string text, string token)
        {
            if (!text.Contains(token)) throw new Exception("Expected interchange token: " + token);
        }
    }
}
