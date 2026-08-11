using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbPrimaryIdCanonicalReadSmoke
    {
        internal static void Run()
        {
            RejectsPaddedProjectId();
            RejectsPaddedZoneId();
            RejectsPaddedFamilyId();
            RejectsPaddedElementId();
            RejectsPaddedRuleId();
            RejectsPaddedRuleOutput();
            RejectsPaddedQuantityName();
            CanonicalPrimaryIdsStillLoad();
        }

        private static void RejectsPaddedProjectId() => Reject(BuildXml().Replace("projectId=\"P1\"", "projectId=\" P1 \""));
        private static void RejectsPaddedZoneId() => Reject(BuildXml().Replace("<zone id=\"Z1\"", "<zone id=\" Z1 \""));
        private static void RejectsPaddedFamilyId() => Reject(BuildXml().Replace("<family id=\"FAM-1\"", "<family id=\" FAM-1 \""));
        private static void RejectsPaddedElementId() => Reject(BuildXml().Replace("<element id=\"E1\"", "<element id=\" E1 \""));
        private static void RejectsPaddedRuleId() => Reject(BuildXml().Replace("<rule id=\"R1\"", "<rule id=\" R1 \""));
        private static void RejectsPaddedRuleOutput() => Reject(BuildXml().Replace("output=\"NetVolumeM3\"", "output=\" NetVolumeM3 \""));
        private static void RejectsPaddedQuantityName() => Reject(BuildXml().Replace("<q name=\"ExistingM3\"", "<q name=\" ExistingM3 \""));

        private static void CanonicalPrimaryIdsStillLoad()
        {
            WithXml(BuildXml(), path =>
            {
                var project = new QsdbProjectStore().Load(path);
                Equal("P1", project.ProjectId);
                Equal("Z1", project.Zones[0].Id);
                Equal("F1", project.Floors[0].Id);
                Equal("FAM-1", project.Families[0].Id);
                Equal("R1", project.QuantityRules[0].Id);
                Equal("NetVolumeM3", project.QuantityRules[0].OutputName);
                Equal("E1", project.Elements[0].Id);
                Equal(2.5d, project.Elements[0].Quantities["ExistingM3"]);
            });
        }

        private static void Reject(string xml) =>
            WithXml(xml, path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));

        private static string BuildXml()
        {
            return
                "<qs3d schema=\"3\" projectId=\"P1\" name=\"Primary identity read\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" changeVersion=\"0\">" +
                "<metadata/>" +
                "<zones><zone id=\"Z1\" name=\"Zone 1\"/></zones>" +
                "<floors><floor id=\"F1\" name=\"Floor 1\" elevationM=\"0\"/></floors>" +
                "<families><family id=\"FAM-1\" name=\"Beam Family\" category=\"Beam\"/></families>" +
                "<rules><rule id=\"R1\" category=\"Beam\" output=\"NetVolumeM3\" expression=\"ExistingM3\" version=\"1\"/></rules>" +
                "<elements><element id=\"E1\" category=\"Beam\" familyId=\"FAM-1\" floorId=\"F1\" zoneId=\"Z1\" dirty=\"15\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\">" +
                "<quantities><q name=\"ExistingM3\" value=\"2.5\"/></quantities>" +
                "</element></elements><audit/>" +
                "</qs3d>";
        }

        private static void WithXml(string xml, Action<string> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-primary-id-read-" + Guid.NewGuid().ToString("N") + ".qsdb");
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

    internal static class QsdbPrimaryIdCanonicalReadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QsdbPrimaryIdCanonicalReadSmoke.Run();
    }
}
