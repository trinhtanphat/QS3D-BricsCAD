using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class SemanticCaptureService
    {
        private static readonly ProjectUnitPolicy Units = new ProjectUnitPolicy(LengthUnit.Millimeter);

        public static int Capture(Document document, ElementCategory category)
        {
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            if (snapshots.Count == 0) return 0;
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var family = ResolveFamily(project, category);
            var count = 0;
            foreach (var snapshot in snapshots)
            {
                var id = category.ToString().ToUpperInvariant() + "-" + snapshot.Handle;
                var element = project.FindElement(id);
                if (element == null)
                {
                    element = new ProjectElement(id, category, family.Id, project.ActiveFloorId, project.ActiveZoneId);
                    project.Elements.Add(element);
                }
                element.Category = category;
                element.FamilyId = family.Id;
                element.FloorId = project.ActiveFloorId;
                element.ZoneId = project.ActiveZoneId;
                element.SourceHandles.Clear(); element.SourceHandles.Add(snapshot.Handle);
                element.DrawingFingerprint = project.DrawingFingerprint;
                element.Properties["Layer"] = snapshot.Layer;
                if (snapshot.LengthDrawingUnits.HasValue) element.Properties["LengthM"] = Units.ToMeters(snapshot.LengthDrawingUnits.Value).ToString("R", CultureInfo.InvariantCulture);
                if (snapshot.AreaDrawingUnitsSquared.HasValue) element.Properties["AreaM2"] = Units.AreaToSquareMeters(snapshot.AreaDrawingUnitsSquared.Value).ToString("R", CultureInfo.InvariantCulture);
                ApplyFamilyDefaults(element, family);
                element.MarkDirty(ElementDirtyFlags.All);
                Regenerate(project, element);
                count++;
            }
            project.Touch();
            return count;
        }

        public static int GenerateRoomFinishes(Document document)
        {
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
            var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rooms = project.Elements.Where(x => x.Category == ElementCategory.Room && x.SourceHandles.Any(handles.Contains)).ToList();
            var created = 0;
            foreach (var room in rooms)
            {
                foreach (var category in new[] { ElementCategory.FloorFinish, ElementCategory.Waterproofing, ElementCategory.Skirting, ElementCategory.WallFinish, ElementCategory.CeilingFinish })
                {
                    var id = room.Id + "-" + category.ToString();
                    var finish = project.FindElement(id);
                    if (finish == null)
                    {
                        var family = ResolveFamily(project, category);
                        finish = new ProjectElement(id, category, family.Id, room.FloorId, room.ZoneId);
                        finish.DependsOn.Add(room.Id);
                        project.Elements.Add(finish);
                        created++;
                    }
                    Copy(room, finish, "AreaM2");
                    Copy(room, finish, "PerimeterM");
                    Copy(room, finish, "HeightM");
                    Copy(room, finish, "OpeningAreaM2");
                    Copy(room, finish, "DoorWidthM");
                    finish.MarkDirty(ElementDirtyFlags.All);
                    Regenerate(project, finish);
                }
            }
            project.Touch();
            return created;
        }

        private static ProjectFamily ResolveFamily(ProjectState project, ElementCategory category)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == category) return active;
            }
            return project.Families.FirstOrDefault(x => x.Category == category) ?? CreateFamily(project, category);
        }

        private static void Regenerate(ProjectState project, ProjectElement element)
        {
            IElementRegenerator? regenerator = null;
            if (element.Category == ElementCategory.ArchitecturalWall || element.Category == ElementCategory.GlassWall || element.Category == ElementCategory.WallPier)
                regenerator = new WallRegenerator();
            else if (element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door)
                regenerator = new OpeningRegenerator();
            else
            {
                var structural = new StructuralRegenerator();
                if (structural.CanRegenerate(element.Category)) regenerator = structural;
                else
                {
                    var takeoff = new GenericTakeoffRegenerator();
                    regenerator = takeoff.CanRegenerate(element.Category) ? (IElementRegenerator)takeoff : new RoomRegenerator();
                }
            }

            if (regenerator.CanRegenerate(element.Category)) regenerator.Regenerate(project, element);
            element.MarkClean(ElementDirtyFlags.All);
        }

        private static ProjectFamily CreateFamily(ProjectState project, ElementCategory category)
        {
            var family = new ProjectFamily("auto-" + category.ToString().ToLowerInvariant(), DefaultName(category), category);
            switch (category)
            {
                case ElementCategory.ArchitecturalWall:
                    family.Properties["ThicknessM"] = "0.2"; family.Properties["HeightM"] = "3.6"; family.Properties["Material"] = "Gạch"; break;
                case ElementCategory.StructuralWall:
                    family.Properties["ThicknessM"] = "0.2"; family.Properties["HeightM"] = "3.6"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Beam:
                    family.Properties["WidthM"] = "0.3"; family.Properties["HeightM"] = "0.5"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Slab:
                    family.Properties["ThicknessM"] = "0.12"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Column:
                    family.Properties["WidthM"] = "0.4"; family.Properties["DepthM"] = "0.4"; family.Properties["HeightM"] = "3.6"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Foundation:
                    family.Properties["ThicknessM"] = "0.5"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Stair:
                    family.Properties["ThicknessM"] = "0.15"; family.Properties["Material"] = "Bê tông"; break;
                case ElementCategory.Railing:
                    family.Properties["Material"] = "Thép"; break;
                case ElementCategory.Earthwork:
                    family.Properties["DepthM"] = "1"; break;
            }
            if (category == ElementCategory.Room || category == ElementCategory.WallFinish) family.Properties["HeightM"] = "3.6";
            if (category == ElementCategory.WallOpening || category == ElementCategory.Door) family.Properties["HeightM"] = "2.2";
            project.Families.Add(family);
            return family;
        }

        private static string DefaultName(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Room: return "Phòng";
                case ElementCategory.ArchitecturalWall: return "Tường Gạch";
                case ElementCategory.StructuralWall: return "Vách BTCT";
                case ElementCategory.Beam: return "Dầm BTCT";
                case ElementCategory.Slab: return "Sàn BTCT";
                case ElementCategory.Column: return "Cột BTCT";
                case ElementCategory.Foundation: return "Móng BTCT";
                case ElementCategory.Stair: return "Cầu thang";
                case ElementCategory.Railing: return "Lan can";
                case ElementCategory.Earthwork: return "Đào đất";
                case ElementCategory.WallOpening: return "Lỗ Mở Vách";
                case ElementCategory.Door: return "Cửa Đi";
                case ElementCategory.FloorFinish: return "Sàn Hoàn Thiện";
                case ElementCategory.Waterproofing: return "Chống Thấm";
                case ElementCategory.Skirting: return "Chân Tường";
                case ElementCategory.WallFinish: return "Hoàn Thiện Tường";
                case ElementCategory.CeilingFinish: return "Trần Hoàn Thiện";
                case ElementCategory.CustomQuantity: return "Quick Takeoff";
                default: return category.ToString();
            }
        }

        private static void ApplyFamilyDefaults(ProjectElement element, ProjectFamily family)
        {
            foreach (var property in family.Properties)
                if (!element.Properties.ContainsKey(property.Key)) element.Properties[property.Key] = property.Value;
            if ((element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) && !element.Properties.ContainsKey("WidthM"))
                element.Properties["WidthM"] = element.Properties.TryGetValue("LengthM", out var length) ? length : "0.9";
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
