using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectDrawingFingerprintCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAssignmentAndNoOpRemainStable();
            PaddedAssignmentsFailBeforeMutation();
            SnapshotRestoreRejectsPaddedPersistedFingerprint();
        }

        private static void CanonicalAssignmentAndNoOpRemainStable()
        {
            var project = new ProjectState("P-FP-CANONICAL", "Fingerprint canonicality");
            var before = project.ChangeVersion;

            project.DrawingFingerprint = "FP-001";
            Equal("FP-001", project.DrawingFingerprint, "canonical value");
            Equal(before + 1L, project.ChangeVersion, "canonical version increment");

            var canonicalVersion = project.ChangeVersion;
            var canonicalUpdatedUtc = project.UpdatedUtc;
            project.DrawingFingerprint = "FP-001";
            Equal(canonicalVersion, project.ChangeVersion, "canonical repeated assignment version");
            Equal(canonicalUpdatedUtc, project.UpdatedUtc, "canonical repeated assignment timestamp");

            project.DrawingFingerprint = string.Empty;
            Equal(string.Empty, project.DrawingFingerprint, "empty clear value");
            Equal(canonicalVersion + 1L, project.ChangeVersion, "empty clear version");

            var emptyVersion = project.ChangeVersion;
            project.DrawingFingerprint = null!;
            Equal(emptyVersion, project.ChangeVersion, "null empty no-op version");
        }

        private static void PaddedAssignmentsFailBeforeMutation()
        {
            foreach (var candidate in new[] { " FP-001", "FP-001 ", "\tFP-001", "FP-001\t", "\rFP-001", "FP-001\n" })
            {
                var project = new ProjectState("P-FP-REJECT", "Fingerprint rejection")
                {
                    DrawingFingerprint = "FP-STABLE"
                };
                var version = project.ChangeVersion;
                var updatedUtc = project.UpdatedUtc;

                Throws<ArgumentException>(() => project.DrawingFingerprint = candidate);

                Equal("FP-STABLE", project.DrawingFingerprint, "rejected assignment value");
                Equal(version, project.ChangeVersion, "rejected assignment version");
                Equal(updatedUtc, project.UpdatedUtc, "rejected assignment timestamp");
            }
        }

        private static void SnapshotRestoreRejectsPaddedPersistedFingerprint()
        {
            var project = new ProjectState("P-FP-SNAPSHOT", "Fingerprint snapshot")
            {
                DrawingPath = "drawing.dwg",
                DrawingFingerprint = "FP-STABLE",
                ActiveZoneId = "ZONE-A",
                ActiveFloorId = "FLOOR-A"
            };
            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            var method = typeof(ProjectState).GetMethod("RestoreSnapshotScalars", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new InvalidOperationException("ProjectState.RestoreSnapshotScalars was not found.");

            try
            {
                method.Invoke(project, new object?[] { "Fingerprint snapshot", "drawing.dwg", " FP-STABLE ", "ZONE-A", "FLOOR-A" });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException)
            {
                Equal("FP-STABLE", project.DrawingFingerprint, "snapshot rejected value");
                Equal(version, project.ChangeVersion, "snapshot rejected version");
                Equal(updatedUtc, project.UpdatedUtc, "snapshot rejected timestamp");
                return;
            }

            throw new InvalidOperationException("ProjectDrawingFingerprintCanonicalitySmoke expected padded persisted fingerprint rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectDrawingFingerprintCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("ProjectDrawingFingerprintCanonicalitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
