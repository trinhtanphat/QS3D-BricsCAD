using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeValidatedSnapshotReaderSmoke
    {
        public static void Run()
        {
            ExportedSnapshotReadsAllPortableFields();
            ReaderCollectionsAreImmutableSnapshots();
            InvalidSnapshotFailsBeforeTypedRead();
            MissingTimestampWarningRemainsReadable();
            WhitespaceOnlyTimestampWarningsRemainReadable();
        }

        private static void ExportedSnapshotReadsAllPortableFields()
        {
            var project = Project();
            var json = ProjectInterchangeJsonExporter.Build(project);
            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(json);

            True(snapshot.Validation.IsValid);
            Equal(ProjectInterchangeJsonExporter.FormatName, snapshot.Format);
            Equal(ProjectInterchangeJsonExporter.FormatVersion, snapshot.FormatVersion);
            Equal("m", snapshot.Units.Length);
            Equal("m2", snapshot.Units.Area);
            Equal("m3", snapshot.Units.Volume);
            Equal("kg", snapshot.Units.Mass);
            Equal(project.ProjectId, snapshot.Project.Id);
            Equal(project.Name, snapshot.Project.Name);
            Equal(project.SchemaVersion, snapshot.Project.SchemaVersion);
            Equal(project.DrawingFingerprint, snapshot.Project.DrawingFingerprint);
            True(snapshot.Project.UpdatedUtc.HasValue);
            Equal(1, snapshot.Zones.Count);
            Equal("Z-01", snapshot.Zones[0].Id);
            Equal(1, snapshot.Floors.Count);
            Near(3.25d, snapshot.Floors[0].ElevationM);
            Equal(1, snapshot.Families.Count);
            Equal(ElementCategory.Beam, snapshot.Families[0].Category);
            Equal("C30", snapshot.Families[0].Properties["Material"]);
            Equal(2, snapshot.Elements.Count);

            var beam = snapshot.Elements.Single(x => x.Id == "E-BEAM");
            Equal(ElementCategory.Beam, beam.Category);
            Equal("FAM-B", beam.FamilyId);
            Equal("F-01", beam.FloorId);
            Equal("Z-01", beam.ZoneId);
            Equal("drawing-fp", beam.DrawingFingerprint);
            Equal("drawing-local", beam.SourceRefScope);
            Equal(1, beam.SourceHandles.Count);
            Equal("1A2B", beam.SourceHandles[0]);
            Equal(1, beam.Dependencies.Count);
            Equal("E-BASE", beam.Dependencies[0]);
            Equal("B-01", beam.Properties["Mark"]);
            Near(5.5d, beam.Quantities["LengthM"]);
            True(beam.UpdatedUtc.HasValue);
        }

        private static void ReaderCollectionsAreImmutableSnapshots()
        {
            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(ProjectInterchangeJsonExporter.Build(Project()));

            Throws<NotSupportedException>(() => ((IList<InterchangeElementSnapshot>)snapshot.Elements).Clear());
            Throws<NotSupportedException>(() => ((IDictionary<string, string>)snapshot.Families[0].Properties).Clear());
            Throws<NotSupportedException>(() => ((IDictionary<string, double>)snapshot.Elements[0].Quantities).Clear());
            Equal(2, snapshot.Elements.Count);
            Equal("C30", snapshot.Families[0].Properties["Material"]);
        }

        private static void InvalidSnapshotFailsBeforeTypedRead()
        {
            Throws<InvalidDataException>(() => ProjectInterchangeValidatedSnapshotReader.Read("{\"format\":\"Wrong\"}"));
        }

        private static void MissingTimestampWarningRemainsReadable()
        {
            var json = ProjectInterchangeJsonExporter.Build(Project());
            var marker = "\"updatedUtc\":";
            var first = json.IndexOf(marker, StringComparison.Ordinal);
            True(first >= 0);
            var valueStart = first + marker.Length;
            True(valueStart < json.Length && json[valueStart] == '"');
            var valueEnd = json.IndexOf('"', valueStart + 1);
            True(valueEnd > valueStart);
            var rewritten = json.Substring(0, valueStart) + "\"\"" + json.Substring(valueEnd + 1);

            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(rewritten);
            True(snapshot.Validation.IsValid);
            True(snapshot.Validation.WarningCount > 0);
            True(!snapshot.Project.UpdatedUtc.HasValue);
            Equal(string.Empty, snapshot.Project.UpdatedUtcRaw);
        }

        private static void WhitespaceOnlyTimestampWarningsRemainReadable()
        {
            var rewritten = ProjectInterchangeJsonExporter.Build(Project());
            const string marker = "\"updatedUtc\":";
            var searchFrom = 0;
            var replaced = 0;
            while (true)
            {
                var next = rewritten.IndexOf(marker, searchFrom, StringComparison.Ordinal);
                if (next < 0) break;
                var valueStart = next + marker.Length;
                True(valueStart < rewritten.Length && rewritten[valueStart] == '"');
                var valueEnd = rewritten.IndexOf('"', valueStart + 1);
                True(valueEnd > valueStart);
                rewritten = rewritten.Substring(0, valueStart) + "\"   \"" + rewritten.Substring(valueEnd + 1);
                searchFrom = valueStart + 5;
                replaced++;
            }

            True(replaced >= 2);
            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(rewritten);
            True(snapshot.Validation.IsValid);
            True(snapshot.Validation.WarningCount >= replaced);
            True(!snapshot.Project.UpdatedUtc.HasValue);
            Equal(string.Empty, snapshot.Project.UpdatedUtcRaw);
            True(snapshot.Elements.All(x => !x.UpdatedUtc.HasValue));
            True(snapshot.Elements.All(x => string.Equals(x.UpdatedUtcRaw, string.Empty, StringComparison.Ordinal)));
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("P-READ", "Reader Project")
            {
                DrawingFingerprint = "drawing-fp",
                UpdatedUtc = new DateTime(2026, 8, 10, 10, 11, 12, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone 01"));
            project.Floors.Add(new FloorDefinition("F-01", "Floor 01", 3.25d));
            var family = new ProjectFamily("FAM-B", "Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);

            var baseElement = new ProjectElement("E-BASE", ElementCategory.Beam, "FAM-B", "F-01", "Z-01")
            {
                DrawingFingerprint = "drawing-fp"
            };
            baseElement.SetProperty("Mark", "BASE");
            baseElement.SetQuantity("LengthM", 1d);
            project.Elements.Add(baseElement);

            var beam = new ProjectElement("E-BEAM", ElementCategory.Beam, "FAM-B", "F-01", "Z-01")
            {
                DrawingFingerprint = "drawing-fp"
            };
            beam.SourceHandles.Add("1A2B");
            beam.DependsOn.Add(baseElement.Id);
            beam.SetProperty("Mark", "B-01");
            beam.SetQuantity("LengthM", 5.5d);
            project.Elements.Add(beam);
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }
        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}