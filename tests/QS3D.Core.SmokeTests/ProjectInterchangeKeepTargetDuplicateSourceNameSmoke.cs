using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeKeepTargetDuplicateSourceNameSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DuplicateIncomingNamesDoNotBlockDiscardedCollisions();
        }

        private static void DuplicateIncomingNamesDoNotBlockDiscardedCollisions()
        {
            var target = new ProjectState("TARGET-KEEP-DUP-NAME", "Target keep duplicate names");
            target.Zones.Add(new ZoneDefinition("ZA", "Target Zone A"));
            target.Zones.Add(new ZoneDefinition("ZB", "Target Zone B"));
            target.Floors.Add(new FloorDefinition("FA", "Target Floor A", 0d));
            target.Floors.Add(new FloorDefinition("FB", "Target Floor B", 3d));
            target.Families.Add(new ProjectFamily("FAMA", "Target Beam A", ElementCategory.Beam));
            target.Families.Add(new ProjectFamily("FAMB", "Target Beam B", ElementCategory.Beam));

            var source = new ProjectState("SOURCE-KEEP-DUP-NAME", "Source keep duplicate names");
            source.Zones.Add(new ZoneDefinition("ZA", "Shared Source Zone"));
            source.Zones.Add(new ZoneDefinition("ZB", "Shared Source Zone"));
            source.Floors.Add(new FloorDefinition("FA", "Shared Source Floor", 1d));
            source.Floors.Add(new FloorDefinition("FB", "Shared Source Floor", 4d));
            source.Families.Add(new ProjectFamily("FAMA", "Shared Source Beam", ElementCategory.Beam));
            source.Families.Add(new ProjectFamily("FAMB", "Shared Source Beam", ElementCategory.Beam));

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                KeepTargetPolicy());

            False(plan.HasBlocks);
            False(plan.HasUnresolvedPolicy);
            True(plan.CanProceedToMutationDesign);
            Equal(6, plan.Items.Count);
            Equal(6, plan.Items.Count(x => x.Action == InterchangeImportResolutionAction.KeepTarget));
            Equal(0, plan.Items.Count(x => x.Action == InterchangeImportResolutionAction.BlockedIncompatible));
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

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeKeepTargetDuplicateSourceNameSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("ProjectInterchangeKeepTargetDuplicateSourceNameSmoke assertion failed.");
        }

        private static void False(bool condition)
        {
            if (condition)
                throw new InvalidOperationException("ProjectInterchangeKeepTargetDuplicateSourceNameSmoke expected false.");
        }
    }
}
