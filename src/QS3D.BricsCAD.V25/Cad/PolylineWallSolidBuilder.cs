using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
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
            public double NativeHeightM { get; set; }
            public bool UsedBevelJoin { get; set; }
            public bool IsWallPierPathProfile { get; set; }
            public WallPierProfileMode WallPierMode { get; set; }
            public double WallPierChamferM { get; set; }
            public double WallPierPerimeterM { get; set; }
            public double WallPierGrossVolumeM3 { get; set; }
            public double WallPierLateralAreaM2 { get; set; }
        }

        public static int BuildSelected(Document document, ProjectState project) =>
            BuildSelected(document, project, ElementCategory.ArchitecturalWall);

        public static int BuildSelected(Document document, ProjectState project, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!IsSupportedWall(category)) throw new ArgumentOutOfRangeException(nameof(category), "Unsupported architectural wall category: " + category);
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var selectedIds = selection.Value.GetObjectIds();
            if (selectedIds.Length == 0) return 0;

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
                    foreach (var id in selectedIds)
                    {
                        var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                        if (polyline == null) continue;
                        if (polyline.Closed) throw new InvalidOperationException("Tường KT centerline POLYLINE phải open. Closed wall loops cần tách thành các wall centerline trước khi Build 3D.");
                        if (polyline.NumberOfVertices < 2) continue;

                        var handle = polyline.Handle.ToString();
                        var matches = project.Elements
                            .Where(x => x.Category == category && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
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
                        var placement = CadVerticalPlacementResolver.Resolve(
                            document,
                            project,
                            element,
                            polyline.Elevation,
                            heightM,
                            bottomOffsetM);
                        var nativeHeightM = placement.HeightM;
                        var miterLimit = ProjectNumber(project, "WallMiterLimit", 4d, 1d);
                        var sagittaM = ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d);
                        var centerline = ReadCenterline(document, polyline, sagittaM);

                        IReadOnlyList<Point2> polygon;
                        double centerlineLengthM;
                        double footprintAreaM2;
                        double footprintPerimeterM;
                        double grossVolumeM3;
                        double lateralAreaM2;
                        bool usedBevelJoin;
                        var wallPierMode = WallPierProfileMode.Rectangular;
                        var wallPierChamferM = 0d;
                        if (category == ElementCategory.WallPier)
                        {
                            wallPierMode = ResolveWallPierMode(element, family);
                            wallPierChamferM = wallPierMode == WallPierProfileMode.Chamfered
                                ? CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "WallPierChamferM", 0.02d), element.Id + "/WallPierChamferM")
                                : 0d;
                            var pathProfile = WallPierPathProfilePlanner.Plan(new WallPierPathProfileInput
                            {
                                Centerline = centerline,
                                ThicknessM = thicknessM,
                                HeightM = nativeHeightM,
                                Mode = wallPierMode,
                                ChamferM = wallPierChamferM,
                                MiterLimit = miterLimit,
                                Tolerance = 1e-8d
                            });
                            polygon = pathProfile.Polygon;
                            centerlineLengthM = pathProfile.CenterlineLengthM;
                            footprintAreaM2 = pathProfile.FootprintAreaM2;
                            footprintPerimeterM = pathProfile.FootprintPerimeterM;
                            grossVolumeM3 = pathProfile.VolumeM3;
                            lateralAreaM2 = pathProfile.LateralAreaM2;
                            usedBevelJoin = pathProfile.UsedBevelJoin;
                        }
                        else
                        {
                            var footprint = new WallFootprintEngine().Build(centerline, thicknessM, miterLimit, 1e-8d);
                            polygon = footprint.Polygon;
                            centerlineLengthM = footprint.CenterlineLength;
                            footprintAreaM2 = footprint.Area;
                            footprintPerimeterM = footprint.Perimeter;
                            grossVolumeM3 = footprintAreaM2 * nativeHeightM;
                            lateralAreaM2 = footprintPerimeterM * nativeHeightM;
                            usedBevelJoin = footprint.UsedBevelJoin;
                        }

                        var profile = new Polyline();
                        Region? region = null;
                        var solid = new Solid3d();
                        try
                        {
                            for (var vertex = 0; vertex < polygon.Count; vertex++)
                            {
                                var point = polygon[vertex];
                                profile.AddVertexAt(vertex, new Point2d(
                                    CadGeometryGuard.ToDrawingUnits(document, point.X, element.Id + "/footprint X"),
                                    CadGeometryGuard.ToDrawingUnits(document, point.Y, element.Id + "/footprint Y")), 0d, 0d, 0d);
                            }
                            profile.Closed = true;
                            profile.Elevation = placement.BottomDrawingUnits;

                            var curves = new DBObjectCollection { profile };
                            var regions = Region.CreateFromCurves(curves);
                            if (regions == null || regions.Count != 1 || !(regions[0] is Region generatedRegion))
                                throw new InvalidOperationException("Không thể tạo một Region hợp lệ từ wall footprint " + element.Id + ".");
                            region = generatedRegion;

                            var height = placement.HeightDrawingUnits;
                            solid.SetDatabaseDefaults(document.Database);
                            solid.CreateExtrudedSolid(region, new Vector3d(0d, 0d, height), new SweepOptions());
                            solid.Layer = polyline.Layer;

                            var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, category);
                            pending.Add(new PendingUpdate
                            {
                                Element = element,
                                PreviousHandle = previousHandle,
                                GeneratedHandle = solid.Handle.ToString(),
                                LengthM = centerlineLengthM,
                                FootprintAreaM2 = footprintAreaM2,
                                ThicknessM = thicknessM,
                                HeightM = heightM,
                                NativeHeightM = nativeHeightM,
                                UsedBevelJoin = usedBevelJoin,
                                IsWallPierPathProfile = category == ElementCategory.WallPier,
                                WallPierMode = wallPierMode,
                                WallPierChamferM = wallPierChamferM,
                                WallPierPerimeterM = footprintPerimeterM,
                                WallPierGrossVolumeM3 = grossVolumeM3,
                                WallPierLateralAreaM2 = lateralAreaM2
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

                    foreach (var update in pending)
                    {
                        GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, category);
                        update.Element.Properties["LengthM"] = update.LengthM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["FootprintAreaM2"] = update.FootprintAreaM2.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["ThicknessM"] = update.ThicknessM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["HeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["WallJoinMode"] = update.UsedBevelJoin ? "Miter+BevelFallback" : "Miter";
                        if (update.IsWallPierPathProfile) CommitWallPierPathSnapshot(update);
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
                            "Polyline wall replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            if (pending.Count > 0)
                CadPostCommitUi.TryRegen(document, "Polyline wall native 3D");
            return pending.Count;
        }

        private static void CommitWallPierPathSnapshot(PendingUpdate update)
        {
            var properties = update.Element.Properties;
            properties["WallPierPathProfileKind"] = "OpenPolyline";
            properties["WallPierPathProfileMode"] = update.WallPierMode.ToString();
            properties["WallPierPathProfileChamferM"] = update.WallPierChamferM.ToString("R", CultureInfo.InvariantCulture);
            properties["WallPierPathProfileCenterlineLengthM"] = update.LengthM.ToString("R", CultureInfo.InvariantCulture);
            properties["WallPierPathProfileThicknessM"] = update.ThicknessM.ToString("R", CultureInfo.InvariantCulture);
            properties["WallPierPathProfileHeightM"] = update.NativeHeightM.ToString("R", CultureInfo.InvariantCulture);
            properties["WallPierPathProfileAreaM2"] = update.FootprintAreaM2.ToString("R", CultureInfo.InvariantCulture);
            properties["WallPierPathProfilePerimeterM"] = update.WallPierPerimeterM.ToString("R", CultureInfo.InvariantCulture);
            properties["WallPierPathProfileGrossVolumeM3"] = update.WallPierGrossVolumeM3.ToString("R", CultureInfo.InvariantCulture);
            properties["WallPierPathProfileLateralAreaM2"] = update.WallPierLateralAreaM2.ToString("R", CultureInfo.InvariantCulture);
            update.Element.MarkDirty(ElementDirtyFlags.Quantity);
        }

        private static WallPierProfileMode ResolveWallPierMode(ProjectElement element, ProjectFamily? family)
        {
            var raw = Text(element, family, "WallPierProfileMode", "Rectangular");
            if (Enum.TryParse(raw, true, out WallPierProfileMode mode)) return mode;
            throw new InvalidOperationException(element.Id + "/WallPierProfileMode không hợp lệ: " + raw);
        }

        private static string Text(ProjectElement element, ProjectFamily? family, string key, string fallback)
        {
            if (element.Properties.TryGetValue(key, out var own) && !string.IsNullOrWhiteSpace(own)) return own.Trim();
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return fallback;
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

        private static bool IsSupportedWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier;
    }
}
