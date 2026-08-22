using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotSaveSizePreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OversizedSerializationFailsBeforeDirectoryMutation();
            NormalSnapshotStillRoundTrips();
        }

        private static void OversizedSerializationFailsBeforeDirectoryMutation()
        {
            var root = TempRoot("oversized");
            var path = Path.Combine(root, "snapshot.xml");
            var snapshot = Snapshot("REV-SIZE-LIMIT");
            snapshot.Elements[0].Properties["Payload"] = new string('x', 4096);

            try
            {
                var store = new RevisionSnapshotStore();
                var saveWithLimit = typeof(RevisionSnapshotStore).GetMethod(
                    "Save",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    types: new[] { typeof(RevisionSnapshot), typeof(string), typeof(long) },
                    modifiers: null) ?? throw new InvalidOperationException("Revision snapshot bounded Save overload is unavailable.");

                try
                {
                    saveWithLimit.Invoke(store, new object[] { snapshot, path, 512L });
                }
                catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException)
                {
                    if (Directory.Exists(root))
                        throw new InvalidOperationException("Oversized revision snapshot mutated the destination directory before size preflight failed.");
                    return;
                }

                throw new InvalidOperationException("Oversized revision snapshot was not rejected by serialized-size preflight.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static void NormalSnapshotStillRoundTrips()
        {
            var root = TempRoot("valid");
            var path = Path.Combine(root, "snapshot.xml");
            var snapshot = Snapshot("REV-SIZE-VALID");
            snapshot.Elements[0].Properties["Note"] = "normal";

            try
            {
                var store = new RevisionSnapshotStore();
                store.Save(snapshot, path);
                var loaded = store.Load(path);

                Require(string.Equals(loaded.Id, snapshot.Id, StringComparison.Ordinal),
                    "Valid revision snapshot id did not round-trip after size preflight was added.");
                Require(loaded.Elements.Count == 1 && string.Equals(loaded.Elements[0].ElementId, "E1", StringComparison.Ordinal),
                    "Valid revision snapshot element did not round-trip after size preflight was added.");
                Require(loaded.Elements[0].Properties.TryGetValue("Note", out var note) && string.Equals(note, "normal", StringComparison.Ordinal),
                    "Valid revision snapshot property did not round-trip after size preflight was added.");
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
                CreatedUtc = new DateTime(2026, 8, 12, 3, 40, 0, DateTimeKind.Utc)
            };
            snapshot.Elements.Add(new RevisionElementSnapshot
            {
                ElementId = "E1",
                Category = ElementCategory.CustomQuantity.ToString(),
                FamilyId = string.Empty,
                FloorId = string.Empty,
                ZoneId = string.Empty
            });
            return snapshot;
        }

        private static string TempRoot(string suffix) =>
            Path.Combine(Path.GetTempPath(), "QS3D-RevisionSize-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void DeleteTree(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
