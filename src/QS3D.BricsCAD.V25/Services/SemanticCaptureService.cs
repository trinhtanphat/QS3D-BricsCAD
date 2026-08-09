using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Rebar;
using QS3D.Core.Services;
using QS3D.Core.Units;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class SemanticCaptureService
    {
        private static readonly ProjectUnitPolicy Units = new ProjectUnitPolicy(LengthUnit.Millimeter);
        public static int Capture(Document document, ElementCategory category) => CaptureSnapshots(document, EntitySnapshotReader.ReadCurrentSelection(document), category);
        public static int CaptureSnapshots(Document document, IEnumerable<EntitySnapshot> snapshots, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document)); if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
            var project = ProjectContextCoordinator.GetOrCreate(document); var family = ResolveFamily(project, category); var count = 0;
            foreach (var snapshot in snapshots) if (CaptureSnapshot(project, snapshot, category, family)) count++;
            project.Touch(); return count;
        }
        public static bool CaptureSnapshot(Document document, EntitySnapshot snapshot, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document)); var project = ProjectContextCoordinator.GetOrCreate(document); var family = ResolveFamily(project, category); var changed = CaptureSnapshot(project, snapshot, category, family); project.Touch(); return changed;
        }
        private static bool CaptureSnapshot(ProjectState project, EntitySnapshot snapshot, ElementCategory category, ProjectFamily family)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var element = project.Elements.FirstOrDefault(x => x.SourceHandles.Any(h => string.Equals(h, snapshot.Handle, StringComparison.OrdinalIgnoreCase)));
            if (element == null)
            {
                var id = category.ToString().ToUpperInvariant() + "-" + snapshot.Handle; element = project.FindElement(id);
                if (element == null) { element = new ProjectElement(id, category, family.Id, project.ActiveFloorId, project.ActiveZoneId); project.Elements.Add(element); }
            }
            element.Category = category; element.FamilyId = family.Id; element.FloorId = project.ActiveFloorId; element.ZoneId = project.ActiveZoneId; element.SourceHandles.Clear(); element.SourceHandles.Add(snapshot.Handle); element.DrawingFingerprint = project.DrawingFingerprint; element.Properties["Layer"] = snapshot.Layer; element.Properties["EntityType"] = snapshot.EntityType;
            ApplyGeometry(element, snapshot); ApplyFamilyDefaults(element, family); element.MarkDirty(ElementDirtyFlags.All); RegenerateElement(project, element); return true;
        }
        private static void ApplyGeometry(ProjectElement element, EntitySnapshot snapshot)
        {
            if (snapshot.LengthDrawingUnits.HasValue)
            {
                var length = Units.ToMeters(snapshot.LengthDrawingUnits.Value).ToString("R", CultureInfo.InvariantCulture); element.Properties["LengthM"] = length;
                if (element.Category == ElementCategory.Room || element.Category == ElementCategory.Slab || element.Category == ElementCategory.Column || element.Category == ElementCategory.Foundation) element.Properties["PerimeterM"] = length;
                if (element.Category == ElementCategory.Rebar) element.Properties["CutLengthM"] = length;
            }
            if (snapshot.AreaDrawingUnitsSquared.HasValue) element.Properties["AreaM2"] = Units.AreaToSquareMeters(snapshot.AreaDrawingUnitsSquared.Value).ToString("R", CultureInfo.InvariantCulture);
            foreach (var pair in snapshot.Metadata) if (!element.Properties.ContainsKey("CAD:" + pair.Key)) element.Properties["CAD:" + pair.Key] = pair.Value;
        }
        public static int GenerateRoomFinishes(Document document)
        {
            var snapshots = EntitySnapshotReader.ReadCurrentSelection(document); var handles = new HashSet<string>(snapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase); var project = ProjectContextCoordinator.GetOrCreate(document); var rooms = project.Elements.Where(x => x.Category == ElementCategory.Room && x.SourceHandles.Any(handles.Contains)).ToList(); var created = 0;
            foreach (var room in rooms) foreach (var category in new[] { ElementCategory.FloorFinish, ElementCategory.Waterproofing, ElementCategory.Skirting, ElementCategory.WallFinish, ElementCategory.CeilingFinish })
            {
                var id = room.Id + "-" + category; var finish = project.FindElement(id);
                if (finish == null) { var family = ResolveFamily(project, category); finish = new ProjectElement(id, category, family.Id, room.FloorId, room.ZoneId); finish.DependsOn.Add(room.Id); project.Elements.Add(finish); created++; }
                Copy(room, finish, "AreaM2"); Copy(room, finish, "PerimeterM"); Copy(room, finish, "HeightM"); Copy(room, finish, "OpeningAreaM2"); Copy(room, finish, "DoorWidthM"); finish.MarkDirty(ElementDirtyFlags.All); RegenerateElement(project, finish);
            }
            project.Touch(); return created;
        }
        public static void RegenerateElement(ProjectState project, ProjectElement element)
        {
            IElementRegenerator regenerator;
            if (new WallRegenerator().CanRegenerate(element.Category)) regenerator = new WallRegenerator();
            else if (new OpeningRegenerator().CanRegenerate(element.Category)) regenerator = new OpeningRegenerator();
            else if (new RoomRegenerator().CanRegenerate(element.Category)) regenerator = new RoomRegenerator();
            else if (new StructuralRegenerator().CanRegenerate(element.Category)) regenerator = new StructuralRegenerator();
            else if (new RebarRegenerator().CanRegenerate(element.Category)) regenerator = new RebarRegenerator();
            else if (new GenericQuantityRegenerator().CanRegenerate(element.Category)) regenerator = new GenericQuantityRegenerator();
            else throw new InvalidOperationException("Chưa có quantity regenerator cho " + element.Category + ".");
            regenerator.Regenerate(project, element); element.MarkClean(ElementDirtyFlags.All);
        }
        private static ProjectFamily ResolveFamily(ProjectState project, ElementCategory category)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId)) { var active = project.FindFamily(activeId); if (active != null && active.Category == category) return active; }
            return project.Families.FirstOrDefault(x => x.Category == category) ?? CreateFamily(project, category);
        }
        private static ProjectFamily CreateFamily(ProjectState project, ElementCategory category)
        {
            var family = new ProjectFamily("auto-" + category.ToString().ToLowerInvariant(), DefaultName(category), category);
            if (category == ElementCategory.ArchitecturalWall) { P(family,"ThicknessM","0.2"); P(family,"HeightM","3.6"); P(family,"Material","Gạch"); }
            if (category == ElementCategory.Room || category == ElementCategory.WallFinish) P(family,"HeightM","3.6");
            if (category == ElementCategory.WallOpening || category == ElementCategory.Door) P(family,"HeightM","2.2");
            if (category == ElementCategory.Beam) { P(family,"WidthM","0.2"); P(family,"HeightM","0.4"); P(family,"Material","Bê tông"); }
            if (category == ElementCategory.Slab) { P(family,"ThicknessM","0.12"); P(family,"Material","Bê tông"); }
            if (category == ElementCategory.Column) { P(family,"WidthM","0.3"); P(family,"DepthM","0.3"); P(family,"HeightM","3.6"); P(family,"Material","Bê tông"); }
            if (category == ElementCategory.StructuralWall) { P(family,"ThicknessM","0.2"); P(family,"HeightM","3.6"); P(family,"Material","Bê tông"); }
            if (category == ElementCategory.Foundation) { P(family,"HeightM","0.5"); P(family,"Material","Bê tông"); }
            if (category == ElementCategory.Earthwork) { P(family,"DepthM","0.5"); P(family,"SwellFactor","0.15"); }
            if (category == ElementCategory.Rebar) { P(family,"Notation","4D16"); P(family,"Grade","CB400-V"); P(family,"Shape","Straight"); }
            project.Families.Add(family); return family;
        }
        private static string DefaultName(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Room: return "Phòng"; case ElementCategory.ArchitecturalWall: return "Tường Gạch"; case ElementCategory.WallOpening: return "Lỗ Mở Vách"; case ElementCategory.Door: return "Cửa Đi";
                case ElementCategory.FloorFinish: return "Sàn Hoàn Thiện"; case ElementCategory.Waterproofing: return "Chống Thấm"; case ElementCategory.Skirting: return "Chân Tường"; case ElementCategory.WallFinish: return "Hoàn Thiện Tường"; case ElementCategory.CeilingFinish: return "Trần Hoàn Thiện";
                case ElementCategory.Beam: return "Dầm BTCT"; case ElementCategory.Slab: return "Sàn BTCT"; case ElementCategory.Column: return "Cột BTCT"; case ElementCategory.StructuralWall: return "Vách BTCT"; case ElementCategory.Foundation: return "Móng BTCT"; case ElementCategory.Earthwork: return "Đào đắp"; case ElementCategory.Rebar: return "Cốt thép"; default: return category.ToString();
            }
        }
        private static void ApplyFamilyDefaults(ProjectElement element, ProjectFamily family)
        {
            foreach (var property in family.Properties) if (!element.Properties.ContainsKey(property.Key)) element.Properties[property.Key] = property.Value;
            if ((element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) && !element.Properties.ContainsKey("WidthM")) element.Properties["WidthM"] = element.Properties.TryGetValue("LengthM", out var length) ? length : "0.9";
            if (element.Category == ElementCategory.Room && element.Properties.TryGetValue("LengthM", out var perimeter)) element.Properties["PerimeterM"] = perimeter;
        }
        private static void P(ProjectFamily family, string key, string value) => family.Properties[key] = value;
        private static void Copy(ProjectElement from, ProjectElement to, string key) { if (from.Properties.TryGetValue(key, out var value)) to.Properties[key] = value; else if (from.Quantities.TryGetValue(key, out var quantity)) to.Properties[key] = quantity.ToString("R", CultureInfo.InvariantCulture); }
    }
}
