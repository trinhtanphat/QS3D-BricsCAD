using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSnapshotActiveContextCanonicalitySmoke
    {
        private static readonly MethodInfo RestoreSnapshotScalars =
            typeof(ProjectState).GetMethod("RestoreSnapshotScalars", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ProjectState.RestoreSnapshotScalars was not found.");

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedZoneBeforeMutation();
            RejectsPaddedFloorBeforeMutation();
            AcceptsCanonicalAndEmptyContexts();
        }

        private static void RejectsPaddedZoneBeforeMutation()
        {
            foreach (var invalidZone in new[] { " zone-a", "zone-a ", "\tzone-a", "zone-a\n" })
            {
                var project = CreateBaseline();
                var beforeVersion = project.ChangeVersion;
                ThrowsArgument(() => InvokeRestore(project, "Changed", "changed.dwg", "FP-CHANGED", invalidZone, "floor-a"));
                AssertBaseline(project, beforeVersion, "padded zone");
            }
        }

        private static void RejectsPaddedFloorBeforeMutation()
        {
            foreach (var invalidFloor in new[] { " floor-a", "floor-a ", "\tfloor-a", "floor-a\r" })
            {
                var project = CreateBaseline();
                var beforeVersion = project.ChangeVersion;
                ThrowsArgument(() => InvokeRestore(project, "Changed", "changed.dwg", "FP-CHANGED", "zone-a", invalidFloor));
                AssertBaseline(project, beforeVersion, "padded floor");
            }
        }

        private static void AcceptsCanonicalAndEmptyContexts()
        {
            var project = CreateBaseline();
            InvokeRestore(project, "Restored", "restored.dwg", "FP-RESTORED", "zone-b", "floor-b");
            Equal("Restored", project.Name, "canonical name");
            Equal("restored.dwg", project.DrawingPath, "canonical drawing path");
            Equal("FP-RESTORED", project.DrawingFingerprint, "canonical fingerprint");
            Equal("zone-b", project.ActiveZoneId, "canonical zone");
            Equal("floor-b", project.ActiveFloorId, "canonical floor");

            InvokeRestore(project, "Restored", "restored.dwg", "FP-RESTORED", string.Empty, null);
            Equal(string.Empty, project.ActiveZoneId, "empty zone");
            Equal(string.Empty, project.ActiveFloorId, "null floor normalizes to empty");
        }

        private static ProjectState CreateBaseline()
        {
            var project = new ProjectState("P-SNAPSHOT-ACTIVE-CONTEXT", "Baseline");
            project.DrawingPath = "baseline.dwg";
            project.DrawingFingerprint = "FP-BASELINE";
            project.ActiveZoneId = "zone-a";
            project.ActiveFloorId = "floor-a";
            return project;
        }

        private static void AssertBaseline(ProjectState project, long expectedVersion, string label)
        {
            Equal("Baseline", project.Name, label + " name atomicity");
            Equal("baseline.dwg", project.DrawingPath, label + " drawing path atomicity");
            Equal("FP-BASELINE", project.DrawingFingerprint, label + " fingerprint atomicity");
            Equal("zone-a", project.ActiveZoneId, label + " zone atomicity");
            Equal("floor-a", project.ActiveFloorId, label + " floor atomicity");
            Equal(expectedVersion, project.ChangeVersion, label + " change-version atomicity");
        }

        private static void InvokeRestore(
            ProjectState project,
            string name,
            string? drawingPath,
            string? drawingFingerprint,
            string? activeZoneId,
            string? activeFloorId)
        {
            RestoreSnapshotScalars.Invoke(project, new object?[]
            {
                name,
                drawingPath,
                drawingFingerprint,
                activeZoneId,
                activeFloorId
            });
        }

        private static void ThrowsArgument(Action action)
        {
            try
            {
                action();
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("ProjectSnapshotActiveContextCanonicalitySmoke expected ArgumentException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ProjectSnapshotActiveContextCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
