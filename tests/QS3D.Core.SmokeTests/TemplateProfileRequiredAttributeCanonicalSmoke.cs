using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Rules;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileRequiredAttributeCanonicalSmoke
    {
        public static void Run()
        {
            CanonicalRequiredAttributesRoundTrip();
            PaddedRequiredAttributesAreRejected();
            TemplateIdentityTextRejectsControlAndXmlInvalidCharacters();
            TemplateIdentityTextPreservesValidUnicodeAndSetterAtomicity();
        }

        private static void CanonicalRequiredAttributesRoundTrip()
        {
            WithCanonicalFixture((store, path) =>
            {
                var loaded = store.Load(path);
                Equal("TPL-1", loaded.Id, "Canonical template id changed on load.");
                Equal("Canonical template", loaded.Name, "Canonical template name changed on load.");
                Equal("F-WALL", loaded.Families[0].Id, "Canonical family id changed on load.");
                Equal("HeightM", FirstPropertyName(loaded.Families[0]), "Canonical family property name changed on load.");
                Equal("R-WALL", loaded.QuantityRules[0].Id, "Canonical rule id changed on load.");
                Equal("NetVolumeM3", loaded.QuantityRules[0].OutputName, "Canonical rule output changed on load.");
                Equal("Length*Height*Thickness", loaded.QuantityRules[0].Expression, "Canonical rule expression changed on load.");
                Equal("1", loaded.QuantityRules[0].Version, "Canonical rule version changed on load.");
            });
        }

        private static void PaddedRequiredAttributesAreRejected()
        {
            AssertRejected("root schema", document => Set(document.Root!, "schema", " 1 "));
            AssertRejected("root id", document => Set(document.Root!, "id", " TPL-1"));
            AssertRejected("root name", document => Set(document.Root!, "name", "Canonical template "));
            AssertRejected("family id", document => Set(First(document, "family"), "id", " F-WALL "));
            AssertRejected("family name", document => Set(First(document, "family"), "name", "Wall "));
            AssertRejected("property name", document => Set(First(document, "p"), "name", " HeightM "));
            AssertRejected("whitespace-only property name", document => Set(First(document, "p"), "name", "   "));
            AssertRejected("rule id", document => Set(First(document, "rule"), "id", " R-WALL "));
            AssertRejected("rule output", document => Set(First(document, "rule"), "output", " NetVolumeM3 "));
            AssertRejected("rule expression", document => Set(First(document, "rule"), "expression", " Length*Height*Thickness "));
            AssertRejected("rule version", document => Set(First(document, "rule"), "version", " 1 "));
        }

        private static void TemplateIdentityTextRejectsControlAndXmlInvalidCharacters()
        {
            var controls = new[] { '\0', '\t', '\n', '\u007F', '\u0085' };
            foreach (var control in controls)
            {
                Throws<ArgumentException>(() => new TemplateProfile("TPL" + control + "1", "Name"), "Template id must reject control characters.");
                Throws<ArgumentException>(() => new TemplateProfile("TPL-1", "Na" + control + "me"), "Template name must reject control characters.");
            }

            var invalidSurrogate = new string(new[] { '\uD800' });
            Throws<ArgumentException>(() => new TemplateProfile("TPL" + invalidSurrogate + "1", "Name"), "Template id must reject XML-invalid surrogate text.");
            Throws<ArgumentException>(() => new TemplateProfile("TPL-1", "Na" + invalidSurrogate + "me"), "Template name must reject XML-invalid surrogate text.");
        }

        private static void TemplateIdentityTextPreservesValidUnicodeAndSetterAtomicity()
        {
            var profile = new TemplateProfile(" TPL-Đ1 ", " Mẫu tiếng Việt ");
            Equal("TPL-Đ1", profile.Id, "Valid Unicode template id should retain existing trim normalization.");
            Equal("Mẫu tiếng Việt", profile.Name, "Valid Unicode template name should retain existing trim normalization.");

            profile.Name = " Tên mẫu mới ";
            Equal("Tên mẫu mới", profile.Name, "Valid Unicode template name setter should normalize surrounding whitespace.");

            Throws<ArgumentException>(() => profile.Name = "Tên\nkhông hợp lệ", "Mutable template name must reject control characters.");
            Equal("Tên mẫu mới", profile.Name, "Rejected mutable template name must not replace the previous canonical value.");
        }

        private static void AssertRejected(string label, Action<XDocument> mutate)
        {
            WithCanonicalFixture((store, path) =>
            {
                var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
                mutate(document);
                document.Save(path, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => store.Load(path), "Expected non-canonical required attribute rejection for " + label + ".");
            });
        }

        private static void WithCanonicalFixture(Action<TemplateProfileStore, string> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-template-required-attrs-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                var profile = new TemplateProfile("TPL-1", "Canonical template");
                var family = new ProjectFamily("F-WALL", "Wall", ElementCategory.ArchitecturalWall);
                family.Properties["HeightM"] = "3";
                profile.Families.Add(family);
                profile.QuantityRules.Add(new QuantityRule(
                    "R-WALL",
                    ElementCategory.ArchitecturalWall,
                    "NetVolumeM3",
                    "Length*Height*Thickness",
                    "1"));

                var store = new TemplateProfileStore();
                store.Save(profile, path);
                action(store, path);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
                TryDelete(path + ".tmp");
            }
        }

        private static XElement First(XDocument document, string localName)
        {
            foreach (var element in document.Descendants())
                if (string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal)) return element;
            throw new Exception("Fixture is missing element " + localName + ".");
        }

        private static string FirstPropertyName(ProjectFamily family)
        {
            foreach (var property in family.Properties) return property.Key;
            throw new Exception("Fixture family has no properties.");
        }

        private static void Set(XElement element, string attribute, string value) => element.SetAttributeValue(attribute, value);

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception(message);
        }
    }
}
