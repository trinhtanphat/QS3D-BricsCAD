using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeFieldMergePlannerSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MixedPrecedenceIsDeterministicAndPreviewOnly();
            UnspecifiedPrecedenceFailsClosed();
            CategoryMismatchBlocksFieldMerge();
        }

        private static void MixedPrecedenceIsDeterministicAndPreviewOnly()
        {
            var target = BuildTarget();
            var source = BuildSource();
            var policy = new ProjectInterchangeFieldMergePolicy
            {
                ZoneName = InterchangeFieldPrecedenceChoice.KeepTarget,
                FloorName = InterchangeFieldPrecedenceChoice.UseSource,
                FloorElevation = InterchangeFieldPrecedenceChoice.UseSource,
                FamilyName = InterchangeFieldPrecedenceChoice.KeepTarget,
                FamilyProperties = InterchangeFieldPrecedenceChoice.UseSource,
                ElementFamily = InterchangeFieldPrecedenceChoice.KeepTarget,
                ElementFloor = InterchangeFieldPrecedenceChoice.KeepTarget,
                ElementZone = InterchangeFieldPrecedenceChoice.KeepTarget,
                ElementDependencies = InterchangeFieldPrecedenceChoice.UseSource,
                ElementProperties = InterchangeFieldPrecedenceChoice.UseSource,
                ElementQuantities = InterchangeFieldPrecedenceChoice.KeepTarget
            };

            var plan = ProjectInterchangeFieldMergePlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);

            True(plan.IsPreviewOnly);
            True(plan.CanProceedToMutationDesign);
            Equal(0, plan.UnresolvedDecisionCount);
            Equal(1, plan.SourceOnlyIdentityCount);
            True(plan.CollidingIdentityCount >= 4);
            True(plan.SourceChoiceCount > 0);
            True(plan.TargetChoiceCount > 0);
            True(plan.GeneratedOutputResetDecisionCount > 0);

            var zoneName = Decision(plan, InterchangeIdentityKind.Zone, "Z-01", "name");
            Equal(InterchangeFieldPrecedenceChoice.KeepTarget, zoneName.Choice);
            Equal("Target Zone", zoneName.TargetValue);
            Equal("Source Zone", zoneName.SourceValue);

            var floorElevation = Decision(plan, InterchangeIdentityKind.Floor, "F-01", "elevationM");
            Equal(InterchangeFieldPrecedenceChoice.UseSource, floorElevation.Choice);
            True(floorElevation.RequiresGeneratedOutputReset);

            var familyMaterial = Decision(plan, InterchangeIdentityKind.Family, "FAM-B", "properties.Material");
            Equal(InterchangeFieldPrecedenceChoice.UseSource, familyMaterial.Choice);
            Equal("C30", familyMaterial.TargetValue);
            Equal("C40", familyMaterial.SourceValue);

            var removedFamilyDefault = Decision(plan, InterchangeIdentityKind.Family, "FAM-B", "properties.LegacyDefault");
            True(removedFamilyDefault.TargetHasValue);
            True(!removedFamilyDefault.SourceHasValue);
            Equal(InterchangeFieldPrecedenceChoice.UseSource, removedFamilyDefault.Choice);

            var sourceFamilyDefault = Decision(plan, InterchangeIdentityKind.Family, "FAM-B", "properties.DepthM");
            True(!sourceFamilyDefault.TargetHasValue);
            True(sourceFamilyDefault.SourceHasValue);

            var dependency = Decision(plan, InterchangeIdentityKind.Element, "E-1", "dependencies");
            Equal(InterchangeFieldPrecedenceChoice.UseSource, dependency.Choice);
            True(dependency.RequiresGeneratedOutputReset);

            var mark = Decision(plan, InterchangeIdentityKind.Element, "E-1", "properties.Mark");
            Equal("TARGET", mark.TargetValue);
            Equal("SOURCE", mark.SourceValue);
            Equal(InterchangeFieldPrecedenceChoice.UseSource, mark.Choice);

            var length = Decision(plan, InterchangeIdentityKind.Element, "E-1", "quantities.LengthM");
            Equal(InterchangeFieldPrecedenceChoice.KeepTarget, length.Choice);
            True(plan.Decisions.All(x => x.Field.IndexOf("GeneratedSolidHandle", StringComparison.OrdinalIgnoreCase) < 0));
            True(plan.Decisions.All(x => x.Field.IndexOf("sourceHandles", StringComparison.OrdinalIgnoreCase) < 0));
            True(plan.Decisions.All(x => x.Field.IndexOf("drawingFingerprint", StringComparison.OrdinalIgnoreCase) < 0));
        }

        private static void UnspecifiedPrecedenceFailsClosed()
        {
            var plan = ProjectInterchangeFieldMergePlanner.Plan(
                BuildTarget(),
                ProjectInterchangeJsonExporter.Build(BuildSource()),
                new ProjectInterchangeFieldMergePolicy());

            True(plan.HasUnresolvedDecisions);
            True(plan.UnresolvedDecisionCount > 0);
            True(!plan.CanProceedToMutationDesign);
            True(plan.Decisions.Any(x => x.Choice == InterchangeFieldPrecedenceChoice.Unspecified));
        }

        private static void CategoryMismatchBlocksFieldMerge()
        {
            var target = new ProjectState("TARGET-CATEGORY", "Target category");
            target.Families.Add(new ProjectFamily("FAM-X", "Target Beam", ElementCategory.Beam));

            var source = new ProjectState("SOURCE-CATEGORY", "Source category");
            source.Families.Add(new ProjectFamily("FAM-X", "Source Column", ElementCategory.Column));

            var policy = new ProjectInterchangeFieldMergePolicy
            {
                FamilyName = InterchangeFieldPrecedenceChoice.UseSource,
                FamilyProperties = InterchangeFieldPrecedenceChoice.UseSource
            };
            var plan = ProjectInterchangeFieldMergePlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                policy);

            True(plan.HasBlocks);
            True(!plan.CanProceedToMutationDesign);
            True(plan.Blockers.Any(x => x.IndexOf("incompatible", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static ProjectState BuildTarget()
        {
            var project = new ProjectState("TARGET-FIELD-MERGE", "Target field merge")
            {
                DrawingFingerprint = "target-drawing"
            };
            project.Zones.Add(new ZoneDefinition("Z-01", "Target Zone"));
            project.Floors.Add(new FloorDefinition("F-01", "Target Floor", 3d));
            var family = new ProjectFamily("FAM-B", "Target Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            family.Properties["LegacyDefault"] = "legacy";
            project.Families.Add(family);

            var base1 = new ProjectElement("E-BASE-1", ElementCategory.Beam, "FAM-B", "F-01", "Z-01");
            base1.SetProperty("Mark", "BASE-1");
            project.Elements.Add(base1);
            var base2 = new ProjectElement("E-BASE-2", ElementCategory.Beam, "FAM-B", "F-01", "Z-01");
            base2.SetProperty("Mark", "BASE-2");
            project.Elements.Add(base2);

            var element = new ProjectElement("E-1", ElementCategory.Beam, "FAM-B", "F-01", "Z-01")
            {
                DrawingFingerprint = "target-drawing"
            };
            element.SourceHandles.Add("AA11");
            element.DependsOn.Add("E-BASE-1");
            element.SetProperty("Mark", "TARGET");
            element.SetProperty("GeneratedSolidHandle", "BB22");
            element.SetQuantity("LengthM", 5d);
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState BuildSource()
        {
            var project = new ProjectState("SOURCE-FIELD-MERGE", "Source field merge")
            {
                DrawingFingerprint = "source-drawing"
            };
            project.Zones.Add(new ZoneDefinition("Z-01", "Source Zone"));
            project.Zones.Add(new ZoneDefinition("Z-NEW", "Source Only Zone"));
            project.Floors.Add(new FloorDefinition("F-01", "Source Floor", 4d));
            var family = new ProjectFamily("FAM-B", "Source Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C40";
            family.Properties["DepthM"] = "0.6";
            project.Families.Add(family);

            var base1 = new ProjectElement("E-BASE-1", ElementCategory.Beam, "FAM-B", "F-01", "Z-01");
            base1.SetProperty("Mark", "BASE-1");
            project.Elements.Add(base1);
            var base2 = new ProjectElement("E-BASE-2", ElementCategory.Beam, "FAM-B", "F-01", "Z-01");
            base2.SetProperty("Mark", "BASE-2");
            project.Elements.Add(base2);

            var element = new ProjectElement("E-1", ElementCategory.Beam, "FAM-B", "F-01", "Z-01")
            {
                DrawingFingerprint = "source-drawing"
            };
            element.SourceHandles.Add("CC33");
            element.DependsOn.Add("E-BASE-2");
            element.SetProperty("Mark", "SOURCE");
            element.SetProperty("SourceOnly", "yes");
            element.SetQuantity("LengthM", 7d);
            project.Elements.Add(element);
            return project;
        }

        private static InterchangeFieldMergeDecision Decision(
            ProjectInterchangeFieldMergePlan plan,
            InterchangeIdentityKind kind,
            string id,
            string field)
        {
            return plan.Decisions.Single(x =>
                x.Kind == kind &&
                string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Field, field, StringComparison.OrdinalIgnoreCase));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeFieldMergePlannerSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("ProjectInterchangeFieldMergePlannerSmoke assertion failed.");
        }
    }
}
