using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbRelationTokenCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-relation-token-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);

            try
            {
                var store = new QsdbProjectStore();
                var project = new ProjectState("REL-TOKEN", "Relation token canonicality");
                var source = new ProjectElement("SOURCE", ElementCategory.Beam);
                source.SourceHandles.Add("AB");
                var dependent = new ProjectElement("DEPENDENT", ElementCategory.CustomQuantity);
                dependent.SourceHandles.Add("CD");
                dependent.DependsOn.Add(source.Id);
                project.Elements.Add(source);
                project.Elements.Add(dependent);

                store.Save(project, path);
                var canonical = File.ReadAllText(path);
                var roundTrip = store.Load(path);
                Require(roundTrip.FindElement("SOURCE")?.SourceHandles.Count == 1, "Canonical QSDB source handle did not round-trip.");
                Require(roundTrip.FindElement("DEPENDENT")?.DependsOn.Count == 1, "Canonical QSDB dependency did not round-trip.");

                Rejects(store, path, canonical, document => FirstHandle(document).Value = " AB ", "padded source handle");
                Rejects(store, path, canonical, document => FirstHandle(document).Value = "   ", "blank source handle");
                Rejects(store, path, canonical, document => FirstDependency(document).Value = " SOURCE ", "padded dependency");
                Rejects(store, path, canonical, document => FirstDependency(document).Value = "   ", "blank dependency");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Rejects(
            QsdbProjectStore store,
            string path,
            string canonical,
            Action<XDocument> mutate,
            string label)
        {
            var document = XDocument.Parse(canonical, LoadOptions.None);
            mutate(document);
            File.WriteAllText(path, document.ToString(SaveOptions.DisableFormatting));
            try
            {
                store.Load(path);
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new InvalidOperationException("QSDB loader accepted " + label + ".");
        }

        private static XElement FirstHandle(XDocument document) =>
            document.Root?.Element("elements")?.Element("element")?.Element("handles")?.Element("h")
            ?? throw new InvalidOperationException("QSDB relation-token smoke is missing its source handle fixture.");

        private static XElement FirstDependency(XDocument document)
        {
            var elements = document.Root?.Element("elements")?.Elements("element")
                ?? throw new InvalidOperationException("QSDB relation-token smoke is missing its element fixtures.");
            foreach (var element in elements)
            {
                var dependency = element.Element("dependencies")?.Element("d");
                if (dependency != null) return dependency;
            }
            throw new InvalidOperationException("QSDB relation-token smoke is missing its dependency fixture.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
