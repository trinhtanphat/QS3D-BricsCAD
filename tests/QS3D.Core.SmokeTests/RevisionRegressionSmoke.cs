using System;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Revisions

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionRegressionSmoke
    {
        public static void Run()
        {
            CaptureRejectsNonFiniteQuantities();
            SignedZeroIsCanonicalized();
            CaptureRejectsDuplicateElementIds();
            CaptureRejectsPaddedReferenceIds();
            CaptureRejectsNonCanonicalMapKeys();
            QuantityDiffRejectsOverflow();
            QuantityDiffRejectsNonCanonicalPayload();
            SummaryRejectsOverflow();
            DuplicateElementIdsAreRejected();
            PaddedElementIdsAreRejected();
            CompareRejectsPaddedReferenceIds();
            CompareRejectsMalformedElementPayload();
            CaptureRecordsProjectIdentity();
            CompareAllowsSameProjectCapturedSnapshots();
            CompareRejectsLegacyBaselineAgainstCapturedRevision();
            CompareRejectsCrossProjectBaseline();
        }

        private static void CaptureRejectsNonFiniteQuantities()
        {
            var project = NewProject();
            var element = new ProjectElement("REV-BAD", ElementCategory.CustomQuantity, string.Empty, "f", "z");
            element.Quantities["Bad"] = double.NaN;
            project.Elements.Add(element);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(project, "bad"));
        }

        private static void SignedZeroIsCanonicalized()
        {
            var project = NewProject();
            var element = new ProjectElement("REV-ZERO", ElementCategory.CustomQuantity, string.Empty, "f", "z");
            element.Quantities["Zero"] = -0d;
            project.Elements.Add(element);

            var captured = new RevisionService().Capture(project, "signed-zero");
            PositiveZero(captured.Elements[0].Quantities["Zero"]);

            var row = new QuantityRevisionRow
            {
                ElementId = "REV-ZERO",
                QuantityName = "Zero",
                Before = 0d,
                After = -0d
            };
            PositiveZero(row.Delta);
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
            var familyElement = new ProjectElement("E-FAMILY", ElementCategory.Beam, "F1", "f", "z");
            SetRawRelation(familyElement, "_familyId", " F1 ");
            familyProject.Elements.Add(familyElement);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(familyProject, "padded-family"));

            var floorProject = NewProject();
            var floorElement = new ProjectElement("E-FLOOR", ElementCategory.Beam, string.Empty, "f", "z");
            SetRawRelation(floorElement, "_floorId", " f ");
            floorProject.Elements.Add(floorElement);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(floorProject, "padded-floor"));

            var zoneProject = NewProject();
            var zoneElement = new ProjectElement("E-ZONE", ElementCategory.Beam, string.Empty, "f", "z");
            SetRawRelation(zoneElement, "_zoneId", " z ");
            zoneProject.Elements.Add(zoneElement);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(zoneProject, "padded-zone"));
        }

        private static void CaptureRejectsNonCanonicalMapKeys()
        {
            var propertyProject = NewProject();
            var propertyElement = new ProjectElement("E-PROP", ElementCategory.Beam, string.Empty, "f", "z");
            propertyElement.Properties[" Mark "] = "B1";
            propertyProject.Elements.Add(propertyElement);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(propertyProject, "padded-property-key"));

            var quantityProject = NewProject();
            var quantityElement = new ProjectElement("E-QUANTITY", ElementCategory.Beam, string.Empty, "f", "z");
            quantityElement.Quantities[" Q "] = 1d;
            quantityProject.Elements.Add(quantityElement);
            Throws<InvalidOperationException>(() => new RevisionService().Capture(quantityProject, "padded-quantity-key"));
        }

        private static void QuantityDiffRejectsOverflow()
        {
            var before = Snapshot("before", "E1", double.MaxValue);
            var after = Snapshot("after", "E1", -double.MaxValue);
            Throws<OverflowException>(() => new QuantityRevisionReport().Build(before, after));
            Throws<OverflowException>(() => new RevisionService().Compare(before, after));
        }

        private static void QuantityDiffRejectsNonCanonicalPayload()
        {
            var paddedBefore = Snapshot("quantity-padded-before", "E1", 1d);
            paddedBefore.Elements[0].Quantities.Clear();
            paddedBefore.Elements[0].Quantities[" Q "] = 1d;
            var paddedAfter = Snapshot("quantity-padded-after", "E1", 1d);
            paddedAfter.Elements[0].Quantities.Clear();
            paddedAfter.Elements[0].Quantities[" Q "] = 1d;
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(paddedBefore, paddedAfter));

            var badCategory = Snapshot("quantity-bad-category", "E1", 1d);
            badCategory.Elements[0].Category = "beam";
            var empty = new RevisionSnapshot { Id = "empty", CreatedUtc = DateTime.UtcNow };
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(empty, badCategory));

            var nonFinite = Snapshot("quantity-non-finite", "E1", double.NaN);
            Throws<InvalidOperationException>(() => new QuantityRevisionReport().Build(empty, nonFinite));
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

        private static void CompareRejectsMalformedElementPayload()
        {
            var empty = new RevisionSnapshot { Id = "empty", CreatedUtc = DateTime.UtcNow };

            var badCategory = Snapshot("compare-bad-category", "E1", 1d);
            badCategory.Elements[0].Category = "beam";
            Throws<InvalidOperationException>(() => new RevisionService().Compare(empty, badCategory));

            var badProperty = Snapshot("compare-bad-property", "E1", 1d);
            badProperty.Elements[0].Properties[" Mark "] = "B1";
            Throws<InvalidOperationException>(() => new RevisionService().Compare(empty, badProperty));

            var badQuantityKey = Snapshot("compare-bad-quantity-key", "E1", 1d);
            badQuantityKey.Elements[0].Quantities.Clear();
            badQuantityKey.Elements[0].Quantities[" Q "] = 1d;
            Throws<InvalidOperationException>(() => new RevisionService().Compare(empty, badQuantityKey));

            var nonFinite = Snapshot("compare-non-finite", "E1", double.NaN);
            Throws<InvalidOperationException>(() => new RevisionService().Compare(empty, nonFinite));

            var paddedSourceHandle = Snapshot("compare-padded-source", "E1", 1d);
            paddedSourceHandle.Elements[0].SourceHandles.Add(" H1 ");
            Throws<InvalidOperationException>(() => new RevisionService().Compare(empty, paddedSourceHandle));

            var duplicateDependency = Snapshot("compare-duplicate-dependency", "E1", 1d);
            duplicateDependency.Elements[0].Dependencies.Add("D1");
            duplicateDependency.Elements[0].Dependencies.Add("d1");
            Throws<InvalidOperationException>(() => new RevisionService().Compare(empty, duplicateDependency));
        }

        private static void CaptureRecordsProjectIdentity()
        {
            var project = NewProject();
            var snapshot = new RevisionService().Capture(project, "project-identity");
            if (!string.Equals(project.ProjectId, snapshot.ProjectId, StringComparison.Ordinal))
                throw new Exception("Captured revision did not preserve ProjectId.");
        }

        private static void CompareAllowsSameProjectCapturedSnapshots()
        {
            var project = NewProject();
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam, string.Empty, "f", "z"));
            var service = new RevisionService();
            var before = service.Capture(project, "before");
            var after = service.Capture(project, "after");
            var deltas = service.Compare(before, after);
            if (deltas.Count != 0)
                throw new Exception("Expected same-project revisions to compare without deltas.");
        }

        private static void CompareRejectsLegacyBaselineAgainstCapturedRevision()
        {
            var project = NewProject();
            var current = new RevisionService().Capture(project, "current");
            var legacyBaseline = new RevisionSnapshot { Id = "legacy", CreatedUtc = DateTime.UtcNow };

            Throws<InvalidOperationException>(() => new RevisionService().Compare(legacyBaseline, current));
        }

        private static void CompareRejectsCrossProjectBaseline()
        {
            var beforeProject = NewProject();
            var afterProject = new ProjectState("revision-regression-other", "Revision Regression Other");
            afterProject.Zones.Add(new ZoneDefinition("z", "Zone"));
            afterProject.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            afterProject.ActiveZoneId = "z";
            afterProject.ActiveFloorId = "f";

            var before = new RevisionService().Capture(beforeProject, "before-project");
            var after = new RevisionService().Capture(afterProject, "after-project");
            Throws<InvalidOperationException>(() => new RevisionService().Compare(before, after));
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

        private static void SetRawRelation(ProjectElement element, string fieldName, string value)
        {
            var field = typeof(ProjectElement).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("ProjectElement relation field " + fieldName + " was not found.");
            if (field.FieldType != typeof(string))
                throw new Exception("ProjectElement relation field " + fieldName + " must remain a string.");
            field.SetValue(element, value);
        }

        private static void PositiveZero(double value)
        {
            if (value != 0d || BitConverter.DoubleToInt64Bits(value) != 0L)
                throw new Exception("Expected canonical positive zero.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}