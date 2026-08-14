using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMetadataNullPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            NullValuesCanonicalizeAtMutationBoundary();
            CanonicalEmptyMetadataRoundTripsThroughQsdb();
        }

        private static void NullValuesCanonicalizeAtMutationBoundary()
        {
            var project = new ProjectState("PROJECT-METADATA-NULL", "Metadata Null");

            project.Metadata["Nullable.Indexer"] = null!;
            Equal(string.Empty, project.Metadata["Nullable.Indexer"]);

            project.Metadata.Add("Nullable.Add", null!);
            Equal(string.Empty, project.Metadata["Nullable.Add"]);

            project.Metadata["Canonical.Value"] = "preserve exactly";
            Equal("preserve exactly", project.Metadata["Canonical.Value"]);
        }

        private static void CanonicalEmptyMetadataRoundTripsThroughQsdb()
        {
            var project = new ProjectState("PROJECT-METADATA-ROUNDTRIP", "Metadata Roundtrip");
            project.Metadata["Nullable.Value"] = null!;
            project.Metadata["Canonical.Value"] = "unchanged";

            var path = Path.Combine(
                Path.GetTempPath(),
                "qs3d-project-metadata-null-" + Guid.NewGuid().ToString("N") + ".qsdb");

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);

                Equal(string.Empty, loaded.Metadata["Nullable.Value"]);
                Equal("unchanged", loaded.Metadata["Canonical.Value"]);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Best-effort cleanup only; persistence assertions above are authoritative.
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
