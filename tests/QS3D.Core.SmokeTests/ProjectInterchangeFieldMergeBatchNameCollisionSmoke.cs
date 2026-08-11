using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeFieldMergeBatchNameCollisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SelectedDuplicateSourceNamesBlockSameScope();
            FamilyDuplicateSourceNamesRemainCategoryScoped();
        }

        private static void SelectedDuplicateSourceNamesBlockSameScope()
        {
            var target = new ProjectState("TARGET-FIELD-NAME-BATCH", "Target field name batch");
            target.Zones.Add(new ZoneDefinition("ZA", "Target Zone A"));
            target.Zones.Add(new ZoneDefinition("ZB", "Target Zone B"));
            target.Floors.Add(new FloorDefinition("FA", "Target Floor A", 0d));
            target.Floors.Add(new FloorDefinition("FB", "Target Floor B", 0d));
            target.Families.Add(new ProjectFamily("FAMA", "Target Beam A", ElementCategory.Beam));
            target.Families.Add(new ProjectFamily("FAMB", "Target Beam B", ElementCategory.Beam));

            var source = new ProjectState("SOURCE-FIELD-NAME-BATCH", "Source field name batch");
            source.Zones.Add(new ZoneDefinition("ZA", "Shared Zone"));
            source.Zones.Add(new ZoneDefinition("ZB", "Shared Zone"));
            source.Floors.Add(new FloorDefinition("FA", "Shared Floor", 0d));
            source.Floors.Add(new FloorDefinition("FB", "Shared Floor", 0d));
            source.Families.Add(new ProjectFamily("FAMA", "Shared Beam", ElementCategory.Beam));
            source.Families.Add(new ProjectFamily("FAMB", "Shared Beam", ElementCategory.Beam));

            var plan = ProjectInterchangeFieldMergePlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                new ProjectInterchangeFieldMergePolicy
                {
                    ZoneName = InterchangeFieldPrecedenceChoice.UseSource,
                    FloorName = InterchangeFieldPrecedenceChoice.UseSource,
                    FamilyName = InterchangeFieldPrecedenceChoice.UseSource
                });

            True(plan.HasBlocks);
            False(plan.CanProceedToMutationDesign);
            True(plan.Blockers.Any(x => x.IndexOf("Zone field merge", StringComparison.OrdinalIgnoreCase) >= 0));
            True(plan.Blockers.Any(x => x.IndexOf("Floor field merge", StringComparison.OrdinalIgnoreCase) >= 0));
            True(plan.Blockers.Any(x => x.IndexOf("Family field merge", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static void FamilyDuplicateSourceNamesRemainCategoryScoped()
        {
            var target = new ProjectState("TARGET-FIELD-NAME-SCOPE", "Target field name scope");
            target.Families.Add(new ProjectFamily("BEAM", "Target Beam", ElementCategory.Beam));
            target.Families.Add(new ProjectFamily("COLUMN", "Target Column", ElementCategory.Column));

            var source = new ProjectState("SOURCE-FIELD-NAME-SCOPE", "Source field name scope");
            source.Families.Add(new ProjectFamily("BEAM", "Shared Family", ElementCategory.Beam));
            source.Families.Add(new ProjectFamily("COLUMN", "Shared Family", ElementCategory.Column));

            var plan = ProjectInterchangeFieldMergePlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                new ProjectInterchangeFieldMergePolicy
                {
                    FamilyName = InterchangeFieldPrecedenceChoice.UseSource
                });

            False(plan.HasBlocks);
            False(plan.HasUnresolvedDecisions);
            True(plan.CanProceedToMutationDesign);
            Equal(2, plan.Decisions.Count(x => x.Kind == InterchangeIdentityKind.Family && x.Field == "name"));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeFieldMergeBatchNameCollisionSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("ProjectInterchangeFieldMergeBatchNameCollisionSmoke assertion failed.");
        }

        private static void False(bool condition)
        {
            if (condition)
                throw new InvalidOperationException("ProjectInterchangeFieldMergeBatchNameCollisionSmoke expected false.");
        }
    }
}
