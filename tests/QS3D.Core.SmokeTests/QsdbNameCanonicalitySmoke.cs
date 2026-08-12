using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbNameCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            PreservesCanonicalRoundTrip();
            RejectsPaddedProjectName();
            RejectsPaddedZoneName();
            RejectsPaddedFloorName();
            RejectsPaddedFamilyName();
        }

        private static void PreservesCanonicalRoundTrip()
        {
            WithFixture(path =>
            {
                var loaded = new QsdbProjectStore().Load(path);
                if (!string.Equals(loaded.Name, "Name canonicality", StringComparison.Ordinal) ||
                    !string.Equals(loaded.Zones[0].Name, "Zone 1", StringComparison.Ordinal) ||
                    !string.Equals(loaded.Floors[0].Name, "Level 1", StringComparison.Ordinal) ||
                    !string.Equals(loaded.Families[0].Name, "Beam Type", StringComparison.Ordinal))
                    throw new InvalidOperationException("Canonical QSDB display names must continue to round-trip unchanged.");
            });
        }

        private static void RejectsPaddedProjectName() => RejectPadded(
            document => document.Root?.Attribute("name"), "project name");

        private static void RejectsPaddedZoneName() => RejectPadded(
            document => document.Root?.Element("zones")?.Element("zone")?.Attribute("name"), "zone name");

        private static void RejectsPaddedFloorName() => RejectPadded(
            document => document.Root?.Element("floors")?.Element("floor")?.Attribute("name"), "floor name");

        private static void RejectsPaddedFamilyName() => RejectPadded(
            document => document.Root?.Element("families")?.Element("family")?.Attribute("name"), "family name");

        private static void RejectPadded(Func<XDocument, XAttribute?> selector, string surface)
        {
            WithFixture(path =>
            {
                var document = XDocument.Load(path, LoadOptions.None);
                var attribute = selector(document) ?? throw new InvalidOperationException("Name smoke fixture is missing " + surface + ".");
                attribute.Value = " " + attribute.Value + " ";
                document.Save(path, SaveOptions.DisableFormatting);
                ExpectInvalidData(path, surface);
            });
        }

        private static void WithFixture(Action<string> assertion)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-name-canonicality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "fixture.qsdb");
            try
            {
                var project = new ProjectState("NAME-CANON", "Name canonicality");
                project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
                project.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
                project.Families.Add(new ProjectFamily("FM1", "Beam Type", ElementCategory.Beam));
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

        private static void ExpectInvalidData(string path, string surface)
        {
            try
            {
                new QsdbProjectStore().Load(path);
                throw new InvalidOperationException("QSDB load must reject a padded persisted " + surface + ".");
            }
            catch (InvalidDataException)
            {
            }
        }
    }
}
