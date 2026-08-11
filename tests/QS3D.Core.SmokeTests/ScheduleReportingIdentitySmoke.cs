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

            ExpectThrowsContaining<InvalidOperationException>(() => MaterialUsageScheduleBuilder.Build(project), "index 1");
            ExpectThrows<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => RoomFinishScheduleBuilder.Build(project));
            ExpectThrows<InvalidOperationException>(() => ProjectQuantityReportBuilder.Group(project));
            ExpectThrows<InvalidOperationException>(() => ProjectQuantityReportBuilder.Detail(project));
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
            AddHandles(element);
            element.Quantities["VolumeM3"] = 1.25d;
            project.Elements.Add(element);

            var row = MaterialUsageScheduleBuilder.Build(project).Single();
            AssertProvenance(project, row.ProjectId, row.DrawingFingerprint, row.SourceHandles);
        }

        private static void CurtainProvenance()
        {
            var project = BaseScheduleProject("schedule-curtain", ElementCategory.GlassWall, out var family);
            var element = new ProjectElement("CW-1", ElementCategory.GlassWall, family.Id, "floor", "zone");
            AddHandles(element);
            project.Elements.Add(element);

            var row = CurtainWallScheduleBuilder.Build(project).Single();
            AssertProvenance(project, row.ProjectId, row.DrawingFingerprint, row.SourceHandles);
        }

        private static void DoorProvenance()
        {
            var project = BaseScheduleProject("schedule-door", ElementCategory.Door, out var family);
            var element = new ProjectElement("D-1", ElementCategory.Door, family.Id, "floor", "zone");
            AddHandles(element);
            project.Elements.Add(element);

            var row = DoorOpeningScheduleBuilder.Build(project).Single();
            AssertProvenance(project, row.ProjectId, row.DrawingFingerprint, row.SourceHandles);
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
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = room.Id;
            finish.Quantities["NetFinishAreaM2"] = 10d;
            finish.MarkClean(ElementDirtyFlags.All);
            AddHandles(finish);
            project.Elements.Add(finish);

            var row = RoomFinishScheduleBuilder.Build(project).Single();
            AssertProvenance(project, row.ProjectId, row.DrawingFingerprint, row.SourceHandles);
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
            element.SourceHandles.Add(" aa ");
            element.SourceHandles.Add("AA");
            element.SourceHandles.Add("Bb");
        }

        private static void AssertProvenance(ProjectState project, string projectId, string drawingFingerprint, IList<string> handles)
        {
            if (!string.Equals(projectId, project.ProjectId, StringComparison.Ordinal) ||
                !string.Equals(drawingFingerprint, project.DrawingFingerprint, StringComparison.Ordinal))
                throw new Exception("Schedule row must retain project and drawing identity provenance.");
            if (handles.Count != 2 || handles[0] != "aa" || handles[1] != "Bb")
                throw new Exception("Schedule row must trim and case-insensitively deduplicate source Handle provenance in first-seen order.");
        }

        private static ProjectState DuplicateProject(string secondId)
        {
            var project = new ProjectState("schedule-identity-duplicate", "Schedule identity duplicate");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Slab, "family", "floor", "zone"));
            project.Elements.Add(new ProjectElement(secondId, ElementCategory.Slab, "family", "floor", "zone"));
            return project;
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
