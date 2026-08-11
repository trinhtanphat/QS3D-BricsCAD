using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeUseSourceVerticalLevelInvalidationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PlanIncludesVerticalLevelReferencesAndNativeCleanup();
        }

        private static void PlanIncludesVerticalLevelReferencesAndNativeCleanup()
        {
            var target = new ProjectState("TARGET-P", "Target")
            {
                DrawingFingerprint = "TARGET-VERTICAL-LEVEL-DWG"
            };
            target.Floors.Add(new FloorDefinition("F1", "Target Level", 0d));

            var element = new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "F1";
            element.Properties["GeneratedSolidHandle"] = "AA11";
            element.Properties[ProjectElement.GeneratedSolidStateKey] = "current";
            target.Elements.Add(element);

            var source = new ProjectState("SOURCE-P", "Source");
            source.Floors.Add(new FloorDefinition("F1", "Source Level", 5d));
            var json = ProjectInterchangeJsonExporter.Build(source);

            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);
            Equal(1, plan.FloorsToReplace);
            True(plan.AffectedTargetElementIds.Contains("E1", StringComparer.OrdinalIgnoreCase));
            True(plan.TargetElementIdsRequiringNativeCleanup.Contains("E1", StringComparer.OrdinalIgnoreCase));
            Equal(1, plan.TargetGeneratedHandlesToClean);
            True(plan.RequiresNativeCleanup);

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Import(
                    target,
                    json,
                    ProjectInterchangeNativeCleanupAuthorization.None));
            Near(0d, target.FindFloor("F1")?.ElevationM ?? double.NaN);
            Equal("AA11", element.Properties["GeneratedSolidHandle"]);

            var authorization = ProjectInterchangeNativeCleanupAuthorization.ForPlan(plan);
            var result = ProjectInterchangeUseSourceSemanticImporter.Import(target, json, authorization);

            Near(5d, target.FindFloor("F1")?.ElevationM ?? double.NaN);
            True(!element.Properties.ContainsKey("GeneratedSolidHandle"));
            Equal(ElementDirtyFlags.All, element.Dirty);
            Equal(1, result.AffectedTargetElementsMarkedDirty);
            Equal(1, result.NativeCleanupElementsAuthorized);
            Equal(1, result.TargetGeneratedHandlesCleaned);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeUseSourceVerticalLevelInvalidationSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void Near(double expected, double actual)
        {
            if (double.IsNaN(actual) || Math.Abs(expected - actual) > 1e-9)
                throw new InvalidOperationException("ProjectInterchangeUseSourceVerticalLevelInvalidationSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value)
                throw new InvalidOperationException("ProjectInterchangeUseSourceVerticalLevelInvalidationSmoke assertion failed.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("ProjectInterchangeUseSourceVerticalLevelInvalidationSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}
