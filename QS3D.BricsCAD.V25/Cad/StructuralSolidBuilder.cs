using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class StructuralSolidBuilder
    {
        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandle { get; set; } = string.Empty;
            public string GeneratedHandle { get; set; } = string.Empty;
            public ElementCategory Category { get; set; }
        }

        public static bool Supports(ElementCategory category) =>
            category == ElementCategory.Beam || category == ElementCategory.Slab || category == ElementCategory.Column ||
            category == ElementCategory.StructuralWall || category == ElementCategory.Foundation || category == ElementCategory.Stair ||
            category == ElementCategory.Railing || category == ElementCategory.Earthwork;

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
            var processedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rollback = ProjectStateSnapshot.Capture(project);
            var cadCommitted = false;

            try
            {
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    foreach (var id in ids)
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                        if (entity == null || entity.IsErased) continue;
                        var handle = entity.Handle.ToString();
                        var matches = project.Elements
                            .Where(x => x.Category == category && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                            .Take(2)
                            .ToList();
                        if (matches.Count == 0) continue;
                        if (matches.Count > 1) throw new InvalidOperationException("CAD source handle " + handle + " đang thuộc nhiều QS3D " + category + " element.");
                        var element = matches[0];
                        if (!processedElements.Add(element.Id)) throw new InvalidOperationException(category + " element " + element.Id + " có nhiều source đang được chọn. Tách/capture từng source thành element riêng trước khi Vẽ 3D.");

                        var family = project.FindFamily(element.FamilyId);
                        Solid3d solid;
                        if (UsesLine(category))
                        {
                            if (!(entity is Line line)) throw new InvalidOperationException(category + " element " + element.Id + " cần source LINE để dựng 3D.");
                            solid = BuildLinePrism(document, project, line, element, family, category);
                        }
                        else
                        {
                            if (!(entity is Polyline polyline) || !polyline.Closed) throw new InvalidOperationException(category + " element " + element.Id + " cần closed POLYLINE để dựng 3D.");
                            solid = BuildClosedPolylinePrism(document, project, polyline, element, family, category);
                        }

                        try
                        {
                            solid.Layer = entity.Layer;
                            var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, category);
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
                            solid.Dispose();
                            throw;
                        }
                    }

                    foreach (var update in pending)
                    {
                        GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, update.Category);
                        update.Element.Properties["GeneratedSolidMode"] = GeometryMode(update.Category);
                    }

                    if (pending.Count > 0) project.Touch();
                    transaction.Commit();
                    cadCommitted = true;
                }
            }
            catch (Exception operationError)
            {
                if (!cadCommitted)
                {
                    try { rollback.Restore(project); }
                    catch (Exception restoreError)
                    {
                        throw new InvalidOperationException(
                            "Structural replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            if (pending.Count > 0)
                CadPostCommitUi.TryRegen(document, "Structural native 3D");
            return pending.Count;
        }

        private static bool UsesLine(ElementCategory category) =>
            category == ElementCategory.Beam || category == ElementCategory.StructuralWall || category == ElementCategory.Railing;

        private static Solid3d BuildLinePrism(Document document, ProjectState project, Line line, ProjectElement element, ProjectFamily? family, ElementCategory category)
        {
            double widthM;
            double heightM;
            switch (category)
            {
                case ElementCategory.Beam:
                    widthM = CadGeometryGuard.Number(element, family, "WidthM", .3d);
                    heightM = CadGeometryGuard.Number(element, family, "HeightM", .5d);
                    break;
                case ElementCategory.StructuralWall:
                    widthM = CadGeometryGuard.Number(element, family, "ThicknessM", .2d);
                    heightM = CadGeometryGuard.Number(element, family, "HeightM", 3.6d);
                    break;
                case ElementCategory.Railing:
                    widthM = CadGeometryGuard.Number(element, family, "ProfileWidthM", .05d);
                    heightM = CadGeometryGuard.Number(element, family, "HeightM", 1.1d);
                    break;
                default:
                    throw new InvalidOperationException("Category không hỗ trợ LINE prism: " + category);
            }
            widthM = CadGeometryGuard.Positive(widthM, element.Id + "/3D width");
            heightM = CadGeometryGuard.Positive(heightM, element.Id + "/3D height");
            var bottomM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
            var placement = category == ElementCategory.Railing
                ? null
                : CadVerticalPlacementResolver.Resolve(
                    document,
                    project,
                    element,
                    line.StartPoint.Z,
                    heightM,
                    bottomM);
            var dx = CadGeometryGuard.Subtract(line.EndPoint.X, line.StartPoint.X, element.Id + "/dx");
            var dy = CadGeometryGuard.Subtract(line.EndPoint.Y, line.StartPoint.Y, element.Id + "/dy");
            var dz = CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z, element.Id + "/dz");
            var planTolerance = CadGeometryGuard.Positive(
                CadGeometryGuard.ToDrawingUnits(document, .005d, element.Id + "/line planarity tolerance"),
                element.Id + "/line planarity tolerance drawing units");
            if (Math.Abs(dz) > planTolerance)
                throw new InvalidOperationException(category + " source LINE hiện yêu cầu gần ngang (|ΔZ| <= 0.005 m): " + element.Id);
            var length = CadGeometryGuard.Hypot(dx, dy, element.Id + "/source length");
            if (length <= 1e-6) throw new InvalidOperationException("Structural LINE quá ngắn: " + element.Id);

            var width = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, widthM, element.Id + "/3D width"), element.Id + "/3D width drawing units");
            var height = placement?.HeightDrawingUnits ?? CadGeometryGuard.Positive(
                CadGeometryGuard.ToDrawingUnits(document, heightM, element.Id + "/3D height"),
                element.Id + "/3D height drawing units");
            var bottom = placement?.BottomDrawingUnits ?? CadGeometryGuard.Add(
                line.StartPoint.Z,
                CadGeometryGuard.ToDrawingUnits(document, bottomM, element.Id + "/BottomOffsetM"),
                element.Id + "/legacy base Z");
            var angle = CadGeometryGuard.Finite(Math.Atan2(dy, dx), element.Id + "/angle");
            var midX = CadGeometryGuard.Midpoint(line.StartPoint.X, line.EndPoint.X, element.Id + "/mid X");
            var midY = CadGeometryGuard.Midpoint(line.StartPoint.Y, line.EndPoint.Y, element.Id + "/mid Y");
            var midZ = CadGeometryGuard.Add(bottom, height / 2d, element.Id + "/mid Z");
            var mid = new Point3d(midX, midY, midZ);

            var solid = new Solid3d();
            solid.SetDatabaseDefaults(document.Database);
            solid.CreateBox(length, width, height);
            solid.TransformBy(Matrix3d.Displacement(new Vector3d(-length / 2d, -width / 2d, -height / 2d)));
            solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
            solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));
            return solid;
        }

        private static Solid3d BuildClosedPolylinePrism(Document document, ProjectState project, Polyline polyline, ProjectElement element, ProjectFamily? family, ElementCategory category)
        {
            var direction = 1d;
            double heightM;
            switch (category)
            {
                case ElementCategory.Slab: heightM = CadGeometryGuard.Number(element, family, "ThicknessM", .12d); break;
                case ElementCategory.Foundation: heightM = CadGeometryGuard.Number(element, family, "ThicknessM", .5d); break;
                case ElementCategory.Stair: heightM = CadGeometryGuard.Number(element, family, "ThicknessM", .15d); break;
                case ElementCategory.Earthwork: heightM = CadGeometryGuard.Number(element, family, "DepthM", 1d); direction = -1d; break;
                case ElementCategory.Column: heightM = CadGeometryGuard.Number(element, family, "HeightM", 3.6d); break;
                default: throw new InvalidOperationException("Category không hỗ trợ closed polyline prism: " + category);
            }
            heightM = CadGeometryGuard.Positive(heightM, element.Id + "/extrusion height");
            var offsetKey = category == ElementCategory.Earthwork ? "TopOffsetM" : "BottomOffsetM";
            var offsetM = CadGeometryGuard.Number(element, family, offsetKey, 0d);
            CadVerticalPlacement? placement = null;
            if (category == ElementCategory.Slab || category == ElementCategory.Foundation || category == ElementCategory.Column)
                placement = CadVerticalPlacementResolver.Resolve(document, project, element, polyline.Elevation, heightM, offsetM);
            var heightMagnitude = placement?.HeightDrawingUnits ?? CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, heightM, element.Id + "/extrusion height"), element.Id + "/extrusion drawing height");
            var height = CadGeometryGuard.Finite(heightMagnitude * direction, element.Id + "/signed extrusion height");
            var offset = placement == null
                ? CadGeometryGuard.ToDrawingUnits(document, offsetM, element.Id + "/" + offsetKey)
                : CadGeometryGuard.Subtract(placement.BottomDrawingUnits, polyline.Elevation, element.Id + "/resolved base displacement");

            var solid = new Solid3d();
            solid.SetDatabaseDefaults(document.Database);
            solid.CreateExtrudedSolid(polyline, new Vector3d(0d, 0d, height), new SweepOptions());
            if (Math.Abs(offset) > 1e-12) solid.TransformBy(Matrix3d.Displacement(new Vector3d(0d, 0d, offset)));
            return solid;
        }

        private static string GeometryMode(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Railing: return "LinePrism";
                case ElementCategory.Stair: return "FootprintMass";
                case ElementCategory.Earthwork: return "DownwardFootprintMass";
                default: return "NativePrism";
            }
        }
    }
}
