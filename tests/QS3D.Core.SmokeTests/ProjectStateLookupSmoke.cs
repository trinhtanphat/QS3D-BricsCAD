using System;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateLookupSmoke
    {
        public static void Run()
        {
            LookupsNormalizeWhitespaceAndCase();
            BlankAndMissingLookupsReturnNull();
            DuplicateLookupsFailClosed();
            FloorAndZoneMutationServicesFailClosedOnDuplicateIds();
        }

        private static void LookupsNormalizeWhitespaceAndCase()
        {
            var project = new ProjectState("P1", "Lookup");
            var element = new ProjectElement(" ELEMENT-1 ", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty);
            var family = new ProjectFamily(" FAMILY-1 ", "Family", ElementCategory.ArchitecturalWall);
            var floor = new FloorDefinition(" FLOOR-1 ", "Floor", 0d);
            var zone = new ZoneDefinition(" ZONE-1 ", "Zone");
            var rule = new QuantityRule(" RULE-1 ", ElementCategory.ArchitecturalWall, "VolumeM3", "1", "v1");
            project.Elements.Add(element);
            project.Families.Add(family);
            project.Floors.Add(floor);
            project.Zones.Add(zone);
            project.QuantityRules.Add(rule);

            Same(element, project.FindElement(" element-1 "));
            Same(family, project.FindFamily(" family-1 "));
            Same(floor, project.FindFloor(" floor-1 "));
            Same(zone, project.FindZone(" zone-1 "));
            Same(rule, project.FindQuantityRule(" rule-1 "));
        }

        private static void BlankAndMissingLookupsReturnNull()
        {
            var project = new ProjectState("P1", "Lookup");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            project.Families.Add(new ProjectFamily("F1", "Family", ElementCategory.ArchitecturalWall));
            project.Floors.Add(new FloorDefinition("FL1", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone"));
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.ArchitecturalWall, "VolumeM3", "1", "v1"));

            Null(project.FindElement("   "));
            Null(project.FindFamily("   "));
            Null(project.FindFloor("   "));
            Null(project.FindZone("   "));
            Null(project.FindQuantityRule("   "));
            Null(project.FindElement("missing"));
            Null(project.FindFamily("missing"));
            Null(project.FindFloor("missing"));
            Null(project.FindZone("missing"));
            Null(project.FindQuantityRule("missing"));
        }

        private static void DuplicateLookupsFailClosed()
        {
            var project = new ProjectState("P1", "Duplicate lookup");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            project.Elements.Add(new ProjectElement("e1", ElementCategory.ArchitecturalWall, string.Empty, string.Empty, string.Empty));
            project.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.ArchitecturalWall));
            project.Families.Add(new ProjectFamily("f1", "Family 2", ElementCategory.ArchitecturalWall));
            project.Floors.Add(new FloorDefinition("FL1", "Floor 1", 0d));
            project.Floors.Add(new FloorDefinition("fl1", "Floor 2", 3d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 2"));
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.ArchitecturalWall, "VolumeM3", "1", "v1"));
            project.QuantityRules.Add(new QuantityRule("r1", ElementCategory.ArchitecturalWall, "AreaM2", "1", "v1"));

            Throws<InvalidOperationException>(() => project.FindElement(" e1 "));
            Throws<InvalidOperationException>(() => project.FindFamily(" f1 "));
            Throws<InvalidOperationException>(() => project.FindFloor(" fl1 "));
            Throws<InvalidOperationException>(() => project.FindZone(" z1 "));
            Throws<InvalidOperationException>(() => project.FindQuantityRule(" r1 "));
        }

        private static void FloorAndZoneMutationServicesFailClosedOnDuplicateIds()
        {
            var project = new ProjectState("P1", "Duplicate catalog mutation");
            project.Floors.Add(new FloorDefinition("FL1", "Floor 1", 0d));
            project.Floors.Add(new FloorDefinition("fl1", "Floor 2", 3d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 2"));

            Throws<InvalidOperationException>(() => ProjectFloorService.SetActive(project, " fl1 "));
            Throws<ArgumentException>(() => ProjectZoneService.SetActive(project, " z1 "));
            Throws<InvalidOperationException>(() => ProjectZoneService.SetActive(project, "z1"));
            if (!string.IsNullOrEmpty(project.ActiveFloorId) || !string.IsNullOrEmpty(project.ActiveZoneId))
                throw new Exception("Duplicate Floor/Zone mutation lookup must fail before changing active catalog state.");
        }

        private static void Same<T>(T expected, T? actual) where T : class
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected normalized lookup to return the stored semantic object.");
        }

        private static void Null(object? value)
        {
            if (value != null) throw new Exception("Expected blank/missing project semantic lookup to return null.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}