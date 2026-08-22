using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbCDataCanonicalitySmoke
    {
        internal static void Run()
        {
            OrdinaryTextStillLoads();
            RejectsSourceHandleCData();
            RejectsDependencyCData();
        }

        private static void OrdinaryTextStillLoads()
        {
            WithSavedProject((store, path, xml) =>
            {
                var loaded = store.Load(path);
                Require(loaded.Elements.Count == 1, "Ordinary QSDB text control did not load its element.");
                Require(loaded.Elements[0].SourceHandles.Count == 1 && loaded.Elements[0].SourceHandles[0] == "AB12",
                    "Ordinary QSDB source-handle text changed.");
                Require(loaded.Elements[0].DependsOn.Count == 1 && loaded.Elements[0].DependsOn[0] == "HOST-1",
                    "Ordinary QSDB dependency text changed.");
            });
        }

        private static void RejectsSourceHandleCData()
        {
            WithSavedProject((store, path, xml) =>
            {
                var mutated = xml.Replace("<h>AB12</h>", "<h><![CDATA[AB12]]></h>", StringComparison.Ordinal);
                Require(!string.Equals(mutated, xml, StringComparison.Ordinal), "Source-handle fixture did not contain canonical ordinary text.");
                File.WriteAllText(path, mutated);
                Throws<InvalidDataException>(() => store.Load(path));
            });
        }

        private static void RejectsDependencyCData()
        {
            WithSavedProject((store, path, xml) =>
            {
                var mutated = xml.Replace("<d>HOST-1</d>", "<d><![CDATA[HOST-1]]></d>", StringComparison.Ordinal);
                Require(!string.Equals(mutated, xml, StringComparison.Ordinal), "Dependency fixture did not contain canonical ordinary text.");
                File.WriteAllText(path, mutated);
                Throws<InvalidDataException>(() => store.Load(path));
            });
        }

        private static void WithSavedProject(Action<QsdbProjectStore, string, string> assertion)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-cdata-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = new ProjectState("cdata-project", "CDATA canonicality");
                var element = new ProjectElement("E1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
                element.SourceHandles.Add("AB12");
                element.DependsOn.Add("HOST-1");
                project.Elements.Add(element);

                var store = new QsdbProjectStore();
                store.Save(project, path);
                var xml = File.ReadAllText(path);
                assertion(store, path, xml);
            }
            finally
            {
                SafeDelete(path);
                SafeDelete(path + ".bak");
                SafeDelete(path + ".tmp");
            }
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

        private static void SafeDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }
    }
}
