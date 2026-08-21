using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectDrawingFingerprintCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Run();
        }

        internal static void Run()
        {
            CanonicalAssignmentTracksOneMutation();
            PaddedAssignmentsFailWithoutMutation();
            EmptyAndRepeatedAssignmentsRemainStable();
            PersistedPaddedFingerprintFailsClosedOnLoad();
        }

        private static void CanonicalAssignmentTracksOneMutation()
        {
            var project = new ProjectState("P-FP-1", "Fingerprint test");
            var beforeVersion = project.ChangeVersion;

            project.DrawingFingerprint = "DWG-FP-001";

            Equal("DWG-FP-001", project.DrawingFingerprint, "Canonical fingerprint must be preserved exactly.");
            Equal(beforeVersion + 1L, project.ChangeVersion, "Canonical fingerprint assignment must advance change version exactly once.");

            var afterVersion = project.ChangeVersion;
            var afterUtc = project.UpdatedUtc;
            project.DrawingFingerprint = "DWG-FP-001";
            Equal(afterVersion, project.ChangeVersion, "Repeated canonical fingerprint assignment must be a no-op.");
            Equal(afterUtc, project.UpdatedUtc, "Repeated canonical fingerprint assignment must not refresh UpdatedUtc.");
        }

        private static void PaddedAssignmentsFailWithoutMutation()
        {
            var project = new ProjectState("P-FP-2", "Fingerprint test")
            {
                DrawingFingerprint = "DWG-FP-BASE"
            };

            var controls = new[]
            {
                " DWG-FP-BASE",
                "DWG-FP-BASE ",
                "\tDWG-FP-BASE",
                "DWG-FP-BASE\t",
                "\rDWG-FP-BASE",
                "DWG-FP-BASE\n"
            };

            foreach (var invalid in controls)
            {
                var beforeValue = project.DrawingFingerprint;
                var beforeVersion = project.ChangeVersion;
                var beforeUtc = project.UpdatedUtc;

                ExpectArgument(() => project.DrawingFingerprint = invalid, "Padded drawing fingerprint must fail closed: " + Escape(invalid));
                Equal(beforeValue, project.DrawingFingerprint, "Rejected fingerprint must not mutate stored identity.");
                Equal(beforeVersion, project.ChangeVersion, "Rejected fingerprint must not mutate change version.");
                Equal(beforeUtc, project.UpdatedUtc, "Rejected fingerprint must not mutate UpdatedUtc.");
            }
        }

        private static void EmptyAndRepeatedAssignmentsRemainStable()
        {
            var project = new ProjectState("P-FP-3", "Fingerprint test")
            {
                DrawingFingerprint = "DWG-FP-CLEAR"
            };

            var beforeClear = project.ChangeVersion;
            project.DrawingFingerprint = string.Empty;
            Equal(string.Empty, project.DrawingFingerprint, "Empty fingerprint must remain supported.");
            Equal(beforeClear + 1L, project.ChangeVersion, "Clearing a non-empty fingerprint must be one mutation.");

            var afterClear = project.ChangeVersion;
            var afterUtc = project.UpdatedUtc;
            project.DrawingFingerprint = null!;
            Equal(afterClear, project.ChangeVersion, "Null must retain existing empty optional-identity semantics.");
            Equal(afterUtc, project.UpdatedUtc, "Repeated empty optional identity must not refresh UpdatedUtc.");
        }

        private static void PersistedPaddedFingerprintFailsClosedOnLoad()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-fingerprint-canonicality-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = new ProjectState("P-FP-4", "Fingerprint persistence")
                {
                    DrawingFingerprint = "DWG-FP-PERSISTED"
                };
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);

                var xml = File.ReadAllText(path);
                const string canonical = "drawingFingerprint=\"DWG-FP-PERSISTED\"";
                const string malformed = "drawingFingerprint=\" DWG-FP-PERSISTED \"";
                if (!xml.Contains(canonical, StringComparison.Ordinal))
                    throw new Exception("QSDB fixture did not serialize the expected canonical project fingerprint.");
                File.WriteAllText(path, xml.Replace(canonical, malformed, StringComparison.Ordinal));

                ExpectArgument(() => store.Load(path), "QSDB load must reject a padded persisted project drawing fingerprint instead of aliasing it.");
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void ExpectArgument(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception(message);
        }

        private static string Escape(string value)
            => value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
