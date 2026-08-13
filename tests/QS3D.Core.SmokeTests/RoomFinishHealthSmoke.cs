using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishHealthSmoke
    {
        public static void Run()
        {
            HealthyLinkedFinishHasNoIssue();
            UnlinkedFinishIsVisibleForRepair();
            OrphanFinishIsError();
            InvalidParentIsError();
            ConflictingProvenanceIsError();
            StaleRoomFinishIsWarning();
            CrossScopeFinishIsErrorAndExcluded();
            PropertyOnlyRoomProvenanceResolvesBoundaryHandles();
        }

        private static void HealthyLinkedFinishHasNoIssue()
        {
            var project = BaseProject();
            var finish = Finish("F-OK", "ROOM", "f1", "z1");
            project.Elements.Add(finish);
            if (new RoomFinishHealthService().Inspect(project).Count != 0) throw new Exception("Healthy linked finish must not produce Room Finish health issues.");
        }

        private static void UnlinkedFinishIsVisibleForRepair()
        {
            var project = BaseProject();
            var finish = new ProjectElement("F-UNLINKED", ElementCategory.CeilingFinish, "finish", "f1", "z1");
            project.Elements.Add(finish);
            Has(project, "UNLINKED_ROOM_FINISH", HealthSeverity.Warning, finish.Id);
        }

        private static void OrphanFinishIsError()
        {
            var project = BaseProject();
            var finish = Finish("F-ORPHAN", "MISSING", "f1", "z1");
            project.Elements.Add(finish);
            Has(project, "ORPHAN_ROOM_FINISH", HealthSeverity.Error, finish.Id);
            if (!AutoRoomLifecycle.IsExcludedFromQuantity(project, finish)) throw new Exception("Orphan finish must be excluded from quantity.");
        }

        private static void InvalidParentIsError()
        {
            var project = BaseProject();
            var wall = new ProjectElement("NOT-ROOM", ElementCategory.ArchitecturalWall, "wall", "f1", "z1");
            project.Elements.Add(wall);
            var finish = Finish("F-WRONG-PARENT", wall.Id, "f1", "z1");
            project.Elements.Add(finish);
            Has(project, "INVALID_ROOM_FINISH_PARENT", HealthSeverity.Error, finish.Id);
            if (!AutoRoomLifecycle.IsExcludedFromQuantity(project, finish)) throw new Exception("Finish linked to non-Room must be excluded from quantity.");
        }

        private static void ConflictingProvenanceIsError()
        {
            var project = BaseProject();
            var second = new ProjectElement("ROOM-2", ElementCategory.Room, "room", "f1", "z1");
            project.Elements.Add(second);
            var finish = Finish("F-CONFLICT", "ROOM", "f1", "z1");
            finish.Properties["ParentRoomId"] = second.Id;
            project.Elements.Add(finish);
            Has(project, "ROOM_PROVENANCE_CONFLICT", HealthSeverity.Error, finish.Id);
        }

        private static void StaleRoomFinishIsWarning()
        {
            var project = BaseProject();
            var room = project.FindElement("ROOM") ?? throw new Exception("Missing room.");
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateStale;
            var finish = Finish("F-STALE", room.Id, "f1", "z1");
            project.Elements.Add(finish);
            Has(project, "STALE_ROOM_FINISH", HealthSeverity.Warning, finish.Id);
            if (!AutoRoomLifecycle.IsExcludedFromQuantity(project, finish)) throw new Exception("Stale-room finish must be excluded from quantity.");
        }

        private static void CrossScopeFinishIsErrorAndExcluded()
        {
            var project = BaseProject();
            project.Floors.Add(new FloorDefinition("f2", "Tầng 2", 3.6d));
            var finish = Finish("F-SCOPE", "ROOM", "f2", "z1");
            project.Elements.Add(finish);
            Has(project, "ROOM_FINISH_SCOPE_MISMATCH", HealthSeverity.Error, finish.Id);
            if (!AutoRoomLifecycle.IsExcludedFromQuantity(project, finish)) throw new Exception("Cross-scope finish must be excluded from quantity.");
        }

        private static void PropertyOnlyRoomProvenanceResolvesBoundaryHandles()
        {
            var project = BaseProject();
            var room = project.FindElement("ROOM") ?? throw new Exception("Missing room.");
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "A1;B2";
            var finish = Finish("F-TRACE", room.Id, "f1", "z1");
            project.Elements.Add(finish);

            var handles = SourceHandleResolver.Resolve(project, new[] { finish.Id });
            if (handles.Count != 2 || !handles.Contains("A1", StringComparer.OrdinalIgnoreCase) || !handles.Contains("B2", StringComparer.OrdinalIgnoreCase))
                throw new Exception("Property-only room provenance must trace back to Room boundary handles.");
        }

        private static ProjectState BaseProject()
        {
            var project = new ProjectState("p", "Room finish health");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            project.Zones.Add(new ZoneDefinition("z1", "Zone 1"));
            project.Families.Add(new ProjectFamily("room", "Phòng", ElementCategory.Room));
            project.Families.Add(new ProjectFamily("finish", "Hoàn thiện", ElementCategory.WallFinish));
            project.Families.Add(new ProjectFamily("wall", "Tường", ElementCategory.ArchitecturalWall));
            project.Elements.Add(new ProjectElement("ROOM", ElementCategory.Room, "room", "f1", "z1"));
            return project;
        }

        private static ProjectElement Finish(string id, string roomId, string floorId, string zoneId)
        {
            var finish = new ProjectElement(id, ElementCategory.WallFinish, "finish", floorId, zoneId);
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = roomId;
            return finish;
        }

        private static void Has(ProjectState project, string code, HealthSeverity severity, string elementId)
        {
            var issue = new RoomFinishHealthService().Inspect(project).SingleOrDefault(x => x.Code == code && x.ElementId == elementId);
            if (issue == null || issue.Severity != severity) throw new Exception("Expected " + code + " / " + severity + " for " + elementId + ".");
        }
    }
}
