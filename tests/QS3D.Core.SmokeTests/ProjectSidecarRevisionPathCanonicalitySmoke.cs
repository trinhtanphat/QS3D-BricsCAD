using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSidecarRevisionPathCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedPathBeforeFilesystemObservation();
            CanonicalPathIdentityRemainsStable();
        }

        private static void RejectsPaddedPathBeforeFilesystemObservation()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-sidecar-path-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var canonical = Path.Combine(root, "project.qsdb");
                File.WriteAllText(canonical, "canonical");
                var padded = canonical + " ";
                File.WriteAllText(padded, "wrong-target");

                var rejected = false;
                try
                {
                    ProjectSidecarRevisionStamp.Capture(padded);
                }
                catch (ArgumentException)
                {
                    rejected = true;
                }

                Require(rejected, "Padded sidecar paths must fail closed before filesystem observation.");
                var stamp = ProjectSidecarRevisionStamp.Capture(canonical);
                Require(!stamp.IsForPath(padded), "Padded path lookup must not match a canonical sidecar stamp.");
                Require(File.ReadAllText(padded) == "wrong-target", "Canonicality validation must not mutate the padded-path target.");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static void CanonicalPathIdentityRemainsStable()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-sidecar-control-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var canonical = Path.Combine(root, "project.qsdb");
                File.WriteAllText(canonical, "stable");
                var stamp = ProjectSidecarRevisionStamp.Capture(canonical);

                Require(stamp.HasAnyFile, "Canonical existing sidecar must remain observable.");
                Require(stamp.IsForPath(canonical), "Canonical absolute path must retain identity.");
                Require(stamp.MatchesCurrent(), "Unchanged canonical sidecar must match its captured revision.");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}