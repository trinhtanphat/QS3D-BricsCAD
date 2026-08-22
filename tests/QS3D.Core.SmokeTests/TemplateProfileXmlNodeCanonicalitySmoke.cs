using System;
using System.IO;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateProfileXmlNodeCanonicalitySmoke
    {
        private const string CanonicalXml = "<qs3dTemplate schema=\"1\" id=\"T1\" name=\"Template\"><families /><rules /><layerMappings /><bqColumns /></qs3dTemplate>";

        internal static void Run()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-template-xml-node-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                CanonicalTemplateStillLoads(directory);
                XmlDeclarationStillLoads(directory);
                RejectsRootWhitespaceCData(directory);
                RejectsDocumentComment(directory);
                RejectsDocumentProcessingInstruction(directory);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void CanonicalTemplateStillLoads(string directory)
        {
            var profile = Load(directory, "canonical.qs3dt", CanonicalXml);
            Require(profile.Id == "T1" && profile.Name == "Template", "Canonical template XML did not preserve profile identity.");
        }

        private static void XmlDeclarationStillLoads(string directory)
        {
            var profile = Load(directory, "declaration.qs3dt", "<?xml version=\"1.0\"?>" + CanonicalXml);
            Require(profile.Id == "T1", "XML declaration changed template profile identity.");
        }

        private static void RejectsRootWhitespaceCData(string directory)
        {
            var mutated = CanonicalXml.Replace("<families />", "<![CDATA[ ]]><families />", StringComparison.Ordinal);
            Throws<InvalidDataException>(() => Load(directory, "cdata.qs3dt", mutated));
        }

        private static void RejectsDocumentComment(string directory)
        {
            Throws<InvalidDataException>(() => Load(directory, "comment.qs3dt", "<!--non-canonical-->" + CanonicalXml));
        }

        private static void RejectsDocumentProcessingInstruction(string directory)
        {
            Throws<InvalidDataException>(() => Load(directory, "pi.qs3dt", "<?qs3d non-canonical?>" + CanonicalXml));
        }

        private static TemplateProfile Load(string directory, string name, string xml)
        {
            var path = Path.Combine(directory, name);
            File.WriteAllText(path, xml);
            return new TemplateProfileStore().Load(path);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
