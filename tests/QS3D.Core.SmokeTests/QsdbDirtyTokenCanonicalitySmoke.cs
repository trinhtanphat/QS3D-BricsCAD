using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbDirtyTokenCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesCanonicalRoundTrip();
            RejectsSignedEquivalentToken();
            RejectsZeroPaddedEquivalentToken();
        }

        private static void PreservesCanonicalRoundTrip()
        {
            WithFixture(path =>
            {
                var loaded = new QsdbProjectStore().Load(path);
                if (loaded.Elements.Count != 1 || loaded.Elements[0].Dirty != ElementDirtyFlags.All)
                    throw new InvalidOperationException("Canonical QSDB dirty flags must continue to round-trip.");
            });
        }

        private static void RejectsSignedEquivalentToken()
        {
            WithFixture(path =>
            {
                MutateDirty(path, "+15");
                ExpectInvalidData(path, "signed dirty token");
            });
        }

        private static void RejectsZeroPaddedEquivalentToken()
        {
            WithFixture(path =>
            {
                MutateDirty(path, "015");
                ExpectInvalidData(path, "zero-padded dirty token");
            });
        }

        private static void WithFixture(Action<string> assertion)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-dirty-canonicality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "fixture.qsdb");
            try
            {
                var project = new ProjectState("DIRTY-CANON", "Dirty canonicality");
                project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam));
                new QsdbProjectStore().SaveNew(project, path);
                assertion(path);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void MutateDirty(string path, string replacement)
        {
            var document = XDocument.Load(path, LoadOptions.None);
            var attribute = document.Root?.Element("elements")?.Element("element")?.Attribute("dirty")
                ?? throw new InvalidOperationException("Dirty smoke fixture is missing the target attribute.");
            attribute.Value = replacement;
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static void ExpectInvalidData(string path, string surface)
        {
            try
            {
                new QsdbProjectStore().Load(path);
                throw new InvalidOperationException("QSDB load must reject a non-canonical equivalent " + surface + ".");
            }
            catch (InvalidDataException)
            {
            }
        }
    }
}
