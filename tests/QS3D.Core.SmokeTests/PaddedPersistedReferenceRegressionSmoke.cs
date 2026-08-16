using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class PaddedPersistedReferenceRegressionSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-padded-persisted-reference-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                AssertCanonicalReferencesRoundTrip(directory);
                AssertBlankActiveReferencesRemainSupported(directory);
                AssertPaddedReferenceRejected(directory, "activeFloorId", null);
                AssertPaddedReferenceRejected(directory, "activeZoneId", null);
                AssertPaddedReferenceRejected(directory, "familyId", "element");
                AssertPaddedReferenceRejected(directory, "floorId", "element");
                AssertPaddedReferenceRejected(directory, "zoneId", "element");
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void AssertCanonicalReferencesRoundTrip(string directory)
        {
            var path = Path.Combine(directory, "canonical.qsdb");
            CreateDocument().Save(path, SaveOptions.DisableFormatting);

            var project = new QsdbProjectStore().Load(path);
            if (!string.Equals(project.ActiveFloorId, "F1", StringComparison.Ordinal) ||
                !string.Equals(project.ActiveZoneId, "Z1", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical persisted active references did not round-trip.");

            var element = project.Elements.Single(x => string.Equals(x.Id, "E1", StringComparison.Ordinal));
            if (!string.Equals(element.FamilyId, "FA1", StringComparison.Ordinal) ||
                !string.Equals(element.FloorId, "F1", StringComparison.Ordinal) ||
                !string.Equals(element.ZoneId, "Z1", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical persisted element references did not round-trip.");
        }

        private static void AssertBlankActiveReferencesRemainSupported(string directory)
        {
            var document = CreateDocument();
            var root = document.Root ?? throw new InvalidOperationException("Regression fixture has no root.");
            root.SetAttributeValue("activeFloorId", string.Empty);
            root.SetAttributeValue("activeZoneId", string.Empty);
            var path = Path.Combine(directory, "blank-active.qsdb");
            document.Save(path, SaveOptions.DisableFormatting);

            var project = new QsdbProjectStore().Load(path);
            if (!string.IsNullOrEmpty(project.ActiveFloorId) || !string.IsNullOrEmpty(project.ActiveZoneId))
                throw new InvalidOperationException("Blank persisted active references must retain blank/null semantics.");
        }

        private static void AssertPaddedReferenceRejected(string directory, string attributeName, string? elementName)
        {
            var document = CreateDocument();
            var root = document.Root ?? throw new InvalidOperationException("Regression fixture has no root.");
            var owner = elementName == null
                ? root
                : root.Descendants(elementName).Single();
            var attribute = owner.Attribute(attributeName) ?? throw new InvalidOperationException("Regression fixture is missing " + attributeName + ".");
            attribute.Value = " " + attribute.Value + " ";

            var path = Path.Combine(directory, "padded-" + attributeName + ".qsdb");
            document.Save(path, SaveOptions.DisableFormatting);

            try
            {
                _ = new QsdbProjectStore().Load(path);
                throw new InvalidOperationException("Padded persisted reference was silently accepted: " + attributeName + ".");
            }
            catch (InvalidDataException)
            {
                // Expected: current-schema validation must fail before Load can normalize the reference.
            }
        }

        private static XDocument CreateDocument()
        {
            const string utc = "2026-08-17T00:00:00.0000000Z";
            return new XDocument(
                new XElement("qs3d",
                    new XAttribute("schema", "4"),
                    new XAttribute("projectId", "P1"),
                    new XAttribute("name", "Persistence regression"),
                    new XAttribute("updatedUtc", utc),
                    new XAttribute("changeVersion", "0"),
                    new XAttribute("drawingPath", string.Empty),
                    new XAttribute("drawingFingerprint", string.Empty),
                    new XAttribute("activeZoneId", "Z1"),
                    new XAttribute("activeFloorId", "F1"),
                    new XElement("metadata"),
                    new XElement("zones",
                        new XElement("zone",
                            new XAttribute("id", "Z1"),
                            new XAttribute("name", "Zone 1"))),
                    new XElement("floors",
                        new XElement("floor",
                            new XAttribute("id", "F1"),
                            new XAttribute("name", "Floor 1"),
                            new XAttribute("elevationM", "0"))),
                    new XElement("families",
                        new XElement("family",
                            new XAttribute("id", "FA1"),
                            new XAttribute("name", "Family 1"),
                            new XAttribute("category", "Room"),
                            new XElement("properties"))),
                    new XElement("rules"),
                    new XElement("elements",
                        new XElement("element",
                            new XAttribute("id", "E1"),
                            new XAttribute("category", "Room"),
                            new XAttribute("familyId", "FA1"),
                            new XAttribute("floorId", "F1"),
                            new XAttribute("zoneId", "Z1"),
                            new XAttribute("drawingFingerprint", string.Empty),
                            new XAttribute("dirty", "0"),
                            new XAttribute("updatedUtc", utc),
                            new XElement("handles"),
                            new XElement("dependencies"),
                            new XElement("properties"),
                            new XElement("quantities"))),
                    new XElement("audit")));
        }
    }
}
