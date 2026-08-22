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
        private const string CanonicalityMessage = "must not contain leading/trailing whitespace";
        private const string UnknownReferenceMessage = "does not reference an existing";

        private static readonly (string Attribute, string? Owner)[] References =
        {
            ("activeFloorId", null),
            ("activeZoneId", null),
            ("familyId", "element"),
            ("floorId", "element"),
            ("zoneId", "element")
        };

        [ModuleInitializer]
        internal static void Run()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-persisted-reference-canonicality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                AssertCanonicalReferencesRoundTrip(directory);
                AssertBlankOptionalReferencesRemainSupported(directory);
                AssertMissingOptionalElementReferencesRemainSupported(directory);

                foreach (var reference in References)
                {
                    AssertMalformedReferenceRejected(directory, reference.Attribute, reference.Owner, value => " " + value, "leading-space");
                    AssertMalformedReferenceRejected(directory, reference.Attribute, reference.Owner, value => value + " ", "trailing-space");
                    AssertMalformedReferenceRejected(directory, reference.Attribute, reference.Owner, value => " " + value + " ", "two-sided-space");
                    AssertMalformedReferenceRejected(directory, reference.Attribute, reference.Owner, _ => "   ", "space-only");
                    AssertMalformedReferenceRejected(directory, reference.Attribute, reference.Owner, value => "\t" + value, "leading-tab");
                    AssertMalformedReferenceRejected(directory, reference.Attribute, reference.Owner, value => value + "\t", "trailing-tab");
                    AssertMalformedReferenceRejected(directory, reference.Attribute, reference.Owner, value => "\u00A0" + value + "\u00A0", "nonbreaking-space");
                    AssertMalformedReferenceRejected(directory, reference.Attribute, reference.Owner, _ => "\u00A0", "nonbreaking-space-only");
                }

                AssertUnknownCanonicalReferencesStillFail(directory);
                AssertMalformedPrimaryRecoversFromCanonicalBackup(directory);
                AssertMalformedPrimaryAndBackupFailClosed(directory);
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
            Equal("F1", project.ActiveFloorId, "Canonical persisted active floor changed on load.");
            Equal("Z1", project.ActiveZoneId, "Canonical persisted active zone changed on load.");
            var element = project.Elements.Single(x => string.Equals(x.Id, "E1", StringComparison.Ordinal));
            Equal("FA1", element.FamilyId, "Canonical persisted family reference changed on load.");
            Equal("F1", element.FloorId, "Canonical persisted floor reference changed on load.");
            Equal("Z1", element.ZoneId, "Canonical persisted zone reference changed on load.");
        }

        private static void AssertBlankOptionalReferencesRemainSupported(string directory)
        {
            var document = CreateDocument();
            var root = Root(document);
            root.SetAttributeValue("activeFloorId", string.Empty);
            root.SetAttributeValue("activeZoneId", string.Empty);
            var element = root.Descendants("element").Single();
            element.SetAttributeValue("familyId", string.Empty);
            element.SetAttributeValue("floorId", string.Empty);
            element.SetAttributeValue("zoneId", string.Empty);
            var path = Path.Combine(directory, "blank-optionals.qsdb");
            document.Save(path, SaveOptions.DisableFormatting);
            var project = new QsdbProjectStore().Load(path);
            if (!string.IsNullOrEmpty(project.ActiveFloorId) || !string.IsNullOrEmpty(project.ActiveZoneId))
                throw new InvalidOperationException("Blank persisted active references must retain optional empty semantics.");
            var loaded = project.Elements.Single(x => string.Equals(x.Id, "E1", StringComparison.Ordinal));
            if (!string.IsNullOrEmpty(loaded.FamilyId) || !string.IsNullOrEmpty(loaded.FloorId) || !string.IsNullOrEmpty(loaded.ZoneId))
                throw new InvalidOperationException("Blank persisted element references must retain optional empty semantics.");
        }

        private static void AssertMissingOptionalElementReferencesRemainSupported(string directory)
        {
            var document = CreateDocument();
            var element = Root(document).Descendants("element").Single();
            element.Attribute("familyId")?.Remove();
            element.Attribute("floorId")?.Remove();
            element.Attribute("zoneId")?.Remove();
            var path = Path.Combine(directory, "missing-element-optionals.qsdb");
            document.Save(path, SaveOptions.DisableFormatting);
            var project = new QsdbProjectStore().Load(path);
            var loaded = project.Elements.Single(x => string.Equals(x.Id, "E1", StringComparison.Ordinal));
            if (!string.IsNullOrEmpty(loaded.FamilyId) || !string.IsNullOrEmpty(loaded.FloorId) || !string.IsNullOrEmpty(loaded.ZoneId))
                throw new InvalidOperationException("Missing optional element references must remain empty after public load.");
        }

        private static void AssertMalformedReferenceRejected(string directory, string attributeName, string? elementName, Func<string, string> mutate, string caseName)
        {
            var document = CreateDocument();
            var root = Root(document);
            var owner = elementName == null ? root : root.Descendants(elementName).Single();
            var attribute = owner.Attribute(attributeName) ?? throw new InvalidOperationException("Regression fixture is missing " + attributeName + ".");
            attribute.Value = mutate(attribute.Value);
            var path = Path.Combine(directory, caseName + "-" + attributeName + ".qsdb");
            document.Save(path, SaveOptions.DisableFormatting);
            AssertInvalidData(path, CanonicalityMessage, "Malformed persisted reference was silently accepted: " + caseName + "/" + attributeName + ".");
        }

        private static void AssertUnknownCanonicalReferencesStillFail(string directory)
        {
            foreach (var reference in References)
            {
                var document = CreateDocument();
                var root = Root(document);
                var owner = reference.Owner == null ? root : root.Descendants(reference.Owner).Single();
                owner.SetAttributeValue(reference.Attribute, "UNKNOWN");
                var path = Path.Combine(directory, "unknown-" + reference.Attribute + ".qsdb");
                document.Save(path, SaveOptions.DisableFormatting);
                AssertInvalidData(path, UnknownReferenceMessage, "Canonical-but-unknown reference was silently accepted: " + reference.Attribute + ".");
            }
        }

        private static void AssertMalformedPrimaryRecoversFromCanonicalBackup(string directory)
        {
            var path = Path.Combine(directory, "fallback-primary.qsdb");
            var primary = CreateDocument();
            Root(primary).SetAttributeValue("activeFloorId", " F1 ");
            primary.Save(path, SaveOptions.DisableFormatting);
            CreateDocument().Save(path + ".bak", SaveOptions.DisableFormatting);

            var result = new QsdbProjectStore().LoadWithBackupFallback(path);
            if (!result.RecoveredFromBackup)
                throw new InvalidOperationException("Padded primary persisted reference must trigger validated backup recovery.");
            Equal(path + ".bak", result.SourcePath, "Backup recovery reported the wrong source path.");
            if (result.PrimaryFailureMessage.IndexOf(CanonicalityMessage, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException("Backup recovery must preserve the canonicality failure reason from the primary QSDB.");
            Equal("F1", result.Project.ActiveFloorId, "Backup recovery returned a normalized value from the corrupt primary instead of the canonical backup.");
        }

        private static void AssertMalformedPrimaryAndBackupFailClosed(string directory)
        {
            var path = Path.Combine(directory, "fallback-both-invalid.qsdb");
            var primary = CreateDocument();
            Root(primary).SetAttributeValue("activeZoneId", " Z1 ");
            primary.Save(path, SaveOptions.DisableFormatting);

            var backup = CreateDocument();
            Root(backup).Descendants("element").Single().SetAttributeValue("familyId", " FA1 ");
            backup.Save(path + ".bak", SaveOptions.DisableFormatting);

            try
            {
                _ = new QsdbProjectStore().LoadWithBackupFallback(path);
                throw new InvalidOperationException("Two malformed persisted-reference files must not produce a recovered project.");
            }
            catch (InvalidDataException ex)
            {
                if (ex.Message.IndexOf("Both the QSDB project and its backup are invalid", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("Dual invalid persisted-reference recovery returned a non-deterministic diagnostic: " + ex.Message);
                if (!(ex.InnerException is AggregateException aggregate) || aggregate.InnerExceptions.Count != 2)
                    throw new InvalidOperationException("Dual invalid persisted-reference recovery must retain both primary and backup failure evidence.");
                foreach (var inner in aggregate.InnerExceptions)
                {
                    if (inner.Message.IndexOf(CanonicalityMessage, StringComparison.OrdinalIgnoreCase) < 0)
                        throw new InvalidOperationException("Dual invalid recovery lost a persisted-reference canonicality failure: " + inner.Message);
                }
            }
        }

        private static void AssertInvalidData(string path, string expectedMessageFragment, string failureMessage)
        {
            try
            {
                _ = new QsdbProjectStore().Load(path);
                throw new InvalidOperationException(failureMessage);
            }
            catch (InvalidDataException ex)
            {
                if (string.IsNullOrWhiteSpace(ex.Message) || ex.Message.IndexOf(expectedMessageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("QSDB persisted-reference rejection returned a non-deterministic diagnostic. Expected fragment '" + expectedMessageFragment + "', got '" + ex.Message + "'.");
            }
        }

        private static XElement Root(XDocument document) => document.Root ?? throw new InvalidOperationException("Regression fixture has no root.");

        private static void Equal(string expected, string? actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(message + " Expected '" + expected + "', got '" + (actual ?? "<null>") + "'.");
        }

        private static XDocument CreateDocument()
        {
            const string utc = "2026-08-17T00:00:00.0000000Z";
            return new XDocument(new XElement("qs3d",
                new XAttribute("schema", "4"), new XAttribute("projectId", "P1"), new XAttribute("name", "Persistence regression"),
                new XAttribute("updatedUtc", utc), new XAttribute("changeVersion", "0"), new XAttribute("drawingPath", string.Empty),
                new XAttribute("drawingFingerprint", string.Empty), new XAttribute("activeZoneId", "Z1"), new XAttribute("activeFloorId", "F1"),
                new XElement("metadata"),
                new XElement("zones", new XElement("zone", new XAttribute("id", "Z1"), new XAttribute("name", "Zone 1"))),
                new XElement("floors", new XElement("floor", new XAttribute("id", "F1"), new XAttribute("name", "Floor 1"), new XAttribute("elevationM", "0"))),
                new XElement("families", new XElement("family", new XAttribute("id", "FA1"), new XAttribute("name", "Family 1"), new XAttribute("category", "Room"), new XElement("properties"))),
                new XElement("rules"),
                new XElement("elements", new XElement("element", new XAttribute("id", "E1"), new XAttribute("category", "Room"),
                    new XAttribute("familyId", "FA1"), new XAttribute("floorId", "F1"), new XAttribute("zoneId", "Z1"),
                    new XAttribute("drawingFingerprint", string.Empty), new XAttribute("dirty", "0"), new XAttribute("updatedUtc", utc),
                    new XElement("handles"), new XElement("dependencies"), new XElement("properties"), new XElement("quantities"))),
                new XElement("audit")));
        }
    }
}
