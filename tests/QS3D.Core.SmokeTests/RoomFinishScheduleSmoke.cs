using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishScheduleSmoke
    {
        public static void Run()
        {
            GroupsAreaAndLengthFinishesByRoom();
            FamilyMaterialAndInstanceOverrideSplitRows();
            SameRoomLabelsRemainSeparateByStableId();
            PreferredQuantityDoesNotEvaluateUnusedLegacyFallbacks();
            GeneratedRoomSourceIdAndDependencyResolveRoom();
            OrphanLinkedFinishIsExcluded();
            UnlinkedFinishRemainsSchedulable();
        }

        private static void GroupsAreaAndLengthFinishesByRoom()
        {
            var project = BaseProject();
            var wallFamily = new ProjectFamily("wf", "S?n n??c", ElementCategory.WallFinish);
            wallFamily.Properties["Material"] = "S?n";
            project.Families.Add(wallFamily);
            var skirtFamily = new ProjectFamily("sk", "Len g?ch", ElementCategory.Skirting);
            skirtFamily.Properties["Material"] = "G?ch";
            project.Families.Add(skirtFamily);

            var first = Finish("wf1", ElementCategory.WallFinish, wallFamily.Id, "room-1");
            first.Quantities["NetFinishAreaM2"] = 30d;
            var skirting = Finish("sk1", ElementCategory.Skirting, skirtFamily.Id, "room-1");
            skirting.Quantities["SkirtingLengthM"] = 14d;
            project.Elements.Add(first); project.Elements.Add(skirting);

            var rows = RoomFinishScheduleBuilder.Build(project);
            var wall = rows.Single(x => x.Category == "WallFinish");
            if (wall.Room != "Ph?ng 101" || wall.Count != 1 || wall.UnitHint != "m?") throw new Exception("Wall-finish grouping/room label failed.");
            Near(30d, wall.AreaM2); Near(30d, wall.PrimaryQuantity);
            var skirt = rows.Single(x => x.Category == "Skirting");
            if (skirt.UnitHint != "m") throw new Exception("Skirting unit failed.");
            Near(14d, skirt.LengthM); Near(14d, skirt.PrimaryQuantity);
        }

        private static void FamilyMaterialAndInstanceOverrideSplitRows()
        {
            var project = BaseProject();
            var roomFamily = project.Families.Single(x => x.Category == ElementCategory.Room);
            var secondRoom = new ProjectElement("room-2", ElementCategory.Room, roomFamily.Id, "f1", "z");
            secondRoom.Properties["RoomName"] = "Ph?ng 102";
            project.Elements.Add(secondRoom);
            ProjectMaterialCatalog.UpsertCustom(project, "tile-a", "G?ch A", "m?", "");
            ProjectMaterialCatalog.UpsertCustom(project, "tile-b", "G?ch B", "m?", "");
            var family = new ProjectFamily("ff", "S?n ho?n thi?n", ElementCategory.FloorFinish);
            family.Properties["Material"] = "G?ch A";
            project.Families.Add(family);
            var inherited = Finish("ff1", ElementCategory.FloorFinish, family.Id, "room-1");
            inherited.Quantities["AreaM2"] = 20d;
            var overridden = Finish("ff2", ElementCategory.FloorFinish, family.Id, "room-2");
            overridden.Properties["Material"] = "G?ch B";
            overridden.Quantities["BottomAreaM2"] = 5d;
            project.Elements.Add(inherited); project.Elements.Add(overridden);
            var rows = RoomFinishScheduleBuilder.Build(project).OrderBy(x => x.Material).ToList();
            if (rows.Count != 2 || rows[0].Material != "G?ch A" || rows[1].Material != "G?ch B") throw new Exception("Material override must split finish schedule rows.");
            Near(20d, rows[0].PrimaryQuantity); Near(5d, rows[1].PrimaryQuantity);
        }

        private static void SameRoomLabelsRemainSeparateByStableId()
        {
            var project = BaseProject();
            var roomFamily = project.Families.Single(x => x.Category == ElementCategory.Room);
            var secondRoom = new ProjectElement("room-2", ElementCategory.Room, roomFamily.Id, "f1", "z");
            secondRoom.Properties["RoomName"] = "Ph?ng 101";
            project.Elements.Add(secondRoom);

            var finishFamily = new ProjectFamily("wf-shared-name", "S?n n??c", ElementCategory.WallFinish);
            project.Families.Add(finishFamily);
            var first = Finish("wf-room-1", ElementCategory.WallFinish, finishFamily.Id, "room-1");
            first.Quantities["NetFinishAreaM2"] = 10d;
            var second = Finish("wf-room-2", ElementCategory.WallFinish, finishFamily.Id, "room-2");
            second.Quantities["NetFinishAreaM2"] = 12d;
            project.Elements.Add(first);
            project.Elements.Add(second);

            var rows = RoomFinishScheduleBuilder.Build(project).Where(x => x.Category == "WallFinish").ToList();
            if (rows.Count != 2) throw new Exception("Rooms with the same display label must remain separate by stable room id.");
            if (rows.Any(x => x.Room != "Ph?ng 101" || x.RoomIds.Count != 1)) throw new Exception("Room label/provenance must remain readable while grouping uses stable ids.");
            Near(22d, rows.Sum(x => x.PrimaryQuantity));
        }

        private static void PreferredQuantityDoesNotEvaluateUnusedLegacyFallbacks()
        {
            var project = BaseProject();
            var family = new ProjectFamily("wf-priority", "S?n ?u ti?n", ElementCategory.WallFinish);
            project.Families.Add(family);
            var finish = Finish("wf-priority-1", ElementCategory.WallFinish, family.Id, "room-1");
            finish.Quantities["NetFinishAreaM2"] = 9d;
            ProjectElementPersistenceFixture.SetQuantity(finish, "SideAreaM2", double.NaN);
            ProjectElementPersistenceFixture.SetQuantity(finish, "AreaM2", -1d);
            project.Elements.Add(finish);

            var row = RoomFinishScheduleBuilder.Build(project).Single(x => x.Category == "WallFinish");
            Near(9d, row.AreaM2);
            Near(9d, row.PrimaryQuantity);
        }

        private static void GeneratedRoomSourceIdAndDependencyResolveRoom()
        {
            var project = BaseProject();
            var family = new ProjectFamily("wf-generated", "S?n generated", ElementCategory.WallFinish);
            project.Families.Add(family);
            var finish = new ProjectElement("room-1-WallFinish", ElementCategory.WallFinish, family.Id, "f1", "z");
            finish.Properties[AutoRoomLifecycle.RoomSourceIdKey] = "room-1";
            finish.DependsOn.Add("room-1");
            finish.Quantities["NetFinishAreaM2"] = 11d;
            project.Elements.Add(finish);

            var row = RoomFinishScheduleBuilder.Build(project).Single();
            if (row.Room != "Ph?ng 101" || row.RoomIds.Count != 1 || row.RoomIds[0] != "room-1")
                throw new Exception("Generated RoomSourceId/DependsOn provenance must resolve to the semantic room.");
            Near(11d, row.PrimaryQuantity);
        }

        private static void OrphanLinkedFinishIsExcluded()
        {
            var project = BaseProject();
            var family = new ProjectFamily("wf-orphan", "S?n orphan", ElementCategory.WallFinish);
            project.Families.Add(family);
            var orphan = new ProjectElement("wf-orphan-1", ElementCategory.WallFinish, family.Id, "f1", "z");
            orphan.Properties[AutoRoomLifecycle.RoomSourceIdKey] = "missing-room";
            orphan.Quantities["NetFinishAreaM2"] = 99d;
            project.Elements.Add(orphan);
            if (RoomFinishScheduleBuilder.Build(project).Count != 0) throw new Exception("Orphan room-linked finishes must be excluded from schedule quantities.");
        }

        private static void UnlinkedFinishRemainsSchedulable()
        {
            var project = BaseProject();
            var family = new ProjectFamily("ceil", "Tr?n", ElementCategory.CeilingFinish);
            project.Families.Add(family);
            var finish = new ProjectElement("ceil1", ElementCategory.CeilingFinish, family.Id, "f1", "z");
            finish.Quantities["TopAreaM2"] = 16d;
            project.Elements.Add(finish);
            var row = RoomFinishScheduleBuilder.Build(project).Single();
            if (row.Room != "(ch?a li?n k?t ph?ng)" || row.RoomIds.Count != 0) throw new Exception("Unlinked finish should remain schedulable with explicit label.");
            Near(16d, row.PrimaryQuantity);
        }

        private static ProjectState BaseProject()
        {
            var project = new ProjectState("p", "Finish schedule");
            project.Floors.Add(new FloorDefinition("f1", "T?ng 1", 0d));
            var roomFamily = new ProjectFamily("room-family", "Ph?ng", ElementCategory.Room);
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Families.Add(roomFamily);
            var room = new ProjectElement("room-1", ElementCategory.Room, roomFamily.Id, "f1", "z");
            room.Properties["RoomName"] = "Ph?ng 101";
            project.Elements.Add(room);
            return project;
        }

        private static ProjectElement Finish(string id, ElementCategory category, string familyId, string roomId)
        {
            var element = new ProjectElement(id, category, familyId, "f1", "z");
            element.Properties["ParentRoomId"] = roomId;
            return element;
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
