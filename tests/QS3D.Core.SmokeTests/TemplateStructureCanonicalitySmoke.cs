using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateStructureCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsMissingRootSection();
            RejectsReorderedRootSections();
            RejectsMissingFamilyPropertiesContainer();
            AcceptsCanonicalStructure();
        }

        private static void RejectsMissingRootSection()
        {
            WithCanonicalTemplate(path =>
            {
                Mutate(path, root => root.Element("rules")?.Remove());
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "missing rules section");
            });
        }

        private static void RejectsReorderedRootSections()
        {
            WithCanonicalTemplate(path =>
            {
                Mutate(path, root =>
                {
                    var columns = root.Element("bqColumns") ?? throw new Exception("TemplateStructureCanonicalitySmoke fixture missing bqColumns.");
                    columns.Remove();
                    root.AddFirst(columns);
                });
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "reordered root sections");
            });
        }

        private static void RejectsMissingFamilyPropertiesContainer()
        {
            WithCanonicalTemplate(path =>
            {
                Mutate(path, root => root.Element("families")?.Element("family")?.Element("properties")?.Remove());
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "missing family properties container");
            });
        }

        private static void AcceptsCanonicalStructure()
        {
            WithCanonicalTemplate(path =>
            {
                var loaded = new TemplateProfileStore().Load(path);
                Equal(1, loaded.Families.Count, "canonical family count");
                Equal(ElementCategory.Beam, loaded.Families[0].Category, "canonical family category");
            });
        }

        private static void WithCanonicalTemplate(Action<string> action)
        {
            WithPath(path =>
            {
                var profile = new TemplateProfile("structure", "Structure");
                profile.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));
                new TemplateProfileStore().Save(profile, path);
                action(path);
            });
        }

        private static void Mutate(string path, Action<XElement> mutation)
        {
            var document = XDocument.Load(path, LoadOptions.None);
            var root = document.Root ?? throw new Exception("TemplateStructureCanonicalitySmoke fixture missing root.");
            mutation(root);
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static void WithPath(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-template-structure-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "profile.qstemplate");
            Directory.CreateDirectory(directory);
            try { action(path); }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch { }
            }
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("TemplateStructureCanonicalitySmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("TemplateStructureCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
