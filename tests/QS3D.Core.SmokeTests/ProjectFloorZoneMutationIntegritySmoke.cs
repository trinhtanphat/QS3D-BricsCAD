using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorZoneMutationIntegritySmoke
    {
        public static void Run()
        {
            FloorActiveAliasIsCanonicalRepair();
            ZonePaddedActiveIdFailsAtomically();
            FloorAssignmentCanonicalIdentityIsNoOp();
            ZoneAssignmentCanonicalIdentityIsNoOp();
            FloorNullTargetFailsAtomically();
            ZoneNullTargetFailsAtomically();
        }

        private static void FloorActiveAliasIsCanonicalRepair()
        {
            var project = new ProjectState("P-FLOOR-ACTIVE-REPAIR", "Floor active repair");
            var floor = ProjectFloorService.Create(project, "F-01", "Floor 01", 0d);
            project.ActiveFloorId = "  f-01  ";
            var beforeVersion = project.ChangeVersion;

            ProjectFloorService.SetActive(project, " F-01 ");

            Equal(beforeVersion + 1L, project.ChangeVersion);
            Equal(floor.Id, project.ActiveFloorId);
            Same(floor, project.FindFloor(floor.Id));

            var canonicalVersion = project.ChangeVersion;
            ProjectFloorService.SetActive(project, floor.Id);
            Equal(canonicalVersion, project.ChangeVersion);
        }

        private static void ZonePaddedActiveIdFailsAtomically()
        {
            var project = new ProjectState("P-ZONE-ACTIVE-STRICT", "Zone active strict");
            var zone = ProjectZoneService.Create(project, "Z-01", "Zone 01");
            var beforeVersion = project.ChangeVersion;

            ThrowsArgument(() => ProjectZoneService.SetActive(project, " Z-01 "));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(zone.Id, project.ActiveZoneId);
            Same(zone, project.FindZone(zone.Id));

            ProjectZoneService.SetActive(project, zone.Id);
            Equal(beforeVersion, project.ChangeVersion);
        }

        private static void FloorAssignmentCanonicalIdentityIsNoOp()
        {
            var project = new ProjectState("P-FLOOR-ASSIGN-NOOP", "Floor assignment no-op");
            var floor = ProjectFloorService.Create(project, "F-01", "Floor 01", 0d);
            var element = new ProjectElement("E-FLOOR", ElementCategory.Beam)
            {
                FloorId = "  f-01  "
            };
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = element.UpdatedUtc;

            var changed = ProjectFloorService.Assign(project, " F-01 ", new[] { element });

            Equal(0, changed);
            Equal(beforeVersion, project.ChangeVersion);
            Equal("f-01", element.FloorId);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(beforeUpdatedUtc, element.UpdatedUtc);
            Same(floor, project.FindFloor(floor.Id));
        }

        private static void ZoneAssignmentCanonicalIdentityIsNoOp()
        {
            var project = new ProjectState("P-ZONE-ASSIGN-NOOP", "Zone assignment no-op");
            var zone = ProjectZoneService.Create(project, "Z-01", "Zone 01");
            var element = new ProjectElement("E-ZONE", ElementCategory.Beam)
            {
                ZoneId = "z-01"
            };
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = element.UpdatedUtc;

            var changed = ProjectZoneService.Assign(project, "Z-01", new[] { element });

            Equal(0, changed);
            Equal(beforeVersion, project.ChangeVersion);
            Equal("z-01", element.ZoneId);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(beforeUpdatedUtc, element.UpdatedUtc);
            Same(zone, project.FindZone(zone.Id));
        }

        private static void FloorNullTargetFailsAtomically()
        {
            var project = new ProjectState("P-FLOOR-NULL", "Floor null target");
            var floor = ProjectFloorService.Create(project, "F-01", "Floor 01", 0d);
            var element = new ProjectElement("E-FLOOR-NULL", ElementCategory.Beam);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = element.UpdatedUtc;

            ThrowsInvalid(
                () => ProjectFloorService.Assign(project, floor.Id, new ProjectElement[] { element, null! }),
                "Floor mutation target collection contains a null element.");

            Equal(beforeVersion, project.ChangeVersion);
            Equal(string.Empty, element.FloorId);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(beforeUpdatedUtc, element.UpdatedUtc);
        }

        private static void ZoneNullTargetFailsAtomically()
        {
            var project = new ProjectState("P-ZONE-NULL", "Zone null target");
            var zone = ProjectZoneService.Create(project, "Z-01", "Zone 01");
            var element = new ProjectElement("E-ZONE-NULL", ElementCategory.Beam);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdatedUtc = element.UpdatedUtc;

            ThrowsInvalid(
                () => ProjectZoneService.Assign(project, zone.Id, new ProjectElement[] { element, null! }),
                "Zone assignment target collection contains a null element.");

            Equal(beforeVersion, project.ChangeVersion);
            Equal(string.Empty, element.ZoneId);
            Equal(ElementDirtyFlags.None, element.Dirty);
            Equal(beforeUpdatedUtc, element.UpdatedUtc);
        }

        private static void ThrowsArgument(Action action)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new Exception("Expected ArgumentException for a noncanonical Zone semantic identity.");
        }

        private static void ThrowsInvalid(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                Equal(expectedMessage, ex.Message);
                return;
            }

            throw new Exception("Expected InvalidOperationException: " + expectedMessage);
        }

        private static void Same(object expected, object? actual)
        {
            if (!ReferenceEquals(expected, actual)) throw new Exception("Expected same object reference.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
