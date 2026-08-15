using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionCaptureIdIntegritySmoke
    {
        internal static void Run()
        {
            InvalidIdsFailAtCaptureBoundary();
            CanonicalIdIsPreservedExactly();
            CapturePreservesProjectIdentity();
        }

        private static void InvalidIdsFailAtCaptureBoundary()
        {
            var service = new RevisionService();
            var project = new ProjectState("revision-id-integrity", "Revision Id Integrity");

            Throws<ArgumentException>(() => service.Capture(project, null!));
            Throws<ArgumentException>(() => service.Capture(project, string.Empty));
            Throws<ArgumentException>(() => service.Capture(project, "   "));
            Throws<ArgumentException>(() => service.Capture(project, " REV-A"));
            Throws<ArgumentException>(() => service.Capture(project, "REV-A "));
        }

        private static void CanonicalIdIsPreservedExactly()
        {
            var service = new RevisionService();
            var project = new ProjectState("revision-id-integrity", "Revision Id Integrity");
            var snapshot = service.Capture(project, "REV-A/2026-08-12");
            Equal("REV-A/2026-08-12", snapshot.Id);
            Equal(DateTimeKind.Utc, snapshot.CreatedUtc.Kind);
        }

        private static void CapturePreservesProjectIdentity()
        {
            var service = new RevisionService();
            var project = new ProjectState("revision-id-project", "Revision Id Project");
            var snapshot = service.Capture(project, "REV-PROJECT-ID");
            Equal(project.ProjectId, snapshot.ProjectId);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class RevisionCaptureIdIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RevisionCaptureIdIntegritySmoke.Run();
    }
}