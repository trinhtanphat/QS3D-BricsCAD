using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotBackupProjectIdentitySmoke
    {
        internal static void Run()
        {
            ForeignValidPrimaryCannotBecomeBackup();
            ForeignValidatedBackupCannotBePreserved();
            SameProjectValidatedBackupRemainsUsable();
        }

        private static void ForeignValidPrimaryCannotBecomeBackup()
        {
            WithStore((store, path) =>
            {
                store.Save(Snapshot("PROJECT-A", "A1"), path);
                var primaryBefore = File.ReadAllBytes(path);

                ExpectIdentityFailure(() => store.Save(Snapshot("PROJECT-B", "B1"), path));

                EqualBytes(primaryBefore, File.ReadAllBytes(path), "foreign-primary rejection primary bytes");
                if (File.Exists(path + ".bak"))
                    throw new Exception("Foreign-primary rejection unexpectedly created a revision backup.");
            });
        }

        private static void ForeignValidatedBackupCannotBePreserved()
        {
            WithStore((store, path) =>
            {
                store.Save(Snapshot("PROJECT-A", "A1"), path);
                store.Save(Snapshot("PROJECT-A", "A2"), path);
                File.WriteAllText(path, "corrupt-primary", new UTF8Encoding(false));
                var primaryBefore = File.ReadAllBytes(path);
                var backupBefore = File.ReadAllBytes(path + ".bak");

                ExpectIdentityFailure(() => store.Save(Snapshot("PROJECT-B", "B1"), path));

                EqualBytes(primaryBefore, File.ReadAllBytes(path), "foreign-backup rejection primary bytes");
                EqualBytes(backupBefore, File.ReadAllBytes(path + ".bak"), "foreign-backup rejection backup bytes");
            });
        }

        private static void SameProjectValidatedBackupRemainsUsable()
        {
            WithStore((store, path) =>
            {
                store.Save(Snapshot("PROJECT-A", "A1"), path);
                store.Save(Snapshot("PROJECT-A", "A2"), path);
                File.WriteAllText(path, "corrupt-primary", new UTF8Encoding(false));

                store.Save(Snapshot("PROJECT-A", "A3"), path);
                var current = store.Load(path);
                if (current.ProjectId != "PROJECT-A" || current.Id != "A3")
                    throw new Exception("Same-project recovery publication did not publish the new revision primary.");

                File.WriteAllText(path, "corrupt-again", new UTF8Encoding(false));
                var fallback = store.LoadWithBackupFallback(path);
                if (fallback.ProjectId != "PROJECT-A")
                    throw new Exception("Same-project revision fallback lost project identity.");
            });
        }

        private static RevisionSnapshot Snapshot(string projectId, string id) => new RevisionSnapshot
        {
            ProjectId = projectId,
            Id = id,
            CreatedUtc = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        private static void ExpectIdentityFailure(Action action)
        {
            try
            {
                action();
                throw new Exception("Revision store accepted a replacement across project identity.");
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf("project identity", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }
        }

        private static void EqualBytes(byte[] expected, byte[] actual, string label)
        {
            if (expected.Length != actual.Length) throw new Exception(label + " length changed.");
            for (var i = 0; i < expected.Length; i++)
                if (expected[i] != actual[i]) throw new Exception(label + " changed at byte " + i + ".");
        }

        private static void WithStore(Action<RevisionSnapshotStore, string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-revision-backup-project-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                action(new RevisionSnapshotStore(), Path.Combine(directory, "baseline.qsrev"));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }
    }

    internal static class RevisionSnapshotBackupProjectIdentityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RevisionSnapshotBackupProjectIdentitySmoke.Run();
    }
}
