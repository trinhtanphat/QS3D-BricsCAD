using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class PolylineWallSolidBuilder
    {
        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandle { get; set; } = string.Empty;
            public string GeneratedHandle { get; set; } = string.Empty;
            public double LengthM { get; set; }
            public double FootprintAreaM2 { get; set; }
            public double ThicknessM { get; set; }
            public double HeightM { get; set; }
            public bool UsedBevelJoin { get; set; }
        }

        public static int BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var selectedIds = selection.Value.GetObjectIds();
            if (selectedIds.Length == 0) return 0;

            var pending = new List<PendingUpdate>();
            var processedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in selectedIds)
                {
                    var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                    if (polyline == null) continue;
                    if (polyline.Closed) throw new InvalidOperationException("Tường KT centerline POLYLINE phải open. Closed wall loops cần tách thành các wall centerline trước khi Build 3D.");
                    if (polyline.NumberOfVertices < 2) continue;

                    var handle = polyline.Handle.ToString();
                    var matches = project.Elements
                        .Where(x => x.Category == ElementCategory.ArchitecturalWall && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0) continue;
                    if (matches.Count > 1) throw new InvalidOperationException("CAD source handle " + handle + " đang thuộc nhiều QS3D wall element.");
                    var element = matches[0];
                    if (!processedElements.Add(element.Id)) throw new InvalidOperationException("Wall element " + element.Id + " có nhiều source đang được chọn. Tách/capture từng source thành element riêng trước khi Vẽ 3D.");

                    var family = project.FindFamily(element.FamilyId);
                    var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", 0.2d), element.Id + "/ThicknessM");
                    var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
                    var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                    var miterLimit = ProjectNumber(project, "WallMiterLimit", 4d, 1d);
                    var sagittaM = ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d);
                    var centerline = ReadCenterline(document, polyline, sagittaM);
                    var footprint = new WallFootprintEngine().Build(centerline, thicknessM, miterLimit, 1e-8d);

                    var profile = new Polyline();
                    Region? region = null;
                    var solid = new Solid3d();
                    try
                    {
                        for (var vertex = 0; vertex < footprint.Polygon.Count; vertex++)
                        {
                            var point = footprint.Polygon[vertex];
                            profile.AddVertexAt(vertex, new Point2d(
                                CadGeometryGuard.ToDrawingUnits(document, point.X, element.Id + "/footprint X"),
                                CadGeometryGuard.ToDrawingUnits(document, point.Y, element.Id + "/footprint Y")), 0d, 0d, 0d);
                        }
                        profile.Closed = true;
                        profile.Elevation = CadGeometryGuard.Add(polyline.Elevation, CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM"), element.Id + "/profile elevation");

                        var curves = new DBObjectCollection { profile };
                        var regions = Region.CreateFromCurves(curves);
                        if (regions == null || regions.Count != 1 || !(regions[0] is Region generatedRegion))
                            throw new InvalidOperationException("Không thể tạo một Region hợp lệ từ wall footprint " + element.Id + ".");
                        region = generatedRegion;

                        var height = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, heightM, element.Id + "/HeightM"), element.Id + "/Height drawing units");
                        solid.SetDatabaseDefaults(document.Database);
                        solid.CreateExtrudedSolid(region, new Vector3d(0d, 0d, height), new SweepOptions());
                        solid.Layer = polyline.Layer;

                        var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                        modelSpace.AppendEntity(solid);
                        transaction.AddNewlyCreatedDBObject(solid, true);
                        GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, ElementCategory.ArchitecturalWall);
                        pending.Add(new PendingUpdate
                        {
                            Element = element,
                            PreviousHandle = previousHandle,
                            GeneratedHandle = solid.Handle.ToString(),
                            LengthM = footprint.CenterlineLength,
                            FootprintAreaM2 = footprint.Area,
                            ThicknessM = thicknessM,
                            HeightM = heightM,
                            UsedBevelJoin = footprint.UsedBevelJoin
                        });
                    }
                    catch
                    {
                        solid.Dispose();
                        throw;
                    }
                    finally
                    {
                        region?.Dispose();
                        profile.Dispose();
                    }
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, ElementCategory.ArchitecturalWall);
                update.Element.Properties["LengthM"] = update.LengthM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["FootprintAreaM2"] = update.FootprintAreaM2.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["ThicknessM"] = update.ThicknessM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["HeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["WallJoinMode"] = update.UsedBevelJoin ? "Miter+BevelFallback" : "Miter";
            }

            if (pending.Count > 0)
            {
                document.Editor.Regen();
                project.Touch();
            }
            return pending.Count;
        }

        private static IReadOnlyList<Point2> ReadCenterline(Document document, Polyline polyline, double maximumSagittaM)
        {
            var result = new List<Point2>();
            for (var segment = 0; segment < polyline.NumberOfVertices - 1; segment++)
            {
                var startDrawing = polyline.GetPoint2dAt(segment);
                var endDrawing = polyline.GetPoint2dAt(segment + 1);
                var start = new Point2(
                    CadGeometryGuard.ToMeters(document, startDrawing.X, "wall polyline X"),
                    CadGeometryGuard.ToMeters(document, startDrawing.Y, "wall polyline Y"));
                var end = new Point2(
                    CadGeometryGuard.ToMeters(document, endDrawing.X, "wall polyline X"),
                    CadGeometryGuard.ToMeters(document, endDrawing.Y, "wall polyline Y"));
                var bulge = CadGeometryGuard.Finite(polyline.GetBulgeAt(segment), "wall polyline bulge");
                IReadOnlyList<Point2> segmentPoints = Math.Abs(bulge) <= 1e-12d
                    ? new[] { start, end }
                    : BulgeArcTessellator.Tessellate(start, end, bulge, maximumSagittaM);
                if (result.Count == 0) result.Add(segmentPoints[0]);
                for (var i = 1; i < segmentPoints.Count; i++) result.Add(segmentPoints[i]);
            }
            return result.AsReadOnly();
        }

        private static double ProjectNumber(ProjectState project, string key, double fallback, double minimum)
        {
            if (!project.Metadata.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text)) return fallback;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
                throw new InvalidOperationException("Project metadata " + key + " không hợp lệ: " + text);
            return value;
        }
    }
}
