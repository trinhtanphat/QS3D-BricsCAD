using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class RepositorySyntheticQsdbSmoke
    {
        internal static void Run()
        {
            var samplePath = FindRepositoryFile("samples", "generated", "QS3D-Sample.qsdb");
            var project = new QsdbProjectStore().Load(samplePath);

            if (project.SchemaVersion != ProjectState.CurrentSchemaVersion)
                throw new Exception("Repository QSDB sample did not migrate to the current schema.");
            if (project.ChangeVersion != 0L)
                throw new Exception("Repository QSDB sample changed the canonical persisted changeVersion.");
            if (project.Elements.Count == 0)
                throw new Exception("Repository QSDB sample unexpectedly contains no semantic elements.");

            RequireMetadata(project, DrawingUnitResolutionPolicy.BoundMetadataKey, LengthUnit.Meter.ToString());
            RequireMetadata(project, DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey, LengthUnit.Meter.ToString());
            RequireMetadata(project, DrawingUnitResolutionPolicy.BindingSourceMetadataKey, DrawingUnitResolutionSource.NativeInsunits.ToString());
            if (project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.OverrideMetadataKey))
                throw new Exception("Repository metre sample must use native INSUNITS binding, not a project override.");

            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(project.Metadata, true, LengthUnit.Meter);
            Console.WriteLine("PASS repository synthetic QSDB strict-load and metre-binding regression");
        }

        private static void RequireMetadata(ProjectState project, string key, string expected)
        {
            if (!project.Metadata.TryGetValue(key, out var actual) || !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new Exception("Repository QSDB sample metadata mismatch for " + key + ": " + (actual ?? "<missing>"));
        }

        private static string FindRepositoryFile(params string[] relativeParts)
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var candidate = current.FullName;
                for (var index = 0; index < relativeParts.Length; index++)
                    candidate = Path.Combine(candidate, relativeParts[index]);
                if (File.Exists(candidate)) return candidate;
                current = current.Parent;
            }

            throw new FileNotFoundException(
                "Could not locate repository-owned synthetic QSDB fixture from " + AppContext.BaseDirectory + ".",
                Path.Combine(relativeParts));
        }
    }
}
