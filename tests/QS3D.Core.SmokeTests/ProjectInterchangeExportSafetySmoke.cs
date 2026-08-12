using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeExportSafetySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsNonUtcBuild();
            FailedExportPreservesExistingDestination();
            SuccessfulExportReplacesDestination();
        }

        private static void RejectsNonUtcBuild()
        {
            var project = NewProject();
            CorruptUpdatedUtc(project, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Unspecified));
            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(project));
        }

        private static void FailedExportPreservesExistingDestination()
        {
            WithPath(path =>
            {
                File.WriteAllText(path, "old-good");
                var project = NewProject();
                CorruptUpdatedUtc(project, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Local));
                Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Export(path, project));
                Equal("old-good", File.ReadAllText(path), "failed export changed the existing destination");
            });
        }

        private static void SuccessfulExportReplacesDestination()
        {
            WithPath(path =>
            {
                File.WriteAllText(path, "old");
                ProjectInterchangeJsonExporter.Export(path, NewProject());
                var json = File.ReadAllText(path);
                if (!json.Contains("\"format\":\"QS3D.SemanticSnapshot\""))
                    throw new InvalidOperationException("ProjectInterchangeExportSafetySmoke: successful export did not publish the semantic snapshot.");
            });
        }

        private static ProjectState NewProject()
        {
            return new ProjectState("P-export-safe", "Export safe")
            {
                UpdatedUtc = new DateTime(2026, 8, 10, 5, 0, 0, DateTimeKind.Utc)
            };
        }

        private static void CorruptUpdatedUtc(ProjectState project, DateTime value)
        {
            var field = typeof(ProjectState).GetField("_updatedUtc", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException("ProjectInterchangeExportSafetySmoke could not access the timestamp backing field.");
            field.SetValue(project, value);
        }

        private static void WithPath(Action<string> action)
        {
            var dir = Path.Combine(Path.GetTempPath(), "qs3d-interchange-export-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try { action(Path.Combine(dir, "snapshot.json")); }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectInterchangeExportSafetySmoke expected " + typeof(T).Name + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("ProjectInterchangeExportSafetySmoke: " + message + ".");
        }
    }
}
