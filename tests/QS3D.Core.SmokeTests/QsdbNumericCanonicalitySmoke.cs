using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbNumericCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesCanonicalRoundTrip();
            RejectsEquivalentFloorElevationToken();
            RejectsEquivalentQuantityToken();
        }

        private static void PreservesCanonicalRoundTrip()
        {
            WithFixture(path =>
            {
                var loaded = new QsdbProjectStore().Load(path);
                if (loaded.Floors.Count != 1 || loaded.Floors[0].ElevationM != 1.25d)
                    throw new InvalidOperationException("Canonical QSDB floor elevation must continue to round-trip.");
                if (loaded.Elements.Count != 1 || !loaded.Elements[0].Quantities.TryGetValue("LengthM", out var value) || value != 2.5d)
                    throw new InvalidOperationException("Canonical QSDB quantity must continue to round-trip.");
            });
        }

        private static void RejectsEquivalentFloorElevationToken()
        {
            WithFixture(path =>
            {
                MutateNumeric(path, document => document.Root?.Element("floors")?.Element("floor")?.Attribute("elevationM"), "1.250");
                ExpectInvalidData(path, "floor elevationM");
            });
        }

        private static void RejectsEquivalentQuantityToken()
        {
            WithFixture(path =>
            {
                MutateNumeric(path, document => document.Root?.Element("elements")?.Element("element")?.Element("quantities")?.Element("q")?.Attribute("value"), "2.5e0");
                ExpectInvalidData(path, "element quantity value");
            });
        }

        private static void WithFixture(Action<string> assertion)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-numeric-canonicality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "fixture.qsdb");
            try
            {
                var project = new ProjectState("NUMERIC-CANON", "Numeric canonicality");
                project.Floors.Add(new FloorDefinition("F1", "Level 1", 1.25d));
                var element = new ProjectElement("E1", ElementCategory.Beam);
                element.SetQuantity("LengthM", 2.5d);
                project.Elements.Add(element);
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

        private static void MutateNumeric(string path, Func<XDocument, XAttribute?> selector, string replacement)
        {
            var document = XDocument.Load(path, LoadOptions.None);
            var attribute = selector(document) ?? throw new InvalidOperationException("Numeric smoke fixture is missing the target attribute.");
            attribute.Value = replacement;
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static void ExpectInvalidData(string path, string surface)
        {
            try
            {
                new QsdbProjectStore().Load(path);
                throw new InvalidOperationException("QSDB load must reject a non-canonical equivalent numeric token at " + surface + ".");
            }
            catch (InvalidDataException)
            {
            }
        }
    }
}
