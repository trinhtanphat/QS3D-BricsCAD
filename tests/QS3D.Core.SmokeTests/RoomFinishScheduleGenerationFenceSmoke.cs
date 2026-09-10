using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishScheduleGenerationFenceSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            StableScheduleBuildsNormally();
            EquivalentFinishReplacementAdvancesRevisionAndFailsClosed();
            DirectSemanticMutationWithoutTouchFailsClosed();
        }

        private static void StableScheduleBuildsNormally()
        {
            var project = BuildProject();
            var rows = RoomFinishScheduleBuilder.Build(project);
            if (rows.Count != 1 || rows[0].Count != 1 || rows[0].ElementIds.Count != 1)
                throw new InvalidOperationException("Room finish generation fence smoke changed stable schedule output.");
            if (Math.Abs(rows[0].AreaM2 - 12.5d) > 1e-12)
                throw new InvalidOperationException("Room finish generation fence smoke changed stable area aggregation.");
        }

        private static void EquivalentFinishReplacementAdvancesRevisionAndFailsClosed()
        {
            var project = BuildProject();
            var snapshot = Snapshot(project);
            InvokeGuard(project, snapshot);
            var version = project.ChangeVersion;
            var source = project.Elements[1];
            var replacement = CloneElement(source);
            project.Elements[1] = replacement;
            if (project.ChangeVersion != version + 1)
                throw new InvalidOperationException("Equivalent Room finish element replacement must advance ProjectState.ChangeVersion exactly once.");
            ThrowsInvalidOperation(() => InvokeGuard(project, snapshot), "Project changed while the room finish schedule was being built");
        }

        private static void DirectSemanticMutationWithoutTouchFailsClosed()
        {
            var project = BuildProject();
            var snapshot = Snapshot(project);
            var version = project.ChangeVersion;
            project.Elements[1].Quantities["BottomAreaM2"] = 13.5d;
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Direct Room finish quantity mutation unexpectedly touched ProjectState.ChangeVersion.");
            ThrowsInvalidOperation(() => InvokeGuard(project, snapshot), "Project changed while the room finish schedule was being built");
        }

        private static ProjectElement CloneElement(ProjectElement source)
        {
            var replacement = new ProjectElement(source.Id, source.Category, source.FamilyId, source.FloorId, source.ZoneId)
            {
                DrawingFingerprint = source.DrawingFingerprint
            };
            foreach (var item in source.Properties) replacement.Properties[item.Key] = item.Value;
            foreach (var item in source.Quantities) replacement.Quantities[item.Key] = item.Value;
            foreach (var handle in source.SourceHandles) replacement.SourceHandles.Add(handle);
            foreach (var dependency in source.DependsOn) replacement.DependsOn.Add(dependency);
            typeof(ProjectElement).GetProperty("UpdatedUtc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(replacement, source.UpdatedUtc);
            return replacement;
        }

        private static object Snapshot(ProjectState project)
        {
            var method = typeof(RoomFinishScheduleBuilder).GetMethod("CaptureProjectRevision", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(RoomFinishScheduleBuilder).FullName, "CaptureProjectRevision");
            try
            {
                return method.Invoke(null, new object[] { project }) ?? throw new InvalidOperationException("Room finish schedule snapshot capture returned null.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void InvokeGuard(ProjectState project, object snapshot)
        {
            var method = typeof(RoomFinishScheduleBuilder).GetMethod("EnsureProjectRevision", BindingFlags.NonPublic | BindingFlags.Static, binder: null, types: new[] { typeof(ProjectState), snapshot.GetType() }, modifiers: null)
                ?? throw new MissingMethodException(typeof(RoomFinishScheduleBuilder).FullName, "EnsureProjectRevision");
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
            var project = new ProjectState("P-RF-FENCE", "Room finish generation fence") { DrawingFingerprint = "FP-RF-FENCE" };
            project.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            var family = new ProjectFamily("RF-FAMILY", "Floor Finish A", ElementCategory.FloorFinish);
            family.Properties["Material"] = "Tile";
            project.Families.Add(family);
            var room = new ProjectElement("R1", ElementCategory.Room, string.Empty, "F1", "Z1");
            room.Properties["RoomName"] = "Room 1";
            project.Elements.Add(room);
            var finishId = RoomFinishIdentityService.CanonicalId(room.Id, ElementCategory.FloorFinish);
            var finish = new ProjectElement(finishId, ElementCategory.FloorFinish, family.Id, "F1", "Z1");
            finish.Properties["RoomId"] = room.Id;
            finish.Quantities["BottomAreaM2"] = 12.5d;
            finish.SourceHandles.Add("ABCD");
            project.Elements.Add(finish);
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
            throw new InvalidOperationException("Expected InvalidOperationException from Room finish generation fence.");
        }
    }
}
