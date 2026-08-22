using System;
using System.IO;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbActiveContextReferentialIntegritySmoke
    {
        internal static void Run()
        {
            RejectsOrphanActiveFloorId();
            RejectsOrphanActiveZoneId();
            AcceptsResolvedActiveContextIds();
        }

        private static void RejectsOrphanActiveFloorId()
        {
            WithProjectFile(
                "<qs3d schema=\"3\" projectId=\"P-ACTIVE-FLOOR\" name=\"Orphan active floor\" updatedUtc=\"2026-08-12T00:00:00.0000000Z\" changeVersion=\"0\" activeFloorId=\"F-MISSING\">" +
                "<metadata/><zones/><floors><floor id=\"F1\" name=\"Level 1\" elevationM=\"0\"/></floors><families/><rules/><elements/><audit/></qs3d>",
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsOrphanActiveZoneId()
        {
            WithProjectFile(
                "<qs3d schema=\"3\" projectId=\"P-ACTIVE-ZONE\" name=\"Orphan active zone\" updatedUtc=\"2026-08-12T00:00:00.0000000Z\" changeVersion=\"0\" activeZoneId=\"Z-MISSING\">" +
                "<metadata/><zones><zone id=\"Z1\" name=\"Zone 1\"/></zones><floors/><families/><rules/><elements/><audit/></qs3d>",
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void AcceptsResolvedActiveContextIds()
        {
            WithProjectFile(
                "<qs3d schema=\"3\" projectId=\"P-ACTIVE-VALID\" name=\"Resolved active context\" updatedUtc=\"2026-08-12T00:00:00.0000000Z\" changeVersion=\"0\" activeFloorId=\"F1\" activeZoneId=\"Z1\">" +
                "<metadata/><zones><zone id=\"Z1\" name=\"Zone 1\"/></zones><floors><floor id=\"F1\" name=\"Level 1\" elevationM=\"0\"/></floors><families/><rules/><elements/><audit/></qs3d>",
                path =>
                {
                    var project = new QsdbProjectStore().Load(path);
                    Equal("F1", project.ActiveFloorId, "Resolved active floor id changed during load.");
                    Equal("Z1", project.ActiveZoneId, "Resolved active zone id changed during load.");
                });
        }

        private static void WithProjectFile(string xml, Action<string> action)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-active-context-referential-" + Guid.NewGuid().ToString("N") + ".qsdb");
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

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
