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
    internal static class WallPierProfileSolidBuilder
    {
        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandle { get; set; } = string.Empty;
            public string GeneratedHandle { get; set; } = string.Empty;
            public double LengthM { get; set; }
            public double ThicknessM { get; set; }
            public double HeightM { get; set; }
            public double FootprintAreaM2 { get; set; }
            public WallPierProfileMode Mode { get; set; }
            public double ChamferM { get; set; }
        }

        public static int BuildSelectedLinePiers(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var ids = selection.Value.GetObjectIds();
            if (ids.Length == 0) return 0;

            var pending = new List<PendingUpdate>();
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                        var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                        if (line == null || line.IsErased) continue;
                        var handle = line.Handle.ToString();
                        var matches = project.Elements
                            .Where(x => x.Category == ElementCategory.WallPier && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                            .Take(2)
                            .ToList();
                        if (matches.Count == 0) continue;
                        if (matches.Count > 1) throw new InvalidOperationException("WallPier source " + handle + " đang thuộc nhiều semantic element.");
                        var element = matches[0];
                        if (!processed.Add(element.Id)) throw new InvalidOperationException("WallPier " + element.Id + " có nhiều source LINE đang được chọn. Tách/capture mỗi source thành element riêng trước khi Vẽ 3D.");

                        var family = project.FindFamily(element.FamilyId);
                        var dx = CadGeometryGuard.Finite(line.EndPoint.X - line.StartPoint.X, element.Id + "/axis dx");
                        var dy = CadGeometryGuard.Finite(line.EndPoint.Y - line.StartPoint.Y, element.Id + "/axis dy");
                        var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, element.Id + "/axis length");
                        if (lengthDrawing <= 1e-8d) throw new InvalidOperationException("WallPier source LINE quá ngắn: " + element.Id);
                        var zDeltaM = Math.Abs(CadGeometryGuard.ToMeters(document, CadGeometryGuard.Finite(line.EndPoint.Z - line.StartPoint.Z, element.Id + "/axis dz"), element.Id + "/axis dz"));
                        if (zDeltaM > 1e-6d) throw new InvalidOperationException("WallPier source LINE phải nằm trên mặt phẳng ngang: " + element.Id);

                        var lengthM = CadGeometryGuard.Positive(CadGeometryGuard.ToMeters(document, lengthDrawing, element.Id + "/LengthM"), element.Id + "/LengthM");
                        var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", 0.2d), element.Id + "/ThicknessM");
                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
                        var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                        var placement = CadVerticalPlacementResolver.Resolve(
                            document,
                            project,
                            element,
                            line.StartPoint.Z,
                            heightM,
                            bottomOffsetM);
                        var mode = ResolveMode(element, family);
                        var chamferM = mode == WallPierProfileMode.Chamfered ? CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "WallPierChamferM", 0.02d), element.Id + "/WallPierChamferM") : 0d;
                        var profilePlan = WallPierProfilePlanner.Plan(new WallPierProfileInput
                        {
                            Mode = mode,
                            WidthM = lengthM,
                            DepthM = thicknessM,
                            HeightM = placement.HeightM,
                            ChamferM = chamferM
                        });

                        var thicknessDrawing = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, thicknessM, element.Id + "/ThicknessM"), element.Id + "/thickness drawing units");
                        var chamferDrawing = mode == WallPierProfileMode.Chamfered
                            ? CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, chamferM, element.Id + "/WallPierChamferM"), element.Id + "/chamfer drawing units")
                            : 0d;
                        var heightDrawing = placement.HeightDrawingUnits;
                        var ux = dx / lengthDrawing;
                        var uy = dy / lengthDrawing;
                        var vx = -uy;
                        var vy = ux;
                        var centerX = CadGeometryGuard.Midpoint(line.StartPoint.X, line.EndPoint.X, element.Id + "/center X");
                        var centerY = CadGeometryGuard.Midpoint(line.StartPoint.Y, line.EndPoint.Y, element.Id + "/center Y");
                        var local = LocalProfile(lengthDrawing, thicknessDrawing, chamferDrawing, mode);

                        var polyline = new Polyline();
                        Region? region = null;
                        var solid = new Solid3d();
                        try
                        {
                            for (var index = 0; index < local.Count; index++)
                            {
                                var point = local[index];
                                var x = CadGeometryGuard.Add(centerX, ux * point.X + vx * point.Y, element.Id + "/profile X");
                                var y = CadGeometryGuard.Add(centerY, uy * point.X + vy * point.Y, element.Id + "/profile Y");
                                polyline.AddVertexAt(index, new Point2d(x, y), 0d, 0d, 0d);
                            }
                            polyline.Closed = true;
                            polyline.Elevation = placement.BottomDrawingUnits;
                            var regions = Region.CreateFromCurves(new DBObjectCollection { polyline });
                            if (regions == null || regions.Count != 1 || !(regions[0] is Region generatedRegion))
                                throw new InvalidOperationException("Không thể tạo Region hợp lệ cho WallPier profile " + element.Id + ".");
                            region = generatedRegion;

                            solid.SetDatabaseDefaults(document.Database);
                            solid.CreateExtrudedSolid(region, new Vector3d(0d, 0d, heightDrawing), new SweepOptions());
                            solid.Layer = line.Layer;
                            var previous = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, ElementCategory.WallPier);
                            pending.Add(new PendingUpdate
                            {
                                Element = element,
                                PreviousHandle = previous,
                                GeneratedHandle = solid.Handle.ToString(),
                                LengthM = lengthM,
                                ThicknessM = thicknessM,
                                HeightM = heightM,
                                FootprintAreaM2 = profilePlan.CrossSectionAreaM2,
                                Mode = mode,
                                ChamferM = chamferM
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
                            polyline.Dispose();
                        }
                    }

                    foreach (var update in pending)
                    {
                        GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, ElementCategory.WallPier);
                        ClearPathProfileSnapshot(update.Element);
                        update.Element.Properties["LengthM"] = update.LengthM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["ThicknessM"] = update.ThicknessM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["HeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["FootprintAreaM2"] = update.FootprintAreaM2.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["WallPierProfileMode"] = update.Mode.ToString();
                        if (update.Mode == WallPierProfileMode.Chamfered)
                            update.Element.Properties["WallPierChamferM"] = update.ChamferM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.MarkDirty(ElementDirtyFlags.Quantity);
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
                            "WallPier profile replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            if (pending.Count > 0)
                CadPostCommitUi.TryRegen(document, "WallPier profile native 3D");
            return pending.Count;
        }

        private static void ClearPathProfileSnapshot(ProjectElement element)
        {
            var keys = element.Properties.Keys
                .Where(x => x.StartsWith("WallPierPathProfile", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (var key in keys) element.Properties.Remove(key);
        }

        private static WallPierProfileMode ResolveMode(ProjectElement element, ProjectFamily? family)
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

        private static IReadOnlyList<Point2d> LocalProfile(double width, double depth, double chamfer, WallPierProfileMode mode)
        {
            var halfWidth = width / 2d;
            var halfDepth = depth / 2d;
            if (mode == WallPierProfileMode.Rectangular)
            {
                return new[]
                {
                    new Point2d(-halfWidth, -halfDepth),
                    new Point2d(halfWidth, -halfDepth),
                    new Point2d(halfWidth, halfDepth),
                    new Point2d(-halfWidth, halfDepth)
                };
            }
            return new[]
            {
                new Point2d(-halfWidth + chamfer, -halfDepth),
                new Point2d(halfWidth - chamfer, -halfDepth),
                new Point2d(halfWidth, -halfDepth + chamfer),
                new Point2d(halfWidth, halfDepth - chamfer),
                new Point2d(halfWidth - chamfer, halfDepth),
                new Point2d(-halfWidth + chamfer, halfDepth),
                new Point2d(-halfWidth, halfDepth - chamfer),
                new Point2d(-halfWidth, -halfDepth + chamfer)
            };
        }
    }
}
