using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbSchemaTokenCanonicalitySmoke
    {
        internal static void Run()
        {
            AcceptsCanonicalCurrentAndLegacyTokens();
            RejectsNonCanonicalAliases();
        }

        private static void AcceptsCanonicalCurrentAndLegacyTokens()
        {
            var path = TempPath("canonical");
            try
            {
                var canonical = WriteCanonicalFixture(path);
                var store = new QsdbProjectStore();

                Equal(ProjectState.CurrentSchemaVersion, store.Load(path).SchemaVersion, "current schema token");

                RewriteSchema(path, canonical, "1");
                Equal(ProjectState.CurrentSchemaVersion, store.Load(path).SchemaVersion, "legacy schema 1 token");

                RewriteSchema(path, canonical, "2");
                Equal(ProjectState.CurrentSchemaVersion, store.Load(path).SchemaVersion, "legacy schema 2 token");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void RejectsNonCanonicalAliases()
        {
            var path = TempPath("aliases");
            try
            {
                var canonical = WriteCanonicalFixture(path);
                var store = new QsdbProjectStore();

                AssertRejected(store, path, canonical, "03", "leading-zero alias");
                AssertRejected(store, path, canonical, "+3", "signed alias");
                AssertRejected(store, path, canonical, " 3 ", "whitespace alias");
            }
            finally
            {
                Cleanup(path);
            }
        }

        private static void AssertRejected(QsdbProjectStore store, string path, string canonical, string token, string label)
        {
            RewriteSchema(path, canonical, token);
            Throws<InvalidDataException>(() => store.Load(path), label);
        }

        private static string WriteCanonicalFixture(string path)
        {
            new QsdbProjectStore().Save(new ProjectState("P-QSDB-SCHEMA", "QSDB schema token smoke"), path);
            return File.ReadAllText(path);
        }

        private static void RewriteSchema(string path, string canonical, string token)
        {
            File.WriteAllText(path, canonical);
            var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
            var root = document.Root ?? throw new Exception("QsdbSchemaTokenCanonicalitySmoke: missing project root fixture.");
            root.SetAttributeValue("schema", token);
            document.Save(path, SaveOptions.DisableFormatting);
        }

        private static string TempPath(string label) =>
            Path.Combine(Path.GetTempPath(), "qs3d-schema-token-" + label + "-" + Guid.NewGuid().ToString("N") + ".qsdb");

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
            throw new Exception("QsdbSchemaTokenCanonicalitySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("QsdbSchemaTokenCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class QsdbSchemaTokenCanonicalitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbSchemaTokenCanonicalitySmoke.Run();
    }
}
