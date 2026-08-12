using System;
using System.IO;
using QS3D.Core.Domain;
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
            RejectsOrphanElementFamilyId();
            RejectsOrphanElementFloorId();
            RejectsOrphanElementZoneId();
            AcceptsResolvedAndBlankElementReferences();
            RejectsOrphanElementReferenceBeforePublication();
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

        private static void RejectsOrphanElementFamilyId()
        {
            WithProjectFile(
                ProjectXml("familyId=\"F-MISSING\"", "<families><family id=\"F1\" name=\"Room family\" category=\"Room\"/></families>"),
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsOrphanElementFloorId()
        {
            WithProjectFile(
                ProjectXml("floorId=\"F-MISSING\"", "<floors><floor id=\"F1\" name=\"Level 1\" elevationM=\"0\"/></floors>"),
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void RejectsOrphanElementZoneId()
        {
            WithProjectFile(
                ProjectXml("zoneId=\"Z-MISSING\"", "<zones><zone id=\"Z1\" name=\"Zone 1\"/></zones>"),
                path => Throws<InvalidDataException>(() => new QsdbProjectStore().Load(path)));
        }

        private static void AcceptsResolvedAndBlankElementReferences()
        {
            var xml =
                "<qs3d schema=\"3\" projectId=\"P-ELEMENT-VALID\" name=\"Resolved element references\" updatedUtc=\"2026-08-12T00:00:00.0000000Z\" changeVersion=\"0\">" +
                "<metadata/><zones><zone id=\"Z1\" name=\"Zone 1\"/></zones>" +
                "<floors><floor id=\"FLOOR1\" name=\"Level 1\" elevationM=\"0\"/></floors>" +
                "<families><family id=\"FAMILY1\" name=\"Room family\" category=\"Room\"/></families><rules/>" +
                "<elements>" +
                "<element id=\"E1\" category=\"Room\" familyId=\"family1\" floorId=\"floor1\" zoneId=\"z1\" dirty=\"0\" updatedUtc=\"2026-08-12T00:00:00.0000000Z\"/>" +
                "<element id=\"E2\" category=\"Room\" familyId=\"\" floorId=\"\" zoneId=\"\" dirty=\"0\" updatedUtc=\"2026-08-12T00:00:00.0000000Z\"/>" +
                "</elements><audit/></qs3d>";

            WithProjectFile(
                xml,
                path =>
                {
                    var project = new QsdbProjectStore().Load(path);
                    Equal(2, project.Elements.Count, "Resolved/blank element reference fixture did not load both elements.");
                    Equal("family1", project.Elements[0].FamilyId, "Resolved family reference changed during load.");
                    Equal(string.Empty, project.Elements[1].FamilyId, "Blank optional family reference changed during load.");
                });
        }

        private static void RejectsOrphanElementReferenceBeforePublication()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-element-reference-publication-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var project = new ProjectState("P-ELEMENT-SAVE", "Orphan element publication");
                project.Elements.Add(new ProjectElement("E1", ElementCategory.Room, "F-MISSING", string.Empty, string.Empty));
                var beforeVersion = project.ChangeVersion;
                var beforeUpdatedUtc = project.UpdatedUtc;

                Throws<InvalidDataException>(() => new QsdbProjectStore().Save(project, path));

                if (File.Exists(path)) throw new Exception("Orphan element reference was published to a QSDB primary file.");
                Equal(beforeVersion, project.ChangeVersion, "Rejected QSDB publication changed ProjectState.ChangeVersion.");
                Equal(beforeUpdatedUtc, project.UpdatedUtc, "Rejected QSDB publication changed ProjectState.UpdatedUtc.");
            }
            finally
            {
                foreach (var candidate in new[] { path, path + ".bak", path + ".tmp" })
                {
                    try { if (File.Exists(candidate)) File.Delete(candidate); } catch { }
                }
            }
        }

        private static string ProjectXml(string elementReference, string catalogSection)
        {
            var zones = catalogSection.StartsWith("<zones>", StringComparison.Ordinal) ? catalogSection : "<zones/>";
            var floors = catalogSection.StartsWith("<floors>", StringComparison.Ordinal) ? catalogSection : "<floors/>";
            var families = catalogSection.StartsWith("<families>", StringComparison.Ordinal) ? catalogSection : "<families/>";
            return
                "<qs3d schema=\"3\" projectId=\"P-ELEMENT-ORPHAN\" name=\"Orphan element reference\" updatedUtc=\"2026-08-12T00:00:00.0000000Z\" changeVersion=\"0\">" +
                "<metadata/>" + zones + floors + families + "<rules/>" +
                "<elements><element id=\"E1\" category=\"Room\" " + elementReference + " dirty=\"0\" updatedUtc=\"2026-08-12T00:00:00.0000000Z\"/></elements>" +
                "<audit/></qs3d>";
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
