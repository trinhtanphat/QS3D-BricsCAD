using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Services;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class SemanticCaptureService
    {
        private static readonly ElementCategory[] RoomFinishCategories =
        {
            ElementCategory.FloorFinish,
            ElementCategory.Waterproofing,
            ElementCategory.Skirting,
            ElementCategory.WallFinish,
            ElementCategory.CeilingFinish
        };

        public static int Capture(Document document, ElementCategory category)
        {
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            if (snapshots.Count == 0) return 0;
            var count = 0;
            foreach (var snapshot in snapshots) if (CaptureSnapshot(document, snapshot, category)) count++;
            return count;
        }

        public static bool CaptureSnapshot(Document document, EntitySnapshot snapshot, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var collision = project.Elements.FirstOrDefault(x => x.Category != category && x.SourceHandles.Any(h => string.Equals(h, snapshot.Handle, StringComparison.OrdinalIgnoreCase)));
            if (collision != null) throw new InvalidOperationException("CAD handle " + snapshot.Handle + " đang được QS3D theo dõi dưới loại " + collision.Category + ". Bỏ theo dõi trước khi đổi loại cấu kiện.");

            var id = category.ToString().ToUpperInvariant() + "-" + snapshot.Handle;
            var element = project.FindElement(id);
            ProjectFamily family;
            if (element == null)
            {
                family = ResolveFamily(project, category);
                element = new ProjectElement(id, category, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                project.Elements.Add(element);
            }
            else
            {
                family = project.FindFamily(element.FamilyId) ?? ResolveFamily(project, category);
                if (string.IsNullOrWhiteSpace(element.FamilyId)) element.FamilyId = family.Id;
            }
            element.Category = category;
            element.SourceHandles.Clear(); element.SourceHandles.Add(snapshot.Handle); element.DrawingFingerprint = project.DrawingFingerprint;
            element.Properties["Layer"] = snapshot.Layer;
            foreach (var key in element.Properties.Keys.Where(x => x.StartsWith("CAD.", StringComparison.OrdinalIgnoreCase)).ToList()) element.Properties.Remove(key);
            foreach (var item in snapshot.Metadata) element.Properties["CAD." + item.Key] = item.Value ?? string.Empty;

            var units = CadUnitService.GetPolicy(document);
            project.Metadata["QS3D.DrawingUnit"] = CadUnitService.Describe(document);
            if (CadUnitService.IsAssumedMillimeter(document)) project.Metadata["QS3D.DrawingUnitAssumption"] = "INSUNITS unsupported/undefined; assumed Millimeter";
            else project.Metadata.Remove("QS3D.DrawingUnitAssumption");
            ReplaceSourceMetric(element, "LengthM", snapshot.LengthDrawingUnits.HasValue ? units.ToMeters(snapshot.LengthDrawingUnits.Value) : (double?)null);
            ReplaceSourceMetric(element, "AreaM2", snapshot.AreaDrawingUnitsSquared.HasValue ? units.AreaToSquareMeters(snapshot.AreaDrawingUnitsSquared.Value) : (double?)null);
            ReplaceSourceMetric(element, "VolumeM3", snapshot.VolumeDrawingUnitsCubed.HasValue ? units.VolumeToCubicMeters(snapshot.VolumeDrawingUnitsCubed.Value) : (double?)null);
            ApplyFamilyDefaults(element, family); element.MarkDirty(ElementDirtyFlags.All); Regenerate(project, element); project.Touch(); return true;
        }

        private static void ReplaceSourceMetric(ProjectElement element, string key, double? value)
        {
            if (!value.HasValue)
            {
                element.Properties.Remove(key);
                return;
            }
            if (double.IsNaN(value.Value) || double.IsInfinity(value.Value)) throw new InvalidOperationException("CAD source metric must be finite: " + key + ".");
            element.Properties[key] = value.Value.ToString("R", CultureInfo.InvariantCulture);
        }

        public static int GenerateRoomFinishes(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rooms = project.Elements
                .Where(x => x.Category == ElementCategory.Room && !AutoRoomLifecycle.IsStaleAutoRoom(x) && SemanticReferenceHandles.Intersects(x, handles))
                .ToList();
            var created = 0;
            foreach (var room in rooms)
            {
                foreach (var category in RoomFinishCategories)
                {
                    var id = room.Id + "-" + category;
                    var finish = project.FindElement(id);
                    if (finish == null)
                    {
                        var family = ResolveFamily(project, category);
                        finish = new ProjectElement(id, category, family.Id, room.FloorId, room.ZoneId);
                        finish.DependsOn.Add(room.Id);
                        project.Elements.Add(finish);
                        created++;
                    }
                    else if (finish.Category != category)
                        throw new InvalidOperationException("Room finish id collision with category " + finish.Category + ": " + id);
                    EnsureRoomDependency(finish, room.Id);
                    SyncFinishFromRoom(room, finish);
                    Regenerate(project, finish);
                }
            }
            if (rooms.Count > 0) project.Touch();
            return created;
        }

        public static int SyncExistingRoomFinishes(ProjectState project, ProjectElement room)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (room.Category != ElementCategory.Room) throw new ArgumentException("Source element must be a Room.", nameof(room));
            var updated = 0;
            foreach (var category in RoomFinishCategories)
            {
                var finish = project.FindElement(room.Id + "-" + category) ?? project.Elements.FirstOrDefault(x =>
                    x.Category == category && x.DependsOn.Any(d => string.Equals(d, room.Id, StringComparison.OrdinalIgnoreCase)));
                if (finish == null) continue;
                EnsureRoomDependency(finish, room.Id);
                SyncFinishFromRoom(room, finish);
                Regenerate(project, finish);
                updated++;
            }
            if (updated > 0) project.Touch();
            return updated;
        }

        private static void SyncFinishFromRoom(ProjectElement room, ProjectElement finish)
        {
            finish.FloorId = room.FloorId;
            finish.ZoneId = room.ZoneId;
            finish.Properties["RoomSourceId"] = room.Id;
            Copy(room, finish, "AreaM2");
            Copy(room, finish, "PerimeterM");
            Copy(room, finish, "HeightM");
            Copy(room, finish, "OpeningAreaM2");
            Copy(room, finish, "DoorWidthM");
            finish.MarkDirty(ElementDirtyFlags.All);
        }

        private static void EnsureRoomDependency(ProjectElement finish, string roomId)
        {
            if (!finish.DependsOn.Any(x => string.Equals(x, roomId, StringComparison.OrdinalIgnoreCase))) finish.DependsOn.Add(roomId);
        }

        private static ProjectFamily ResolveFamily(ProjectState project, ElementCategory category)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId)) { var active = project.FindFamily(activeId); if (active != null && active.Category == category) return active; }
            return project.Families.FirstOrDefault(x => x.Category == category) ?? CreateFamily(project, category);
        }

        private static void Regenerate(ProjectState project, ProjectElement element)
        {
            IElementRegenerator? regenerator = null;
            if (element.Category == ElementCategory.ArchitecturalWall || element.Category == ElementCategory.GlassWall || element.Category == ElementCategory.WallPier) regenerator = new WallRegenerator();
            else if (element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) regenerator = new OpeningRegenerator();
            else
            {
                var structural = new StructuralRegenerator();
                if (structural.CanRegenerate(element.Category)) regenerator = structural;
                else { var takeoff = new GenericTakeoffRegenerator(); regenerator = takeoff.CanRegenerate(element.Category) ? (IElementRegenerator)takeoff : new RoomRegenerator(); }
            }
            if (regenerator.CanRegenerate(element.Category)) regenerator.Regenerate(project, element);
            element.MarkClean(ElementGeometryPolicy.SemanticCleanFlags(element.Category));
        }

        private static ProjectFamily CreateFamily(ProjectState project, ElementCategory category)
        {
            var family = new ProjectFamily("auto-" + category.ToString().ToLowerInvariant(), DefaultName(category), category);
            switch (category)
            {
                case ElementCategory.ArchitecturalWall: family.Properties["ThicknessM"] = "0.2"; family.Properties["HeightM"] = "3.6"; family.Properties["Material"] = "Gạch"; break;
                case ElementCategory.StructuralWall: family.Properties["ThicknessM"] = "0.2"; family.Properties["HeightM"] = "3.6"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Beam: family.Properties["WidthM"] = "0.3"; family.Properties["HeightM"] = "0.5"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Slab: family.Properties["ThicknessM"] = "0.12"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Column: family.Properties["WidthM"] = "0.4"; family.Properties["DepthM"] = "0.4"; family.Properties["HeightM"] = "3.6"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Foundation: family.Properties["ThicknessM"] = "0.5"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Stair: family.Properties["ThicknessM"] = "0.15"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Railing: family.Properties["Material"] = "Thép"; break;
                case ElementCategory.Earthwork: family.Properties["DepthM"] = "1"; break;
            }
            if (category == ElementCategory.Room || category == ElementCategory.WallFinish) family.Properties["HeightM"] = "3.6";
            if (category == ElementCategory.WallOpening || category == ElementCategory.Door) family.Properties["HeightM"] = "2.2";
            project.Families.Add(family); return family;
        }

        private static string DefaultName(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Room: return "Phòng"; case ElementCategory.ArchitecturalWall: return "Tường Gạch"; case ElementCategory.StructuralWall: return "Vách BTCT"; case ElementCategory.Beam: return "Dầm BTCT";
                case ElementCategory.Slab: return "Sàn BTCT"; case ElementCategory.Column: return "Cột BTCT"; case ElementCategory.Foundation: return "Móng BTCT"; case ElementCategory.Stair: return "Cầu thang";
                case ElementCategory.Railing: return "Lan can"; case ElementCategory.Earthwork: return "Đào đất"; case ElementCategory.WallOpening: return "Lỗ Mở Vách"; case ElementCategory.Door: return "Cửa Đi";
                case ElementCategory.FloorFinish: return "Sàn Hoàn Thiện"; case ElementCategory.Waterproofing: return "Chống Thấm"; case ElementCategory.Skirting: return "Chân Tường"; case ElementCategory.WallFinish: return "Hoàn Thiện Tường";
                case ElementCategory.CeilingFinish: return "Trần Hoàn Thiện"; case ElementCategory.CustomQuantity: return "Quick Takeoff"; default: return category.ToString();
            }
        }

        private static void ApplyFamilyDefaults(ProjectElement element, ProjectFamily family)
        {
            foreach (var property in family.Properties) if (!element.Properties.ContainsKey(property.Key)) element.Properties[property.Key] = property.Value;
            if ((element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) && !element.Properties.ContainsKey("WidthM")) element.Properties["WidthM"] = element.Properties.TryGetValue("LengthM", out var length) ? length : "0.9";
            if (element.Category == ElementCategory.Room && element.Properties.TryGetValue("LengthM", out var perimeter)) element.Properties["PerimeterM"] = perimeter;
            if ((element.Category == ElementCategory.Slab || element.Category == ElementCategory.Foundation || element.Category == ElementCategory.Stair || element.Category == ElementCategory.Earthwork) && element.Properties.TryGetValue("LengthM", out var outline) && !element.Properties.ContainsKey("PerimeterM")) element.Properties["PerimeterM"] = outline;
        }

        private static void Copy(ProjectElement from, ProjectElement to, string key)
        {
            if (from.Properties.TryGetValue(key, out var value)) to.Properties[key] = value;
            else if (from.Quantities.TryGetValue(key, out var quantity)) to.Properties[key] = quantity.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
