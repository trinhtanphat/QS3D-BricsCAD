using System;
using System.IO;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbTimestampValidationSmoke
    {
        public static void Run()
        {
            RejectsMissingCurrentRootTimestamp();
            RejectsMissingCurrentElementTimestamp();
            RejectsMissingCurrentAuditTimestamp();
            LegacyV1MissingTimestampsStillMigrates();
        }

        private static void RejectsMissingCurrentRootTimestamp()
        {
            WithProjectFile(
                "<qs3d schema=\"3\" projectId=\"P1\" name=\"Missing root timestamp\" changeVersion=\"0\"><metadata/><zones/><floors/><families/><rules/><elements/><audit/></qs3d>",
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsMissingCurrentElementTimestamp()
        {
            WithProjectFile(
                "<qs3d schema=\"3\" projectId=\"P2\" name=\"Missing element timestamp\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" changeVersion=\"0\">" +
                "<metadata/><zones/><floors/><families/><rules/><elements>" +
                "<element id=\"E1\" category=\"ArchitecturalWall\" dirty=\"15\"><handles/><dependencies/><properties/><quantities/></element>" +
                "</elements><audit/></qs3d>",
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsMissingCurrentAuditTimestamp()
        {
            WithProjectFile(
                "<qs3d schema=\"3\" projectId=\"P3\" name=\"Missing audit timestamp\" updatedUtc=\"2026-08-11T00:00:00.0000000Z\" changeVersion=\"0\">" +
                "<metadata/><zones/><floors/><families/><rules/><elements/><audit>" +
                "<event action=\"seed\" elementId=\"\" detail=\"\" actor=\"\" correlationId=\"\"/>" +
                "</audit></qs3d>",
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void LegacyV1MissingTimestampsStillMigrates()
        {
            WithProjectFile(
                "<qs3d schema=\"1\" projectId=\"legacy\" name=\"Legacy timestamps\">" +
                "<metadata/><zones/><floors/><families/><elements>" +
                "<element id=\"E1\" category=\"ArchitecturalWall\"><handles/><dependencies/><properties/><quantities/></element>" +
                "</elements></qs3d>",
                path =>
                {
                    var project = new QsdbProjectStore().Load(path);
                    Equal(DateTime.UnixEpoch, project.UpdatedUtc, "Legacy root timestamp was not synthesized during migration.");
                    Equal(DateTime.UnixEpoch, project.Elements[0].UpdatedUtc, "Legacy element timestamp was not synthesized during migration.");
                });
        }

        private static void WithProjectFile(string xml, Action<string> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-timestamp-validation-" + Guid.NewGuid().ToString("N") + ".qsdb");
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

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
