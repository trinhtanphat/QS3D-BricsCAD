using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbNamedCategoryTokenSmoke
    {
        internal static void Run()
        {
            RejectsNumericCategoryAliases();
            AcceptsCaseInsensitiveNamedTokens();
        }

        private static void RejectsNumericCategoryAliases()
        {
            var path = TempPath("numeric");
            try
            {
                WriteCanonicalFixture(path);
                var canonical = File.ReadAllText(path);
                var numeric = ((int)ElementCategory.ArchitecturalWall).ToString(CultureInfo.InvariantCulture);
                var store = new QsdbProjectStore();

                RewriteCategory(path, canonical, "families", "family", numeric);
                Throws<InvalidDataException>(() => store.Load(path), "numeric family category");

                RewriteCategory(path, canonical, "rules", "rule", numeric);
                Throws<InvalidDataException>(() => store.Load(path), "numeric rule category");

                RewriteCategory(path, canonical, "elements", "element", numeric);
                Throws<InvalidDataException>(() => store.Load(path), "numeric element category");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void AcceptsCaseInsensitiveNamedTokens()
        {
            var path = TempPath("named");
            try
            {
                WriteCanonicalFixture(path);
                var document = XDocument.Load(path);
                var token = ElementCategory.ArchitecturalWall.ToString().ToLowerInvariant();
                SetCategory(document, "families", "family", token);
                SetCategory(document, "rules", "rule", token);
                SetCategory(document, "elements", "element", token);
                document.Save(path, SaveOptions.DisableFormatting);

                var loaded = new QsdbProjectStore().Load(path);
                Equal(ElementCategory.ArchitecturalWall, loaded.Families[0].Category, "family named token");
                Equal(ElementCategory.ArchitecturalWall, loaded.QuantityRules[0].Category, "rule named token");
                Equal(ElementCategory.ArchitecturalWall, loaded.Elements[0].Category, "element named token");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void WriteCanonicalFixture(string path)
        {
            var project = new ProjectState("P-QSDB-CATEGORY", "QSDB category smoke");
            project.Families.Add(new ProjectFamily("F1", "Wall", ElementCategory.ArchitecturalWall));
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.ArchitecturalWall, "NetVolumeM3", "1", "1"));
            project.Elements.Add(new ProjectElement("E1", ElementCategory.ArchitecturalWall, "F1", "", ""));
            new QsdbProjectStore().Save(project, path);
        }

        private static void RewriteCategory(string path, string canonical, string section, string item, string token)
        {
            File.WriteAllText(path, canonical);
            var document = XDocument.Load(path);
            SetCategory(document, section, item, token);
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static void SetCategory(XDocument document, string section, string item, string token)
        {
            var element = document.Root?.Element(section)?.Element(item)
                ?? throw new Exception("QsdbNamedCategoryTokenSmoke: missing " + section + "/" + item + " fixture.");
            element.SetAttributeValue("category", token);
        }

        private static string TempPath(string label) =>
            Path.Combine(Path.GetTempPath(), "qs3d-category-token-" + label + "-" + Guid.NewGuid().ToString("N") + ".qsdb");

        private static void Cleanup(string path)
        {
            Delete(path);
            Delete(path + ".bak");
            Delete(path + ".lock");
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;
            var prefix = Path.GetFileName(path) + ".";
            foreach (var file in Directory.GetFiles(directory, prefix + "*.tmp")) Delete(file);
        }

        private static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("QsdbNamedCategoryTokenSmoke " + label + ": expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("QsdbNamedCategoryTokenSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class QsdbNamedCategoryTokenSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbNamedCategoryTokenSmoke.Run();
    }
}
