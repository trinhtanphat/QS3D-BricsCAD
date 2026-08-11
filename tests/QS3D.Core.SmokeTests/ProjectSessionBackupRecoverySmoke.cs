using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectSessionBackupRecoverySmoke
    {
        public static void Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-project-session-recovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "project.qsdb");
            try
            {
                var store = new QsdbProjectStore();
                var baseline = new ProjectState("P-SESSION-RECOVERY", "Project session recovery");
                baseline.Metadata["marker"] = "backup";
                store.SaveNew(baseline, path);
                baseline.Metadata["marker"] = "primary-before-corrupt";
                store.Save(baseline, path);

                using (var unlocked = new ProjectSession(new ProjectState("P-UNLOCKED", "Unlocked"), path, store))
                {
                    Throws<InvalidOperationException>(() => unlocked.Reload());
                    Throws<InvalidOperationException>(() => unlocked.Save());
                }

                File.WriteAllText(path, "corrupt primary");

                using var session = new ProjectSession(new ProjectState("P-SEED", "Seed"), path, store);
                session.AcquireWriteLock();
                session.Reload();
                Equal("P-SESSION-RECOVERY", session.Project.ProjectId);
                Equal("backup", session.Project.Metadata["marker"]);

                session.Project.Metadata["marker"] = "repaired";
                var validatedBackup = File.ReadAllBytes(path + ".bak");
                File.Delete(path + ".bak");
                Throws<FileNotFoundException>(() => session.Save());
                Equal("repaired", session.Project.Metadata["marker"]);

                File.WriteAllBytes(path + ".bak", validatedBackup);
                session.Save();
                Equal("repaired", store.Load(path).Metadata["marker"]);
                Equal("backup", store.Load(path + ".bak").Metadata["marker"]);

                session.Reload();
                Equal("repaired", session.Project.Metadata["marker"]);
                session.Project.Metadata["marker"] = "normal-save";
                session.Save();
                Equal("normal-save", store.Load(path).Metadata["marker"]);
                Equal("repaired", store.Load(path + ".bak").Metadata["marker"]);

                var stableProject = session.Project;
                File.WriteAllText(path, "corrupt primary again");
                File.WriteAllText(path + ".bak", "corrupt backup too");
                Throws<InvalidDataException>(() => session.Reload());
                Same(stableProject, session.Project);
                Equal("normal-save", session.Project.Metadata["marker"]);
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Same(object expected, object? actual)
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected same object reference.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Expected " + typeof(T).Name + ", got " + ex.GetType().Name + ".", ex);
            }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectSessionBackupRecoverySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectSessionBackupRecoverySmoke.Run();
    }
}
