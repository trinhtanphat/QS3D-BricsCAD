using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementIdentityXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidPersistedIdentities();
            RejectedSettersPreservePriorState();
            SupplementaryUnicodeRoundTripsThroughQsdb();
        }

        private static void RejectsXmlInvalidPersistedIdentities()
        {
            Throws<ArgumentException>(() => new ProjectElement("E-\uD800", ElementCategory.Beam));
            Throws<ArgumentException>(() => new ProjectElement("E-FAMILY", ElementCategory.Beam, "F-\uD800", string.Empty, string.Empty));
            Throws<ArgumentException>(() => new ProjectElement("E-FLOOR", ElementCategory.Beam, string.Empty, "L-\uD800", string.Empty));
            Throws<ArgumentException>(() => new ProjectElement("E-ZONE", ElementCategory.Beam, string.Empty, string.Empty, "Z-\uD800"));

            var element = new ProjectElement("E-FP", ElementCategory.Beam);
            Throws<ArgumentException>(() => element.DrawingFingerprint = "DWG-\uD800");
        }

        private static void RejectedSettersPreservePriorState()
        {
            var element = new ProjectElement("E-ATOMIC", ElementCategory.Beam, "F-OLD", "L-OLD", "Z-OLD")
            {
                DrawingFingerprint = "DWG-OLD"
            };
            element.MarkClean(ElementDirtyFlags.All);
            var beforeDirty = element.Dirty;
            var beforeUpdatedUtc = element.UpdatedUtc;

            Throws<ArgumentException>(() => element.FamilyId = "F-\uD800");
            Equal("F-OLD", element.FamilyId, "Rejected FamilyId assignment");
            Require(element.Dirty == beforeDirty, "Rejected FamilyId assignment changed Dirty state.");
            Require(element.UpdatedUtc == beforeUpdatedUtc, "Rejected FamilyId assignment changed UpdatedUtc.");

            Throws<ArgumentException>(() => element.FloorId = "L-\uD800");
            Equal("L-OLD", element.FloorId, "Rejected FloorId assignment");
            Require(element.Dirty == beforeDirty, "Rejected FloorId assignment changed Dirty state.");
            Require(element.UpdatedUtc == beforeUpdatedUtc, "Rejected FloorId assignment changed UpdatedUtc.");

            Throws<ArgumentException>(() => element.ZoneId = "Z-\uD800");
            Equal("Z-OLD", element.ZoneId, "Rejected ZoneId assignment");
            Require(element.Dirty == beforeDirty, "Rejected ZoneId assignment changed Dirty state.");
            Require(element.UpdatedUtc == beforeUpdatedUtc, "Rejected ZoneId assignment changed UpdatedUtc.");

            Throws<ArgumentException>(() => element.DrawingFingerprint = "DWG-\uD800");
            Equal("DWG-OLD", element.DrawingFingerprint, "Rejected DrawingFingerprint assignment");
            Require(element.Dirty == beforeDirty, "Rejected DrawingFingerprint assignment changed Dirty state.");
            Require(element.UpdatedUtc == beforeUpdatedUtc, "Rejected DrawingFingerprint assignment changed UpdatedUtc.");
        }

        private static void SupplementaryUnicodeRoundTripsThroughQsdb()
        {
            const string compass = "\U0001F9ED";
            var familyId = "F-" + compass;
            var floorId = "L-" + compass;
            var zoneId = "Z-" + compass;
            var elementId = "E-" + compass;
            var fingerprint = "DWG-" + compass;

            var project = new ProjectState("ELEMENT-XML", "ProjectElement identity XML persistability");
            project.Families.Add(new ProjectFamily(familyId, "Family " + compass, ElementCategory.Beam));
            project.Floors.Add(new FloorDefinition(floorId, "Floor " + compass, 0d));
            project.Zones.Add(new ZoneDefinition(zoneId, "Zone " + compass));
            var element = new ProjectElement(elementId, ElementCategory.Beam, familyId, floorId, zoneId)
            {
                DrawingFingerprint = fingerprint
            };
            project.Elements.Add(element);

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-element-identity-xml-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);

            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var loadedElement = loaded.FindElement(elementId) ?? throw new InvalidOperationException("Supplementary-Unicode element did not round-trip.");

                Equal(elementId, loadedElement.Id, "Element id QSDB round-trip");
                Equal(familyId, loadedElement.FamilyId, "FamilyId QSDB round-trip");
                Equal(floorId, loadedElement.FloorId, "FloorId QSDB round-trip");
                Equal(zoneId, loadedElement.ZoneId, "ZoneId QSDB round-trip");
                Equal(fingerprint, loadedElement.DrawingFingerprint, "DrawingFingerprint QSDB round-trip");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " mismatch.");
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
