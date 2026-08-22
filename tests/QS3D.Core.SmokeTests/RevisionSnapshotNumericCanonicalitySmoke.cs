using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotNumericCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsSerializerOwnedToken();
            RejectsNonCanonicalToken("1.0");
            RejectsNonCanonicalToken("+1");
            RejectsNonCanonicalToken(" 1 ");
        }

        private static void AcceptsSerializerOwnedToken()
        {
            var snapshot = Load("1");
            if (snapshot.Elements.Count != 1 ||
                !snapshot.Elements[0].Quantities.TryGetValue("LengthM", out var value) ||
                value != 1d)
                throw new InvalidOperationException("RevisionSnapshotNumericCanonicalitySmoke canonical quantity did not round-trip.");
        }

        private static void RejectsNonCanonicalToken(string token)
        {
            try
            {
                Load(token);
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new InvalidOperationException(
                "RevisionSnapshotNumericCanonicalitySmoke expected non-canonical quantity token to fail: " + token);
        }

        private static RevisionSnapshot Load(string quantityToken)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-revision-numeric-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "snapshot.qsrev");
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(path,
                    "<qs3dRevision id='R-NUM' createdUtc='2026-08-12T00:00:00.0000000Z'>" +
                    "<elements><element id='E1' category='Beam' familyId='' floorId='' zoneId=''>" +
                    "<properties/><quantities><q name='LengthM' value='" + quantityToken + "'/></quantities>" +
                    "<sourceHandles/><dependencies/>" +
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
    }
}
