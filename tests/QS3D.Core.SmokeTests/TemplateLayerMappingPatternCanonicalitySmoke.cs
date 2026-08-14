using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateLayerMappingPatternCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPersistedPattern(" A-BEAM");
            RejectsPersistedPattern("A-BEAM ");
            RejectsSaveBeforeFilesystemMutation(" A-BEAM");
            RejectsSaveBeforeFilesystemMutation("A-BEAM ");
            AcceptsCanonicalPattern();
        }

        private static void RejectsPersistedPattern(string replacement)
        {
            WithTemplate(path =>
            {
                ReplacePattern(path, replacement);
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "persisted layer pattern " + replacement);
            });
        }

        private static void RejectsSaveBeforeFilesystemMutation(string pattern)
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-template-layer-pattern-preflight-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "nested", "profile.qstemplate");
            var profile = new TemplateProfile("layer-pattern-preflight", "Layer Pattern Preflight");
            profile.LayerMappings[pattern] = "Beam";

            try
            {
                Throws<InvalidDataException>(() => new TemplateProfileStore().Save(profile, path), "save layer pattern " + pattern);
                if (Directory.Exists(root))
                    throw new Exception("TemplateLayerMappingPatternCanonicalitySmoke invalid in-memory pattern must fail before filesystem mutation: " + pattern);
            }
            finally
            {
                try { Directory.Delete(root, true); }
                catch { }
            }
        }

        private static void AcceptsCanonicalPattern()
        {
            WithTemplate(path =>
            {
                var loaded = new TemplateProfileStore().Load(path);
                Equal("Beam", loaded.LayerMappings["A-BEAM"], "canonical mapping pattern");
            });
        }

        private static void WithTemplate(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-template-layer-pattern-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "profile.qstemplate");
            Directory.CreateDirectory(directory);
            try
            {
                var profile = new TemplateProfile("layer-pattern", "Layer Pattern");
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

        private static void ReplacePattern(string path, string replacement)
        {
            const string canonical = "pattern=\"A-BEAM\"";
            var text = File.ReadAllText(path);
            if (text.IndexOf(canonical, StringComparison.Ordinal) < 0)
                throw new Exception("TemplateLayerMappingPatternCanonicalitySmoke fixture missing canonical A-BEAM pattern.");
            File.WriteAllText(path, text.Replace(canonical, "pattern=\"" + replacement + "\""));
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("TemplateLayerMappingPatternCanonicalitySmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("TemplateLayerMappingPatternCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
