using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotIdentityCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalAndLegacyOptionalIdentityLoads();
            PaddedRevisionIdFailsClosed();
            PaddedElementIdFailsClosed();
            PaddedOptionalIdentityFailsClosed("familyId");
            PaddedOptionalIdentityFailsClosed("floorId");
            PaddedOptionalIdentityFailsClosed("zoneId");
        }

        private static void CanonicalAndLegacyOptionalIdentityLoads()
        {
            var directory = TempDirectory();
            try
            {
                var store = new RevisionSnapshotStore();
                var canonicalPath = Path.Combine(directory, "canonical.qsrev");
                File.WriteAllText(canonicalPath, Document("R1", "E1", "F1", "L1", "Z1"));
                var canonical = store.Load(canonicalPath);
                Require(string.Equals(canonical.Id, "R1", StringComparison.Ordinal), "canonical revision id changed during load");
                Require(string.Equals(canonical.Elements[0].ElementId, "E1", StringComparison.Ordinal), "canonical element id changed during load");
                Require(string.Equals(canonical.Elements[0].FamilyId, "F1", StringComparison.Ordinal), "canonical family id changed during load");
                Require(string.Equals(canonical.Elements[0].FloorId, "L1", StringComparison.Ordinal), "canonical floor id changed during load");
                Require(string.Equals(canonical.Elements[0].ZoneId, "Z1", StringComparison.Ordinal), "canonical zone id changed during load");

                var legacyPath = Path.Combine(directory, "legacy-optional.qsrev");
                File.WriteAllText(legacyPath,
                    "<qs3dRevision id='legacy' createdUtc='2026-08-11T00:00:00Z'><elements>" +
                    "<element id='E1' category='Beam'><properties/><quantities/><sourceHandles/></element>" +
                    "</elements></qs3dRevision>");
                var legacy = store.Load(legacyPath);
                Require(legacy.Elements[0].FamilyId.Length == 0, "missing legacy familyId did not load as empty");
                Require(legacy.Elements[0].FloorId.Length == 0, "missing legacy floorId did not load as empty");
                Require(legacy.Elements[0].ZoneId.Length == 0, "missing legacy zoneId did not load as empty");
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static void PaddedRevisionIdFailsClosed()
        {
            Reject(Document(" R1 ", "E1", string.Empty, string.Empty, string.Empty), "padded revision id");
        }

        private static void PaddedElementIdFailsClosed()
        {
            Reject(Document("R1", " E1 ", string.Empty, string.Empty, string.Empty), "padded element id");
        }

        private static void PaddedOptionalIdentityFailsClosed(string attribute)
        {
            var family = string.Equals(attribute, "familyId", StringComparison.Ordinal) ? " F1 " : string.Empty;
            var floor = string.Equals(attribute, "floorId", StringComparison.Ordinal) ? " L1 " : string.Empty;
            var zone = string.Equals(attribute, "zoneId", StringComparison.Ordinal) ? " Z1 " : string.Empty;
            Reject(Document("R1", "E1", family, floor, zone), "padded " + attribute);
        }

        private static void Reject(string xml, string label)
        {
            var directory = TempDirectory();
            try
            {
                var path = Path.Combine(directory, "invalid.qsrev");
                File.WriteAllText(path, xml);
                Throws<InvalidDataException>(() => new RevisionSnapshotStore().Load(path), label);
            }
            finally
            {
                DeleteDirectory(directory);
            }
        }

        private static string Document(string revisionId, string elementId, string familyId, string floorId, string zoneId)
        {
            return "<qs3dRevision id='" + revisionId + "' createdUtc='2026-08-11T00:00:00Z'><elements>" +
                   "<element id='" + elementId + "' category='Beam' familyId='" + familyId + "' floorId='" + floorId + "' zoneId='" + zoneId + "'>" +
                   "<properties/><quantities/><sourceHandles/><dependencies/>" +
                   "</element></elements></qs3dRevision>";
        }

        private static string TempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-revision-identity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            try { if (Directory.Exists(path)) Directory.Delete(path, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("RevisionSnapshotIdentityCanonicalitySmoke expected " + typeof(T).Name + " for " + label + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("RevisionSnapshotIdentityCanonicalitySmoke: " + message);
        }
    }
}
