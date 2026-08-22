using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class ColumnTieSolidBuilder
    {
        private const string HandlesKey = "GeneratedTieRebarHandles";
        private const int MaxTiesPerElement = 800;
        private const int MaxTiesPerBatch = 2000;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public double DiameterMm { get; set; }
            public double ActualSpacingM { get; set; }
            public double CoverM { get; set; }
        }

        public static int BuildSelected(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var ids = selection.Value.GetObjectIds();
            if (ids.Length == 0) return 0;

            var ownership = GeneratedTieRebarOwnershipGuard.Build(project);
            var pending = new List<PendingUpdate>();
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var totalTies = 0;
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
                        var polyline = transaction.GetObject(id, OpenMode.ForRead, false) as Polyline;
                        if (polyline == null || polyline.IsErased) continue;
                        ValidateRectangle(polyline);
                        var handle = polyline.Handle.ToString();
                        var matches = project.Elements
                            .Where(x => x.Category == ElementCategory.Column && x.SourceHandles.Any(h => string.Equals(h, handle, StringComparison.OrdinalIgnoreCase)))
                            .Take(2).ToList();
                        if (matches.Count == 0) continue;
                        if (matches.Count > 1) throw new InvalidOperationException("Column source " + handle + " đang thuộc nhiều QS3D element.");
                        var element = matches[0];
                        if (!processed.Add(element.Id)) throw new InvalidOperationException("Column element " + element.Id + " có nhiều selected source. Tách semantic ownership trước khi tạo tie 3D.");
                        var family = project.FindFamily(element.FamilyId);

                        var geometry = RectangleGeometry(polyline, element.Id);
                        var widthM = CadGeometryGuard.ToMeters(document, geometry.Width, element.Id + "/tie width");
                        var depthM = CadGeometryGuard.ToMeters(document, geometry.Depth, element.Id + "/tie depth");
                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
                        var coverM = CadGeometryGuard.Number(element, family, "RebarCoverM", 0.04d);
                        if (coverM < 0d) throw new InvalidOperationException(element.Id + "/RebarCoverM phải >= 0.");
                        var diameterMm = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "RebarTieDiameterMm", 8d), element.Id + "/RebarTieDiameterMm");
                        var spacingMm = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "RebarTieSpacingMm", 150d), element.Id + "/RebarTieSpacingMm");
                        var bottomClearanceM = CadGeometryGuard.Number(element, family, "RebarTieBottomClearanceM", 0d);
                        var topClearanceM = CadGeometryGuard.Number(element, family, "RebarTieTopClearanceM", 0d);
                        if (bottomClearanceM < 0d) throw new InvalidOperationException(element.Id + "/RebarTieBottomClearanceM phải >= 0.");
                        if (topClearanceM < 0d) throw new InvalidOperationException(element.Id + "/RebarTieTopClearanceM phải >= 0.");
                        var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);

                        var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
                        {
                            WidthM = widthM,
                            DepthM = depthM,
                            HeightM = heightM,
                            CoverM = coverM,
                            DiameterMm = diameterMm,
                            SpacingMm = spacingMm,
                            BottomClearanceM = bottomClearanceM,
                            TopClearanceM = topClearanceM
                        });
                        if (layout.ElevationsM.Count > MaxTiesPerElement) throw new InvalidOperationException(element.Id + " vượt giới hạn " + MaxTiesPerElement + " tie 3D/element.");
                        if (totalTies > MaxTiesPerBatch - layout.ElevationsM.Count) throw new InvalidOperationException("Tie 3D batch vượt giới hạn " + MaxTiesPerBatch + " solid.");

                        ErasePrevious(document, transaction, project, element, ownership);
                        var update = new PendingUpdate { Element = element, DiameterMm = diameterMm, ActualSpacingM = layout.ActualSpacingM, CoverM = coverM };
                        var radius = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, diameterMm / 2000d, element.Id + "/tie radius"), element.Id + "/tie radius drawing units");
                        var bottomOffset = CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM");
                        foreach (var elevationM in layout.ElevationsM)
                        {
                            var elevation = CadGeometryGuard.ToDrawingUnits(document, elevationM, element.Id + "/tie elevation");
                            var localZ = CadGeometryGuard.Add(bottomOffset, elevation, element.Id + "/tie local Z");
                            var z = CadGeometryGuard.Add(polyline.Elevation, localZ, element.Id + "/tie Z");
                            var tie = BuildTie(document, geometry, layout, z, radius, element.Id);
                            try
                            {
                                tie.Layer = polyline.Layer;
                                modelSpace.AppendEntity(tie);
                                transaction.AddNewlyCreatedDBObject(tie, true);
                                GeneratedRebarNativeOwnershipService.MarkGenerated(document, transaction, tie, project, element, HandlesKey);
                                update.Handles.Add(tie.Handle.ToString());
                                tie = null!;
                            }
                            finally { tie?.Dispose(); }
                        }
                        pending.Add(update);
                        totalTies = checked(totalTies + update.Handles.Count);
                    }

                    foreach (var update in pending) CommitSemanticUpdate(project, update);
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
                            "Column tie replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return totalTies;
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingUpdate update)
        {
            update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
            update.Element.Properties["GeneratedTieRebarCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedTieRebarDiameterMm"] = update.DiameterMm.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedTieRebarActualSpacingM"] = update.ActualSpacingM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedTieRebarCoverM"] = update.CoverM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedTieRebarMode"] = "ColumnRectangularTies";
            update.Element.ClearGeneratedTieRebarStale();
            AuditTrail.ForProject(project).Record("geometry.rebar.column.tie", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " ties");
        }

        private sealed class RectGeometry
        {
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public double Ux { get; set; }
            public double Uy { get; set; }
            public double Vx { get; set; }
            public double Vy { get; set; }
            public double Width { get; set; }
            public double Depth { get; set; }
        }

        private static RectGeometry RectangleGeometry(Polyline polyline, string label)
        {
            var p0 = polyline.GetPoint2dAt(0); var p1 = polyline.GetPoint2dAt(1); var p2 = polyline.GetPoint2dAt(2); var p3 = polyline.GetPoint2dAt(3);
            var e1x = CadGeometryGuard.Subtract(p1.X, p0.X, label + "/edge1 X");
            var e1y = CadGeometryGuard.Subtract(p1.Y, p0.Y, label + "/edge1 Y");
            var e2x = CadGeometryGuard.Subtract(p2.X, p1.X, label + "/edge2 X");
            var e2y = CadGeometryGuard.Subtract(p2.Y, p1.Y, label + "/edge2 Y");
            var width = CadGeometryGuard.Hypot(e1x, e1y, label + "/width");
            var depth = CadGeometryGuard.Hypot(e2x, e2y, label + "/depth");
            if (width <= 1e-8d || depth <= 1e-8d) throw new InvalidOperationException("Column footprint bị suy biến: " + label);
            var ux = e1x / width; var uy = e1y / width; var vx = e2x / depth; var vy = e2y / depth;
            var dot = CadGeometryGuard.Add(
                CadGeometryGuard.Multiply(ux, vx, label + "/rectangle dot X"),
                CadGeometryGuard.Multiply(uy, vy, label + "/rectangle dot Y"),
                label + "/rectangle orthogonality");
            if (Math.Abs(dot) > 1e-6d) throw new InvalidOperationException("Column footprint không vuông góc: " + label);
            var expectedP2X = CadGeometryGuard.Add(CadGeometryGuard.Add(p0.X, e1x, label + "/expected P2 X"), e2x, label + "/expected P2 X");
            var expectedP2Y = CadGeometryGuard.Add(CadGeometryGuard.Add(p0.Y, e1y, label + "/expected P2 Y"), e2y, label + "/expected P2 Y");
            var expectedP3X = CadGeometryGuard.Add(p0.X, e2x, label + "/expected P3 X");
            var expectedP3Y = CadGeometryGuard.Add(p0.Y, e2y, label + "/expected P3 Y");
            var tolerance = CadGeometryGuard.Add(CadGeometryGuard.Multiply(Math.Max(width, depth), 1e-6d, label + "/rectangle tolerance scale"), 1e-8d, label + "/rectangle tolerance");
            if (Distance(p2.X, p2.Y, expectedP2X, expectedP2Y, label + "/P2 closure") > tolerance || Distance(p3.X, p3.Y, expectedP3X, expectedP3Y, label + "/P3 closure") > tolerance)
                throw new InvalidOperationException("Column footprint phải là rectangle kín theo thứ tự vertex: " + label);
            return new RectGeometry
            {
                CenterX = CadGeometryGuard.Midpoint(p0.X, p2.X, label + "/center X"),
                CenterY = CadGeometryGuard.Midpoint(p0.Y, p2.Y, label + "/center Y"),
                Ux = ux, Uy = uy, Vx = vx, Vy = vy, Width = width, Depth = depth
            };
        }

        private static Solid3d BuildTie(Document document, RectGeometry geometry, ColumnTieLayout layout, double z, double radius, string label)
        {
            Solid3d? result = null;
            try
            {
                z = CadGeometryGuard.Finite(z, label + "/tie Z");
                radius = CadGeometryGuard.Positive(radius, label + "/tie radius");
                for (var i = 1; i < layout.ClosedPath.Count; i++)
                {
                    var a = layout.ClosedPath[i - 1]; var b = layout.ClosedPath[i];
                    var ax = CadGeometryGuard.ToDrawingUnits(document, a.X, label + "/tie X");
                    var ay = CadGeometryGuard.ToDrawingUnits(document, a.Y, label + "/tie Y");
                    var bx = CadGeometryGuard.ToDrawingUnits(document, b.X, label + "/tie X");
                    var by = CadGeometryGuard.ToDrawingUnits(document, b.Y, label + "/tie Y");
                    var start = World(geometry, ax, ay, z, label + "/tie start " + i);
                    var end = World(geometry, bx, by, z, label + "/tie end " + i);
                    var dx = CadGeometryGuard.Subtract(end.X, start.X, label + "/tie direction X");
                    var dy = CadGeometryGuard.Subtract(end.Y, start.Y, label + "/tie direction Y");
                    var length = CadGeometryGuard.Hypot(dx, dy, label + "/tie segment length");
                    if (length <= 1e-9d) throw new InvalidOperationException("Tie path contains a degenerate segment: " + label);
                    var unit = new Vector3d(dx / length, dy / length, 0d);
                    var overlap = Math.Min(CadGeometryGuard.Multiply(radius, 0.75d, label + "/tie overlap radius"), CadGeometryGuard.Multiply(length, 0.1d, label + "/tie overlap length"));
                    var extendedStart = new Point3d(
                        CadGeometryGuard.Subtract(start.X, CadGeometryGuard.Multiply(unit.X, overlap, label + "/tie overlap X"), label + "/tie extended X"),
                        CadGeometryGuard.Subtract(start.Y, CadGeometryGuard.Multiply(unit.Y, overlap, label + "/tie overlap Y"), label + "/tie extended Y"),
                        z);
                    var extendedLength = CadGeometryGuard.Add(length, CadGeometryGuard.Multiply(overlap, 2d, label + "/tie double overlap"), label + "/tie extended length");
                    var part = Cylinder(document, extendedStart, unit, extendedLength, radius, label + "/tie segment " + i);
                    if (result == null) { result = part; continue; }
                    try { result.BooleanOperation(BooleanOperationType.BoolUnite, part); }
                    finally { part.Dispose(); }
                }
                if (result == null) throw new InvalidOperationException("Không tạo được rectangular tie solid: " + label);
                var completed = result; result = null; return completed;
            }
            finally { result?.Dispose(); }
        }

        private static Point3d World(RectGeometry geometry, double localX, double localY, double z, string label)
        {
            var xOffset = CadGeometryGuard.Add(
                CadGeometryGuard.Multiply(geometry.Ux, localX, label + "/Ux"),
                CadGeometryGuard.Multiply(geometry.Vx, localY, label + "/Vx"),
                label + "/X offset");
            var yOffset = CadGeometryGuard.Add(
                CadGeometryGuard.Multiply(geometry.Uy, localX, label + "/Uy"),
                CadGeometryGuard.Multiply(geometry.Vy, localY, label + "/Vy"),
                label + "/Y offset");
            return new Point3d(
                CadGeometryGuard.Add(geometry.CenterX, xOffset, label + "/X"),
                CadGeometryGuard.Add(geometry.CenterY, yOffset, label + "/Y"),
                CadGeometryGuard.Finite(z, label + "/Z"));
        }

        private static Solid3d Cylinder(Document document, Point3d start, Vector3d unit, double length, double radius, string label)
        {
            length = CadGeometryGuard.Positive(length, label + "/length");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            var magnitude = CadGeometryGuard.Hypot3(unit.X, unit.Y, unit.Z, label + "/axis magnitude");
            if (magnitude <= 1e-12d) throw new InvalidOperationException("Tie axis không hợp lệ: " + label);
            var direction = new Vector3d(unit.X / magnitude, unit.Y / magnitude, unit.Z / magnitude);
            var startX = CadGeometryGuard.Finite(start.X, label + "/start X");
            var startY = CadGeometryGuard.Finite(start.Y, label + "/start Y");
            var startZ = CadGeometryGuard.Finite(start.Z, label + "/start Z");
            var solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateFrustum(length, radius, radius, radius);
                var dot = Math.Max(-1d, Math.Min(1d, direction.Z));
                var angle = Math.Acos(dot);
                var axis = Vector3d.ZAxis.CrossProduct(direction);
                if (axis.Length > 1e-12d) solid.TransformBy(Matrix3d.Rotation(angle, axis, Point3d.Origin));
                else if (direction.Z < 0d) solid.TransformBy(Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin));
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(startX, startY, startZ)));
                var completed = solid; solid = null!; return completed;
            }
            catch (Exception ex) { throw new InvalidOperationException("Không tạo được tie cylinder " + label + ": " + ex.Message, ex); }
            finally { solid?.Dispose(); }
        }

        private static void ErasePrevious(Document document, Transaction transaction, ProjectState project, ProjectElement element, GeneratedTieRebarOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ownership.EnsureTieOwned(handle, element);
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count == 0) continue;
                if (ids.Count > 1) throw new InvalidOperationException("Generated tie handle " + handle + " resolves to multiple CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                var solid = entity as Solid3d;
                if (solid == null) throw new InvalidOperationException("Generated tie handle " + handle + " is live but not Solid3d. Refusing destructive erase.");
                GeneratedRebarNativeOwnershipService.RequireMatchingOwnership(solid, project, element, HandlesKey, "erase generated column tie " + handle);
                solid.Erase();
            }
        }

        private static void ValidateRectangle(Polyline polyline)
        {
            if (!polyline.Closed || polyline.NumberOfVertices != 4) throw new InvalidOperationException("QS3DREBARTIES3D yêu cầu closed 4-vertex rectangle POLYLINE.");
            var normal = polyline.Normal;
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(Math.Abs(normal.Z) - 1d) > 1e-9d)
                throw new InvalidOperationException("QS3DREBARTIES3D yêu cầu footprint nằm mặt phẳng XY.");
            for (var i = 0; i < 4; i++) if (Math.Abs(polyline.GetBulgeAt(i)) > 1e-12d) throw new InvalidOperationException("QS3DREBARTIES3D chưa hỗ trợ rectangle có bulge.");
            CadGeometryGuard.Finite(polyline.Elevation, "Column tie footprint elevation");
        }

        private static double Distance(double x1, double y1, double x2, double y2, string label)
        {
            var dx = CadGeometryGuard.Subtract(x2, x1, label + "/dx");
            var dy = CadGeometryGuard.Subtract(y2, y1, label + "/dy");
            return CadGeometryGuard.Hypot(dx, dy, label);
        }
    }
}