using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateNullScalarPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullAssignmentsCanonicalizeAndPreserveNoOps();
            CanonicalEmptyScalarsRoundTripThroughQsdb();
        }

        private static void NullAssignmentsCanonicalizeAndPreserveNoOps()
        {
            var project = new ProjectState("PROJECT-NULL-SCALARS", "Null Scalars");
            var initialVersion = project.ChangeVersion;
            var initialUpdatedUtc = project.UpdatedUtc;

            project.DrawingPath = null!;
            project.DrawingFingerprint = null!;
            project.ActiveZoneId = null!;
            project.ActiveFloorId = null!;

            Equal(string.Empty, project.DrawingPath);
            Equal(string.Empty, project.DrawingFingerprint);
            Equal(string.Empty, project.ActiveZoneId);
            Equal(string.Empty, project.ActiveFloorId);
            Equal(initialVersion, project.ChangeVersion);
            Equal(initialUpdatedUtc, project.UpdatedUtc);

            project.DrawingPath = "  C:/Exact Path.dwg  ";
            Equal("  C:/Exact Path.dwg  ", project.DrawingPath);
            var changedVersion = project.ChangeVersion;

            project.DrawingPath = null!;
            Equal(string.Empty, project.DrawingPath);
            Equal(changedVersion + 1L, project.ChangeVersion);
        }

        private static void CanonicalEmptyScalarsRoundTripThroughQsdb()
        {
            var project = new ProjectState("PROJECT-NULL-SCALARS-ROUNDTRIP", "Null Scalars Roundtrip");
            project.DrawingPath = null!;
            project.DrawingFingerprint = null!;
            project.ActiveZoneId = null!;
            project.ActiveFloorId = null!;

            var path = Path.Combine(
                Path.GetTempPath(),
                "qs3d-project-null-scalars-" + Guid.NewGuid().ToString("N") + ".qsdb");

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);

                Equal(string.Empty, loaded.DrawingPath);
                Equal(string.Empty, loaded.DrawingFingerprint);
                Equal(string.Empty, loaded.ActiveZoneId);
                Equal(string.Empty, loaded.ActiveFloorId);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup only; persistence assertions above are authoritative.
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
