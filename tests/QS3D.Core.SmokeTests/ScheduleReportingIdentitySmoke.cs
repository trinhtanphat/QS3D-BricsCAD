using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ScheduleReportingIdentitySmoke
    {
        internal static void Run()
        {
            ExactDuplicateIdsFailClosed();
            CaseVariantDuplicateIdsFailClosed();
            NullProjectElementsFailClosed();
            MalformedReferenceIdentitiesFailClosed();
            NoncanonicalMutableReferenceIdsFailClosed();
            NoncanonicalStoredSourceHandlesFailClosed();
            UniqueIdsRemainAccepted();
            ProvenanceIsRetainedAcrossSchedules();
        }

        private static void ExactDuplicateIdsFailClosed()
        {
            AssertAllScheduleBuildersReject(DuplicateProject("E1"));
        }

        private static void CaseVariantDuplicateIdsFailClosed()
        {
            AssertAllScheduleBuildersReject(DuplicateProject("e1"));
        }

        private static void NullProjectElementsFailClosed()
        {
            var project = new ProjectState("schedule-null-element", "Schedule null element");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family", "floor", "zone"));
            project.Elements.Add(null!);

            AssertAllProjectReportBuildersReject(project, "element index 1");
        }

        private static void MalformedReferenceIdentitiesFailClosed()
        {
            var nullFloor = new ProjectState("schedule-null-floor", "Schedule null floor");
            nullFloor.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            var nullFloorVersion = nullFloor.ChangeVersion;
            ExpectThrows<ArgumentNullException>(() => nullFloor.Floors.Add(null!));
            if (nullFloor.ChangeVersion != nullFloorVersion)
                throw new Exception("Rejected null Floor admission must not advance project revision.");

            var duplicateFloor = new ProjectState("schedule-duplicate-floor", "Schedule duplicate floor");
            duplicateFloor.Floors.Add(new FloorDefinition("Floor-A", "Floor A", 0d));
            duplicateFloor.Floors.Add(new FloorDefinition(" floor-a ", "Floor duplicate", 3d));
            AssertAllProjectReportBuildersReject(duplicateFloor, "floor id 'floor-a'");

            var nullZone = new ProjectState("schedule-null-zone", "Schedule null zone");
            nullZone.Zones.Add(new ZoneDefinition("zone", "Zone"));
            var nullZoneVersion = nullZone.ChangeVersion;
            ExpectThrows<ArgumentNullException>(() => nullZone.Zones.Add(null!));
            if (nullZone.ChangeVersion != nullZoneVersion)
                throw new Exception("Rejected null Zone admission must not advance project revision.");

            var duplicateZone = new ProjectState("schedule-duplicate-zone", "Schedule duplicate zone");
            duplicateZone.Zones.Add(new ZoneDefinition("Zone-A", "Zone A"));
            duplicateZone.Zones.Add(new ZoneDefinition(" zone-a ", "Zone duplicate"));
            AssertAllProjectReportBuildersReject(duplicateZone, "zone id 'zone-a'");

            var nullFamily = new ProjectState("schedule-null-family", "Schedule null family");
            nullFamily.Families.Add(new ProjectFamily("family", "Family", ElementCategory.Slab));
            var nullFamilyVersion = nullFamily.ChangeVersion;
            ExpectThrows<ArgumentNullException>(() => nullFamily.Families.Add(null!));
            if (nullFamily.ChangeVersion != nullFamilyVersion)
                throw new Exception("Rejected null Family admission must not advance project revision.");

            var duplicateFamily = new ProjectState("schedule-duplicate-family", "Schedule duplicate family");
            duplicateFamily.Families.Add(new ProjectFamily("Family-A", "Family A", ElementCategory.Slab));
            duplicateFamily.Families.Add(new ProjectFamily(" family-a ", "Family duplicate", ElementCategory.Slab));
            AssertAllProjectReportBuildersReject(duplicateFamily, "family id 'family-a'");
        }

        private static void NoncanonicalMutableReferenceIdsFailClosed()
        {
            var familyProject = BaseScheduleProject("schedule-reference-family", ElementCategory.Slab, out var family);
            var familyElement = new ProjectElement("REF-FAMILY", ElementCategory.Slab, family.Id, "floor", "zone");
            familyElement.FamilyId = " FAMILY ";
            if (familyElement.FamilyId != "FAMILY")
                throw new Exception("ProjectElement.FamilyId setter must canonicalize surrounding whitespace.");
            SetRawRelationId(familyElement, "_familyId", " FAMILY ");
            familyProject.Elements.Add(familyElement);
            AssertAllProjectReportBuildersReject(familyProject, "noncanonical family reference id");

            var floorProject = BaseScheduleProject("schedule-reference-floor", ElementCategory.Slab, out family);
            var floorElement = new ProjectElement("REF-FLOOR", ElementCategory.Slab, family.Id, "floor", "zone");
            floorElement.FloorId = " floor ";
            if (floorElement.FloorId != "floor")
                throw new Exception("ProjectElement.FloorId setter must canonicalize surrounding whitespace.");
            SetRawRelationId(floorElement, "_floorId", " floor ");
            floorProject.Elements.Add(floorElement);
            AssertAllProjectReportBuildersReject(floorProject, "noncanonical floor reference id");

            var zoneProject = BaseScheduleProject("schedule-reference-zone", ElementCategory.Slab, out family);
            var zoneElement = new ProjectElement("REF-ZONE", ElementCategory.Slab, family.Id, "floor", "zone");
            zoneElement.ZoneId = " ZONE ";
            if (zoneElement.ZoneId != "ZONE")
                throw new Exception("ProjectElement.ZoneId setter must canonicalize surrounding whitespace.");
            SetRawRelationId(zoneElement, "_zoneId", " ZONE ");
            zoneProject.Elements.Add(zoneElement);
            AssertAllProjectReportBuildersReject(zoneProject, "noncanonical zone reference id");
        }

        private static void NoncanonicalStoredSourceHandlesFailClosed()
        {
            var paddedProject = BaseScheduleProject("schedule-source-handle-padding", ElementCategory.Slab, out var family);
            family.Properties["Material"] = "Concrete";
            var padded = new ProjectElement("HANDLE-PAD", ElementCategory.Slab, family.Id, "floor", "zone");
            padded.SourceHandles.Add(" AA ");
            padded.Quantities["VolumeM3"] = 1d;
            paddedProject.Elements.Add(padded);
            ExpectThrowsContaining<InvalidOperationException>(
                () => MaterialUsageScheduleBuilder.Build(paddedProject),
                "non-canonical stored SourceHandles entry");

            var duplicateProject = BaseScheduleProject("schedule-source-handle-duplicate", ElementCategory.Slab, out family);
            family.Properties["Material"] = "Concrete";
            var duplicate = new ProjectElement("HANDLE-DUP", ElementCategory.Slab, family.Id, "floor", "zone");
            duplicate.SourceHandles.Add("A");
            duplicate.SourceHandles.Add("0A");
            duplicate.Quantities["VolumeM3"] = 1d;
            duplicateProject.Elements.Add(duplicate);
            ExpectThrowsContaining<InvalidOperationException>(
                () => MaterialUsageScheduleBuilder.Build(duplicateProject),
                "duplicate stored SourceHandles identity");
        }

        private static void UniqueIdsRemainAccepted()
        {
            var project = new ProjectState("schedule-identity-valid", "Schedule identity valid");
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            project.Families.Add(new ProjectFamily("family", "Family", ElementCategory.Slab));
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family", "floor", "zone"));
            project.Elements.Add(new ProjectElement("E2", ElementCategory.Slab, "family", "floor", "zone"));

            if (MaterialUsageScheduleBuilder.Build(project).Count != 0 ||
                CurtainWallScheduleBuilder.Build(project).Count != 0 ||
                DoorOpeningScheduleBuilder.Build(project).Count != 0 ||
                RoomFinishScheduleBuilder.Build(project).Count != 0)
                throw new Exception("Schedule identity guard must not change valid non-schedule project output.");
        }

        private static void ProvenanceIsRetainedAcrossSchedules()
        {
            MaterialProvenance();
            CurtainProvenance();
            DoorProvenance();
            RoomFinishProvenance();
        }

        private static void MaterialProvenance()
        {
            var project = BaseScheduleProject("schedule-material", ElementCategory.Slab, out var family);
            family.Properties["Material"] = "Concrete";
            var element = new ProjectElement("MAT-1", ElementCategory.Slab, family.Id, "floor", "zone");
            element.FamilyId = "FAMILY";
            element.FloorId = "FLOOR";
            element.ZoneId = "ZONE";
            AddHandles(element);
            element.Quantities["VolumeM3"] = 1.25d;
            project.Elements.Add(element);

            var row = MaterialUsageScheduleBuilder.Build(project).Single();
            AssertProvenance(project, row.ProjectId, row.DrawingFingerprint, row.SourceHandles);
            if (row.Floor != "Floor" || row.FamilyName != "Family")
                throw new Exception("Material schedule must resolve canonical case-variant Floor/Family references before lookup/grouping.");
        }

        private static void CurtainProvenance()
        {
            var project = BaseScheduleProject("schedule-curtain", ElementCategory.GlassWall, out var family);
            var element = new ProjectElement("CW-1", ElementCategory.GlassWall, family.Id, "floor", "zone");
            element.FamilyId = "FAMILY";
            element.FloorId = "FLOOR";
            AddHandles(element);
            project.Elements.Add(element);

            var row = CurtainWallScheduleBuilder.Build(project).Single();
            AssertProvenance(project, row.ProjectId, row.DrawingFingerprint, row.SourceHandles);
            if (row.Floor != "Floor" || row.FamilyName != "Family")
                throw new Exception("Curtain wall schedule must resolve canonical case-variant Floor/Family references before lookup/grouping.");
        }

        private static void DoorProvenance()
        {
            var project = BaseScheduleProject("schedule-door", ElementCategory.Door, out var family);
            var element = new ProjectElement("D-1", ElementCategory.Door, family.Id, "floor", "zone");
            element.FamilyId = "FAMILY";
            element.FloorId = "FLOOR";
            AddHandles(element);
            project.Elements.Add(element);

            var row = DoorOpeningScheduleBuilder.Build(project).Single();
            AssertProvenance(project, row.ProjectId, row.DrawingFingerprint, row.SourceHandles);
            if (row.Floor != "Floor" || row.FamilyName != "Family")
                throw new Exception("Door/opening schedule must resolve canonical case-variant Floor/Family references before lookup/grouping.");
        }

        private static void RoomFinishProvenance()
        {
            var project = new ProjectState("schedule-finish", "Schedule finish") { DrawingFingerprint = "DWG-SCHEDULE-FINISH" };
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            project.Families.Add(new ProjectFamily("room-family", "Room", ElementCategory.Room));
            var finishFamily = new ProjectFamily("finish-family", "Finish", ElementCategory.WallFinish);
            finishFamily.Properties["Material"] = "Paint";
            project.Families.Add(finishFamily);

            var room = new ProjectElement("ROOM-1", ElementCategory.Room, "room-family", "floor", "zone");
            project.Elements.Add(room);
            var finish = new ProjectElement(
                RoomFinishIdentityService.CanonicalId(room.Id, ElementCategory.WallFinish),
                ElementCategory.WallFinish,
                finishFamily.Id,
                room.FloorId,
                room.ZoneId);
            finish.FamilyId = "FINISH-FAMILY";
            finish.FloorId = "FLOOR";
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            finish.Quantities["NetFinishAreaM2"] = 10d;
            finish.MarkClean(ElementDirtyFlags.All);
            AddHandles(finish);
            project.Elements.Add(finish);

            var row = RoomFinishScheduleBuilder.Build(project).Single();
            AssertProvenance(project, row.ProjectId, row.DrawingFingerprint, row.SourceHandles);
            if (row.Floor != "Floor" || row.FamilyName != "Finish")
                throw new Exception("Room finish schedule must resolve canonical case-variant Floor/Family references before lookup/grouping.");
        }

        private static ProjectState BaseScheduleProject(string id, ElementCategory category, out ProjectFamily family)
        {
            var project = new ProjectState(id, id) { DrawingFingerprint = "DWG-" + id.ToUpperInvariant() };
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            family = new ProjectFamily("family", "Family", category);
            project.Families.Add(family);
            return project;
        }

        private static void AddHandles(ProjectElement element)
        {
            element.SourceHandles.Add("AA");
            element.SourceHandles.Add("Bb");
        }

        private static void SetRawRelationId(ProjectElement element, string fieldName, string value)
        {
            var field = typeof(ProjectElement).GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new Exception("Missing ProjectElement backing field '" + fieldName + "'.");
            field.SetValue(element, value);
        }

        private static void AssertProvenance(ProjectState project, string projectId, string drawingFingerprint, IList<string> handles)
        {
            if (!string.Equals(projectId, project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(drawingFingerprint, project.DrawingFingerprint, StringComparison.Ordinal))
                throw new Exception("Schedule row must retain project and drawing identity provenance.");
            if (handles.Count != 2 || handles[0] != "AA" || handles[1] != "Bb")
                throw new Exception("Schedule row must retain canonical source Handle provenance in first-seen order.");
        }

        private static ProjectState DuplicateProject(string secondId)
        {
            var project = new ProjectState("schedule-identity-duplicate", "Schedule identity duplicate");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family", "floor", "zone"));
            project.Elements.Add(new ProjectElement(secondId, ElementCategory.Slab, "family", "floor", "zone"));
            return project;
        }

        private static void AssertAllProjectReportBuildersReject(ProjectState project, string messagePart)
        {
            ExpectThrowsContaining<InvalidOperationException>(() => MaterialUsageScheduleBuilder.Build(project), messagePart);
            ExpectThrowsContaining<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project), messagePart);
            ExpectThrowsContaining<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project), messagePart);
            ExpectThrowsContaining<InvalidOperationException>(() => RoomFinishScheduleBuilder.Build(project), messagePart);
            ExpectThrowsContaining<InvalidOperationException>(() => ProjectQuantityReportBuilder.Group(project), messagePart);
            ExpectThrowsContaining<InvalidOperationException>(() => ProjectQuantityReportBuilder.Detail(project), messagePart);
        }

        private static void AssertAllScheduleBuildersReject(ProjectState project)
        {
            ExpectThrows<InvalidOperationException>(() => MaterialUsageScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => RoomFinishScheduleBuilder.Build(project));
        }

        private static void ExpectThrowsContaining<T>(Action action, string messagePart) where T : Exception
        {
            try { action(); }
            catch (T ex)
            {
                if (ex.Message.IndexOf(messagePart, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Expected " + typeof(T).Name + " containing '" + messagePart + "', got: " + ex.Message);
                return;
            }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private static void ExpectThrows<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}