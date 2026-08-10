using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeImportNameCollisionSmoke
    {
        internal static void Run()
        {
            NewIdentityNameCollisionsAreBlocked();
            SameIdentityKeepTargetDoesNotApplyConflictingSourceName();
            SameIdentityUseSourceRenameCollisionIsBlocked();
            FamilyNameCollisionIsScopedByCategory();
        }

        private static void NewIdentityNameCollisionsAreBlocked()
        {
            var target = new ProjectState("target", "Target");
            target.Zones.Add(new ZoneDefinition("TZ", "Shared Zone"));
            target.Floors.Add(new FloorDefinition("TF", "Shared Floor", 0d));
            target.Families.Add(new ProjectFamily("TFAM", "Shared Family", ElementCategory.Beam));

            var source = Project(
                "source",
                "SZ",
                "Shared Zone",
                "SF",
                "Shared Floor",
                "SFAM",
                "Shared Family",
                ElementCategory.Beam,
                "SE");

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                ExplicitPolicy());

            True(plan.HasBlocks);
            True(!plan.CanProceedToMutationDesign);
            Blocked(plan, InterchangeIdentityKind.Zone, "SZ");
            Blocked(plan, InterchangeIdentityKind.Floor, "SF");
            Blocked(plan, InterchangeIdentityKind.Family, "SFAM");
            Equal(InterchangeImportResolutionAction.AddSourceSemanticData,
                plan.Items.Single(x => x.Kind == InterchangeIdentityKind.Element && x.Id == "SE").Action);
        }

        private static void SameIdentityKeepTargetDoesNotApplyConflictingSourceName()
        {
            var target = new ProjectState("target", "Target");
            target.Zones.Add(new ZoneDefinition("Z1", "Target Zone"));
            target.Zones.Add(new ZoneDefinition("Z2", "Source Zone"));
            target.Floors.Add(new FloorDefinition("F1", "Target Floor", 0d));
            target.Floors.Add(new FloorDefinition("F2", "Source Floor", 3d));
            target.Families.Add(new ProjectFamily("FAM1", "Target Family", ElementCategory.Beam));
            target.Families.Add(new ProjectFamily("FAM2", "Source Family", ElementCategory.Beam));

            var source = Project(
                "source",
                "Z1",
                "Source Zone",
                "F1",
                "Source Floor",
                "FAM1",
                "Source Family",
                ElementCategory.Beam,
                "SE");

            var policy = ExplicitPolicy();
            policy.ZoneCollision = InterchangeExistingIdentityAction.KeepTarget;
            policy.FloorCollision = InterchangeExistingIdentityAction.KeepTarget;
            policy.FamilyCollision = InterchangeExistingIdentityAction.KeepTarget;

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);

            True(!plan.HasBlocks);
            Equal(InterchangeImportResolutionAction.KeepTarget,
                plan.Items.Single(x => x.Kind == InterchangeIdentityKind.Zone && x.Id == "Z1").Action);
            Equal(InterchangeImportResolutionAction.KeepTarget,
                plan.Items.Single(x => x.Kind == InterchangeIdentityKind.Floor && x.Id == "F1").Action);
            Equal(InterchangeImportResolutionAction.KeepTarget,
                plan.Items.Single(x => x.Kind == InterchangeIdentityKind.Family && x.Id == "FAM1").Action);
        }

        private static void SameIdentityUseSourceRenameCollisionIsBlocked()
        {
            var target = new ProjectState("target", "Target");
            target.Zones.Add(new ZoneDefinition("Z1", "Target Zone"));
            target.Zones.Add(new ZoneDefinition("Z2", "Source Zone"));
            target.Floors.Add(new FloorDefinition("F1", "Target Floor", 0d));
            target.Floors.Add(new FloorDefinition("F2", "Source Floor", 3d));
            target.Families.Add(new ProjectFamily("FAM1", "Target Family", ElementCategory.Beam));
            target.Families.Add(new ProjectFamily("FAM2", "Source Family", ElementCategory.Beam));

            var source = Project(
                "source",
                "Z1",
                "Source Zone",
                "F1",
                "Source Floor",
                "FAM1",
                "Source Family",
                ElementCategory.Beam,
                "SE");

            var policy = ExplicitPolicy();
            policy.ZoneCollision = InterchangeExistingIdentityAction.UseSourceSemanticData;
            policy.FloorCollision = InterchangeExistingIdentityAction.UseSourceSemanticData;
            policy.FamilyCollision = InterchangeExistingIdentityAction.UseSourceSemanticData;

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);

            True(plan.HasBlocks);
            Blocked(plan, InterchangeIdentityKind.Zone, "Z1");
            Blocked(plan, InterchangeIdentityKind.Floor, "F1");
            Blocked(plan, InterchangeIdentityKind.Family, "FAM1");
        }

        private static void FamilyNameCollisionIsScopedByCategory()
        {
            var target = new ProjectState("target", "Target");
            target.Families.Add(new ProjectFamily("TFAM", "Shared Family", ElementCategory.Column));

            var source = Project(
                "source",
                "SZ",
                "Source Zone",
                "SF",
                "Source Floor",
                "SFAM",
                "Shared Family",
                ElementCategory.Beam,
                "SE");

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                ExplicitPolicy());

            var family = plan.Items.Single(x => x.Kind == InterchangeIdentityKind.Family && x.Id == "SFAM");
            Equal(InterchangeImportResolutionAction.AddSourceSemanticData, family.Action);
        }

        private static ProjectState Project(
            string projectId,
            string zoneId,
            string zoneName,
            string floorId,
            string floorName,
            string familyId,
            string familyName,
            ElementCategory category,
            string elementId)
        {
            var project = new ProjectState(projectId, "Project " + projectId);
            project.Zones.Add(new ZoneDefinition(zoneId, zoneName));
            project.Floors.Add(new FloorDefinition(floorId, floorName, 0d));
            project.Families.Add(new ProjectFamily(familyId, familyName, category));
            var element = new ProjectElement(elementId, category, familyId, floorId, zoneId);
            element.SetQuantity("LengthM", 1d);
            project.Elements.Add(element);
            return project;
        }

        private static ProjectInterchangeImportPolicy ExplicitPolicy()
        {
            return new ProjectInterchangeImportPolicy
            {
                ZoneCollision = InterchangeExistingIdentityAction.KeepTarget,
                FloorCollision = InterchangeExistingIdentityAction.KeepTarget,
                FamilyCollision = InterchangeExistingIdentityAction.KeepTarget,
                ElementCollision = InterchangeExistingIdentityAction.KeepTarget,
                ProjectId = InterchangeProjectIdPolicy.AllowDifferent,
                DrawingFingerprint = InterchangeDrawingFingerprintPolicy.AllowDifferentOrUnknown,
                SourceHandles = InterchangeSourceHandlePolicy.Discard,
                GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.Unspecified
            };
        }

        private static void Blocked(ProjectInterchangeImportResolutionPlan plan, InterchangeIdentityKind kind, string id)
        {
            var item = plan.Items.Single(x => x.Kind == kind && x.Id == id);
            Equal(InterchangeImportResolutionAction.BlockedIncompatible, item.Action);
            True(item.Reason.IndexOf("rename/remap", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }
    }

    internal static class ProjectInterchangeImportNameCollisionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeImportNameCollisionSmoke.Run();
    }
}
