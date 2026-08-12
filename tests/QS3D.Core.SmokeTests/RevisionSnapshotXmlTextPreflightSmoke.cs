using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotXmlTextPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            InvalidRevisionIdFailsBeforeFilesystemMutation();
            InvalidPropertyValueFailsBeforeFilesystemMutation();
            LoneSurrogateFailsBeforeFilesystemMutation();
            SupplementaryUnicodeRoundTrips();
        }

        private static void InvalidRevisionIdFailsBeforeFilesystemMutation()
        {
            var snapshot = Snapshot("REV-\u0001");
            AssertPreflightFailure(snapshot, "invalid-id");
        }

        private static void InvalidPropertyValueFailsBeforeFilesystemMutation()
        {
            var snapshot = Snapshot("REV-PROPERTY-CONTROL");
            snapshot.Elements[0].Properties["Note"] = "bad\u0001value";
            AssertPreflightFailure(snapshot, "invalid-property-control");
        }

        private static void LoneSurrogateFailsBeforeFilesystemMutation()
        {
            var snapshot = Snapshot("REV-LONE-SURROGATE");
            snapshot.Elements[0].Properties["Note"] = new string(new[] { '\uD800' });
            AssertPreflightFailure(snapshot, "invalid-property-surrogate");
        }

        private static void SupplementaryUnicodeRoundTrips()
        {
            var root = TempRoot("valid-supplementary");
            var path = Path.Combine(root, "snapshot.xml");
            const string expected = "Valid supplementary \U0001F642 text";
            var snapshot = Snapshot("REV-SUPPLEMENTARY");
            snapshot.Elements[0].Properties["Note"] = expected;
            snapshot.Elements[0].Properties["NullValue"] = null!;

            try
            {
                var store = new RevisionSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);
                if (!loaded.Elements[0].Properties.TryGetValue("Note", out var actual) ||
                    !string.Equals(actual, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Valid supplementary Unicode revision property did not round-trip exactly.");
                if (!loaded.Elements[0].Properties.TryGetValue("NullValue", out var nullValue) ||
                    !string.Equals(nullValue, string.Empty, StringComparison.Ordinal))
                    throw new InvalidOperationException("Null revision property value no longer preserves empty-string serialization semantics.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static RevisionSnapshot Snapshot(string id)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = id,
                CreatedUtc = DateTime.UtcNow
            };
            var element = new RevisionElementSnapshot
            {
                ElementId = "E-XML-TEXT",
                Category = ElementCategory.Beam.ToString()
            };
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private static void AssertPreflightFailure(RevisionSnapshot snapshot, string suffix)
        {
            var root = TempRoot(suffix);
            var path = Path.Combine(root, "snapshot.xml");
            try
            {
                Throws<InvalidDataException>(() => new RevisionSnapshotStore().Save(snapshot, path));
                if (Directory.Exists(root))
                    throw new InvalidOperationException("Invalid revision XML text mutated the filesystem before failing preflight: " + suffix + ".");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static string TempRoot(string suffix) =>
            Path.Combine(Path.GetTempPath(), "QS3D-RevisionXmlText-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void DeleteTree(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
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
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
