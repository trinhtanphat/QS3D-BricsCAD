using System;
using System.Reflection;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectFloorZoneCanonicalReferenceSmoke
    {
        public static void Run()
        {
            FloorReferenceIdentityIsCanonical();
            PaddedActiveFloorBlocksDelete();
            ZoneReferenceIdentityFailsClosed();
            PaddedActiveZoneFailsClosed();
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

        private static void ZoneReferenceIdentityFailsClosed()
        {
            var project = new ProjectState("P-ZONE-REF", "Zone reference test");
            var zone = ProjectZoneService.Create(project, "Z-01", "Zone 01");
            var fallback = ProjectZoneService.Create(project, "Z-02", "Zone 02");
            project.ActiveZoneId = fallback.Id;
            var element = new ProjectElement("E-ZONE", ElementCategory.Beam);
            SetRawZoneId(element, "  z-01  ");
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeVersion = project.ChangeVersion;
            var beforeCount = project.Zones.Count;
            var beforeName = zone.Name;

            ThrowsArgument(() => ProjectZoneService.ReferenceCount(project, zone.Id));
            ThrowsArgument(() => ProjectZoneService.Update(project, zone.Id, "Zone 01 renamed"));
            ThrowsArgument(() => ProjectZoneService.Delete(project, zone.Id));

            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeCount, project.Zones.Count);
            Equal(beforeName, zone.Name);
            Equal("  z-01  ", RawZoneId(element));
            Same(zone, project.FindZone(zone.Id));
        }

        private static void PaddedActiveZoneFailsClosed()
        {
            var project = new ProjectState("P-ZONE-ACTIVE", "Zone active test");
            var zone = ProjectZoneService.Create(project, "Zone-A", "Zone A");
            SetRawActiveZoneId(project, "  zONE-a  ");
            var beforeVersion = project.ChangeVersion;

            ThrowsArgument(() => ProjectZoneService.Delete(project, zone.Id));

            Equal(beforeVersion, project.ChangeVersion);
            Same(zone, project.FindZone(zone.Id));
            Equal("  zONE-a  ", RawActiveZoneId(project));
        }

        private static void SetRawZoneId(ProjectElement element, string value)
        {
            var field = typeof(ProjectElement).GetField("_zoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectElement._zoneId field was not found.");
            field.SetValue(element, value);
        }

        private static string RawZoneId(ProjectElement element)
        {
            var field = typeof(ProjectElement).GetField("_zoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectElement._zoneId field was not found.");
            return field.GetValue(element) as string ?? throw new Exception("ProjectElement._zoneId was not a string.");
        }

        private static void SetRawActiveZoneId(ProjectState project, string value)
        {
            var field = typeof(ProjectState).GetField("_activeZoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectState._activeZoneId field was not found.");
            field.SetValue(project, value);
        }

        private static string RawActiveZoneId(ProjectState project)
        {
            var field = typeof(ProjectState).GetField("_activeZoneId", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new Exception("ProjectState._activeZoneId field was not found.");
            return field.GetValue(project) as string ?? throw new Exception("ProjectState._activeZoneId was not a string.");
        }

        private static void ThrowsReferencedFloor(Action action)
        {
            ThrowsInvalid(action, "semantic element(s). Reassign or clear Floor/Level references before deletion.");
        }

        private static void ThrowsActiveFloor(Action action)
        {
            ThrowsInvalid(action, "Cannot delete the active floor. Activate another floor first.");
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

            throw new Exception("Expected ArgumentException for noncanonical Zone semantic identity/reference.");
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
