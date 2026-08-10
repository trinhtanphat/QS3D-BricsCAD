using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeImportResolutionPlannerSmoke
    {
        public static void Run()
        {
            NoImplicitPolicyDefaultsAreAllowed();
            AllNewIdentitiesCanBeResolvedWithoutTargetMutation();
            ExistingElementSourceReplacementRequiresGeneratedReset();
            KeepTargetDoesNotRequireGeneratedReset();
            CategoryMismatchIsBlockedRegardlessOfPolicy();
            ProjectAndFingerprintRequirementsBlockMismatches();
            SourceHandleDispositionIsExplicitProvenanceOnly();
            UnsupportedPolicyEnumFailsClosed();
        }

        private static void NoImplicitPolicyDefaultsAreAllowed()
        {
            var source = Project("source", "SRC-FP", "E-S", ElementCategory.Beam);
            var target = new ProjectState("target", "Target");
            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                new ProjectInterchangeImportPolicy());

            True(plan.HasUnresolvedPolicy);
            True(!plan.CanProceedToMutationDesign);
            True(plan.PolicyErrors.Any(x => x.Contains("ZoneCollision")));
            True(plan.PolicyErrors.Any(x => x.Contains("ProjectId")));
            True(plan.PolicyErrors.Any(x => x.Contains("SourceHandles")));
        }

        private static void AllNewIdentitiesCanBeResolvedWithoutTargetMutation()
        {
            var source = Project("source", "SRC-FP", "E-S", ElementCategory.Beam);
            var target = new ProjectState("target", "Target") { DrawingFingerprint = "TARGET-FP" };
            var beforeUpdated = target.UpdatedUtc;
            var policy = ExplicitPolicy();
            policy.ProjectId = InterchangeProjectIdPolicy.AllowDifferent;
            policy.DrawingFingerprint = InterchangeDrawingFingerprintPolicy.AllowDifferentOrUnknown;

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);

            True(!plan.HasUnresolvedPolicy);
            True(!plan.HasBlocks);
            True(plan.CanProceedToMutationDesign);
            Equal(4, plan.Items.Count);
            True(plan.Items.All(x => x.Action == InterchangeImportResolutionAction.AddSourceSemanticData));
            Equal(0, target.Zones.Count);
            Equal(0, target.Floors.Count);
            Equal(0, target.Families.Count);
            Equal(0, target.Elements.Count);
            Equal(beforeUpdated, target.UpdatedUtc);
        }

        private static void ExistingElementSourceReplacementRequiresGeneratedReset()
        {
            var source = Project("P", "FP", "E-1", ElementCategory.Beam);
            var target = Project("P", "FP", "E-1", ElementCategory.Beam);
            var policy = ExplicitPolicy();
            policy.ElementCollision = InterchangeExistingIdentityAction.UseSourceSemanticData;
            policy.GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.Unspecified;

            var blocked = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);
            True(blocked.HasUnresolvedPolicy);
            True(blocked.PolicyErrors.Any(x => x.Contains("ClearOwnershipAndRequireRebuild")));
            True(!blocked.CanProceedToMutationDesign);
            var element = blocked.Items.Single(x => x.Kind == InterchangeIdentityKind.Element);
            Equal(InterchangeImportResolutionAction.UseSourceSemanticData, element.Action);
            True(element.RequiresGeneratedOutputReset);

            policy.GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild;
            var resolved = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);
            True(!resolved.HasUnresolvedPolicy);
            True(resolved.CanProceedToMutationDesign);
            True(resolved.Items.Single(x => x.Kind == InterchangeIdentityKind.Element).RequiresGeneratedOutputReset);
        }

        private static void KeepTargetDoesNotRequireGeneratedReset()
        {
            var source = Project("P", "FP", "E-1", ElementCategory.Beam);
            var target = Project("P", "FP", "E-1", ElementCategory.Beam);
            var policy = ExplicitPolicy();
            policy.ElementCollision = InterchangeExistingIdentityAction.KeepTarget;
            policy.GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.Unspecified;

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);
            True(!plan.HasUnresolvedPolicy);
            var element = plan.Items.Single(x => x.Kind == InterchangeIdentityKind.Element);
            Equal(InterchangeImportResolutionAction.KeepTarget, element.Action);
            True(!element.RequiresGeneratedOutputReset);
        }

        private static void CategoryMismatchIsBlockedRegardlessOfPolicy()
        {
            var source = Project("P", "FP", "E-X", ElementCategory.Beam);
            var target = Project("P", "FP", "E-X", ElementCategory.Column);
            var policy = ExplicitPolicy();
            policy.FamilyCollision = InterchangeExistingIdentityAction.UseSourceSemanticData;
            policy.ElementCollision = InterchangeExistingIdentityAction.UseSourceSemanticData;
            policy.GeneratedOutputReset = InterchangeGeneratedOutputResetPolicy.ClearOwnershipAndRequireRebuild;

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);
            True(plan.HasBlocks);
            True(!plan.CanProceedToMutationDesign);
            True(plan.Items.Any(x => x.Kind == InterchangeIdentityKind.Family && x.Action == InterchangeImportResolutionAction.BlockedIncompatible));
            True(plan.Items.Any(x => x.Kind == InterchangeIdentityKind.Element && x.Action == InterchangeImportResolutionAction.BlockedIncompatible));
        }

        private static void ProjectAndFingerprintRequirementsBlockMismatches()
        {
            var source = Project("source", "SRC-FP", "E-S", ElementCategory.Beam);
            var target = new ProjectState("target", "Target") { DrawingFingerprint = "TARGET-FP" };
            var policy = ExplicitPolicy();
            policy.ProjectId = InterchangeProjectIdPolicy.RequireMatch;
            policy.DrawingFingerprint = InterchangeDrawingFingerprintPolicy.RequireMatch;

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);
            Equal(2, plan.GlobalBlocks.Count);
            True(plan.HasBlocks);
            True(!plan.CanProceedToMutationDesign);
            Equal(InterchangeDrawingFingerprintRelation.Different, plan.DrawingFingerprintRelation);
        }

        private static void SourceHandleDispositionIsExplicitProvenanceOnly()
        {
            var source = Project("source", "FP", "E-S", ElementCategory.Beam);
            source.Elements[0].SourceHandles.Add("ABC");
            var target = new ProjectState("target", "Target");
            var policy = ExplicitPolicy();
            policy.SourceHandles = InterchangeSourceHandlePolicy.PreserveAsProvenanceOnly;

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);
            Equal(InterchangeSourceHandlePolicy.PreserveAsProvenanceOnly, plan.SourceHandlePolicy);
            True(plan.CanProceedToMutationDesign);
        }

        private static void UnsupportedPolicyEnumFailsClosed()
        {
            var source = Project("source", "FP", "E-S", ElementCategory.Beam);
            var policy = ExplicitPolicy();
            policy.ZoneCollision = (InterchangeExistingIdentityAction)99;
            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                new ProjectState("target", "Target"),
                ProjectInterchangeJsonExporter.Build(source),
                policy);
            True(plan.HasUnresolvedPolicy);
            True(plan.PolicyErrors.Any(x => x.Contains("unsupported policy value")));
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

        private static ProjectState Project(string projectId, string fingerprint, string elementId, ElementCategory category)
        {
            var project = new ProjectState(projectId, "Project " + projectId)
            {
                DrawingFingerprint = fingerprint,
                UpdatedUtc = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z-1", "Zone"));
            project.Floors.Add(new FloorDefinition("F-1", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FAM-1", "Family", category));
            var element = new ProjectElement(elementId, category, "FAM-1", "F-1", "Z-1")
            {
                DrawingFingerprint = fingerprint
            };
            element.SetProperty("Mark", "M");
            element.SetQuantity("LengthM", 1d);
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
    }
}
