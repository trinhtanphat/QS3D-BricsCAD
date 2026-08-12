using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateBqColumnCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedPersistedColumn();
            RejectsDuplicatePersistedColumn();
            RejectsNonCanonicalPersistedOrder();
            PreservesProgrammaticNormalizationAndCanonicalRoundTrip();
        }

        private static void RejectsPaddedPersistedColumn()
        {
            WithCanonicalTemplate(path =>
            {
                Mutate(path, columns => columns.Elements("column").First().SetAttributeValue("name", " Area "));
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "padded column");
            });
        }

        private static void RejectsDuplicatePersistedColumn()
        {
            WithCanonicalTemplate(path =>
            {
                Mutate(path, columns => columns.Add(new XElement("column", new XAttribute("name", "area"))));
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "case-insensitive duplicate column");
            });
        }

        private static void RejectsNonCanonicalPersistedOrder()
        {
            WithCanonicalTemplate(path =>
            {
                Mutate(path, columns =>
                {
                    var values = columns.Elements("column").Select(x => new XElement(x)).Reverse().ToArray();
                    columns.ReplaceNodes(values);
                });
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path), "reversed column order");
            });
        }

        private static void PreservesProgrammaticNormalizationAndCanonicalRoundTrip()
        {
            WithPath(path =>
            {
                var store = new TemplateProfileStore();
                var profile = new TemplateProfile("bq-programmatic", "BQ Programmatic");
                profile.VisibleBqColumns.Add("Count");
                profile.VisibleBqColumns.Add(" Area ");
                profile.VisibleBqColumns.Add("COUNT");
                store.Save(profile, path);

                var loaded = store.Load(path);
                Equal(2, loaded.VisibleBqColumns.Count, "canonical column count");
                Equal("Area", loaded.VisibleBqColumns[0], "canonical first column");
                Equal("Count", loaded.VisibleBqColumns[1], "canonical second column");
            });
        }

        private static void WithCanonicalTemplate(Action<string> action)
        {
            WithPath(path =>
            {
                var profile = new TemplateProfile("bq-canonical", "BQ Canonical");
                profile.VisibleBqColumns.Add("Area");
                profile.VisibleBqColumns.Add("Count");
                new TemplateProfileStore().Save(profile, path);
                action(path);
            });
        }

        private static void Mutate(string path, Action<XElement> mutation)
        {
            var document = XDocument.Load(path, LoadOptions.None);
            var columns = document.Root?.Element("bqColumns") ?? throw new Exception("TemplateBqColumnCanonicalitySmoke fixture missing bqColumns.");
            mutation(columns);
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static void WithPath(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-template-bq-" + Guid.NewGuid().ToString("N"));
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
            throw new Exception("TemplateBqColumnCanonicalitySmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("TemplateBqColumnCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
