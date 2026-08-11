using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeFieldMergeImporterSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MixedReviewedMergeAppliesOnlySelectedSourceGroups();
            TargetRevisionChangeRejectsReviewedAuthorization();
            SourceSnapshotChangeRejectsReviewedAuthorization();
            GeneratedHandleChangeRejectsReviewedAuthorization();
            AmbiguousGeneratedOwnershipBlocksAuthorization();
            DestructiveCleanupRequiresTargetDrawingFingerprint();
            CleanupReportingUsesRequiredSemantics();
            SourceOnlyIdentityBlocksExecution();
            FamilyReassignmentPreservesTargetPropertiesWhenRequested();
        }

        private static void MixedReviewedMergeAppliesOnlySelectedSourceGroups()
        {
            var target = BuildTarget();
            var source = BuildSource("SOURCE");
            var policy = MixedPolicy();
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);

            True(plan.CanExecute);
            True(plan.FieldPlan.SourceChoiceCount > 0);
            True(plan.AffectedTargetElementIds.Contains("E-1", StringComparer.OrdinalIgnoreCase));
            Equal(0, plan.TargetGeneratedHandlesToClean);

            var result = ProjectInterchangeFieldMergeImporter.Import(target, json, policy, plan.CreateAuthorization());

            Equal("Target Zone", target.FindZone("Z-1")!.Name);
            Equal("Source Floor", target.FindFloor("F-1")!.Name);
            Equal(4d, target.FindFloor("F-1")!.ElevationM);
            Equal("Target Beam", target.FindFamily("FAM-B")!.Name);
            Equal("C40", target.FindFamily("FAM-B")!.Properties["Material"]);
            Equal("0.6", target.FindFamily("FAM-B")!.Properties["DepthM"]);
            Equal("SOURCE", target.FindElement("E-1")!.Properties["Mark"]);
            Equal("C40", target.FindElement("E-1")!.Properties["Material"]);
            Equal(5d, target.FindElement("E-1")!.Quantities["LengthM"]);
            Equal(plan.FieldPlan.SourceChoiceCount, result.SourceFieldsApplied);
            Equal(ProjectInterchangeFieldMergeImporter.ImportMode, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey]);
        }

        private static void TargetRevisionChangeRejectsReviewedAuthorization()
        {
            var target = BuildTarget();
            var source = BuildSource("SOURCE");
            var policy = MixedPolicy();
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);
            var authorization = plan.CreateAuthorization();
            var beforeName = target.FindFloor("F-1")!.Name;

            target.Touch();
            Throws<InvalidOperationException>(() => ProjectInterchangeFieldMergeImporter.Import(target, json, policy, authorization));
            Equal(beforeName, target.FindFloor("F-1")!.Name);
        }

        private static void SourceSnapshotChangeRejectsReviewedAuthorization()
        {
            var target = BuildTarget();
            var first = BuildSource("SOURCE-A");
            var second = BuildSource("SOURCE-B");
            var policy = MixedPolicy();
            var firstJson = ProjectInterchangeJsonExporter.Build(first);
            var secondJson = ProjectInterchangeJsonExporter.Build(second);
            var authorization = ProjectInterchangeFieldMergeImporter.Plan(target, firstJson, policy).CreateAuthorization();

            Throws<InvalidOperationException>(() => ProjectInterchangeFieldMergeImporter.Import(target, secondJson, policy, authorization));
            Equal("TARGET", target.FindElement("E-1")!.Properties["Mark"]);
        }

        private static void GeneratedHandleChangeRejectsReviewedAuthorization()
        {
            var target = BuildTarget();
            target.FindElement("E-1")!.Properties["GeneratedSolidHandle"] = "AA11";
            var source = BuildSource("SOURCE");
            var policy = MixedPolicy();
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);
            True(plan.RequiresNativeCleanup);
            Equal(1, plan.TargetGeneratedHandlesToClean);
            var authorization = plan.CreateAuthorization();

            target.FindElement("E-1")!.Properties["GeneratedSolidHandle"] = "BB22";
            Throws<InvalidOperationException>(() => ProjectInterchangeFieldMergeImporter.Import(target, json, policy, authorization));
            Equal("TARGET", target.FindElement("E-1")!.Properties["Mark"]);
            Equal("BB22", target.FindElement("E-1")!.Properties["GeneratedSolidHandle"]);
        }

        private static void AmbiguousGeneratedOwnershipBlocksAuthorization()
        {
            var target = BuildTarget();
            target.FindElement("E-1")!.Properties["GeneratedSolidHandle"] = "AA11";
            var conflicting = new ProjectElement("E-2", ElementCategory.Beam, "FAM-B", "F-1", "Z-1");
            conflicting.Properties["GeneratedSolidHandle"] = "AA11";
            target.Elements.Add(conflicting);

            var source = BuildSource("SOURCE");
            var plan = ProjectInterchangeFieldMergeImporter.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                MixedPolicy());

            True(!plan.CanExecute);
            True(plan.ExecutionBlockers.Any(x =>
                x.IndexOf("ambiguous", StringComparison.OrdinalIgnoreCase) >= 0 ||
                x.IndexOf("exclusively owned", StringComparison.OrdinalIgnoreCase) >= 0));
            Throws<InvalidOperationException>(() => plan.CreateAuthorization());
            Equal("TARGET", target.FindElement("E-1")!.Properties["Mark"]);
            Equal("AA11", target.FindElement("E-1")!.Properties["GeneratedSolidHandle"]);
            Equal("AA11", target.FindElement("E-2")!.Properties["GeneratedSolidHandle"]);
        }

        private static void DestructiveCleanupRequiresTargetDrawingFingerprint()
        {
            var target = BuildTarget();
            target.DrawingFingerprint = string.Empty;
            target.FindElement("E-1")!.Properties["GeneratedSolidHandle"] = "AA11";

            var source = BuildSource("SOURCE");
            var plan = ProjectInterchangeFieldMergeImporter.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                MixedPolicy());

            True(!plan.CanExecute);
            True(plan.ExecutionBlockers.Any(x => x.IndexOf("drawing fingerprint", StringComparison.OrdinalIgnoreCase) >= 0));
            Throws<InvalidOperationException>(() => plan.CreateAuthorization());
            Equal("TARGET", target.FindElement("E-1")!.Properties["Mark"]);
            Equal("AA11", target.FindElement("E-1")!.Properties["GeneratedSolidHandle"]);
        }

        private static void CleanupReportingUsesRequiredSemantics()
        {
            const string legacyCleanedKey = "Interchange.LastImport.TargetGeneratedHandlesCleaned";
            var target = BuildTarget();
            var element = target.FindElement("E-1")!;
            element.Properties["GeneratedSolidHandle"] = "AA11";
            target.Metadata[legacyCleanedKey] = "legacy-stale";
            var source = BuildSource("SOURCE");
            var policy = MixedPolicy();
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);
            True(plan.RequiresNativeCleanup);
            Equal(1, plan.TargetGeneratedHandlesToClean);

            var result = ProjectInterchangeFieldMergeImporter.Import(target, json, policy, plan.CreateAuthorization());

            Equal(1, result.NativeCleanupHandlesRequired);
