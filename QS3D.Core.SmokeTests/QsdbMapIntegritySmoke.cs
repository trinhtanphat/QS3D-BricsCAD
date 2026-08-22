using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbMapIntegritySmoke
    {
        internal static void Run()
        {
            DuplicateMetadataKeysFailClosedCaseInsensitively();
        }

        private static void DuplicateMetadataKeysFailClosedCaseInsensitively()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-duplicate-map-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = new ProjectState("dup-map", "Duplicate Map");
                project.Metadata["Contract"] = "first";
                var store = new QsdbProjectStore();
                store.Save(project, path);

                var document = XDocument.Load(path, LoadOptions.None);
                var metadata = document.Root?.Element("metadata") ?? throw new Exception("QSDB smoke fixture is missing metadata.");
                metadata.Add(new XElement("p", new XAttribute("name", "contract"), new XAttribute("value", "second")));
                document.Save(path, SaveOptions.DisableFormatting);

                var threw = false;
                try { store.Load(path); }
                catch (InvalidDataException ex) { threw = ex.Message.IndexOf("Duplicate QSDB map key", StringComparison.OrdinalIgnoreCase) >= 0; }
                if (!threw) throw new Exception("QSDB duplicate metadata keys must fail closed instead of using last-wins semantics.");
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }

    internal static class QsdbMapIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbMapIntegritySmoke.Run();
    }
}
