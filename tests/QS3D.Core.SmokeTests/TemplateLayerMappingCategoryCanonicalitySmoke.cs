using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateLayerMappingCategoryCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsCategory("beam");
            RejectsCategory(" Beam ");
            RejectsCategory(((int)ElementCategory.Beam).ToString(CultureInfo.InvariantCulture));
            RejectsSaveBeforeFilesystemMutation("beam");
            RejectsSaveBeforeFilesystemMutation(" Beam ");
            RejectsSaveBeforeFilesystemMutation(((int)ElementCategory.Beam).ToString(CultureInfo.InvariantCulture));
            AcceptsCanonicalCategory();
        }

        private static void RejectsCategory(string replacement)
        {
            WithTemplate(path =>
            {
                ReplaceCategory(path, replacement);
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "layer category " + replacement);
            });
        }

        private static void RejectsSaveBeforeFilesystemMutation(string category)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-template-layer-category-preflight-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "nested", "profile.qstemplate");
            var profile = new TemplateProfile("layer-category-preflight", "Layer Category Preflight");
            profile.LayerMappings["A-BEAM"] = category;

            try
            {
                Throws<InvalidDataException>(() => new TemplateProfileStore().Save(profile, path), "save layer category " + category);
                if (Directory.Exists(root))
                    throw new Exception("TemplateLayerMappingCategoryCanonicalitySmoke invalid in-memory layer category must fail before filesystem mutation: " + category);
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AcceptsCanonicalCategory()
        {
            WithTemplate(path =>
            {
                var loaded = new TemplateProfileStore().Load(path);
                Equal("Beam", loaded.LayerMappings["A-BEAM"], "canonical mapping category");
            });
        }

        private static void WithTemplate(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-template-layer-category-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "profile.qstemplate");
            Directory.CreateDirectory(directory);
            try
            {
                var profile = new TemplateProfile("layer-category", "Layer Category");
                profile.LayerMappings["A-BEAM"] = "Beam";
                new TemplateProfileStore().Save(profile, path);
                action(path);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch { }
            }
        }

        private static void ReplaceCategory(string path, string replacement)
        {
            const string canonical = "category=\"Beam\"";
            var text = File.ReadAllText(path);
            if (text.IndexOf(canonical, StringComparison.Ordinal) < 0)
                throw new Exception("TemplateLayerMappingCategoryCanonicalitySmoke fixture missing canonical Beam category.");
            File.WriteAllText(path, text.Replace(canonical, "category=\"" + replacement + "\""));
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("TemplateLayerMappingCategoryCanonicalitySmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("TemplateLayerMappingCategoryCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}