#pragma warning disable CS0618
            Equal(1, result.TargetGeneratedHandlesCleaned);
#pragma warning restore CS0618
            Equal("1", target.Metadata[ProjectInterchangeFieldMergeImporter.LastNativeCleanupHandlesRequiredKey]);
            True(!target.Metadata.ContainsKey(legacyCleanedKey));
            True(!element.Properties.ContainsKey("GeneratedSolidHandle"));
        }

        private static void SourceOnlyIdentityBlocksExecution()
        {
            var target = BuildTarget();
            var source = BuildSource("SOURCE");
            source.Zones.Add(new ZoneDefinition("Z-NEW", "Source only"));
            var plan = ProjectInterchangeFieldMergeImporter.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                MixedPolicy());

            True(!plan.CanExecute);
            True(plan.ExecutionBlockers.Any(x => x.IndexOf("source-only", StringComparison.OrdinalIgnoreCase) >= 0));
            Throws<InvalidOperationException>(() => plan.CreateAuthorization());
        }

        private static void FamilyReassignmentPreservesTargetPropertiesWhenRequested()
        {
            var target = new ProjectState("TARGET-FAMILY-REL", "Target") { DrawingFingerprint = "target-fp" };
            var familyA = new ProjectFamily("FAM-A", "A", ElementCategory.Beam);
            familyA.Properties["Material"] = "A-DEFAULT";
            var familyB = new ProjectFamily("FAM-B", "B", ElementCategory.Beam);
            familyB.Properties["Material"] = "B-DEFAULT";
            target.Families.Add(familyA);
            target.Families.Add(familyB);
            var targetElement = new ProjectElement("E-1", ElementCategory.Beam, "FAM-A", string.Empty, string.Empty);
            targetElement.Properties["Material"] = "OVERRIDE";
            targetElement.Properties["KeepMe"] = "TARGET";
            target.Elements.Add(targetElement);

            var source = new ProjectState("SOURCE-FAMILY-REL", "Source") { DrawingFingerprint = "source-fp" };
            var sourceA = new ProjectFamily("FAM-A", "A", ElementCategory.Beam);
            sourceA.Properties["Material"] = "A-DEFAULT";
            var sourceB = new ProjectFamily("FAM-B", "B", ElementCategory.Beam);
            sourceB.Properties["Material"] = "B-DEFAULT";
            source.Families.Add(sourceA);
            source.Families.Add(sourceB);
            var sourceElement = new ProjectElement("E-1", ElementCategory.Beam, "FAM-B", string.Empty, string.Empty);
            sourceElement.Properties["Material"] = "OVERRIDE";
            sourceElement.Properties["KeepMe"] = "TARGET";
            source.Elements.Add(sourceElement);

            var policy = new ProjectInterchangeFieldMergePolicy
            {
                ElementFamily = InterchangeFieldPrecedenceChoice.UseSource,
                ElementProperties = InterchangeFieldPrecedenceChoice.KeepTarget
            };
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);
            True(plan.CanExecute);

            ProjectInterchangeFieldMergeImporter.Import(target, json, policy, plan.CreateAuthorization());
            Equal("FAM-B", targetElement.FamilyId);
            Equal("OVERRIDE", targetElement.Properties["Material"]);
            Equal("TARGET", targetElement.Properties["KeepMe"]);
        }

        private static ProjectState BuildTarget()
        {
            var target = new ProjectState("TARGET-FIELD-EXEC", "Target") { DrawingFingerprint = "target-fp" };
            target.Zones.Add(new ZoneDefinition("Z-1", "Target Zone"));
            target.Floors.Add(new FloorDefinition("F-1", "Target Floor", 3d));
            var family = new ProjectFamily("FAM-B", "Target Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            target.Families.Add(family);
            var element = new ProjectElement("E-1", ElementCategory.Beam, "FAM-B", "F-1", "Z-1");
            element.Properties["Material"] = "C30";
            element.Properties["Mark"] = "TARGET";
            element.Quantities["LengthM"] = 5d;
            target.Elements.Add(element);
            return target;
        }

        private static ProjectState BuildSource(string mark)
        {
            var source = new ProjectState("SOURCE-FIELD-EXEC", "Source") { DrawingFingerprint = "source-fp" };
            source.Zones.Add(new ZoneDefinition("Z-1", "Source Zone"));
            source.Floors.Add(new FloorDefinition("F-1", "Source Floor", 4d));
            var family = new ProjectFamily("FAM-B", "Source Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C40";
            family.Properties["DepthM"] = "0.6";
            source.Families.Add(family);
            var element = new ProjectElement("E-1", ElementCategory.Beam, "FAM-B", "F-1", "Z-1");
            element.Properties["Material"] = "C40";
            element.Properties["Mark"] = mark;
            element.Quantities["LengthM"] = 7d;
            source.Elements.Add(element);
            return source;
        }

        private static ProjectInterchangeFieldMergePolicy MixedPolicy() => new ProjectInterchangeFieldMergePolicy
        {
            ZoneName = InterchangeFieldPrecedenceChoice.KeepTarget,
            FloorName = InterchangeFieldPrecedenceChoice.UseSource,
            FloorElevation = InterchangeFieldPrecedenceChoice.UseSource,
            FamilyName = InterchangeFieldPrecedenceChoice.KeepTarget,
            FamilyProperties = InterchangeFieldPrecedenceChoice.UseSource,
            ElementFamily = InterchangeFieldPrecedenceChoice.KeepTarget,
            ElementFloor = InterchangeFieldPrecedenceChoice.KeepTarget,
            ElementZone = InterchangeFieldPrecedenceChoice.KeepTarget,
            ElementDependencies = InterchangeFieldPrecedenceChoice.KeepTarget,
            ElementProperties = InterchangeFieldPrecedenceChoice.UseSource,
            ElementQuantities = InterchangeFieldPrecedenceChoice.KeepTarget
        };

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeFieldMergeImporterSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition) throw new InvalidOperationException("ProjectInterchangeFieldMergeImporterSmoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectInterchangeFieldMergeImporterSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
