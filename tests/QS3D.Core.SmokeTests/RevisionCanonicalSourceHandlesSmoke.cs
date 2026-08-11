using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionCanonicalSourceHandlesSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalHandlesAreSortedWithoutMutation();
            CompareIgnoresHandleOrderAndCase();
            CompareReportsRealHandleSetChange();
            BlankHandleFailsClosed();
            PaddedHandleFailsClosed();
            DuplicateHandleFailsClosed();
        }

        private static void CanonicalHandlesAreSortedWithoutMutation()
        {
            var project = Project();
            var element = project.FindElement("E1")!;
            element.SourceHandles.Add("BB");
            element.SourceHandles.Add("AA");
            var before = element.SourceHandles.ToArray();

            var snapshot = new RevisionService().Capture(project, "r1");
            var captured = snapshot.Elements.Single().SourceHandles;
            Equal(2, captured.Count);
            Equal("AA", captured[0]);
            Equal("BB", captured[1]);
            Equal(before[0], element.SourceHandles[0]);
            Equal(before[1], element.SourceHandles[1]);
        }

        private static void CompareIgnoresHandleOrderAndCase()
        {
            var before = Snapshot("AA", "BB");
            var after = Snapshot("bb", "aa");
            var deltas = new RevisionService().Compare(before, after);
            Equal(0, deltas.Count);
        }

        private static void CompareReportsRealHandleSetChange()
        {
            var before = Snapshot("AA", "BB");
            var after = Snapshot("AA", "CC");
            var deltas = new RevisionService().Compare(before, after);
            Equal(1, deltas.Count);
            Equal(1, deltas[0].Fields.Count);
            Equal("SourceHandles", deltas[0].Fields[0].Field);
            Equal("AA,BB", deltas[0].Fields[0].Before);
            Equal("AA,CC", deltas[0].Fields[0].After);
        }

        private static void BlankHandleFailsClosed()
        {
            var project = Project();
            project.FindElement("E1")!.SourceHandles.Add(" ");
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "r"));
        }

        private static void PaddedHandleFailsClosed()
        {
            var project = Project();
            project.FindElement("E1")!.SourceHandles.Add(" AA ");
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "r"));
        }

        private static void DuplicateHandleFailsClosed()
        {
            var project = Project();
            var element = project.FindElement("E1")!;
            element.SourceHandles.Add("AA");
            element.SourceHandles.Add("aa");
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "r"));
        }

        private static RevisionSnapshot Snapshot(params string[] handles)
        {
            var snapshot = new RevisionSnapshot { Id = "r", CreatedUtc = DateTime.UtcNow };
            var element = new RevisionElementSnapshot
            {
                ElementId = "E1",
                Category = ElementCategory.Beam.ToString(),
                FamilyId = string.Empty,
                FloorId = "F",
                ZoneId = "Z"
            };
            foreach (var handle in handles) element.SourceHandles.Add(handle);
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("P-REV-HANDLES", "Revision Handles");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam, string.Empty, "F", "Z"));
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
