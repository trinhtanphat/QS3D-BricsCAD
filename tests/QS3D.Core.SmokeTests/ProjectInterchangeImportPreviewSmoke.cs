using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeImportPreviewSmoke
    {
        public static void Run()
        {
            NewIdentitiesArePreviewedWithoutMutation();
            ExistingSameCategoryRequiresPolicy();
            CategoryMismatchIsIncompatible();
            InvalidSnapshotStopsBeforeCollisionPlanning();
            FingerprintRelationIsDescriptive();
            AmbiguousTargetIdsFailClosed();
        }

        private static void NewIdentitiesArePreviewedWithoutMutation()
        {
            var source = SourceProject("source", "SRC-FP", "Z-S", "F-S", "FAM-S", "E-S", ElementCategory.Wall);
            var target = new ProjectState("target", "Target") { DrawingFingerprint = "TGT-FP" };
            var targetUpdated = new DateTime(2026, 8, 10, 1, 2, 3, DateTimeKind.Utc);
            target.UpdatedUtc = targetUpdated;

            var beforeName = target.Name;
            var beforeZoneCount = target.Zones.Count;
            var beforeFloorCount = target.Floors.Count;
            var beforeFamilyCount = target.Families.Count;
            var beforeElementCount = target.Elements.Count;
            var json = ProjectInterchangeJsonExporter.Build(source);

            var preview = ProjectInterchangeImportPreview.Plan(target, json);

            True(preview.Validation.IsValid);
            Equal(4, preview.TotalIdentityCount);
            Equal(4, preview.NewIdentityCount);
            Equal(0, preview.CollisionCount);
            True(!preview.RequiresIdentityPolicy);
            Equal(InterchangeDrawingFingerprintRelation.Different, preview.DrawingFingerprintRelation);
            Equal(beforeName, target.Name);
            Equal(beforeZoneCount, target.Zones.Count);
            Equal(beforeFloorCount, target.Floors.Count);
            Equal(beforeFamilyCount, target.Families.Count);
            Equal(beforeElementCount, target.Elements.Count);
            Equal(targetUpdated, target.UpdatedUtc);
        }

        private static void ExistingSameCategoryRequiresPolicy()
        {
            var source = SourceProject("source", "FP", "Z-1", "F-1", "FAM-1", "E-1", ElementCategory.Beam);
            var target = SourceProject("target", "FP", "Z-1", "F-1", "FAM-1", "E-1", ElementCategory.Beam);

            var preview = ProjectInterchangeImportPreview.Plan(target, ProjectInterchangeJsonExporter.Build(source));

            True(preview.Validation.IsValid);
            Equal(4, preview.TotalIdentityCount);
            Equal(0, preview.NewIdentityCount);
            Equal(4, preview.PolicyCollisionCount);
            Equal(0, preview.IncompatibleCollisionCount);
            True(preview.RequiresIdentityPolicy);
            True(preview.Items.All(x => x.Disposition == InterchangeIdentityDisposition.ExistingNeedsPolicy));
        }

        private static void CategoryMismatchIsIncompatible()
        {
            var source = SourceProject("source", "FP", "Z-S", "F-S", "FAM-X", "E-X", ElementCategory.Wall);
            var target = SourceProject("target", "FP", "Z-T", "F-T", "FAM-X", "E-X", ElementCategory.Column);

            var preview = ProjectInterchangeImportPreview.Plan(target, ProjectInterchangeJsonExporter.Build(source));

            Equal(2, preview.IncompatibleCollisionCount);
            True(preview.Items.Any(x => x.Kind == InterchangeIdentityKind.Family && x.Disposition == InterchangeIdentityDisposition.ExistingIncompatible));
            True(preview.Items.Any(x => x.Kind == InterchangeIdentityKind.Element && x.Disposition == InterchangeIdentityDisposition.ExistingIncompatible));
        }

        private static void InvalidSnapshotStopsBeforeCollisionPlanning()
        {
            var target = new ProjectState("target", "Target");
            var preview = ProjectInterchangeImportPreview.Plan(target, "{\"format\":\"Wrong\"}");

            True(!preview.Validation.IsValid);
            Equal(0, preview.TotalIdentityCount);
            Equal(0, preview.Items.Count);
            Equal(0, preview.CollisionCount);
        }

        private static void FingerprintRelationIsDescriptive()
        {
            var source = SourceProject("source", "SAME", "Z-S", "F-S", "FAM-S", "E-S", ElementCategory.Slab);
            var json = ProjectInterchangeJsonExporter.Build(source);

            var same = new ProjectState("target", "Target") { DrawingFingerprint = "SAME" };
            var different = new ProjectState("target", "Target") { DrawingFingerprint = "OTHER" };
            var unknown = new ProjectState("target", "Target") { DrawingFingerprint = string.Empty };

            Equal(InterchangeDrawingFingerprintRelation.Match, ProjectInterchangeImportPreview.Plan(same, json).DrawingFingerprintRelation);
            Equal(InterchangeDrawingFingerprintRelation.Different, ProjectInterchangeImportPreview.Plan(different, json).DrawingFingerprintRelation);
            Equal(InterchangeDrawingFingerprintRelation.Unknown, ProjectInterchangeImportPreview.Plan(unknown, json).DrawingFingerprintRelation);
        }

        private static void AmbiguousTargetIdsFailClosed()
        {
            var source = SourceProject("source", "FP", "Z-S", "F-S", "FAM-S", "E-S", ElementCategory.Wall);
            var target = new ProjectState("target", "Target");
            target.Zones.Add(new ZoneDefinition("DUP", "First"));
            target.Zones.Add(new ZoneDefinition("dup", "Second"));

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeImportPreview.Plan(target, ProjectInterchangeJsonExporter.Build(source)));
        }

        private static ProjectState SourceProject(
            string projectId,
            string fingerprint,
            string zoneId,
            string floorId,
            string familyId,
            string elementId,
            ElementCategory category)
        {
            var project = new ProjectState(projectId, "Project " + projectId)
            {
                DrawingFingerprint = fingerprint,
                UpdatedUtc = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition(zoneId, "Zone"));
            project.Floors.Add(new FloorDefinition(floorId, "Floor", 0));
            project.Families.Add(new ProjectFamily(familyId, "Family", category));
            var element = new ProjectElement(elementId, category, familyId, floorId, zoneId)
            {
                DrawingFingerprint = fingerprint
            };
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
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
