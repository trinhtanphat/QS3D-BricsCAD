using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallScheduleGenerationFenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            StableGenerationRemainsAccepted();
            DirectElementReplacementIsRejectedWithoutProjectVersionHelp();
            InPlaceCurtainQuantityMutationIsRejected();
            InPlaceFloorNameMutationIsRejected();
            InPlaceFamilyNameMutationIsRejected();
            InPlaceProvenanceMutationIsRejected();
            Console.WriteLine("PASS curtain wall schedule generation fence");
        }

        private static void StableGenerationRemainsAccepted()
        {
            var project = NewProject(out _);
            var rows = CurtainWallScheduleBuilder.Build(project);
            Require(rows.Count == 1, "stable curtain schedule row count changed");
            Require(rows[0].WallCount == 1, "stable curtain schedule wall count changed");
            Near(3d, rows[0].TotalWallLengthM, "stable curtain schedule length changed");
            Require(rows[0].Floor == "Floor 1", "stable curtain schedule floor changed");
            Require(rows[0].FamilyName == "Curtain", "stable curtain schedule family changed");
            Require(rows[0].SourceHandles.Count == 1 && rows[0].SourceHandles[0] == "AA01", "stable curtain schedule provenance changed");
        }

        private static void DirectElementReplacementIsRejectedWithoutProjectVersionHelp()
        {
            var project = NewProject(out var wall);
            var snapshot = CaptureFence(project);
            var version = project.ChangeVersion;
            project.Elements[0] = NewWall("GW-2");
            Require(project.ChangeVersion == version, "test prerequisite changed: direct element replacement unexpectedly touched project version");
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "direct element replacement");
            project.Elements[0] = wall;
        }

        private static void InPlaceCurtainQuantityMutationIsRejected()
        {
            var project = NewProject(out var wall);
            var snapshot = CaptureFence(project);
            var version = project.ChangeVersion;
            var previousUpdatedUtc = wall.UpdatedUtc;
            SpinWait.SpinUntil(() => DateTime.UtcNow > previousUpdatedUtc, 1000);
            wall.SetQuantity("LengthM", 4d);
            Require(project.ChangeVersion == version, "test prerequisite changed: element quantity mutation unexpectedly touched project version");
            Require(wall.UpdatedUtc != previousUpdatedUtc, "element quantity mutation did not advance element revision evidence");
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "in-place curtain quantity mutation");
        }

        private static void InPlaceFloorNameMutationIsRejected()
        {
            var project = NewProject(out _);
            var snapshot = CaptureFence(project);
            project.Floors[0].Name = "Changed floor";
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "in-place floor name mutation");
        }

        private static void InPlaceFamilyNameMutationIsRejected()
        {
            var project = NewProject(out _);
            var snapshot = CaptureFence(project);
            project.Families[0].Name = "Changed family";
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "in-place family name mutation");
        }

        private static void InPlaceProvenanceMutationIsRejected()
        {
            var project = NewProject(out var wall);
            var snapshot = CaptureFence(project);
            var version = project.ChangeVersion;
            wall.SourceHandles[0] = "BB02";
            Require(project.ChangeVersion == version, "test prerequisite changed: provenance mutation unexpectedly touched project version");
            ExpectGenerationDrift(() => InvokeFence(project, snapshot), "in-place provenance mutation");
        }

        private static object CaptureFence(ProjectState project)
        {
            var method = typeof(CurtainWallScheduleBuilder).GetMethod("CaptureProjectRevision", BindingFlags.Static | BindingFlags.NonPublic);
            Require(method != null, "Curtain wall schedule semantic snapshot method is missing");
            return method!.Invoke(null, new object[] { project })!;
        }

        private static void InvokeFence(ProjectState project, object snapshot)
        {
            var method = typeof(CurtainWallScheduleBuilder).GetMethod("EnsureProjectRevision", BindingFlags.Static | BindingFlags.NonPublic);
            Require(method != null, "Curtain wall schedule generation fence method is missing");
            try
            {
                method!.Invoke(null, new[] { (object)project, snapshot });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void ExpectGenerationDrift(Action action, string label)
        {
            try { action(); }
            catch (InvalidOperationException ex)
            {
                Require(ex.Message.IndexOf("Project changed while the curtain wall schedule was being built", StringComparison.Ordinal) >= 0,
                    label + " produced the wrong fail-closed diagnostic: " + ex.Message);
                return;
            }
            throw new InvalidOperationException(label + " was accepted across mixed curtain schedule generations.");
        }

        private static ProjectState NewProject(out ProjectElement wall)
        {
            var project = new ProjectState("P-CURTAIN-FENCE", "Curtain generation fence");
            project.DrawingFingerprint = "DRAWING-CURTAIN-FENCE";
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            project.Families.Add(new ProjectFamily("CW", "Curtain", ElementCategory.GlassWall));
            wall = NewWall("GW-1");
            project.Elements.Add(wall);
            return project;
        }

        private static ProjectElement NewWall(string id)
        {
            var wall = new ProjectElement(id, ElementCategory.GlassWall, "CW", "F1", string.Empty);
            wall.SetQuantity("LengthM", 3d);
            wall.SetQuantity("GrossWallAreaM2", 9d);
            wall.SetQuantity("OpeningAreaM2", 1d);
            wall.SetQuantity("CurtainNetGlassAreaM2", 7d);
            wall.SetQuantity("CurtainFrameFaceAreaM2", 1d);
            wall.SetQuantity("CurtainFrameLengthM", 12d);
            wall.SetQuantity("CurtainPanelCount", 4d);
            wall.SetQuantity("CurtainVerticalFrameCount", 3d);
            wall.SetQuantity("CurtainHorizontalFrameCount", 2d);
            wall.SetQuantity("CurtainMinClearPanelWidthM", 1d);
            wall.SetQuantity("CurtainMaxClearPanelWidthM", 1.5d);
            wall.SetQuantity("CurtainMinClearPanelHeightM", 1d);
            wall.SetQuantity("CurtainMaxClearPanelHeightM", 1.5d);
            wall.SourceHandles.Add("AA01");
            return wall;
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-10d)
                throw new InvalidOperationException(message + ": expected=" + expected + ", actual=" + actual);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
