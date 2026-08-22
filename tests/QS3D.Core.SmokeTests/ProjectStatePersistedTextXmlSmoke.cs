using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStatePersistedTextXmlSmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidPersistedTextBeforeMutation();
            PreservesSupplementaryUnicodeRoundTrip();
        }

        private static void RejectsXmlInvalidPersistedTextBeforeMutation()
        {
            foreach (var invalid in new[] { new string(new[] { '\uD800' }), new string(new[] { '\uDC00' }) })
            {
                ExpectArgument(() => new ZoneDefinition(invalid, "Zone 1"), "ZoneDefinition.Id");
                ExpectArgument(() => new ZoneDefinition("Z1", invalid), "ZoneDefinition.Name constructor");

                var zone = new ZoneDefinition("Z1", "Zone 1");
                var zoneName = zone.Name;
                ExpectArgument(() => zone.Name = invalid, "ZoneDefinition.Name setter");
                Equal(zoneName, zone.Name, "Rejected ZoneDefinition.Name mutation changed the live name.");

                ExpectArgument(() => new FloorDefinition(invalid, "Level 1", 0d), "FloorDefinition.Id");
                ExpectArgument(() => new FloorDefinition("F1", invalid, 0d), "FloorDefinition.Name constructor");

                var floor = new FloorDefinition("F1", "Level 1", 0d);
                var floorName = floor.Name;
                ExpectArgument(() => floor.Name = invalid, "FloorDefinition.Name setter");
                Equal(floorName, floor.Name, "Rejected FloorDefinition.Name mutation changed the live name.");

                ExpectArgument(() => new ProjectFamily(invalid, "Room family", ElementCategory.Room), "ProjectFamily.Id");
                ExpectArgument(() => new ProjectFamily("FAMILY1", invalid, ElementCategory.Room), "ProjectFamily.Name constructor");

                var family = new ProjectFamily("FAMILY1", "Room family", ElementCategory.Room);
                var familyName = family.Name;
                ExpectArgument(() => family.Name = invalid, "ProjectFamily.Name setter");
                Equal(familyName, family.Name, "Rejected ProjectFamily.Name mutation changed the live name.");

                ExpectArgument(() => new ProjectState(invalid, "Project XML"), "ProjectState.ProjectId");
                ExpectArgument(() => new ProjectState("P-XML-INVALID", invalid), "ProjectState.Name constructor");

                var project = new ProjectState("P-XML-INVALID", "Project XML");
                project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
                project.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
                project.DrawingPath = "before.dwg";
                project.DrawingFingerprint = "before-fingerprint";
                project.ActiveZoneId = "Z1";
                project.ActiveFloorId = "F1";

                AssertRejectedProjectScalar(project, () => project.Name, () => project.Name = invalid, "Name");
                AssertRejectedProjectScalar(project, () => project.DrawingPath, () => project.DrawingPath = invalid, "DrawingPath");
                AssertRejectedProjectScalar(project, () => project.DrawingFingerprint, () => project.DrawingFingerprint = invalid, "DrawingFingerprint");
                AssertRejectedProjectScalar(project, () => project.ActiveZoneId, () => project.ActiveZoneId = invalid, "ActiveZoneId");
                AssertRejectedProjectScalar(project, () => project.ActiveFloorId, () => project.ActiveFloorId = invalid, "ActiveFloorId");
            }
        }

        private static void PreservesSupplementaryUnicodeRoundTrip()
        {
            const string supplementary = "\U0001F642";
            var path = Path.Combine(Path.GetTempPath(), "qs3d-projectstate-text-xml-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var zone = new ZoneDefinition("Z-" + supplementary, "Zone " + supplementary);
                var floor = new FloorDefinition("F-" + supplementary, "Level " + supplementary, 0d);
                var family = new ProjectFamily("FAMILY-" + supplementary, "Family " + supplementary, ElementCategory.Room);
                var project = new ProjectState("P-" + supplementary, "Project " + supplementary)
                {
                    DrawingPath = "Drawing-" + supplementary + ".dwg",
                    DrawingFingerprint = "Fingerprint-" + supplementary
                };
                project.Zones.Add(zone);
                project.Floors.Add(floor);
                project.Families.Add(family);
                project.ActiveZoneId = zone.Id;
                project.ActiveFloorId = floor.Id;

                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);

                Equal(project.ProjectId, loaded.ProjectId, "ProjectId supplementary Unicode did not round-trip exactly.");
                Equal(project.Name, loaded.Name, "Project Name supplementary Unicode did not round-trip exactly.");
                Equal(project.DrawingPath, loaded.DrawingPath, "DrawingPath supplementary Unicode did not round-trip exactly.");
                Equal(project.DrawingFingerprint, loaded.DrawingFingerprint, "DrawingFingerprint supplementary Unicode did not round-trip exactly.");
                Equal(project.ActiveZoneId, loaded.ActiveZoneId, "ActiveZoneId supplementary Unicode did not round-trip exactly.");
                Equal(project.ActiveFloorId, loaded.ActiveFloorId, "ActiveFloorId supplementary Unicode did not round-trip exactly.");

                var loadedZone = loaded.FindZone(zone.Id) ?? throw new InvalidOperationException("Supplementary-Unicode zone was not restored.");
                Equal(zone.Id, loadedZone.Id, "Zone Id supplementary Unicode did not round-trip exactly.");
                Equal(zone.Name, loadedZone.Name, "Zone Name supplementary Unicode did not round-trip exactly.");

                var loadedFloor = loaded.FindFloor(floor.Id) ?? throw new InvalidOperationException("Supplementary-Unicode floor was not restored.");
                Equal(floor.Id, loadedFloor.Id, "Floor Id supplementary Unicode did not round-trip exactly.");
                Equal(floor.Name, loadedFloor.Name, "Floor Name supplementary Unicode did not round-trip exactly.");

                var loadedFamily = loaded.FindFamily(family.Id) ?? throw new InvalidOperationException("Supplementary-Unicode family was not restored.");
                Equal(family.Id, loadedFamily.Id, "Family Id supplementary Unicode did not round-trip exactly.");
                Equal(family.Name, loadedFamily.Name, "Family Name supplementary Unicode did not round-trip exactly.");
            }
            finally
            {
                DeleteIfExists(path);
                DeleteIfExists(path + ".bak");
                DeleteIfExists(path + ".tmp");
            }
        }

        private static void AssertRejectedProjectScalar(ProjectState project, Func<string> read, Action mutation, string label)
        {
            var beforeValue = read();
            var beforeChangeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;

            ExpectArgument(mutation, label);

            Equal(beforeValue, read(), "Rejected " + label + " mutation changed the live value.");
            Equal(beforeChangeVersion, project.ChangeVersion, "Rejected " + label + " mutation changed ChangeVersion.");
            Equal(beforeUpdatedUtc, project.UpdatedUtc, "Rejected " + label + " mutation changed UpdatedUtc.");
        }

        private static void ExpectArgument(Action action, string label)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Expected ArgumentException for XML-invalid " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void DeleteIfExists(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
