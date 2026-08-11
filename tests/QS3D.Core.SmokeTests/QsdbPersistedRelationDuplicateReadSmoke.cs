using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbPersistedRelationDuplicateReadSmoke
    {
        internal static void Run()
        {
            RejectsCaseInsensitiveDuplicateSourceHandles();
            RejectsCaseInsensitiveDuplicateDependencies();
            UniqueCanonicalListsPreserveOrder();
        }

        private static void RejectsCaseInsensitiveDuplicateSourceHandles()
        {
            WithXml(
                BuildXml("<handles><h>AB12</h><h>ab12</h></handles>"),
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsCaseInsensitiveDuplicateDependencies()
        {
            WithXml(
                BuildXml("<dependencies><d>HOST-A</d><d>host-a</d></dependencies>"),
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void UniqueCanonicalListsPreserveOrder()
        {
            WithXml(
                BuildXml("<handles><h>AB12</h><h>CD34</h></handles><dependencies><d>HOST-B</d><d>HOST-A</d></dependencies>"),
                path =>
                {
                    var project = new QsdbProjectStore().Load(path);
                    var element = project.Elements[0];
                    Equal(2, element.SourceHandles.Count);
                    Equal("AB12", element.SourceHandles[0]);
                    Equal("CD34", element.SourceHandles[1]);
                    Equal(2, element.DependsOn.Count);
                    Equal("HOST-B", element.DependsOn[0]);
                    Equal("HOST-A", element.DependsOn[1]);
                });
        }

        private static string BuildXml(string children)
        {
            return
                "<qs3d schema=\"3\" projectId=\"P1\" name=\"Duplicate relation list\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" changeVersion=\"0\">" +
                "<metadata/><zones/><floors/><families/><rules/><elements>" +
                "<element id=\"E1\" category=\"Beam\" dirty=\"15\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\">" +
                children +
                "</element></elements><audit/></qs3d>";
        }

        private static void WithXml(string xml, Action<string> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-relation-duplicate-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                File.WriteAllText(path, xml);
                action(path);
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }
    }

    internal static class QsdbPersistedRelationDuplicateReadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbPersistedRelationDuplicateReadSmoke.Run();
    }
}
