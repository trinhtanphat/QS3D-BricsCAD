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
            var source = SourceProject("source", "SRC-FP", "Z-S", "F-S", "FAM-S", "E-S", ElementCategory.ArchitecturalWall);
            var target = new ProjectState("target", "Target") { DrawingFingerprint = "TGT-FP" };
            var targetUpdated = new DateTime(2026, 8, 10, 1, 2, 3, DateTimeKind.Utc);
            target.UpdatedUtc = targetUpdated;
            var beforeName = target.Name;
            var json = ProjectInterchangeJsonExporter.Build(source);
            var preview = ProjectInterchangeImportPreview.Plan(target, json);
            True(preview.Validation.IsValid);
            Equal(4, preview.TotalIdentityCount);
            Equal(4, preview.NewIdentityCount);
            Equal(0, preview.CollisionCount);
            True(!preview.RequiresIdentityPolicy);
            Equal(InterchangeDrawingFingerprintRelation.Different, preview.DrawingFingerprintRelation);
            Equal(beforeName, target.Name);
            Equal(0, target.Zones.Count);
            Equal(0, target.Floors.Count);
            Equal(0, target.Families.Count);
            Equal(0, target.Elements.Count);
            Equal(targetUpdated, target.UpdatedUtc);
        }

        private static void ExistingSameCategoryRequiresPolicy()
        {
            var source = SourceProject("source", "FP", "Z-1", "F-1", "FAM-1", "E-1", ElementCategory.Beam);
            var target = SourceProject("target", "FP", "Z-1", "F-1", "FAM-1", "E-1", ElementCategory.Beam);
            var preview = ProjectInterchangeImportPreview.Plan(target, ProjectInterchangeJsonExporter.Build(source));
            Equal(4, preview.PolicyCollisionCount);
            Equal(0, preview.IncompatibleCollisionCount);
            True(preview.RequiresIdentityPolicy);
            True(preview.Items.All(x => x.Disposition == InterchangeIdentityDisposition.ExistingNeedsPolicy));
        }

        private static void CategoryMismatchIsIncompatible()
        {
            var source = SourceProject("source", "FP", "Z-S", "F-S", "FAM-X", "E-X", ElementCategory.ArchitecturalWall);
            var target = SourceProject("target", "FP", "Z-T", "F-T", "FAM-X", "E-X", ElementCategory.Column);
            var preview = ProjectInterchangeImportPreview.Plan(target, ProjectInterchangeJsonExporter.Build(source));
            Equal(2, preview.IncompatibleCollisionCount);
            True(preview.Items.Any(x => x.Kind == InterchangeIdentityKind.Family && x.Disposition == InterchangeIdentityDisposition.ExistingIncompatible));
            True(preview.Items.Any(x => x.Kind == InterchangeIdentityKind.Element && x.Disposition == InterchangeIdentityDisposition.ExistingIncompatible));
        }

        private static void InvalidSnapshotStopsBeforeCollisionPlanning()
        {
            var preview = ProjectInterchangeImportPreview.Plan(new ProjectState("target", "Target"), "{\"format\":\"Wrong\"}");
            True(!preview.Validation.IsValid);
            Equal(0, preview.TotalIdentityCount);
            Equal(0, preview.Items.Count);
            Equal(0, preview.CollisionCount);
        }

        private static void FingerprintRelationIsDescriptive()
        {
            var source = SourceProject("source", "SAME", "Z-S", "F-S", "FAM-S", "E-S", ElementCategory.Slab);
            var json = ProjectInterchangeJsonExporter.Build(source);
            Equal(InterchangeDrawingFingerprintRelation.Match, ProjectInterchangeImportPreview.Plan(new ProjectState("target", "T") { DrawingFingerprint = "SAME" }, json).DrawingFingerprintRelation);
            Equal(InterchangeDrawingFingerprintRelation.Different, ProjectInterchangeImportPreview.Plan(new ProjectState("target", "T") { DrawingFingerprint = "OTHER" }, json).DrawingFingerprintRelation);
            Equal(InterchangeDrawingFingerprintRelation.Unknown, ProjectInterchangeImportPreview.Plan(new ProjectState("target", "T"), json).DrawingFingerprintRelation);
        }

        private static void AmbiguousTargetIdsFailClosed()
        {
            var source = SourceProject("source", "FP", "Z-S", "F-S", "FAM-S", "E-S", ElementCategory.ArchitecturalWall);
            var target = new ProjectState("target", "Target");
            target.Zones.Add(new ZoneDefinition("DUP", "First"));
            target.Zones.Add(new ZoneDefinition("dup", "Second"));
            Throws<InvalidOperationException>(() => ProjectInterchangeImportPreview.Plan(target, ProjectInterchangeJsonExporter.Build(source)));
        }

        private static ProjectState SourceProject(string projectId, string fingerprint, string zoneId, string floorId, string familyId, string elementId, ElementCategory category)
        {
            var project = new ProjectState(projectId, "Project " + projectId)
            {
                DrawingFingerprint = fingerprint,
                UpdatedUtc = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition(zoneId, "Zone"));
            project.Floors.Add(new FloorDefinition(floorId, "Floor", 0));
            project.Families.Add(new ProjectFamily(familyId, "Family", category));
            project.Elements.Add(new ProjectElement(elementId, category, familyId, floorId, zoneId) { DrawingFingerprint = fingerprint });
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
