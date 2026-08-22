using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeImportResolutionNameCollisionSmoke
    {
        internal static void Run()
        {
            DuplicateIncomingNamesBlockBeforeMutationDesign();
            CrossCategoryFamilyNamesRemainValid();
            KeepTargetIgnoresRuntimeIncompatibleSourceValues();
            UseSourceBlocksRuntimeIncompatibleReplacementValues();
            NewCatalogIdentityOverRuntimeLimitIsBlocked();
        }

        private static void DuplicateIncomingNamesBlockBeforeMutationDesign()
        {
            var target = NewTarget();
            var source = new ProjectState("source", "Source");
            source.Zones.Add(new ZoneDefinition("ZA", "Shared Zone"));
            source.Zones.Add(new ZoneDefinition("ZB", "Shared Zone"));
            source.Floors.Add(new FloorDefinition("FA", "Shared Floor", 0d));
            source.Floors.Add(new FloorDefinition("FB", "Shared Floor", 3d));
            source.Families.Add(new ProjectFamily("FAMA", "Shared Family", ElementCategory.Beam));
            source.Families.Add(new ProjectFamily("FAMB", "Shared Family", ElementCategory.Beam));

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source), KeepTargetPolicy());

            True(plan.HasBlocks);
            False(plan.CanProceedToMutationDesign);
            Equal(2, plan.Items.Count(x => x.Kind == InterchangeIdentityKind.Zone && x.Action == InterchangeImportResolutionAction.BlockedIncompatible));
            Equal(2, plan.Items.Count(x => x.Kind == InterchangeIdentityKind.Floor && x.Action == InterchangeImportResolutionAction.BlockedIncompatible));
            Equal(2, plan.Items.Count(x => x.Kind == InterchangeIdentityKind.Family && x.Action == InterchangeImportResolutionAction.BlockedIncompatible));
        }

        private static void CrossCategoryFamilyNamesRemainValid()
        {
            var target = NewTarget();
            var source = new ProjectState("source", "Source");
            source.Families.Add(new ProjectFamily("BEAM-FAM", "Shared Family", ElementCategory.Beam));
            source.Families.Add(new ProjectFamily("COLUMN-FAM", "Shared Family", ElementCategory.Column));

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source), KeepTargetPolicy());

            False(plan.HasBlocks);
            True(plan.CanProceedToMutationDesign);
            Equal(2, plan.Items.Count(x => x.Kind == InterchangeIdentityKind.Family && x.Action == InterchangeImportResolutionAction.AddSourceSemanticData));
        }

        private static void KeepTargetIgnoresRuntimeIncompatibleSourceValues()
        {
            var target = NewTarget();
            var source = new ProjectState("source", "Source");
            source.Zones.Add(new ZoneDefinition("TARGET-ZONE", new string('Z', 121)));
            var family = new ProjectFamily("TARGET-FAM", "Source Family", ElementCategory.Column);
            family.Properties["LongValue"] = new string('X', 1001);
            source.Families.Add(family);

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source), KeepTargetPolicy());

            False(plan.HasBlocks);
            True(plan.CanProceedToMutationDesign);
            Equal(InterchangeImportResolutionAction.KeepTarget, Item(plan, InterchangeIdentityKind.Zone, "TARGET-ZONE").Action);
            Equal(InterchangeImportResolutionAction.KeepTarget, Item(plan, InterchangeIdentityKind.Family, "TARGET-FAM").Action);
        }

        private static void UseSourceBlocksRuntimeIncompatibleReplacementValues()
        {
            var target = NewTarget();
            var source = new ProjectState("source", "Source");
            source.Zones.Add(new ZoneDefinition("TARGET-ZONE", new string('Z', 121)));
            var family = new ProjectFamily("TARGET-FAM", "Source Family", ElementCategory.Column);
            family.Properties["LongValue"] = new string('X', 1001);
            source.Families.Add(family);

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source), UseSourcePolicy());

            True(plan.HasBlocks);
            False(plan.CanProceedToMutationDesign);
            Equal(InterchangeImportResolutionAction.BlockedIncompatible, Item(plan, InterchangeIdentityKind.Zone, "TARGET-ZONE").Action);
            Equal(InterchangeImportResolutionAction.BlockedIncompatible, Item(plan, InterchangeIdentityKind.Family, "TARGET-FAM").Action);
        }

        private static void NewCatalogIdentityOverRuntimeLimitIsBlocked()
        {
            var target = NewTarget();
            var source = new ProjectState("source", "Source");
            var longZoneId = new string('Q', 65);
            source.Zones.Add(new ZoneDefinition(longZoneId, "Source Zone"));

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(target, ProjectInterchangeJsonExporter.Build(source), KeepTargetPolicy());

            True(plan.HasBlocks);
            False(plan.CanProceedToMutationDesign);
            var item = Item(plan, InterchangeIdentityKind.Zone, longZoneId);
            Equal(InterchangeImportResolutionAction.BlockedIncompatible, item.Action);
            True(item.Reason.IndexOf("runtime", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static ProjectState NewTarget()
        {
            var target = new ProjectState("target", "Target");
            target.Zones.Add(new ZoneDefinition("TARGET-ZONE", "Target Zone"));
            target.Floors.Add(new FloorDefinition("TARGET-FLOOR", "Target Floor", 0d));
            target.Families.Add(new ProjectFamily("TARGET-FAM", "Target Family", ElementCategory.Column));
            target.Elements.Add(new ProjectElement("TARGET-ELEM", ElementCategory.Column, "TARGET-FAM", "TARGET-FLOOR", "TARGET-ZONE"));
            return target;
        }

        private static ProjectInterchangeImportPolicy KeepTargetPolicy() => new ProjectInterchangeImportPolicy
        {
            ZoneCollision = InterchangeExistingIdentityAction.KeepTarget,
            FloorCollision = InterchangeExistingIdentityAction.KeepTarget,
            FamilyCollision = InterchangeExistingIdentityAction.KeepTarget,
            ElementCollision = InterchangeExistingIdentityAction.KeepTarget,
            ProjectId = InterchangeProjectIdPolicy.AllowDifferent,
            DrawingFingerprint = InterchangeDrawingFingerprintPolicy.AllowDifferentOrUnknown,
            SourceHandles = InterchangeSourceHandlePolicy.Discard,
            GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild
        };

        private static ProjectInterchangeImportPolicy UseSourcePolicy() => new ProjectInterchangeImportPolicy
        {
            ZoneCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
            FloorCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
            FamilyCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
            ElementCollision = InterchangeExistingIdentityAction.UseSourceSemanticData,
            ProjectId = InterchangeProjectIdPolicy.AllowDifferent,
            DrawingFingerprint = InterchangeDrawingFingerprintPolicy.AllowDifferentOrUnknown,
            SourceHandles = InterchangeSourceHandlePolicy.Discard,
            GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild
        };

        private static InterchangeImportResolutionItem Item(ProjectInterchangeImportResolutionPlan plan, InterchangeIdentityKind kind, string id) =>
            plan.Items.Single(x => x.Kind == kind && string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected condition to be false.");
        }
    }

    internal static class ProjectInterchangeImportResolutionNameCollisionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeImportResolutionNameCollisionSmoke.Run();
    }
}
