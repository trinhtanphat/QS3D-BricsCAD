using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningSourceInstanceFenceSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            StableScheduleBuildsNormally();
            EquivalentElementReplacementWithoutTouchFailsClosed();
        }

        private static void StableScheduleBuildsNormally()
        {
            var project = BuildProject();
            var rows = DoorOpeningScheduleBuilder.Build(project);
            if (rows.Count != 1 || rows[0].Count != 1)
                throw new InvalidOperationException("Door/opening source-instance fence smoke changed stable schedule output.");
        }

        private static void EquivalentElementReplacementWithoutTouchFailsClosed()
        {
            var project = BuildProject();
            var snapshot = Snapshot(project);
            InvokeGuard(project, snapshot);
            var version = project.ChangeVersion;
            var source = project.Elements[0];
            var replacement = new ProjectElement(source.Id, source.Category, source.FamilyId, source.FloorId, source.ZoneId)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            foreach (var item in source.Properties) replacement.Properties[item.Key] = item.Value;
            foreach (var item in source.Quantities) replacement.Quantities[item.Key] = item.Value;
            foreach (var handle in source.SourceHandles) replacement.SourceHandles.Add(handle);
            foreach (var dependency in source.DependsOn) replacement.DependsOn.Add(dependency);
            typeof(ProjectElement).GetProperty("UpdatedUtc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .SetValue(replacement, source.UpdatedUtc);
            project.Elements[0] = replacement;
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Equivalent Door/opening element replacement unexpectedly touched ProjectState.ChangeVersion.");
            ThrowsInvalidOperation(() => InvokeGuard(project, snapshot), "Project changed while the door/opening schedule was being built");
        }

        private static object Snapshot(ProjectState project)
        {
            var method = typeof(DoorOpeningScheduleBuilder).GetMethod("CaptureProjectRevision", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(DoorOpeningScheduleBuilder).FullName, "CaptureProjectRevision");
            try
            {
                return method.Invoke(null, new object[] { project })
                    ?? throw new InvalidOperationException("Door/opening schedule snapshot capture returned null.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void InvokeGuard(ProjectState project, object snapshot)
        {
            var method = typeof(DoorOpeningScheduleBuilder).GetMethod(
                "EnsureProjectRevision",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ProjectState), snapshot.GetType() },
                modifiers: null)
                ?? throw new MissingMethodException(typeof(DoorOpeningScheduleBuilder).FullName, "EnsureProjectRevision");
            try
            {
                method.Invoke(null, new[] { (object)project, snapshot });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-DOOR-FENCE", "Door opening source instance fence") { DrawingFingerprint = "FP-DOOR-FENCE" };
            project.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            var family = new ProjectFamily("D1", "Door A", ElementCategory.Door);
            family.Properties["WidthM"] = "0.9";
            family.Properties["HeightM"] = "2.1";
            project.Families.Add(family);
            var element = new ProjectElement("E1", ElementCategory.Door, "D1", "F1", "Z1");
            element.Quantities["OpeningAreaM2"] = 1.89d;
            element.SourceHandles.Add("ABCD");
            project.Elements.Add(element);
            return project;
        }

        private static void ThrowsInvalidOperation(Action action, string expectedMessagePart)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessagePart, StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Expected message containing '" + expectedMessagePart + "', got '" + ex.Message + "'.", ex);
            }
            throw new InvalidOperationException("Expected InvalidOperationException from Door/opening source-instance fence.");
        }
    }
}
