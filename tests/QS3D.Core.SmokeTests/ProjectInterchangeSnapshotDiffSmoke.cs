using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSnapshotDiffSmoke
    {
        public static void Run()
        {
            IdenticalSnapshotsHaveNoChanges();
            AddedRemovedAndChangedObjectsAreClassified();
            ElementPortableFieldsAreCompared();
            ProvenanceCollectionsAreOrderInsensitive();
            CompareJsonIsValidationFirst();
            ResultCollectionsAreImmutable();
        }

        private static void IdenticalSnapshotsHaveNoChanges()
        {
            var json = ProjectInterchangeJsonExporter.Build(Project("E-1", "B-01", 5d));
            var diff = ProjectInterchangeSnapshotDiff.CompareJson(json, json);
            True(!diff.HasChanges);
            Equal(0, diff.Changes.Count);
            Equal(0, diff.AddedCount);
            Equal(0, diff.RemovedCount);
            Equal(0, diff.ChangedCount);
        }

        private static void AddedRemovedAndChangedObjectsAreClassified()
        {
            var left = Project("E-LEFT", "B-01", 5d);
            var right = Project("E-RIGHT", "B-02", 7d);
            right.Name = "Project changed";
            right.Zones[0].Name = "Zone changed";
            right.Floors[0].Name = "Floor changed";
            right.Families[0].Name = "Family changed";

            var diff = ProjectInterchangeSnapshotDiff.CompareJson(
                ProjectInterchangeJsonExporter.Build(left),
                ProjectInterchangeJsonExporter.Build(right));

            True(diff.Changes.Any(x => x.ObjectKind == InterchangeSnapshotObjectKind.Project && x.ChangeKind == InterchangeSnapshotChangeKind.Changed && x.Fields.Contains("name")));
            True(diff.Changes.Any(x => x.ObjectKind == InterchangeSnapshotObjectKind.Zone && x.Id == "Z-01" && x.Fields.Contains("name")));
            True(diff.Changes.Any(x => x.ObjectKind == InterchangeSnapshotObjectKind.Floor && x.Id == "F-01" && x.Fields.Contains("name")));
            True(diff.Changes.Any(x => x.ObjectKind == InterchangeSnapshotObjectKind.Family && x.Id == "FAM-B" && x.Fields.Contains("name")));
            True(diff.Changes.Any(x => x.ObjectKind == InterchangeSnapshotObjectKind.Element && x.Id == "E-LEFT" && x.ChangeKind == InterchangeSnapshotChangeKind.Removed));
            True(diff.Changes.Any(x => x.ObjectKind == InterchangeSnapshotObjectKind.Element && x.Id == "E-RIGHT" && x.ChangeKind == InterchangeSnapshotChangeKind.Added));
        }

        private static void ElementPortableFieldsAreCompared()
        {
            var left = Project("E-1", "B-01", 5d);
            var right = Project("E-1", "B-02", 7d);
            var rightElement = right.Elements.Single(x => x.Id == "E-1");
            rightElement.SourceHandles.Clear();
            rightElement.SourceHandles.Add("FFFF");
            rightElement.DependsOn.Clear();
            rightElement.DependsOn.Add("E-BASE-2");
            var base2 = new ProjectElement("E-BASE-2", ElementCategory.Beam, "FAM-B", "F-01", "Z-01");
            base2.SetProperty("Mark", "BASE-2");
            base2.SetQuantity("LengthM", 1d);
            right.Elements.Add(base2);

            var diff = ProjectInterchangeSnapshotDiff.CompareJson(
                ProjectInterchangeJsonExporter.Build(left),
                ProjectInterchangeJsonExporter.Build(right));
            var change = diff.Changes.Single(x => x.ObjectKind == InterchangeSnapshotObjectKind.Element && x.Id == "E-1");

            Equal(InterchangeSnapshotChangeKind.Changed, change.ChangeKind);
            if (!change.Fields.Contains("sourceHandles"))
                throw new Exception("Expected sourceHandles change; actual fields: " + string.Join(",", change.Fields) + ".");
            True(change.Fields.Contains("dependencies"));
            True(change.Fields.Contains("properties"));
            True(change.Fields.Contains("quantities"));
        }

        private static void ProvenanceCollectionsAreOrderInsensitive()
        {
            var left = Project("E-1", "B-01", 5d);
            var right = Project("E-1", "B-01", 5d);
            var leftElement = left.Elements.Single(x => x.Id == "E-1");
            var rightElement = right.Elements.Single(x => x.Id == "E-1");
            leftElement.SourceHandles.Add("BBBB");
            rightElement.SourceHandles.Insert(0, "bbbb");
            var dep2Left = new ProjectElement("E-BASE-2", ElementCategory.Beam, "FAM-B", "F-01", "Z-01");
            dep2Left.SetProperty("Mark", "BASE-2"); dep2Left.SetQuantity("LengthM", 1d);
            var dep2Right = new ProjectElement("E-BASE-2", ElementCategory.Beam, "FAM-B", "F-01", "Z-01");
            dep2Right.SetProperty("Mark", "BASE-2"); dep2Right.SetQuantity("LengthM", 1d);
            left.Elements.Add(dep2Left); right.Elements.Add(dep2Right);
            leftElement.DependsOn.Add("E-BASE-2");
            rightElement.DependsOn.Insert(0, "e-base-2");

            var diff = ProjectInterchangeSnapshotDiff.CompareJson(
                ProjectInterchangeJsonExporter.Build(left),
                ProjectInterchangeJsonExporter.Build(right));
            var change = diff.Changes.FirstOrDefault(x => x.ObjectKind == InterchangeSnapshotObjectKind.Element && x.Id == "E-1");
            if (change != null)
            {
                True(!change.Fields.Contains("sourceHandles"));
                True(!change.Fields.Contains("dependencies"));
            }
        }

        private static void CompareJsonIsValidationFirst()
        {
            var valid = ProjectInterchangeJsonExporter.Build(Project("E-1", "B-01", 5d));
            Throws<System.IO.InvalidDataException>(() => ProjectInterchangeSnapshotDiff.CompareJson("{\"format\":\"Wrong\"}", valid));
            Throws<System.IO.InvalidDataException>(() => ProjectInterchangeSnapshotDiff.CompareJson(valid, "{\"format\":\"Wrong\"}"));
        }

        private static void ResultCollectionsAreImmutable()
        {
            var left = Project("E-LEFT", "B-01", 5d);
            var right = Project("E-RIGHT", "B-01", 5d);
            var diff = ProjectInterchangeSnapshotDiff.CompareJson(
                ProjectInterchangeJsonExporter.Build(left),
                ProjectInterchangeJsonExporter.Build(right));
            Throws<NotSupportedException>(() => ((IList<InterchangeSnapshotChange>)diff.Changes).Clear());
            var changed = diff.Changes.FirstOrDefault(x => x.ChangeKind == InterchangeSnapshotChangeKind.Changed);
            if (changed != null) Throws<NotSupportedException>(() => ((IList<string>)changed.Fields).Clear());
        }

        private static ProjectState Project(string elementId, string mark, double length)
        {
            var project = new ProjectState("P-DIFF", "Diff Project")
            {
                DrawingFingerprint = "drawing-fp",
                UpdatedUtc = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone 01"));
            project.Floors.Add(new FloorDefinition("F-01", "Floor 01", 3d));
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

            var element = new ProjectElement(elementId, ElementCategory.Beam, "FAM-B", "F-01", "Z-01")
            {
                DrawingFingerprint = "drawing-fp"
            };
            element.SourceHandles.Add("AAAA");
            element.DependsOn.Add("E-BASE");
            element.SetProperty("Mark", mark);
            element.SetQuantity("LengthM", length);
            project.Elements.Add(element);
            return project;
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
