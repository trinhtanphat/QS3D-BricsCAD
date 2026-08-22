using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeUseSourceTargetBindingSmoke
    {
        internal static void Run()
        {
            FreshAuthorizationExecutes();
            CrossProjectAuthorizationIsRejected();
            CrossDrawingAuthorizationIsRejected();
            StaleRevisionAuthorizationIsRejected();
        }

        private static void FreshAuthorizationExecutes()
        {
            var target = TargetProject("TARGET", "TARGET-DWG");
            var json = SourceJson();
            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);
            var authorization = ProjectInterchangeNativeCleanupAuthorization.ForPlan(plan);

            True(authorization.IsHandleBound);
            True(authorization.IsTargetBound);
            Equal("TARGET", plan.TargetProjectId);
            Equal("TARGET-DWG", plan.TargetDrawingFingerprint);
            Equal(target.ChangeVersion, plan.TargetChangeVersion);

            var result = ProjectInterchangeUseSourceSemanticImporter.Import(target, json, authorization);
            Equal(1, result.ElementsReplaced);
            Equal("SOURCE", (target.FindElement("E1") ?? throw new Exception("Imported target missing.")).Properties["Mark"]);
        }

        private static void CrossProjectAuthorizationIsRejected()
        {
            var reviewed = TargetProject("TARGET-A", "SHARED-DWG");
            var json = SourceJson();
            var authorization = ProjectInterchangeNativeCleanupAuthorization.ForPlan(
                ProjectInterchangeUseSourceSemanticImporter.Plan(reviewed, json));
            var other = TargetProject("TARGET-B", "SHARED-DWG");

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Import(other, json, authorization));
            Equal("TARGET", (other.FindElement("E1") ?? throw new Exception("Other target missing.")).Properties["Mark"]);
        }

        private static void CrossDrawingAuthorizationIsRejected()
        {
            var reviewed = TargetProject("TARGET", "DWG-A");
            var json = SourceJson();
            var authorization = ProjectInterchangeNativeCleanupAuthorization.ForPlan(
                ProjectInterchangeUseSourceSemanticImporter.Plan(reviewed, json));
            var otherDrawing = TargetProject("TARGET", "DWG-B");

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Import(otherDrawing, json, authorization));
            Equal("TARGET", (otherDrawing.FindElement("E1") ?? throw new Exception("Other drawing target missing.")).Properties["Mark"]);
        }

        private static void StaleRevisionAuthorizationIsRejected()
        {
            var target = TargetProject("TARGET", "TARGET-DWG");
            var json = SourceJson();
            var authorization = ProjectInterchangeNativeCleanupAuthorization.ForPlan(
                ProjectInterchangeUseSourceSemanticImporter.Plan(target, json));
            target.Touch();

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Import(target, json, authorization));
            Equal("TARGET", (target.FindElement("E1") ?? throw new Exception("Stale target missing.")).Properties["Mark"]);
        }

        private static ProjectState TargetProject(string projectId, string drawingFingerprint)
        {
            var target = new ProjectState(projectId, "Target")
            {
                DrawingFingerprint = drawingFingerprint
            };
            var element = new ProjectElement("E1", ElementCategory.Beam)
            {
                DrawingFingerprint = drawingFingerprint
            };
            element.SourceHandles.Add("TARGET-H");
            element.Properties["Mark"] = "TARGET";
            element.Properties["GeneratedSolidHandle"] = "AA11";
            element.Properties[ProjectElement.GeneratedSolidStateKey] = "current";
            target.Elements.Add(element);
            return target;
        }

        private static string SourceJson()
        {
            var source = new ProjectState("SOURCE", "Source")
            {
                DrawingFingerprint = "SOURCE-DWG",
                UpdatedUtc = new DateTime(2026, 8, 11, 7, 55, 0, DateTimeKind.Utc)
            };
            var element = new ProjectElement("E1", ElementCategory.Beam)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            element.SourceHandles.Add("SOURCE-H");
            element.Properties["Mark"] = "SOURCE";
            source.Elements.Add(element);
            return ProjectInterchangeJsonExporter.Build(source);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeUseSourceTargetBindingSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeUseSourceTargetBindingSmoke.Run();
    }
}
