using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectZoneServiceXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidCreateBeforeProjectMutation();
            RejectsXmlInvalidUpdateBeforeProjectMutation();
            SupplementaryUnicodeRoundTripsThroughServiceAndQsdb();
        }

        private static void RejectsXmlInvalidCreateBeforeProjectMutation()
        {
            var project = new ProjectState("ZONE-SERVICE-CREATE", "Zone service create XML");
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeActive = project.ActiveZoneId;
            var beforeCount = project.Zones.Count;

            Throws<ArgumentException>(() => ProjectZoneService.Create(project, "Z-\uD800", "Valid zone"));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Zone id create changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Zone id create changed project timestamp.");
            Require(project.ActiveZoneId == beforeActive, "XML-invalid Zone id create changed active Zone.");
            Require(project.Zones.Count == beforeCount, "XML-invalid Zone id create changed Zone collection.");

            Throws<ArgumentException>(() => ProjectZoneService.Create(project, "Z-VALID", "Zone \uD800"));
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Zone name create changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Zone name create changed project timestamp.");
            Require(project.ActiveZoneId == beforeActive, "XML-invalid Zone name create changed active Zone.");
            Require(project.Zones.Count == beforeCount, "XML-invalid Zone name create changed Zone collection.");
        }

        private static void RejectsXmlInvalidUpdateBeforeProjectMutation()
        {
            var project = new ProjectState("ZONE-SERVICE-UPDATE", "Zone service update XML");
            var zone = ProjectZoneService.Create(project, "Z-UPDATE", "Original zone");
            var beforeName = zone.Name;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = project.UpdatedUtc;
            var beforeCount = project.Zones.Count;

            Throws<ArgumentException>(() => ProjectZoneService.Update(project, zone.Id, "Invalid \uD800 zone"));

            Require(zone.Name == beforeName, "XML-invalid Zone update changed the prior Zone name.");
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Zone update changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Zone update changed project timestamp.");
            Require(project.Zones.Count == beforeCount, "XML-invalid Zone update changed Zone collection.");

            Throws<ArgumentException>(() => ProjectZoneService.Update(project, "Z-\uD800", "Unused"));
            Require(zone.Name == beforeName, "XML-invalid Zone lookup id changed Zone name.");
            Require(project.ChangeVersion == beforeVersion, "XML-invalid Zone lookup id changed project revision.");
            Require(project.UpdatedUtc == beforeUpdatedUtc, "XML-invalid Zone lookup id changed project timestamp.");
        }

        private static void SupplementaryUnicodeRoundTripsThroughServiceAndQsdb()
        {
            const string compass = "\U0001F9ED";
            var zoneId = "Z-" + compass;
            var createdName = "Khu " + compass;
            var updatedName = "Khu cập nhật " + compass;
            var project = new ProjectState("ZONE-SERVICE-ROUNDTRIP", "Zone service Unicode roundtrip");

            var zone = ProjectZoneService.Create(project, zoneId, createdName);
            ProjectZoneService.Update(project, zone.Id, updatedName);

            Require(zone.Id == zoneId, "Supplementary-Unicode Zone id changed in service memory.");
            Require(zone.Name == updatedName, "Supplementary-Unicode Zone update did not preserve exact text.");

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-zone-service-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var roundTripped = loaded.FindZone(zoneId) ?? throw new InvalidOperationException("Supplementary-Unicode Zone was not found after QSDB round-trip.");
                Require(roundTripped.Id == zoneId, "Supplementary-Unicode Zone id changed across QSDB round-trip.");
                Require(roundTripped.Name == updatedName, "Supplementary-Unicode Zone name changed across QSDB round-trip.");
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
