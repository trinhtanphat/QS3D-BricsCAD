using System;
<<<<<<< origin/main
using System.Collections.Generic;
=======
>>>>>>> origin/agent/review-hardening-20260810
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
<<<<<<< origin/main
        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandle { get; set; } = string.Empty;
            public string GeneratedHandle { get; set; } = string.Empty;
            public ElementCategory Category { get; set; }
        }

        public static bool Supports(ElementCategory category) =>
            category == ElementCategory.Beam || category == ElementCategory.Slab || category == ElementCategory.Column ||
            category == ElementCategory.StructuralWall || category == ElementCategory.Foundation;

        public static int BuildSelected(Document document, ProjectState project, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!Supports(category)) return 0;
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var ids = selection.Value.GetObjectIds();
            if (ids.Length == 0) return 0;
            var pending = new List<PendingUpdate>();

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in ids)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null) continue;
                    var handle = entity.Handle.ToString();
                    var element = project.Elements.FirstOrDefault(x => x.Category == category && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)));
                    if (element == null) continue;

                    Solid3d? solid = null;
                    try
                    {
                        if ((category == ElementCategory.Beam || category == ElementCategory.StructuralWall) && entity is Line line)
                            solid = BuildLinePrism(document, line, element, project.FindFamily(element.FamilyId), category);
                        else if ((category == ElementCategory.Slab || category == ElementCategory.Column || category == ElementCategory.Foundation) && entity is Polyline polyline && polyline.Closed)
                            solid = BuildClosedPolylinePrism(document, polyline, element, project.FindFamily(element.FamilyId), category);
                        if (solid == null) continue;

                        solid.Layer = entity.Layer;
                        var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, element);
                        modelSpace.AppendEntity(solid);
                        transaction.AddNewlyCreatedDBObject(solid, true);
                        pending.Add(new PendingUpdate
                        {
                            Element = element,
                            PreviousHandle = previousHandle,
                            GeneratedHandle = solid.Handle.ToString(),
                            Category = category
                        });
                    }
                    catch
                    {
                        solid?.Dispose();
                        throw;
                    }
                }
                transaction.Commit();
            }

            foreach (var update in pending)
                GeneratedGeometryService.CommitReplacement(update.Element, update.PreviousHandle, update.GeneratedHandle, update.Category);

            if (pending.Count > 0)
            {
                document.Editor.Regen();
                project.Touch();
            }
            return pending.Count;
=======
        public static bool Supports(ElementCategory category) => category == ElementCategory.Beam || category == ElementCategory.Slab || category == ElementCategory.Column || category == ElementCategory.StructuralWall || category == ElementCategory.Foundation;

        public static int BuildSelected(Document document, ProjectState project, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document)); if (project == null) throw new ArgumentNullException(nameof(project));
            if (!Supports(category)) return 0;
            var selection = document.Editor.SelectImplied(); if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var ids = selection.Value.GetObjectIds(); if (ids.Length == 0) return 0; var created = 0;
            using (document.LockDocument()) using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead); var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in ids)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity; if (entity == null) continue;
                    var handle = entity.Handle.ToString(); var element = project.Elements.FirstOrDefault(x => x.Category == category && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase))); if (element == null) continue;
                    Solid3d? solid = null;
                    try
                    {
                        GeneratedGeometryService.ErasePrevious(document, transaction, element);
                        if ((category == ElementCategory.Beam || category == ElementCategory.StructuralWall) && entity is Line line) solid = BuildLinePrism(document, line, element, project.FindFamily(element.FamilyId), category);
                        else if ((category == ElementCategory.Slab || category == ElementCategory.Column || category == ElementCategory.Foundation) && entity is Polyline polyline && polyline.Closed) solid = BuildClosedPolylinePrism(document, polyline, element, project.FindFamily(element.FamilyId), category);
                        if (solid == null) continue;
                        solid.Layer = entity.Layer; modelSpace.AppendEntity(solid); transaction.AddNewlyCreatedDBObject(solid, true); GeneratedGeometryService.Track(element, solid.Handle.ToString(), category); created++;
                    }
                    catch { solid?.Dispose(); throw; }
                }
                transaction.Commit();
            }
            if (created > 0) { document.Editor.Regen(); project.Touch(); } return created;
>>>>>>> origin/agent/review-hardening-20260810
        }

        private static Solid3d BuildLinePrism(Document document, Line line, ProjectElement element, ProjectFamily? family, ElementCategory category)
        {
            var widthM = category == ElementCategory.Beam ? Number(element, family, "WidthM", .3d) : Number(element, family, "ThicknessM", .2d);
<<<<<<< origin/main
            var heightM = Number(element, family, "HeightM", category == ElementCategory.Beam ? .5d : 3.6d);
            var bottomM = Number(element, family, "BottomOffsetM", 0d);
            var dx = line.EndPoint.X - line.StartPoint.X;
            var dy = line.EndPoint.Y - line.StartPoint.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 1e-6 || widthM <= 0d || heightM <= 0d) throw new InvalidOperationException("Kích thước structural LINE không hợp lệ.");

            var width = CadUnitService.MetersToDrawingUnits(document, widthM);
            var height = CadUnitService.MetersToDrawingUnits(document, heightM);
            var bottom = CadUnitService.MetersToDrawingUnits(document, bottomM);
            var angle = Math.Atan2(dy, dx);
            var mid = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2d, (line.StartPoint.Y + line.EndPoint.Y) / 2d, line.StartPoint.Z + bottom + height / 2d);
            var solid = new Solid3d();
            solid.SetDatabaseDefaults(document.Database);
            solid.CreateBox(length, width, height);
            solid.TransformBy(Matrix3d.Displacement(new Vector3d(-length / 2d, -width / 2d, -height / 2d)));
            solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
            solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));
            return solid;
