using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbSaveSizePreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OversizedSerializationFailsBeforeFilesystemMutationAndRestoresStamp();
            NormalProjectStillRoundTrips();
        }

        private static void OversizedSerializationFailsBeforeFilesystemMutationAndRestoresStamp()
        {
            var root = TempRoot("oversized");
            var path = Path.Combine(root, "project.qsdb");
            var project = Project("QSDB-SIZE-LIMIT");
            project.Metadata["Payload"] = new string('x', 4096);
            project.SchemaVersion = 2;
            project.Touch();

            var schemaVersion = project.SchemaVersion;
            var changeVersion = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            try
            {
                var store = new QsdbProjectStore();
                var boundedSave = typeof(QsdbProjectStore).GetMethod(
                    "Save",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    types: new[] { typeof(ProjectState), typeof(string), typeof(long) },
                    modifiers: null) ?? throw new InvalidOperationException("QSDB bounded Save overload is unavailable.");

                try
                {
                    boundedSave.Invoke(store, new object[] { project, path, 512L });
                }
                catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException dataError)
                {
                    if (!string.Equals(dataError.Message, "QSDB project exceeds the maximum supported file size of 64 MiB.", StringComparison.Ordinal))
                        throw new InvalidOperationException("Unexpected QSDB save-size preflight error.", dataError);
                    Require(!Directory.Exists(root),
                        "Oversized QSDB save mutated the destination directory before serialized-size preflight failed.");
                    Require(project.SchemaVersion == schemaVersion,
                        "Oversized QSDB save did not restore the original SchemaVersion.");
                    Require(project.ChangeVersion == changeVersion,
                        "Oversized QSDB save did not restore the original ChangeVersion.");
                    Require(project.UpdatedUtc == updatedUtc,
                        "Oversized QSDB save did not restore the original UpdatedUtc.");
                    return;
                }

                throw new InvalidOperationException("Oversized QSDB serialization was not rejected by the bounded save preflight.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static void NormalProjectStillRoundTrips()
        {
            var root = TempRoot("valid");
            var path = Path.Combine(root, "project.qsdb");
            var project = Project("QSDB-SIZE-VALID");
            project.Metadata["Note"] = "normal";

            try
            {
                var store = new QsdbProjectStore();
                store.Save(project, path);
                var loaded = store.Load(path);

                Require(string.Equals(loaded.ProjectId, project.ProjectId, StringComparison.Ordinal),
                    "Valid QSDB project id did not round-trip after save-size preflight was added.");
                Require(loaded.Metadata.TryGetValue("Note", out var note) && string.Equals(note, "normal", StringComparison.Ordinal),
                    "Valid QSDB metadata did not round-trip after save-size preflight was added.");
                Require(loaded.ChangeVersion == project.ChangeVersion,
                    "Valid QSDB persisted ChangeVersion diverged from the post-save project stamp.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static ProjectState Project(string id)
        {
            return new ProjectState(id, "QSDB size preflight");
        }

        private static string TempRoot(string suffix) =>
            Path.Combine(Path.GetTempPath(), "QS3D-QsdbSize-" + suffix + "-" + Guid.NewGuid().ToString("N"));

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
