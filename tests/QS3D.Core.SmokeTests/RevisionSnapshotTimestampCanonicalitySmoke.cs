using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotTimestampCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalUtcTimestampLoads();
            PaddedTimestampFailsClosed();
            ExplicitOffsetFailsClosed();
        }

        private static void CanonicalUtcTimestampLoads()
        {
            var snapshot = Load("2026-08-11T00:00:00.0000000Z");
            Require(snapshot.CreatedUtc == new DateTime(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc),
                "canonical UTC timestamp did not round-trip");
            Require(snapshot.CreatedUtc.Kind == DateTimeKind.Utc, "canonical timestamp did not keep UTC kind");
        }

        private static void PaddedTimestampFailsClosed()
        {
            Reject(" 2026-08-11T00:00:00.0000000Z ");
        }

        private static void ExplicitOffsetFailsClosed()
        {
            Reject("2026-08-11T07:00:00.0000000+07:00");
        }

        private static RevisionSnapshot Load(string createdUtc)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-revision-timestamp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "snapshot.qsrev");
            try
            {
                File.WriteAllText(path,
                    "<qs3dRevision id='R' createdUtc='" + createdUtc + "'><elements>" +
                    "<element id='E1' category='Beam' familyId='' floorId='' zoneId=''>" +
                    "<properties/><quantities/><sourceHandles/><dependencies/>" +
                    "</element></elements></qs3dRevision>");
                return new RevisionSnapshotStore().Load(path);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Reject(string createdUtc)
        {
            try
            {
                Load(createdUtc);
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new InvalidOperationException("RevisionSnapshotTimestampCanonicalitySmoke expected a non-canonical timestamp to fail closed.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("RevisionSnapshotTimestampCanonicalitySmoke: " + message);
        }
    }
}
