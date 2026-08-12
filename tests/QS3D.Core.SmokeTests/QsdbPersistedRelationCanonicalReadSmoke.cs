using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbPersistedRelationCanonicalReadSmoke
    {
        internal static void Run()
        {
            RejectsPaddedActiveFloorId();
            RejectsPaddedElementFamilyId();
            RejectsPaddedSourceHandle();
            RejectsPaddedDependencyId();
            RejectsEmptySourceHandle();
            CanonicalRelationsStillLoad();
        }

        private static void RejectsPaddedActiveFloorId()
        {
            WithXml(BuildXml("activeFloorId=\" F1 \"", string.Empty), path =>
                Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsPaddedElementFamilyId()
        {
            WithXml(BuildXml(string.Empty, "familyId=\" FAM-1 \""), path =>
                Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsPaddedSourceHandle()
        {
            WithXml(BuildXml(string.Empty, string.Empty, "<handles><h> AB12 </h></handles>"), path =>
                Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsPaddedDependencyId()
        {
            WithXml(BuildXml(string.Empty, string.Empty, "<dependencies><d> E0 </d></dependencies>"), path =>
                Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsEmptySourceHandle()
        {
            WithXml(BuildXml(string.Empty, string.Empty, "<handles><h>   </h></handles>"), path =>
                Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void CanonicalRelationsStillLoad()
        {
            var children = "<handles><h>AB12</h></handles><dependencies><d>E0</d></dependencies>";
            WithXml(BuildXml("activeZoneId=\"Z1\" activeFloorId=\"F1\"", "familyId=\"FAM-1\" floorId=\"F1\" zoneId=\"Z1\"", children, includeCanonicalTargets: true), path =>
            {
                var project = new QsdbProjectStore().Load(path);
                Equal("F1", project.ActiveFloorId);
                Equal("Z1", project.ActiveZoneId);
                Equal(2, project.Elements.Count);
                var element = project.FindElement("E1") ?? throw new Exception("Expected element E1.");
                Equal("FAM-1", element.FamilyId);
                Equal("F1", element.FloorId);
                Equal("Z1", element.ZoneId);
                Equal("AB12", element.SourceHandles[0]);
                Equal("E0", element.DependsOn[0]);
            });
        }

        private static string BuildXml(
            string rootAttributes,
            string elementAttributes,
            string elementChildren = "",
            bool includeCanonicalTargets = false)
        {
            var relationTargets = includeCanonicalTargets
                ? "<zones><zone id=\"Z1\" name=\"Zone 1\"/></zones>" +
                  "<floors><floor id=\"F1\" name=\"Floor 1\" elevationM=\"0\"/></floors>" +
                  "<families><family id=\"FAM-1\" name=\"Beam Family\" category=\"Beam\"/></families>"
                : "<zones/><floors/><families/>";
            var dependencyTarget = includeCanonicalTargets
                ? "<element id=\"E0\" category=\"Beam\" dirty=\"15\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\"/>"
                : string.Empty;
            return
                "<qs3d schema=\"3\" projectId=\"P1\" name=\"Canonical relation read\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" changeVersion=\"0\" " + rootAttributes + ">" +
                "<metadata/>" + relationTargets + "<rules/><elements>" + dependencyTarget +
                "<element id=\"E1\" category=\"Beam\" dirty=\"15\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" " + elementAttributes + ">" +
                elementChildren +
                "</element></elements><audit/></qs3d>";
        }

        private static void WithXml(string xml, Action<string> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-relation-read-" + Guid.NewGuid().ToString("N") + ".qsdb");
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

    internal static class QsdbPersistedRelationCanonicalReadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbPersistedRelationCanonicalReadSmoke.Run();
    }
}
