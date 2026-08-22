using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorServiceXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidCreateBeforeProjectMutation();
            RejectsXmlInvalidUpdateBeforeProjectMutation();
            SupplementaryUnicodeRoundTripsThroughServiceAndQsdb();
        }

        private static void RejectsXmlInvalidCreateBeforeProjectMutation()
        {
            var project = new ProjectState("FLOOR-SERVICE-CREATE", "Floor service create XML");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeActive = project.ActiveFloorId;
            var beforeCount = project.Floors.Count;

            Throws<ArgumentException>(() => ProjectFloorService.Create(project, "F-\uD800", "Valid floor", 0d));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Floor id create changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Floor id create changed project timestamp.");
            Require(project.ActiveFloorId == beforeActive, "XML-invalid Floor id create changed active Floor.");
            Require(project.Floors.Count == beforeCount, "XML-invalid Floor id create changed Floor collection.");

            Throws<ArgumentException>(() => ProjectFloorService.Create(project, "F-VALID", "Floor \uD800", 3.2d));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Floor name create changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Floor name create changed project timestamp.");
            Require(project.ActiveFloorId == beforeActive, "XML-invalid Floor name create changed active Floor.");
            Require(project.Floors.Count == beforeCount, "XML-invalid Floor name create changed Floor collection.");
        }

        private static void RejectsXmlInvalidUpdateBeforeProjectMutation()
        {
            var project = new ProjectState("FLOOR-SERVICE-UPDATE", "Floor service update XML");
            var floor = ProjectFloorService.Create(project, "F-UPDATE", "Original floor", 2.75d);
            var beforeName = floor.Name;
            var beforeElevation = floor.ElevationM;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeCount = project.Floors.Count;
            var beforeActive = project.ActiveFloorId;

            Throws<ArgumentException>(() => ProjectFloorService.Update(project, floor.Id, "Invalid \uD800 floor", 9.25d));

            Require(floor.Name == beforeName, "XML-invalid Floor update changed the prior Floor name.");
            Require(floor.ElevationM == beforeElevation, "XML-invalid Floor update changed the prior Floor elevation.");
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Floor update changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Floor update changed project timestamp.");
            Require(project.Floors.Count == beforeCount, "XML-invalid Floor update changed Floor collection.");
            Require(project.ActiveFloorId == beforeActive, "XML-invalid Floor update changed active Floor.");

            Throws<ArgumentException>(() => ProjectFloorService.Update(project, "F-\uD800", "Unused", 10d));
            Require(floor.Name == beforeName, "XML-invalid Floor lookup id changed Floor name.");
            Require(floor.ElevationM == beforeElevation, "XML-invalid Floor lookup id changed Floor elevation.");
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Floor lookup id changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Floor lookup id changed project timestamp.");
            Require(project.Floors.Count == beforeCount, "XML-invalid Floor lookup id changed Floor collection.");
            Require(project.ActiveFloorId == beforeActive, "XML-invalid Floor lookup id changed active Floor.");
        }

        private static void SupplementaryUnicodeRoundTripsThroughServiceAndQsdb()
        {
            const string marker = "\U0001F9ED";
            var floorId = "F-" + marker;
            var createdName = "Tầng " + marker;
            var updatedName = "Tầng cập nhật " + marker;
            var project = new ProjectState("FLOOR-SERVICE-ROUNDTRIP", "Floor service Unicode roundtrip");

            var floor = ProjectFloorService.Create(project, floorId, createdName, -1.25d);
            ProjectFloorService.Update(project, floor.Id, updatedName, 4.5d);

            Require(floor.Id == floorId, "Supplementary-Unicode Floor id changed in service memory.");
            Require(floor.Name == updatedName, "Supplementary-Unicode Floor update did not preserve exact text.");
            Require(floor.ElevationM == 4.5d, "Supplementary-Unicode Floor update changed elevation unexpectedly.");

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-floor-service-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var roundTripped = loaded.FindFloor(floorId) ?? throw new InvalidOperationException("Supplementary-Unicode Floor was not found after QSDB round-trip.");
                Require(roundTripped.Id == floorId, "Supplementary-Unicode Floor id changed across QSDB round-trip.");
                Require(roundTripped.Name == updatedName, "Supplementary-Unicode Floor name changed across QSDB round-trip.");
                Require(roundTripped.ElevationM == 4.5d, "Floor elevation changed across QSDB round-trip.");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
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
    }
}
