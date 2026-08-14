using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingReferenceIdCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CanonicalReferencesRemainAccepted();
            PaddedFloorReferenceFailsClosed();
            PaddedFamilyReferenceFailsClosed();
            PaddedZoneReferenceFailsClosed();
            BlankReferencesRemainAllowed();
        }

        private static void CanonicalReferencesRemainAccepted()
        {
            var project = CreateProject();
            var rows = DoorOpeningScheduleBuilder.Build(project);
            if (rows.Count != 1) throw new Exception("Expected one valid door schedule row.");
            if (!string.Equals(rows[0].Floor, "Floor One", StringComparison.Ordinal))
                throw new Exception("Canonical Floor reference did not resolve its label.");
            if (!string.Equals(rows[0].FamilyName, "Door Family", StringComparison.Ordinal))
                throw new Exception("Canonical Family reference did not resolve its label.");
        }

        private static void PaddedFloorReferenceFailsClosed()
        {
            var project = CreateProject();
            var element = project.Elements[0];
            element.FloorId = " F1 ";
            AssertEqual("F1", element.FloorId, "Floor relation setter did not canonicalize its value.");
            InjectRawRelation(element, "_floorId", " F1 ");
            AssertEqual(" F1 ", element.FloorId, "Raw padded Floor relation was not injected.");
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void PaddedFamilyReferenceFailsClosed()
        {
            var project = CreateProject();
            var element = project.Elements[0];
            element.FamilyId = " FAM1 ";
            AssertEqual("FAM1", element.FamilyId, "Family relation setter did not canonicalize its value.");
            InjectRawRelation(element, "_familyId", " FAM1 ");
            AssertEqual(" FAM1 ", element.FamilyId, "Raw padded Family relation was not injected.");
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void PaddedZoneReferenceFailsClosed()
        {
            var project = CreateProject();
            var element = project.Elements[0];
            element.ZoneId = " Z1 ";
            AssertEqual("Z1", element.ZoneId, "Zone relation setter did not canonicalize its value.");
            InjectRawRelation(element, "_zoneId", " Z1 ");
            AssertEqual(" Z1 ", element.ZoneId, "Raw padded Zone relation was not injected.");
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void BlankReferencesRemainAllowed()
        {
            var project = CreateProject();
            var element = project.Elements[0];
            element.FamilyId = string.Empty;
            element.FloorId = "   ";
            element.ZoneId = string.Empty;
            AssertEqual(string.Empty, element.FloorId, "Whitespace Floor relation did not normalize to unbound.");
            InjectRawRelation(element, "_floorId", "   ");
            AssertEqual("   ", element.FloorId, "Raw whitespace Floor relation was not injected.");
            _ = DoorOpeningScheduleBuilder.Build(project);
        }

        private static void InjectRawRelation(ProjectElement element, string fieldName, string rawValue)
        {
            var field = typeof(ProjectElement).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(string))
                throw new InvalidOperationException("ReportingReferenceIdCanonicalitySmoke cannot inject raw relation field " + fieldName + ".");
            field.SetValue(element, rawValue);
        }

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new Exception(message);
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("P", "Reporting reference ID canonicality");
            var floor = new FloorDefinition("F1", "Floor One", 0d);
            var zone = new ZoneDefinition("Z1", "Zone One");
            var family = new ProjectFamily("FAM1", "Door Family", ElementCategory.Door);
            family.Properties["WidthM"] = "0.9";
            family.Properties["HeightM"] = "2.1";
            project.Floors.Add(floor);
            project.Zones.Add(zone);
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Door, family.Id, floor.Id, zone.Id));
            return project;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
