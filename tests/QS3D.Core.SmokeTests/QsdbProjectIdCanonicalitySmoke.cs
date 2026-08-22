using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbProjectIdCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-projectid-canonicality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "project.qsdb");

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(new ProjectState("PROJECT-ID", "Canonical Project"), path);

                var canonical = store.Load(path);
                if (!string.Equals(canonical.ProjectId, "PROJECT-ID", StringComparison.Ordinal))
                    throw new InvalidOperationException("Canonical QSDB ProjectId must round-trip unchanged.");

                var document = XDocument.Load(path, LoadOptions.None);
                var root = document.Root ?? throw new InvalidOperationException("Smoke QSDB is missing its root element.");
                root.SetAttributeValue("projectId", " PROJECT-ID ");
                document.Save(path, SaveOptions.DisableFormatting);

                try
                {
                    store.Load(path);
                }
                catch (InvalidDataException)
                {
                    return;
                }

                throw new InvalidOperationException("QSDB loader must reject a padded non-canonical projectId instead of trimming it into another identity.");
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
