using System;
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
            project.Elements[0].FloorId = " F1 ";
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void PaddedFamilyReferenceFailsClosed()
        {
            var project = CreateProject();
            project.Elements[0].FamilyId = " FAM1 ";
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void PaddedZoneReferenceFailsClosed()
        {
            var project = CreateProject();
            project.Elements[0].ZoneId = " Z1 ";
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void BlankReferencesRemainAllowed()
        {
            var project = CreateProject();
            project.Elements[0].FamilyId = string.Empty;
            project.Elements[0].FloorId = "   ";
            project.Elements[0].ZoneId = string.Empty;
            _ = DoorOpeningScheduleBuilder.Build(project);
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
