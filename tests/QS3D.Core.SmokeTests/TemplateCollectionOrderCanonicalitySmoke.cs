using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Rules;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateCollectionOrderCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsReversedFamilies();
            RejectsReversedRules();
            RejectsReversedLayerMappings();
            RejectsReversedFamilyProperties();
            AcceptsCanonicalCollectionOrder();
        }

        private static void RejectsReversedFamilies()
        {
            WithCanonicalTemplate(path =>
            {
                ReverseChildren(path, "families", "family");
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "reversed families");
            });
        }

        private static void RejectsReversedRules()
        {
            WithCanonicalTemplate(path =>
            {
                ReverseChildren(path, "rules", "rule");
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "reversed rules");
            });
        }

        private static void RejectsReversedLayerMappings()
        {
            WithCanonicalTemplate(path =>
            {
                ReverseChildren(path, "layerMappings", "map");
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "reversed layer mappings");
            });
        }

        private static void RejectsReversedFamilyProperties()
        {
            WithCanonicalTemplate(path =>
            {
                var document = XDocument.Load(path, LoadOptions.None);
                var properties = document.Root?.Element("families")?.Elements("family").FirstOrDefault()?.Element("properties")
                    ?? throw new Exception("TemplateCollectionOrderCanonicalitySmoke fixture missing family properties.");
                var reversed = properties.Elements("p").Select(x => new XElement(x)).Reverse().ToArray();
                properties.ReplaceNodes(reversed);
                document.Save(path, SaveOptions.DisableFormatting);
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "reversed family properties");
            });
        }

        private static void AcceptsCanonicalCollectionOrder()
        {
            WithCanonicalTemplate(path =>
            {
                var loaded = new TemplateProfileStore().Load(path);
                Equal("A-FAMILY", loaded.Families[0].Id, "first family");
                Equal("B-FAMILY", loaded.Families[1].Id, "second family");
                Equal("A-RULE", loaded.QuantityRules[0].Id, "first rule");
                Equal("B-RULE", loaded.QuantityRules[1].Id, "second rule");
                Equal("Beam", loaded.LayerMappings["A-LAYER"], "A layer mapping");
                Equal("Slab", loaded.LayerMappings["B-LAYER"], "B layer mapping");
            });
        }

        private static void WithCanonicalTemplate(Action<string> action)
        {
            WithPath(path =>
            {
                var profile = new TemplateProfile("collection-order", "Collection Order");
                var familyB = new ProjectFamily("B-FAMILY", "B Family", ElementCategory.Slab);
                familyB.Properties["Beta"] = "2";
                familyB.Properties["Alpha"] = "1";
                var familyA = new ProjectFamily("A-FAMILY", "A Family", ElementCategory.Beam);
                familyA.Properties["Beta"] = "2";
                familyA.Properties["Alpha"] = "1";
                profile.Families.Add(familyB);
                profile.Families.Add(familyA);
                profile.QuantityRules.Add(new QuantityRule("B-RULE", ElementCategory.Slab, "BOutput", "1", "1"));
                profile.QuantityRules.Add(new QuantityRule("A-RULE", ElementCategory.Beam, "AOutput", "1", "1"));
                profile.LayerMappings["B-LAYER"] = "Slab";
                profile.LayerMappings["A-LAYER"] = "Beam";
                new TemplateProfileStore().Save(profile, path);
                action(path);
            });
        }

        private static void ReverseChildren(string path, string containerName, string childName)
        {
            var document = XDocument.Load(path, LoadOptions.None);
            var container = document.Root?.Element(containerName)
                ?? throw new Exception("TemplateCollectionOrderCanonicalitySmoke fixture missing " + containerName + ".");
            var reversed = container.Elements(childName).Select(x => new XElement(x)).Reverse().ToArray();
            container.ReplaceNodes(reversed);
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static void WithPath(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-template-order-" + Guid.NewGuid().ToString("N"));
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
            throw new Exception("TemplateCollectionOrderCanonicalitySmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("TemplateCollectionOrderCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
