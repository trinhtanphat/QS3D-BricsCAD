using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningScheduleSmoke
    {
        public static void Run()
        {
            GroupsDoorsByDimensionsAndDistinctHosts();
            InstanceOverrideSplitsFamilyInheritedRow();
            RejectsInvalidSemanticDimensions();
            RejectsDerivedAreaUnderflow();
        }

        private static void GroupsDoorsByDimensionsAndDistinctHosts()
        {
            var project = new ProjectState("p", "Door schedule");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            var family = new ProjectFamily("door-family", "Cửa D1", ElementCategory.Door);
            family.Properties["WidthM"] = "0.9";
            family.Properties["HeightM"] = "2.2";
            family.Properties["SillHeightM"] = "0";
            family.Properties["ThicknessM"] = "0.1";
            family.Properties["Material"] = "Gỗ";
            project.Families.Add(family);

            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Elements.Add(new ProjectElement("wall-a", ElementCategory.ArchitecturalWall));
            project.Elements.Add(new ProjectElement("wall-b", ElementCategory.ArchitecturalWall));
            var first = Door("d1", family.Id, "f1", "wall-a");
            first.Quantities["OpeningAreaM2"] = 1.95d;
            var second = Door("d2", family.Id, "f1", "wall-b");
            second.Quantities["OpeningAreaM2"] = 1.95d;
            project.Elements.Add(first);
            project.Elements.Add(second);

            var rows = DoorOpeningScheduleBuilder.Build(project);
            if (rows.Count != 1) throw new Exception("Expected one grouped door row.");
            var row = rows[0];
            if (row.Floor != "Tầng 1" || row.Category != "Door" || row.FamilyName != "Cửa D1" || row.Material != "Gỗ")
                throw new Exception("Door schedule labels/family inheritance failed.");
            if (row.Count != 2 || row.HostCount != 2 || row.ElementIds.Count != 2 || row.HostIds.Count != 2)
                throw new Exception("Door count/host provenance failed.");
            Near(0.9d, row.WidthM);
            Near(2.2d, row.HeightM);
            Near(0d, row.SillHeightM);
            Near(0.1d, row.ThicknessM);
            Near(3.9d, row.OpeningAreaM2);
        }

        private static void InstanceOverrideSplitsFamilyInheritedRow()
        {
            var project = new ProjectState("p2", "Door overrides");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            var family = new ProjectFamily("opening-family", "Lỗ mở", ElementCategory.WallOpening);
            family.Properties["WidthM"] = "1.0";
            family.Properties["HeightM"] = "2.0";
            family.Properties["BottomOffsetM"] = "0.15";
            project.Families.Add(family);

            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Elements.Add(new ProjectElement("wall-a", ElementCategory.ArchitecturalWall));
            var inherited = new ProjectElement("o1", ElementCategory.WallOpening, family.Id, "f1", "z");
            inherited.Properties["HostWallId"] = "wall-a";
            var overridden = new ProjectElement("o2", ElementCategory.WallOpening, family.Id, "f1", "z");
            overridden.Properties["WidthM"] = "1.2";
            overridden.Properties["HostWallId"] = "wall-a";
            project.Elements.Add(inherited);
            project.Elements.Add(overridden);

            var rows = DoorOpeningScheduleBuilder.Build(project).OrderBy(x => x.WidthM).ToList();
            if (rows.Count != 2) throw new Exception("Width override must split schedule rows.");
            Near(1.0d, rows[0].WidthM);
            Near(2.0d, rows[0].HeightM);
            Near(0.15d, rows[0].SillHeightM);
            Near(2.0d, rows[0].OpeningAreaM2);
            Near(1.2d, rows[1].WidthM);
            Near(2.4d, rows[1].OpeningAreaM2);
            if (rows[0].HostCount != 1 || rows[1].HostCount != 1) throw new Exception("Host count must remain distinct per grouped row.");
        }

        private static void RejectsInvalidSemanticDimensions()
        {
            var project = new ProjectState("p3", "Bad opening");
            var family = new ProjectFamily("door", "Bad Door", ElementCategory.Door);
            family.Properties["WidthM"] = "-0.9";
            family.Properties["HeightM"] = "2.2";
            project.Families.Add(family);
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Elements.Add(new ProjectElement("d1", ElementCategory.Door, family.Id, "f", "z"));
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));

            family.Properties["WidthM"] = "0.9";
            family.Properties["HeightM"] = "NaN";
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void RejectsDerivedAreaUnderflow()
        {
            var project = new ProjectState("p4", "Tiny opening");
            var family = new ProjectFamily("tiny-door", "Tiny Door", ElementCategory.Door);
            family.Properties["WidthM"] = "1e-200";
            family.Properties["HeightM"] = "1e-200";
            project.Families.Add(family);
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Elements.Add(new ProjectElement("tiny", ElementCategory.Door, family.Id, "f", "z"));

            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));

            var explicitArea = new ProjectState("p5", "Explicit tiny area");
            explicitArea.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            explicitArea.Zones.Add(new ZoneDefinition("z", "Zone"));
            var explicitFamily = new ProjectFamily("tiny-door", "Tiny Door", ElementCategory.Door);
            explicitFamily.Properties["WidthM"] = "1e-200";
            explicitFamily.Properties["HeightM"] = "1e-200";
            explicitArea.Families.Add(explicitFamily);
            var element = new ProjectElement("tiny", ElementCategory.Door, explicitFamily.Id, "f", "z");
            element.Quantities["OpeningAreaM2"] = 0d;
            explicitArea.Elements.Add(element);
            var rows = DoorOpeningScheduleBuilder.Build(explicitArea);
            if (rows.Count != 1 || rows[0].OpeningAreaM2 != 0d)
                throw new Exception("Explicit stored zero OpeningAreaM2 must retain existing semantics.");
        }

        private static ProjectElement Door(string id, string familyId, string floorId, string hostId)
        {
            var element = new ProjectElement(id, ElementCategory.Door, familyId, floorId, "z");
            element.Properties["HostWallId"] = hostId;
            return element;
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
