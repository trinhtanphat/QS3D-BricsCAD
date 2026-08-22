using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbChangeVersionCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesCanonicalRoundTrip();
            RejectsZeroPaddedEquivalentToken();
        }

        private static void PreservesCanonicalRoundTrip()
        {
            WithFixture((store, project, path) =>
            {
                var loaded = store.Load(path);
                if (loaded.ChangeVersion != project.ChangeVersion)
                    throw new InvalidOperationException("Canonical QSDB changeVersion must continue to round-trip unchanged.");
            });
        }

        private static void RejectsZeroPaddedEquivalentToken()
        {
            WithFixture((store, project, path) =>
            {
                var document = XDocument.Load(path, LoadOptions.None);
                var attribute = document.Root?.Attribute("changeVersion")
                    ?? throw new InvalidOperationException("changeVersion smoke fixture is missing the target attribute.");
                attribute.Value = "0" + attribute.Value;
                document.Save(path, SaveOptions.DisableFormatting);

                try
                {
                    store.Load(path);
                    throw new InvalidOperationException("QSDB load must reject a zero-padded equivalent changeVersion token.");
                }
                catch (InvalidDataException)
                {
                }
            });
        }

        private static void WithFixture(Action<QsdbProjectStore, ProjectState, string> assertion)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-changeversion-canonicality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "fixture.qsdb");
            try
            {
                var project = new ProjectState("CHANGEVERSION-CANON", "ChangeVersion canonicality");
                project.Touch();
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                assertion(store, project, path);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
