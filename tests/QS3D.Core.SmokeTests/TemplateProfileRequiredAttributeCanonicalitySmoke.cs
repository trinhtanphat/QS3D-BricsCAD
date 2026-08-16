using System;
using System.IO;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileRequiredAttributeCanonicalitySmoke
    {
        private const string Canonical =
            "<qs3dTemplate schema=\"1\" id=\"T1\" name=\"Mẫu QS3D\">" +
            "<families><family id=\"F1\" name=\"Family 1\" category=\"Wall\">" +
            "<properties><p name=\"Width\" value=\"  keep optional whitespace  \" /></properties>" +
            "</family></families>" +
            "<rules><rule id=\"R1\" category=\"Wall\" output=\"VolumeM3\" expression=\"VolumeM3\" version=\"1\" /></rules>" +
            "<layerMappings /><bqColumns />" +
            "</qs3dTemplate>";

        internal static void Run()
        {
            CanonicalRequiredAttributesLoadUnchanged();
            RejectsPaddedRootAttributes();
            RejectsPaddedFamilyAndPropertyAttributes();
            RejectsPaddedRuleAttributes();
            RejectsWhitespaceOnlyRequiredAttributes();
        }

        private static void CanonicalRequiredAttributesLoadUnchanged()
        {
            var profile = Load(Canonical);
            Assert(profile.Id == "T1", "canonical template id must remain unchanged");
            Assert(profile.Name == "Mẫu QS3D", "canonical Unicode template name must remain unchanged");
            Assert(profile.Families.Count == 1 && profile.Families[0].Id == "F1", "canonical family identity must load");
            Assert(profile.Families[0].Name == "Family 1", "canonical family name must load");
            Assert(profile.Families[0].Properties.TryGetValue("Width", out var value) && value == "  keep optional whitespace  ",
                "optional family-property values must not be canonicalized by the required-attribute guard");
            Assert(profile.QuantityRules.Count == 1 && profile.QuantityRules[0].Id == "R1", "canonical quantity rule must load");
        }

        private static void RejectsPaddedRootAttributes()
        {
            ExpectInvalid(Canonical.Replace("schema=\"1\"", "schema=\" 1 \""), "padded schema");
            ExpectInvalid(Canonical.Replace("id=\"T1\"", "id=\" T1\""), "leading-space root id");
            ExpectInvalid(Canonical.Replace("name=\"Mẫu QS3D\"", "name=\"Mẫu QS3D \""), "trailing-space root name");
        }

        private static void RejectsPaddedFamilyAndPropertyAttributes()
        {
            ExpectInvalid(Canonical.Replace("id=\"F1\"", "id=\" F1 \""), "padded family id");
            ExpectInvalid(Canonical.Replace("name=\"Family 1\"", "name=\" Family 1\""), "padded family name");
            ExpectInvalid(Canonical.Replace("name=\"Width\"", "name=\"Width \""), "padded property name");
        }

        private static void RejectsPaddedRuleAttributes()
        {
            ExpectInvalid(Canonical.Replace("id=\"R1\"", "id=\"R1 \""), "padded rule id");
            ExpectInvalid(Canonical.Replace("output=\"VolumeM3\"", "output=\" VolumeM3\""), "padded rule output");
            ExpectInvalid(Canonical.Replace("expression=\"VolumeM3\"", "expression=\"VolumeM3 \""), "padded rule expression");
            ExpectInvalid(Canonical.Replace("version=\"1\"", "version=\" 1\""), "padded rule version");
        }

        private static void RejectsWhitespaceOnlyRequiredAttributes()
        {
            ExpectInvalid(Canonical.Replace("id=\"T1\"", "id=\"   \""), "blank root id");
            ExpectInvalid(Canonical.Replace("name=\"Width\"", "name=\"\t\""), "blank property name");
        }

        private static TemplateProfile Load(string xml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-template-required-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                File.WriteAllText(path, xml);
                return new TemplateProfileStore().Load(path);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void ExpectInvalid(string xml, string label)
        {
            try
            {
                Load(xml);
                throw new InvalidOperationException("Expected InvalidDataException for " + label + ".");
            }
            catch (InvalidDataException)
            {
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Template required-attribute regression: " + message + ".");
        }
    }
}