=======
            var heightM = Number(element, family, "HeightM", category == ElementCategory.Beam ? .5d : 3.6d); var bottomM = Number(element, family, "BottomOffsetM", 0d);
            var dx = line.EndPoint.X - line.StartPoint.X; var dy = line.EndPoint.Y - line.StartPoint.Y; var length = Math.Sqrt(dx * dx + dy * dy); if (length <= 1e-6 || widthM <= 0d || heightM <= 0d) throw new InvalidOperationException("Kích thước structural LINE không hợp lệ.");
            var width = CadUnitService.MetersToDrawingUnits(document, widthM); var height = CadUnitService.MetersToDrawingUnits(document, heightM); var bottom = CadUnitService.MetersToDrawingUnits(document, bottomM); var angle = Math.Atan2(dy, dx);
            var mid = new Point3d((line.StartPoint.X + line.EndPoint.X) / 2d, (line.StartPoint.Y + line.EndPoint.Y) / 2d, line.StartPoint.Z + bottom + height / 2d);
            var solid = new Solid3d(); solid.SetDatabaseDefaults(document.Database); solid.CreateBox(length, width, height);
            solid.TransformBy(Matrix3d.Displacement(new Vector3d(-length / 2d, -width / 2d, -height / 2d))); solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin)); solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z))); return solid;
>>>>>>> origin/agent/review-hardening-20260810
        }

        private static Solid3d BuildClosedPolylinePrism(Document document, Polyline polyline, ProjectElement element, ProjectFamily? family, ElementCategory category)
        {
<<<<<<< origin/main
            var heightM = category == ElementCategory.Slab
                ? Number(element, family, "ThicknessM", .12d)
                : category == ElementCategory.Foundation
                    ? Number(element, family, "ThicknessM", .5d)
                    : Number(element, family, "HeightM", 3.6d);
            var bottomM = Number(element, family, "BottomOffsetM", 0d);
            if (heightM <= 0d) throw new InvalidOperationException("Chiều cao extrusion phải lớn hơn 0.");

            var height = CadUnitService.MetersToDrawingUnits(document, heightM);
            var bottom = CadUnitService.MetersToDrawingUnits(document, bottomM);
            var solid = new Solid3d();
            solid.SetDatabaseDefaults(document.Database);
            solid.CreateExtrudedSolid(polyline, new Vector3d(0d, 0d, height), new SweepOptions());
            if (Math.Abs(bottom) > 1e-12) solid.TransformBy(Matrix3d.Displacement(new Vector3d(0d, 0d, bottom)));
            return solid;
=======
            var heightM = category == ElementCategory.Slab ? Number(element, family, "ThicknessM", .12d) : category == ElementCategory.Foundation ? Number(element, family, "ThicknessM", .5d) : Number(element, family, "HeightM", 3.6d);
            var bottomM = Number(element, family, "BottomOffsetM", 0d); if (heightM <= 0d) throw new InvalidOperationException("Chiều cao extrusion phải lớn hơn 0.");
            var height = CadUnitService.MetersToDrawingUnits(document, heightM); var bottom = CadUnitService.MetersToDrawingUnits(document, bottomM);
            var solid = new Solid3d(); solid.SetDatabaseDefaults(document.Database); solid.CreateExtrudedSolid(polyline, new Vector3d(0d, 0d, height), new SweepOptions()); if (Math.Abs(bottom) > 1e-12) solid.TransformBy(Matrix3d.Displacement(new Vector3d(0d, 0d, bottom))); return solid;
>>>>>>> origin/agent/review-hardening-20260810
        }

        private static double Number(ProjectElement element, ProjectFamily? family, string name, double fallback)
        {
<<<<<<< origin/main
            if (element.Properties.TryGetValue(name, out var value) && TryFinite(value, out var direct)) return direct;
            if (family != null && family.Properties.TryGetValue(name, out value) && TryFinite(value, out var inherited)) return inherited;
            return fallback;
        }

        private static bool TryFinite(string value, out double number)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return false;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }
=======
            if (element.Properties.TryGetValue(name, out var value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var direct)) return direct;
            if (family != null && family.Properties.TryGetValue(name, out value) && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var inherited)) return inherited;
            return fallback;
        }
>>>>>>> origin/agent/review-hardening-20260810
    }
}
