using System;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionRegressionSmoke
    {
        public static void Run()
        {
            CaptureRejectsNonFiniteQuantities();
            CaptureRejectsDuplicateElementIds();
            CaptureRejectsPaddedReferenceIds();
            QuantityDiffRejectsOverflow();
            SummaryRejectsOverflow();
            DuplicateElementIdsAreRejected();
            PaddedElementIdsAreRejected();
            CompareRejectsPaddedReferenceIds();
        }

        private static void CaptureRejectsNonFiniteQuantities()
        {
            var project = NewProject();
            var element = new ProjectElement("REV-BAD", ElementCategory.CustomQuantity, string.Empty, "f", "z");
            element.Quantities["Bad"] = double.NaN;
            project.Elements.Add(element);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "bad"));
        }

        private static void CaptureRejectsDuplicateElementIds()
        {
            var project = NewProject();
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam, string.Empty, "f", "z"));
            project.Elements.Add(new ProjectElement("e1", ElementCategory.Beam, string.Empty, "f", "z"));
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "duplicate-capture"));
        }

        private static void CaptureRejectsPaddedReferenceIds()
        {
            var familyProject = NewProject();
            var familyElement = new ProjectElement("E-FAMILY", ElementCategory.Beam, string.Empty, "f", "z") { FamilyId = " F1 " };
            familyProject.Elements.Add(familyElement);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(familyProject, "padded-family"));

            var floorProject = NewProject();
            var floorElement = new ProjectElement("E-FLOOR", ElementCategory.Beam, string.Empty, "f", "z") { FloorId = " f " };
            floorProject.Elements.Add(floorElement);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(floorProject, "padded-floor"));

            var zoneProject = NewProject();
            var zoneElement = new ProjectElement("E-ZONE", ElementCategory.Beam, string.Empty, "f", "z") { ZoneId = " z " };
            zoneProject.Elements.Add(zoneElement);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(zoneProject, "padded-zone"));
        }

        private static void QuantityDiffRejectsOverflow()
        {
            var before = Snapshot("before", "E1", double.MaxValue);
            var after = Snapshot("after", "E1", -double.MaxValue);
            Throws<OverflowException>(() => new QuantityRevisionReport().Build(before, after));
            Throws<OverflowException>(() => new RevisionService().Compare(before, after));
        }

        private static void SummaryRejectsOverflow()
        {
            var rows = new[]
            {
                new QuantityRevisionRow { ElementId = "E1", QuantityName = "Q", Before = double.MaxValue, After = 0d },
                new QuantityRevisionRow { ElementId = "E2", QuantityName = "Q", Before = double.MaxValue, After = 0d }
            };
            Throws<OverflowException>(() => new QuantityRevisionReport().Summarize(rows));
        }

        private static void DuplicateElementIdsAreRejected()
        {
            var snapshot = Snapshot("duplicate", "E1", 1d);
            var duplicate = new RevisionElementSnapshot { ElementId = "e1", Category = "Beam" };
            duplicate.Quantities["Q"] = 2d;
            snapshot.Elements.Add(duplicate);
            var empty = new RevisionSnapshot { Id = "empty", CreatedUtc = DateTime.UtcNow };
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(snapshot, empty));
            Throws<InvalidOperationException>(() => new RevisionService().Compare(snapshot, empty));
        }

        private static void PaddedElementIdsAreRejected()
        {
            var padded = Snapshot("padded", " E1 ", 1d);
            var canonical = Snapshot("canonical", "E1", 1d);
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(padded, canonical));
            Throws<InvalidOperationException>(() => new RevisionService().Compare(padded, canonical));

            var collision = Snapshot("collision", "E1", 1d);
            var paddedCollision = new RevisionElementSnapshot { ElementId = " E1 ", Category = "Beam" };
            paddedCollision.Quantities["Q"] = 2d;
            collision.Elements.Add(paddedCollision);
            var empty = new RevisionSnapshot { Id = "empty", CreatedUtc = DateTime.UtcNow };
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(collision, empty));
            Throws<InvalidOperationException>(() => new RevisionService().Compare(collision, empty));
        }

        private static void CompareRejectsPaddedReferenceIds()
        {
            var canonical = Snapshot("canonical-references", "E1", 1d);
            canonical.Elements[0].FamilyId = "F1";
            canonical.Elements[0].FloorId = "FLOOR-1";
            canonical.Elements[0].ZoneId = "ZONE-1";

            var paddedFamily = Snapshot("padded-family-reference", "E1", 1d);
            paddedFamily.Elements[0].FamilyId = " F1 ";
            Throws<InvalidOperationException>(() => new RevisionService().Compare(paddedFamily, canonical));

            var paddedFloor = Snapshot("padded-floor-reference", "E1", 1d);
            paddedFloor.Elements[0].FloorId = " FLOOR-1 ";
            Throws<InvalidOperationException>(() => new RevisionService().Compare(paddedFloor, canonical));

            var paddedZone = Snapshot("padded-zone-reference", "E1", 1d);
            paddedZone.Elements[0].ZoneId = " ZONE-1 ";
            Throws<InvalidOperationException>(() => new RevisionService().Compare(paddedZone, canonical));
        }

        private static RevisionSnapshot Snapshot(string id, string elementId, double quantity)
        {
            var snapshot = new RevisionSnapshot { Id = id, CreatedUtc = DateTime.UtcNow };
            var element = new RevisionElementSnapshot { ElementId = elementId, Category = "Beam" };
            element.Quantities["Q"] = quantity;
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("revision-regression", "Revision Regression");
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
