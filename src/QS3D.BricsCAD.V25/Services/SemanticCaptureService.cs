using System;
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
            var family = project.Families.FirstOrDefault(x => x.Category == category) ?? CreateFamily(project, category);
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
                element.SourceHandles.Clear();
                element.SourceHandles.Add(snapshot.Handle);
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
            var handles = snapshots.Select(x => x.Handle).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rooms = project.Elements.Where(x => x.Category == ElementCategory.Room && x.SourceHandles.Any(handles.Contains)).ToList();
            var created = 0;
            foreach (var room in rooms)
            {
                foreach (var category in new[] { ElementCategory.FloorFinish, ElementCategory.Skirting, ElementCategory.WallFinish, ElementCategory.CeilingFinish })
                {
                    var id = room.Id + "-" + category.ToString();
                    if (project.FindElement(id) != null) continue;
                    var family = project.Families.FirstOrDefault(x => x.Category == category) ?? CreateFamily(project, category);
                    var finish = new ProjectElement(id, category, family.Id, room.FloorId, room.ZoneId);
                    finish.DependsOn.Add(room.Id);
                    Copy(room, finish, "AreaM2");
                    Copy(room, finish, "PerimeterM");
                    Copy(room, finish, "HeightM");
                    finish.MarkDirty(ElementDirtyFlags.All);
                    Regenerate(project, finish);
                    project.Elements.Add(finish);
                    created++;
                }
            }
            project.Touch();
            return created;
        }

        private static void Regenerate(ProjectState project, ProjectElement element)
        {
            IElementRegenerator regenerator = element.Category == ElementCategory.ArchitecturalWall || element.Category == ElementCategory.GlassWall || element.Category == ElementCategory.WallPier
                ? (IElementRegenerator)new WallRegenerator()
                : element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door
                    ? new OpeningRegenerator()
                    : new RoomRegenerator();
            regenerator.Regenerate(project, element);
            element.MarkClean(ElementDirtyFlags.All);
        }

        private static ProjectFamily CreateFamily(ProjectState project, ElementCategory category)
        {
            var family = new ProjectFamily("auto-" + category.ToString().ToLowerInvariant(), category.ToString(), category);
            if (category == ElementCategory.ArchitecturalWall) { family.Properties["ThicknessM"] = "0.2"; family.Properties["HeightM"] = "3.6"; }
            if (category == ElementCategory.Room || category == ElementCategory.WallFinish) family.Properties["HeightM"] = "3.6";
            if (category == ElementCategory.WallOpening || category == ElementCategory.Door) family.Properties["HeightM"] = "2.2";
            project.Families.Add(family);
            return family;
        }

        private static void ApplyFamilyDefaults(ProjectElement element, ProjectFamily family)
        {
            foreach (var property in family.Properties) if (!element.Properties.ContainsKey(property.Key)) element.Properties[property.Key] = property.Value;
            if ((element.Category == ElementCategory.WallOpening || element.Category == ElementCategory.Door) && !element.Properties.ContainsKey("WidthM"))
                element.Properties["WidthM"] = element.Properties.TryGetValue("LengthM", out var length) ? length : "0.9";
            if (element.Category == ElementCategory.Room && element.Properties.TryGetValue("LengthM", out var perimeter)) element.Properties["PerimeterM"] = perimeter;
        }

        private static void Copy(ProjectElement from, ProjectElement to, string key)
        {
            if (from.Properties.TryGetValue(key, out var value)) to.Properties[key] = value;
            else if (from.Quantities.TryGetValue(key, out var quantity)) to.Properties[key] = quantity.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
