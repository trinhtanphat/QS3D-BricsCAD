using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class AutoRoomLifecycleSmoke
    {
        public static void Run()
        {
            SourceSignatureIsDeterministic();
            ReusesMatchingProvenance();
            DuplicateProvenanceIsRejected();
            TopologyChangeMarksStale();
            StaleRoomsAndDependentsAreExcludedFromBq();
            ReactivationClearsStaleState();
            FamilyDefaultsPreserveInstanceOverrides();
        }

        private static void SourceSignatureIsDeterministic()
        {
            var signature = AutoRoomLifecycle.NormalizeSourceHandles(new[] { "b2", "A1", "a1", " B2 " });
            Equal("A1;B2", signature);
        }

        private static void ReusesMatchingProvenance()
        {
            var project = NewProject();
            var room = AutoRoom("ROOM-OLD", "A;B;C", project);
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateStale;
            project.Elements.Add(room);
            var found = AutoRoomLifecycle.FindBySourceSignature(project, "c;b;a", "f", "z");
            True(ReferenceEquals(room, found));
        }

        private static void DuplicateProvenanceIsRejected()
        {
            var project = NewProject();
            project.Elements.Add(AutoRoom("R1", "A;B", project));
            project.Elements.Add(AutoRoom("R2", "B;A", project));
            Throws<InvalidOperationException>(() => AutoRoomLifecycle.FindBySourceSignature(project, "A;B", "f", "z"));
        }

        private static void TopologyChangeMarksStale()
        {
            var project = NewProject();
            var old = AutoRoom("OLD", "A;B;C;D", project);
            var active = AutoRoom("ACTIVE", "A;E;F", project);
            var unrelated = AutoRoom("OTHER", "X;Y;Z", project);
            project.Elements.Add(old); project.Elements.Add(active); project.Elements.Add(unrelated);
            var stale = AutoRoomLifecycle.MarkStaleForSelection(
                project,
                new HashSet<string>(new[] { active.Id }, StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(new[] { "A", "B", "C", "D", "E", "F" }, StringComparer.OrdinalIgnoreCase),
                "f", "z", new DateTime(2026, 8, 10, 1, 2, 3, DateTimeKind.Utc));
            Equal(1, stale.Count);
            Equal(old.Id, stale[0].Id);
            True(AutoRoomLifecycle.IsStaleAutoRoom(old));
            True(!AutoRoomLifecycle.IsStaleAutoRoom(active));
            True(!AutoRoomLifecycle.IsStaleAutoRoom(unrelated));
            Equal("2026-08-10T01:02:03.0000000Z", old.Properties["BoundaryStaleUtc"]);
        }

        private static void StaleRoomsAndDependentsAreExcludedFromBq()
        {
            var project = NewProject();
            var stale = AutoRoom("STALE", "A;B;C", project);
            stale.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateStale;
            stale.SetQuantity("PerimeterM", 10d);
            stale.MarkClean(ElementDirtyFlags.All);
            var active = AutoRoom("ACTIVE", "D;E;F", project);
            AutoRoomLifecycle.MarkActive(active, "D;E;F");
            active.SetQuantity("PerimeterM", 12d);
            active.MarkClean(ElementDirtyFlags.All);
            var finish = new ProjectElement("STALE-FINISH", ElementCategory.FloorFinish, "finish", "f", "z");
            finish.DependsOn.Add(stale.Id);
            finish.SetQuantity("AreaM2", 20d);
            finish.MarkClean(ElementDirtyFlags.All);
            project.Families.Add(new ProjectFamily("finish", "Finish", ElementCategory.FloorFinish));
            project.Elements.Add(stale); project.Elements.Add(active); project.Elements.Add(finish);

            True(AutoRoomLifecycle.IsExcludedFromQuantity(project, stale));
            True(AutoRoomLifecycle.IsExcludedFromQuantity(project, finish));
            True(!AutoRoomLifecycle.IsExcludedFromQuantity(project, active));
            var rows = ProjectQuantityReportBuilder.Group(project);
            Equal(1, rows.Count);
            Equal(1, rows[0].Count);
            Equal(active.Id, rows[0].ElementIds.Single());
        }

        private static void ReactivationClearsStaleState()
        {
            var project = NewProject();
            var room = AutoRoom("R", "A;B", project);
            room.Properties[AutoRoomLifecycle.BoundaryStateKey] = AutoRoomLifecycle.BoundaryStateStale;
            room.Properties["BoundaryStaleUtc"] = "old";
            room.Properties["BoundaryStaleReason"] = "old";
            AutoRoomLifecycle.MarkActive(room, "b;a");
            True(!AutoRoomLifecycle.IsStaleAutoRoom(room));
            Equal("A;B", room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey]);
            True(!room.Properties.ContainsKey("BoundaryStaleUtc"));
            True(!room.Properties.ContainsKey("BoundaryStaleReason"));
        }

        private static void FamilyDefaultsPreserveInstanceOverrides()
        {
            var project = NewProject();
            var oldFamily = project.FindFamily("room") ?? throw new Exception("Missing room family.");
            oldFamily.Properties["HeightM"] = "3.0";
            oldFamily.Properties["WidthM"] = "4.0";
            oldFamily.Properties["LegacyCode"] = "OLD";

            var nextFamily = new ProjectFamily("room-next", "Room Next", ElementCategory.Room);
            nextFamily.Properties["HeightM"] = "3.6";
            nextFamily.Properties["WidthM"] = "5.0";
            nextFamily.Properties["FireRating"] = "A";
            project.Families.Add(nextFamily);

            var room = AutoRoom("R-FAMILY", "A;B;C", project);
            room.Properties["HeightM"] = "3.0";
            room.Properties["WidthM"] = "9.0";
            room.Properties["LegacyCode"] = "OLD";
            project.Elements.Add(room);

            AutoRoomLifecycle.SyncFamilyDefaults(project, room, nextFamily);
            Equal("room-next", room.FamilyId);
            Equal("3.6", room.Properties["HeightM"]);
            Equal("9.0", room.Properties["WidthM"]);
            Equal("A", room.Properties["FireRating"]);
            True(!room.Properties.ContainsKey("LegacyCode"));

            room.Properties["HeightM"] = "4.2";
            nextFamily.Properties["HeightM"] = "4.0";
            nextFamily.Properties.Remove("FireRating");
            AutoRoomLifecycle.SyncFamilyDefaults(project, room, nextFamily);
            Equal("4.2", room.Properties["HeightM"]);
            Equal("9.0", room.Properties["WidthM"]);
            True(!room.Properties.ContainsKey("FireRating"));
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("p", "AutoRoom");
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.ActiveFloorId = "f";
            project.ActiveZoneId = "z";
            project.Families.Add(new ProjectFamily("room", "Room", ElementCategory.Room));
            return project;
        }

        private static ProjectElement AutoRoom(string id, string handles, ProjectState project)
        {
            var room = new ProjectElement(id, ElementCategory.Room, "room", "f", "z");
            room.Properties[AutoRoomLifecycle.BoundaryModeKey] = AutoRoomLifecycle.BoundaryModeAutoNetwork;
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = handles;
            room.Properties[AutoRoomLifecycle.BoundarySourceSignatureKey] = AutoRoomLifecycle.NormalizeSourceHandles(handles.Split(';'));
            return room;
        }

        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
