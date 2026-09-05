using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningScheduleGenerationFenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            StableGenerationRemainsAccepted();
            DirectElementReplacementIsRejectedWithoutProjectVersionHelp();
            InPlaceOpeningQuantityMutationIsRejected();
            InPlaceDoorPropertyMutationIsRejected();
            InPlaceFloorNameMutationIsRejected();
            InPlaceFamilyPropertyMutationIsRejected();
            InPlaceHostCategoryMutationIsRejected();
            InPlaceProvenanceMutationIsRejected();
            Console.WriteLine("PASS door/opening schedule generation fence");
        }

        private static void StableGenerationRemainsAccepted()
        {
            var project = NewProject(out _, out _);
            var rows = DoorOpeningScheduleBuilder.Build(project);
            Require(rows.Count == 1, "stable door schedule row count changed");
            Require(rows[0].Count == 1 && rows[0].HostCount == 1, "stable door schedule counts changed");
            Near(2d, rows[0].OpeningAreaM2, "stable door schedule area changed");
            Require(rows[0].Floor == "Floor 1", "stable door schedule floor changed");
            Require(rows[0].FamilyName == "Door Family", "stable door schedule family changed");
            Require(rows[0].SourceHandles.Count == 1 && rows[0].SourceHandles[0] == "D001", "stable door schedule provenance changed");
        }

        private static void DirectElementReplacementIsRejectedWithoutProjectVersionHelp()
        {
            var project = NewProject(out var door, out _);
            var snapshot = CaptureFence(project);
            var version = project.ChangeVersion;
            project.Elements[0] = NewDoor("D-2");
            Require(project.ChangeVersion == version, "direct replacement unexpectedly changed project version");
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "direct element replacement");
            project.Elements[0] = door;
        }

        private static void InPlaceOpeningQuantityMutationIsRejected()
        {
            var project = NewProject(out var door, out _);
            var snapshot = CaptureFence(project);
            var version = project.ChangeVersion;
            door.SetQuantity("OpeningAreaM2", 3d);
            Require(project.ChangeVersion == version, "quantity mutation unexpectedly changed project version");
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "opening quantity mutation");
        }

        private static void InPlaceDoorPropertyMutationIsRejected()
        {
            var project = NewProject(out var door, out _);
            var snapshot = CaptureFence(project);
            door.Properties["WidthM"] = "1.5";
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "door property mutation");
        }

        private static void InPlaceFloorNameMutationIsRejected()
        {
            var project = NewProject(out _, out _);
            var snapshot = CaptureFence(project);
            project.Floors[0].Name = "Changed floor";
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "floor name mutation");
        }

        private static void InPlaceFamilyPropertyMutationIsRejected()
        {
            var project = NewProject(out _, out _);
            var snapshot = CaptureFence(project);
            project.Families[0].Properties["Material"] = "Changed";
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "family property mutation");
        }

        private static void InPlaceHostCategoryMutationIsRejected()
        {
            var project = NewProject(out _, out var host);
            var snapshot = CaptureFence(project);
            host.Category = ElementCategory.Beam;
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "host category mutation");
        }

        private static void InPlaceProvenanceMutationIsRejected()
        {
            var project = NewProject(out var door, out _);
            var snapshot = CaptureFence(project);
            door.SourceHandles[0] = "D002";
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "provenance mutation");
        }

        private static object CaptureFence(ProjectState project)
        {
            var method = typeof(DoorOpeningScheduleBuilder).GetMethod("CaptureProjectRevision", BindingFlags.Static | BindingFlags.NonPublic);
            Require(method != null, "Door/opening semantic snapshot method is missing");
            return method!.Invoke(null, new object[] { project })!;
        }

        private static void InvokeFence(ProjectState project, object snapshot)
        {
            var method = typeof(DoorOpeningScheduleBuilder).GetMethod("EnsureProjectRevision", BindingFlags.Static | BindingFlags.NonPublic);
            Require(method != null, "Door/opening generation fence method is missing");
            try { method!.Invoke(null, new[] { (object)project, snapshot }); }
            catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
        }

        private static void ExpectGenerationDrift(Action action, string label)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                Require(ex.Message.IndexOf("Project changed while the door/opening schedule was being built", StringComparison.Ordinal) >= 0,
                    label + " produced wrong diagnostic: " + ex.Message);
                return;
            }
            throw new InvalidOperationException(label + " was accepted across mixed door/opening schedule generations.");
        }

        private static ProjectState NewProject(out ProjectElement door, out ProjectElement host)
        {
            var project = new ProjectState("P-DOOR-FENCE", "Door fence");
            project.DrawingFingerprint = "DRAWING-DOOR-FENCE";
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            var family = new ProjectFamily("DF", "Door Family", ElementCategory.Door);
            family.Properties["Material"] = "Timber";
            project.Families.Add(family);
            door = NewDoor("D-1");
            host = new ProjectElement("W-1", ElementCategory.ArchitecturalWall, string.Empty, "F1", string.Empty);
            project.Elements.Add(door);
            project.Elements.Add(host);
            return project;
        }

        private static ProjectElement NewDoor(string id)
        {
            var door = new ProjectElement(id, ElementCategory.Door, "DF", "F1", string.Empty);
            door.Properties["WidthM"] = "1";
            door.Properties["HeightM"] = "2";
            door.Properties["HostWallId"] = "W-1";
            door.SetQuantity("OpeningAreaM2", 2d);
            door.SourceHandles.Add("D001");
            return door;
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-10d) throw new InvalidOperationException(message + ": expected=" + expected + ", actual=" + actual);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
