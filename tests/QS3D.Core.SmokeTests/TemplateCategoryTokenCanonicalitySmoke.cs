using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateCategoryTokenCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var numericBeam = ((int)ElementCategory.Beam).ToString(CultureInfo.InvariantCulture);
            RejectsFamilyCategory("beam");
            RejectsFamilyCategory(" Beam ");
            RejectsFamilyCategory(numericBeam);
            RejectsRuleCategory("beam");
            RejectsRuleCategory(" Beam ");
            RejectsRuleCategory(numericBeam);
            AcceptsCanonicalCategoryTokens();
        }

        private static void RejectsFamilyCategory(string replacement)
        {
            WithTemplatePath(path =>
            {
                var store = new TemplateProfileStore();
                var profile = new TemplateProfile("family-category", "Family Category");
                profile.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));
                store.Save(profile, path);
                ReplaceBeamCategory(path, replacement);
                Throws<InvalidDataException>(() => store.Load(path), "family category " + replacement);
            });
        }

        private static void RejectsRuleCategory(string replacement)
        {
            WithTemplatePath(path =>
            {
                var store = new TemplateProfileStore();
                var profile = new TemplateProfile("rule-category", "Rule Category");
                profile.QuantityRules.Add(new QuantityRule("beam-rule", ElementCategory.Beam, "Result", "1", "1"));
                store.Save(profile, path);
                ReplaceBeamCategory(path, replacement);
                Throws<InvalidDataException>(() => store.Load(path), "rule category " + replacement);
            });
        }

        private static void AcceptsCanonicalCategoryTokens()
        {
            WithTemplatePath(path =>
            {
                var store = new TemplateProfileStore();
                var profile = new TemplateProfile("canonical-category", "Canonical Category");
                profile.Families.Add(new ProjectFamily("beam", "Beam", ElementCategory.Beam));
                profile.QuantityRules.Add(new QuantityRule("slab-rule", ElementCategory.Slab, "Result", "1", "1"));
                store.Save(profile, path);

                var loaded = store.Load(path);
                Equal(ElementCategory.Beam, loaded.Families[0].Category, "canonical family category");
                Equal(ElementCategory.Slab, loaded.QuantityRules[0].Category, "canonical rule category");
            });
        }

        private static void ReplaceBeamCategory(string path, string replacement)
        {
            const string canonical = "category=\"Beam\"";
            var text = File.ReadAllText(path);
            if (text.IndexOf(canonical, StringComparison.Ordinal) < 0)
                throw new Exception("TemplateCategoryTokenCanonicalitySmoke fixture missing canonical Beam category.");
            File.WriteAllText(path, text.Replace(canonical, "category=\"" + replacement + "\""));
        }

        private static void WithTemplatePath(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-template-category-" + Guid.NewGuid().ToString("N"));
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
            throw new Exception("TemplateCategoryTokenCanonicalitySmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("TemplateCategoryTokenCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
