using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeImportResolutionCapacityBlockSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CatalogAdditionBeyondRuntimeCapacityBlocksBeforeMutationDesign();
        }

        private static void CatalogAdditionBeyondRuntimeCapacityBlocksBeforeMutationDesign()
        {
            var target = new ProjectState("TARGET-CAPACITY", "Target capacity");
            for (var i = 0; i < 2000; i++)
            {
                var suffix = i.ToString("D4", CultureInfo.InvariantCulture);
                target.Zones.Add(new ZoneDefinition("Z" + suffix, "Zone " + suffix));
            }

            var source = new ProjectState("SOURCE-CAPACITY", "Source capacity");
            source.Zones.Add(new ZoneDefinition("Z-OVERFLOW", "Overflow Zone"));

            var plan = ProjectInterchangeImportResolutionPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                KeepTargetPolicy());

            True(plan.HasBlocks);
            False(plan.CanProceedToMutationDesign);
            True(plan.GlobalBlocks.Any(x =>
                x.IndexOf("2001 Zone identities", StringComparison.OrdinalIgnoreCase) >= 0 &&
                x.IndexOf("2000", StringComparison.OrdinalIgnoreCase) >= 0));
            Equal(
                InterchangeImportResolutionAction.AddSourceSemanticData,
                plan.Items.Single(x => x.Kind == InterchangeIdentityKind.Zone && x.Id == "Z-OVERFLOW").Action);
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
                throw new InvalidOperationException("ProjectInterchangeImportResolutionCapacityBlockSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("ProjectInterchangeImportResolutionCapacityBlockSmoke assertion failed.");
        }

        private static void False(bool condition)
        {
            if (condition)
                throw new InvalidOperationException("ProjectInterchangeImportResolutionCapacityBlockSmoke expected false.");
        }
    }
}
