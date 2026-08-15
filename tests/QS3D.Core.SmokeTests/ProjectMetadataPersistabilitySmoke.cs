using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMetadataPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("META-PERSIST", "Metadata persistability");
            var originalVersion = project.ChangeVersion;
            var originalUpdatedUtc = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc);
            project.UpdatedUtc = originalUpdatedUtc;
            const string key = "Display.Preference";
            const string value = "  line 1\nline 2  ";

            project.Metadata[key] = value;
            Equal(value, project.Metadata[key], "Valid generic metadata value");
            Equal(originalVersion + 1L, project.ChangeVersion, "Generic metadata semantic revision");
            if (project.UpdatedUtc <= originalUpdatedUtc)
                throw new InvalidOperationException("Generic metadata project timestamp did not advance.");

            RejectWithoutMutation(project, () => project.Metadata.Add("", "x"), key, value, "Blank metadata key");
            RejectWithoutMutation(project, () => project.Metadata[" padded "] = "x", key, value, "Padded metadata key");
            RejectWithoutMutation(project, () => project.Metadata["bad\u0001key"] = "x", key, value, "XML-illegal metadata key");
            RejectWithoutMutation(project, () => project.Metadata[key] = "bad\u0001value", key, value, "XML-illegal metadata value");

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-metadata-persistability-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                if (!loaded.Metadata.TryGetValue(key, out var loadedValue))
                    throw new InvalidOperationException("Valid generic metadata did not round-trip through QSDB.");
                Equal(value, loadedValue, "Generic metadata QSDB round-trip value");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void RejectWithoutMutation(ProjectState project, Action mutation, string existingKey, string existingValue, string label)
        {
            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var count = project.Metadata.Count;
            var rejected = false;
            try
            {
                mutation();
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            if (!rejected) throw new InvalidOperationException(label + " was accepted.");
            Equal(count, project.Metadata.Count, label + " metadata count");
            Equal(existingValue, project.Metadata[existingKey], label + " existing value");
            Equal(version, project.ChangeVersion, label + " semantic revision");
            Equal(updatedUtc, project.UpdatedUtc, label + " project timestamp");
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " mismatch. Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void Equal(long expected, long actual, string label)
        {
            if (expected != actual) throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual) throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void Equal(DateTime expected, DateTime actual, string label)
        {
            if (expected != actual) throw new InvalidOperationException(label + " changed unexpectedly.");
        }
    }
}
