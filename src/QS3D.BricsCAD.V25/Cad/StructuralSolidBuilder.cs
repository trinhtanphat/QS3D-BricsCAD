using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class StructuralSolidBuilder
    {
        public static int BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                var prompt = document.Editor.GetSelection();
                if (prompt.Status != PromptStatus.OK || prompt.Value == null) return 0;
                selection = prompt;
            }
            var live = CadHandleService.GetLiveHandles(document);
            var created = 0;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in selection.Value.GetObjectIds())
                {
                    var source = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (source == null) continue;
                    var handle = source.Handle.ToString();
                    var element = project.Elements.FirstOrDefault(x => x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)) && Supports(x.Category));
                    if (element == null) continue;
                    if (element.Properties.TryGetValue("GeneratedStructuralSolidHandle", out var existing) && !string.IsNullOrWhiteSpace(existing) && live.Contains(existing)) continue;
                    element.Properties.Remove("GeneratedStructuralSolidHandle");
                    var family = project.FindFamily(element.FamilyId);
                    var solid = CreateSolid(document, source, element, family);
                    if (solid == null) continue;
                    solid.Layer = source.Layer;
                    modelSpace.AppendEntity(solid);
                    transaction.AddNewlyCreatedDBObject(solid, true);
                    element.Properties["GeneratedStructuralSolidHandle"] = solid.Handle.ToString();
                    element.Properties["GeneratedStructuralSolidCategory"] = element.Category.ToString();
                    created++;
                }
                transaction.Commit();
            }
            if (created > 0) { project.Touch(); document.Editor.Regen(); }
            return created;
        }

        private static Solid3d? CreateSolid(Document document, Entity source, ProjectElement element, ProjectFamily? family)
        {
            if ((element.Category == ElementCategory.Beam || element.Category == ElementCategory.StructuralWall) && source is Line line)
                return CreateLinePrism(document, line, element, family);
            if ((element.Category == ElementCategory.Slab || element.Category == ElementCategory.Column || element.Category == ElementCategory.Foundation) && source is Polyline polyline && polyline.Closed)
                return CreateClosedPolylinePrism(document, polyline, element, family);
            return null;
        }

        private static Solid3d CreateLinePrism(Document document, Line line, ProjectElement element, ProjectFamily? family)
        {
            var widthM = element.Category == ElementCategory.Beam ? Number(element, family, "WidthM", .3d) : Number(element, family, "ThicknessM", .2d);
            var heightM = Number(element, family, "HeightM", element.Category == ElementCategory.Beam ? .5d : 3.6d);
            var offsetM = Number(element, family, "BottomOffsetM", 0d);
            var dx = line.EndPoint.X - line.StartPoint.X; var dy = line.EndPoint.Y - line.StartPoint.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 1e-6 || widthM <= 0d || heightM <= 0d) throw new InvalidOperationException("Structural LINE dimensions are invalid.");
            var width = widthM * 1000d; var height = heightM * 1000d;
            var mid = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2d, (line.StartPoint.Y + line.EndPoint.Y) / 2d, line.StartPoint.Z + offsetM * 1000d + height / 2d);
            var solid = new Solid3d(); solid.SetDatabaseDefaults(document.Database); solid.CreateBox(length, width, height);
            solid.TransformBy(Matrix3d.Rotation(Math.Atan2(dy, dx), Vector3d.ZAxis, Point3d.Origin));
            solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));
            return solid;
        }

        private static Solid3d CreateClosedPolylinePrism(Document document, Polyline polyline, ProjectElement element, ProjectFamily? family)
        {
            var heightM = element.Category == ElementCategory.Slab
                ? Number(element, family, "ThicknessM", .12d)
                : Number(element, family, "HeightM", element.Category == ElementCategory.Column ? 3.6d : .5d);
            if (element.Category == ElementCategory.Foundation) heightM = Number(element, family, "ThicknessM", heightM);
            var offsetM = Number(element, family, "BottomOffsetM", 0d);
            if (heightM <= 0d) throw new InvalidOperationException("Structural extrusion height must be positive.");
            var solid = new Solid3d(); solid.SetDatabaseDefaults(document.Database);
            solid.CreateExtrudedSolid(polyline, new Vector3d(0d, 0d, heightM * 1000d), new SweepOptions());
            if (Math.Abs(offsetM) > 1e-12) solid.TransformBy(Matrix3d.Displacement(new Vector3d(0d, 0d, offsetM * 1000d)));
            return solid;
        }

        private static bool Supports(ElementCategory category) => category == ElementCategory.Beam || category == ElementCategory.Slab || category == ElementCategory.Column || category == ElementCategory.StructuralWall || category == ElementCategory.Foundation;
        private static double Number(ProjectElement element, ProjectFamily? family, string key, double fallback)
        {
            if (element.Properties.TryGetValue(key, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var direct)) return direct;
            if (family != null && family.Properties.TryGetValue(key, out value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var inherited)) return inherited;
            return fallback;
        }
    }
}
