using System;
using System.IO;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateRequiredAttributeCanonicalitySmoke
    {
        internal static void Run()
        {
            const string canonical = "<qs3dTemplate schema=\"1\" id=\"profile\" name=\"Profile\"><families><family id=\"wall\" name=\"Wall\" category=\"ArchitecturalWall\"><properties><p name=\"ThicknessM\" value=\" 0.20 \" /></properties></family></families><rules><rule id=\"wall-net\" category=\"ArchitecturalWall\" output=\"NetVolumeM3\" expression=\"Length*Height*Thickness\" version=\"1\" /></rules><layerMappings></layerMappings><bqColumns></bqColumns></qs3dTemplate>";

            var store = new TemplateProfileStore();
            var canonicalProfile = Load(store, canonical);
            if (canonicalProfile.Families.Count != 1 || canonicalProfile.QuantityRules.Count != 1)
                throw new InvalidOperationException("Canonical template did not load expected family/rule content.");
            if (!string.Equals(canonicalProfile.Families[0].Properties["ThicknessM"], " 0.20 ", StringComparison.Ordinal))
                throw new InvalidOperationException("Free-form template property values must preserve surrounding whitespace.");

            Reject(store, canonical.Replace("schema=\"1\"", "schema=\" 1\""), "root schema");
            Reject(store, canonical.Replace("id=\"profile\"", "id=\"profile \""), "root id");
            Reject(store, canonical.Replace("id=\"wall\"", "id=\" wall\""), "family id");
            Reject(store, canonical.Replace("name=\"ThicknessM\"", "name=\"ThicknessM \""), "family property name");
            Reject(store, canonical.Replace("output=\"NetVolumeM3\"", "output=\" NetVolumeM3\""), "rule output");
            Reject(store, canonical.Replace("version=\"1\"", "version=\"1 \""), "rule version");
        }

        private static TemplateProfile Load(TemplateProfileStore store, string xml)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-template-required-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                File.WriteAllText(path, xml);
                return store.Load(path);
            }
            finally
            {
                try { File.Delete(path); } catch { }
            }
        }

        private static void Reject(TemplateProfileStore store, string xml, string label)
        {
            try
            {
                Load(store, xml);
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new InvalidOperationException("Expected InvalidDataException for padded required template attribute: " + label);
        }
    }
}
