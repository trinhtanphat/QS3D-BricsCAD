using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeImportCoordinatorSmoke
    {
        internal static void Run()
        {
            CollisionModeIsExplicitAndNeverFallsBack();
            ImportAsNewPlanSurfacesRemapWithoutMutation();
            UseSourcePlanPropagatesNativeCleanupRequirement();
            ExecuteRejectsCleanupAuthorityForOtherModes();
            UseSourceExecuteRequiresAndConsumesExplicitAuthorization();
            ProvenanceToggleSelectsCombinedExecution();
            InvalidModeFailsClosed();
        }

        private static void CollisionModeIsExplicitAndNeverFallsBack()
        {
            var target = TargetProject(includeGenerated: false);
            var json = ProjectInterchangeJsonExporter.Build(SourceCollisionProject(withFingerprint: true));

            Throws<InvalidOperationException>(() => ProjectInterchangeImportCoordinator.Plan(
                target,
                json,
                Request(ProjectInterchangeImportExecutionMode.AppendOnly, false)));

            var keep = ProjectInterchangeImportCoordinator.Plan(
                target,
                json,
                Request(ProjectInterchangeImportExecutionMode.KeepTarget, false));
            Equal(ProjectInterchangeImportExecutionMode.KeepTarget, keep.Mode);
            Equal(1, keep.TargetIdentitiesToKeep);
            Equal(0, keep.SemanticIdentitiesToReplace);

            var useSource = ProjectInterchangeImportCoordinator.Plan(
                target,
                json,
                Request(ProjectInterchangeImportExecutionMode.UseSourceSemanticData, false));
            Equal(ProjectInterchangeImportExecutionMode.UseSourceSemanticData, useSource.Mode);
            Equal(1, useSource.SemanticIdentitiesToReplace);
            Equal(0, useSource.TargetIdentitiesToKeep);

            Equal("TARGET", (target.FindElement("E1") ?? throw new Exception("Target missing.")).Properties["Mark"]);
            Equal(1, target.Elements.Count);
        }

        private static void ImportAsNewPlanSurfacesRemapWithoutMutation()
        {
            var target = TargetProject(includeGenerated: false);
            var json = ProjectInterchangeJsonExporter.Build(SourceCollisionProject(withFingerprint: true));
            var plan = ProjectInterchangeImportCoordinator.Plan(
                target,
                json,
                Request(ProjectInterchangeImportExecutionMode.ImportAsNew, true));

            True(plan.CanExecute);
            Equal(ProjectInterchangeImportExecutionMode.ImportAsNew, plan.Mode);
            Equal(1, plan.SemanticIdentitiesToAdd);
            True(plan.IdsToRemap > 0);
            Equal(1, plan.SourceHandleCount);
            Equal(1, target.Elements.Count);
            Equal("TARGET", (target.FindElement("E1") ?? throw new Exception("Target missing.")).Properties["Mark"]);
        }

        private static void UseSourcePlanPropagatesNativeCleanupRequirement()
        {
            var target = TargetProject(includeGenerated: true);
            var json = ProjectInterchangeJsonExporter.Build(SourceCollisionProject(withFingerprint: true));
            var plan = ProjectInterchangeImportCoordinator.Plan(
                target,
                json,
                Request(ProjectInterchangeImportExecutionMode.UseSourceSemanticData, true));

            True(plan.CanExecute);
            True(plan.RequiresNativeCleanup);
            Equal(1, plan.NativeCleanupElementIds.Count);
            Equal("E1", plan.NativeCleanupElementIds[0]);
            Equal(1, plan.SemanticIdentitiesToReplace);
            Equal(1, plan.SourceHandleCount);
        }

        private static void ExecuteRejectsCleanupAuthorityForOtherModes()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = ProjectInterchangeJsonExporter.Build(SourceNewProject());
            Throws<InvalidOperationException>(() => ProjectInterchangeImportCoordinator.Execute(
                target,
                json,
                Request(ProjectInterchangeImportExecutionMode.AppendOnly, false),
                ProjectInterchangeNativeCleanupAuthorization.ForElementIds(new[] { "UNRELATED" })));
            Equal(0, target.Elements.Count);
        }

        private static void UseSourceExecuteRequiresAndConsumesExplicitAuthorization()
        {
            var target = TargetProject(includeGenerated: true);
            var json = ProjectInterchangeJsonExporter.Build(SourceCollisionProject(withFingerprint: true));
            var request = Request(ProjectInterchangeImportExecutionMode.UseSourceSemanticData, false);

            Throws<InvalidOperationException>(() => ProjectInterchangeImportCoordinator.Execute(
                target,
                json,
                request,
                ProjectInterchangeNativeCleanupAuthorization.None));
            Equal("TARGET", (target.FindElement("E1") ?? throw new Exception("Target missing.")).Properties["Mark"]);
            True((target.FindElement("E1") ?? throw new Exception("Target missing.")).Properties.ContainsKey("GeneratedSolidHandle"));

            var plan = ProjectInterchangeImportCoordinator.Plan(target, json, request);
            var result = ProjectInterchangeImportCoordinator.Execute(
                target,
                json,
                request,
                ProjectInterchangeNativeCleanupAuthorization.ForElementIds(plan.NativeCleanupElementIds));

            Equal(ProjectInterchangeImportExecutionMode.UseSourceSemanticData, result.Mode);
            Equal(1, result.SemanticIdentitiesReplaced);
            Equal(1, result.NativeCleanupElementsAuthorized);
            var replaced = target.FindElement("E1") ?? throw new Exception("Replaced target missing.");
            Equal("SOURCE", replaced.Properties["Mark"]);
            False(replaced.Properties.ContainsKey("GeneratedSolidHandle"));
            Equal(0, replaced.SourceHandles.Count);
        }

        private static void ProvenanceToggleSelectsCombinedExecution()
        {
            var target = new ProjectState("TARGET", "Target");
            var json = ProjectInterchangeJsonExporter.Build(SourceNewProject());
            var request = Request(ProjectInterchangeImportExecutionMode.AppendOnly, true);
            var result = ProjectInterchangeImportCoordinator.Execute(
                target,
                json,
                request,
                ProjectInterchangeNativeCleanupAuthorization.None);

            Equal(ProjectInterchangeImportExecutionMode.AppendOnly, result.Mode);
            True(result.PreserveSourceHandleProvenance);
            Equal(1, result.SourceHandlesPreservedAsProvenance);
            var imported = target.FindElement("E2") ?? throw new Exception("Append target missing.");
            Equal(0, imported.SourceHandles.Count);
            Equal("SOURCE-H2", string.Join("|", ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "SOURCE-NEW", "E2")));
            Equal(ProjectInterchangeAppendProvenanceImporter.ImportMode, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey]);
        }

        private static void InvalidModeFailsClosed()
        {
            var target = new ProjectState("TARGET", "Target");
            var request = Request((ProjectInterchangeImportExecutionMode)999, false);
            Throws<ArgumentOutOfRangeException>(() => ProjectInterchangeImportCoordinator.Plan(target, "{}", request));
        }

        private static ProjectInterchangeImportRequest Request(ProjectInterchangeImportExecutionMode mode, bool provenance) =>
            new ProjectInterchangeImportRequest
            {
                Mode = mode,
                PreserveSourceHandleProvenance = provenance
            };

        private static ProjectState TargetProject(bool includeGenerated)
        {
            var target = new ProjectState("TARGET", "Target")
            {
                DrawingFingerprint = "TARGET-DWG"
            };
            var element = new ProjectElement("E1", ElementCategory.Beam)
            {
                DrawingFingerprint = target.DrawingFingerprint
            };
            element.SourceHandles.Add("TARGET-H");
            element.Properties["Mark"] = "TARGET";
            if (includeGenerated)
            {
                element.Properties["GeneratedSolidHandle"] = "AA11";
                element.Properties[ProjectElement.GeneratedSolidStateKey] = "current";
            }
            target.Elements.Add(element);
            return target;
        }

        private static ProjectState SourceCollisionProject(bool withFingerprint)
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = withFingerprint ? "SOURCE-DWG" : string.Empty,
                UpdatedUtc = new DateTime(2026, 8, 11, 1, 20, 0, DateTimeKind.Utc)
            };
            var element = new ProjectElement("E1", ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            element.SourceHandles.Add("SOURCE-H1");
            element.Properties["Mark"] = "SOURCE";
            source.Elements.Add(element);
            return source;
        }

        private static ProjectState SourceNewProject()
        {
            var source = new ProjectState("SOURCE-NEW", "Source New")
            {
                DrawingFingerprint = "SOURCE-NEW-DWG",
                UpdatedUtc = new DateTime(2026, 8, 11, 1, 22, 0, DateTimeKind.Utc)
            };
            var element = new ProjectElement("E2", ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            element.SourceHandles.Add("SOURCE-H2");
            element.Properties["Mark"] = "SOURCE-NEW";
            source.Elements.Add(element);
            return source;
        }

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

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeImportCoordinatorSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeImportCoordinatorSmoke.Run();
    }
}
