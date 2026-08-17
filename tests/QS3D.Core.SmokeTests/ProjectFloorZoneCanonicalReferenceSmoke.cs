using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorZoneCanonicalReferenceSmoke
    {
        public static void Run()
        {
            FloorReferenceIdentityIsCanonical();
            PaddedActiveFloorBlocksDelete();
            ZoneReferenceIdentityIsCanonical();
            PaddedActiveZoneBlocksDelete();
        }

        private static void FloorReferenceIdentityIsCanonical()
        {
            var project = new ProjectState("P-FLOOR-REF", "Floor reference test");
            var floor = ProjectFloorService.Create(project, "F-01", "Floor 01", 0d);
            var fallback = ProjectFloorService.Create(project, "F-02", "Floor 02", 3d);
            project.ActiveFloorId = fallback.Id;
            var element = new ProjectElement("E-FLOOR", ElementCategory.Beam)
            {
                FloorId = "  f-01  "
            };
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            Equal(1, ProjectFloorService.ReferenceCount(project, " F-01 "));
            True(ProjectFloorService.ReferencesFloor(element, " F-01 "));

            ProjectFloorService.Update(project, floor.Id, floor.Name, 0.25d);
            True((element.Dirty & ElementDirtyFlags.Relations) != 0);
            True((element.Dirty & ElementDirtyFlags.Quantity) != 0);
            True((element.Dirty & ElementDirtyFlags.Geometry) != 0);

            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.Floors.Count;
            ThrowsReferencedFloor(() => ProjectFloorService.Delete(project, floor.Id));
            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeCount, project.Floors.Count);
            Same(floor, project.FindFloor(floor.Id));
        }

        private static void PaddedActiveFloorBlocksDelete()
        {
            var project = new ProjectState("P-FLOOR-ACTIVE", "Floor active test");
            var floor = ProjectFloorService.Create(project, "Floor-A", "Floor A", 0d);
            project.ActiveFloorId = "  fLOOR-a  ";
            var beforeVersion = project.ChangeVersion;

            ThrowsActiveFloor(() => ProjectFloorService.Delete(project, " floor-A "));

            Equal(beforeVersion, project.ChangeVersion);
            Same(floor, project.FindFloor(floor.Id));
            Equal("fLOOR-a", project.ActiveFloorId);
        }

        private static void ZoneReferenceIdentityIsCanonical()
        {
            var project = new ProjectState("P-ZONE-REF", "Zone reference test");
            var zone = ProjectZoneService.Create(project, "Z-01", "Zone 01");
            var fallback = ProjectZoneService.Create(project, "Z-02", "Zone 02");
            project.ActiveZoneId = fallback.Id;
            var element = new ProjectElement("E-ZONE", ElementCategory.Beam)
            {
                ZoneId = "z-01"
            };
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            ThrowsArgument(() => ProjectZoneService.ReferenceCount(project, " Z-01 "));
            Equal(1, ProjectZoneService.ReferenceCount(project, "z-01"));
            ProjectZoneService.Update(project, zone.Id, "Zone 01 renamed");
            True((element.Dirty & ElementDirtyFlags.Relations) != 0);
            True((element.Dirty & ElementDirtyFlags.Quantity) != 0);

            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.Zones.Count;
            ThrowsReferencedZone(() => ProjectZoneService.Delete(project, zone.Id));
            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeCount, project.Zones.Count);
            Same(zone, project.FindZone(zone.Id));
        }

        private static void PaddedActiveZoneBlocksDelete()
        {
            var project = new ProjectState("P-ZONE-ACTIVE", "Zone active test");
            var zone = ProjectZoneService.Create(project, "Zone-A", "Zone A");
            project.ActiveZoneId = "zONE-a";
            var beforeVersion = project.ChangeVersion;

            ThrowsArgument(() => ProjectZoneService.Delete(project, " zone-A "));
            Equal(beforeVersion, project.ChangeVersion);
            Same(zone, project.FindZone(zone.Id));

            ThrowsActiveZone(() => ProjectZoneService.Delete(project, "ZONE-A"));
            Equal(beforeVersion, project.ChangeVersion);
            Same(zone, project.FindZone(zone.Id));
            Equal("zONE-a", project.ActiveZoneId);
        }

        private static void ThrowsReferencedFloor(Action action)
        {
            ThrowsInvalid(action, "semantic element(s). Reassign or clear Floor/Level references before deletion.");
        }

        private static void ThrowsActiveFloor(Action action)
        {
            ThrowsInvalid(action, "Cannot delete the active floor. Activate another floor first.");
        }

        private static void ThrowsReferencedZone(Action action)
        {
            ThrowsInvalid(action, "semantic element(s). Reassign them before deletion.");
        }

        private static void ThrowsActiveZone(Action action)
        {
            ThrowsInvalid(action, "Cannot delete the active zone. Activate another zone first.");
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

            throw new Exception("Expected ArgumentException for noncanonical Zone identity.");
        }

        private static void ThrowsInvalid(Action action, string expectedMessagePart)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessagePart, StringComparison.Ordinal) < 0)
                    throw new Exception("Unexpected error: " + ex.Message);
                return;
            }

            throw new Exception("Expected InvalidOperationException containing: " + expectedMessagePart);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
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
