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
        private const double BeamArcSagittaM = .002d;
        private const int MaxBeamPathSegments = 2048;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandle { get; set; } = string.Empty;
            public string GeneratedHandle { get; set; } = string.Empty;
            public ElementCategory Category { get; set; }
            public CadElementVerticalPlacement? VerticalPlacement { get; set; }
            public ObjectId SourceId { get; set; }
            public ObjectId GeneratedSolidId { get; set; }
            public IReadOnlyList<string> AppliedSlabOpeningIds { get; set; } = Array.Empty<string>();
        }

        private readonly struct BeamPathPoint
        {
            public BeamPathPoint(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }
            public double Y { get; }
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
            var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);
            var cadCommitted = false;
            SourceReconcileUndoCoordinator.PendingTransition? undoTransition = null;

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

                        if (undoTransition == null)
                        {
                            undoTransition = SourceReconcileUndoCoordinator.BeginTransition(
                                document,
                                transaction,
                                project,
                                rollback,
                                rollbackStamp);
                            // Stage the native revision before PrepareReplacement erases
                            // the retiring solid, so Undo restores the matching semantic snapshot.
                            undoTransition.StageNativeMarker();
                        }

                        var family = project.FindFamily(element.FamilyId);
                        Solid3d solid;
                        CadElementVerticalPlacement? vertical;
                        if (category == ElementCategory.Beam)
                        {
                            solid = BuildBeamPrism(document, project, entity, element, family, out vertical);
                        }
                        else if (UsesLine(category))
                        {
                            if (!(entity is Line line)) throw new InvalidOperationException(category + " element " + element.Id + " cần source LINE để dựng 3D.");
                            solid = BuildLinePrism(document, project, line, element, family, category, out vertical);
                        }
                        else if (entity is Polyline polyline && polyline.Closed)
                        {
                            solid = BuildClosedProfilePrism(document, project, polyline, polyline.Elevation, element, family, category, out vertical);
                        }
                        else if ((category == ElementCategory.Slab || category == ElementCategory.Column) && entity is Circle circle)
                        {
                            EnsureWcsXy(circle.Normal, element.Id + "/circle");
                            solid = BuildClosedProfilePrism(document, project, circle, circle.Center.Z, element, family, category, out vertical);
                        }
                        else
                        {
                            throw new InvalidOperationException(category + " element " + element.Id + " cần closed POLYLINE" +
                                (category == ElementCategory.Slab || category == ElementCategory.Column ? " hoặc CIRCLE" : string.Empty) + " để dựng 3D.");
                        }

                        try
                        {
                            solid.Layer = entity.Layer;
                            var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                            var appliedSlabOpeningIds = SlabOpeningPeerReplayService.CaptureAppliedOpeningIds(project, element, previousHandle);
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, category);
                            pending.Add(new PendingUpdate
                            {
                                Element = element,
                                PreviousHandle = previousHandle,
                                GeneratedHandle = solid.Handle.ToString(),
                                Category = category,
                                VerticalPlacement = vertical,
                                SourceId = id,
                                GeneratedSolidId = solid.ObjectId,
                                AppliedSlabOpeningIds = appliedSlabOpeningIds
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
                        if (update.VerticalPlacement != null)
                            CadElementVerticalPlacement.CommitSnapshot(update.Element, "GeneratedSolid", update.VerticalPlacement);
                    }

                    foreach (var update in pending.Where(x => x.Category == ElementCategory.Slab && x.AppliedSlabOpeningIds.Count > 0))
                    {
                        var source = transaction.GetObject(update.SourceId, OpenMode.ForRead, false) as Polyline;
                        if (source == null || source.IsErased || !source.Closed)
                            throw new InvalidOperationException("Rebuilt Slab with applied slabOpen peers must retain one live closed POLYLINE source: " + update.Element.Id);
                        var generated = transaction.GetObject(update.GeneratedSolidId, OpenMode.ForWrite, false) as Solid3d;
                        if (generated == null || generated.IsErased)
                            throw new InvalidOperationException("Rebuilt Slab Solid3d disappeared before slabOpen peer replay: " + update.Element.Id);

                        SlabOpeningPeerReplayService.ReplayAppliedOpenings(
                            document,
                            transaction,
                            project,
                            update.Element,
                            source,
                            generated,
                            update.PreviousHandle,
                            update.AppliedSlabOpeningIds);
                    }

                    if (pending.Count > 0)
                    {
                        project.Touch();
                        var afterSnapshot = ProjectStateSnapshot.Capture(project);
                        if (undoTransition == null)
                            throw new InvalidOperationException("Structural semantic Undo transition was not initialized for pending generated geometry.");
                        undoTransition.StageAfter(project, afterSnapshot);
                    }
                    transaction.Commit();
                    undoTransition?.ConfirmCommitted();
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
            finally
            {
                undoTransition?.Dispose();
            }

            if (pending.Count > 0)
                CadPostCommitUi.TryRegen(document, "Structural native 3D");
            return pending.Count;
        }

        private static bool UsesLine(ElementCategory category) =>
            category == ElementCategory.StructuralWall || category == ElementCategory.Railing;

        private static Solid3d BuildBeamPrism(
            Document document,
            ProjectState project,
            Entity entity,
            ProjectElement element,
            ProjectFamily? family,
            out CadElementVerticalPlacement? vertical)
        {
            if (entity is Line line)
                return BuildLinePrism(document, project, line, element, family, ElementCategory.Beam, out vertical);

            var points = ReadBeamPath(document, entity, element.Id, out var sourceZ, out var closed);
            var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "WidthM", .3d), element.Id + "/3D width");
            vertical = CadElementVerticalPlacement.Resolve(document, project, element, family, sourceZ, "HeightM", .5d);
            var width = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, widthM, element.Id + "/3D width"), element.Id + "/3D width drawing units");
            var height = vertical.HeightDrawing;
            var overlap = width / 2d;
            Solid3d? result = null;
            try
            {
                var segmentCount = points.Count - 1;
                if (segmentCount <= 0 || segmentCount > MaxBeamPathSegments)
                    throw new InvalidOperationException("Beam path segment count không hợp lệ: " + element.Id);

                for (var index = 0; index < segmentCount; index++)
                {
                    var start = points[index];
                    var end = points[index + 1];
                    var dx = CadGeometryGuard.Subtract(end.X, start.X, element.Id + "/beam path dx");
                    var dy = CadGeometryGuard.Subtract(end.Y, start.Y, element.Id + "/beam path dy");
                    var length = CadGeometryGuard.Hypot(dx, dy, element.Id + "/beam path segment");
                    if (length <= 1e-6d) throw new InvalidOperationException("Beam path chứa segment quá ngắn: " + element.Id);
                    var ux = dx / length;
                    var uy = dy / length;
                    var before = closed || index > 0 ? overlap : 0d;
                    var after = closed || index + 1 < segmentCount ? overlap : 0d;
                    var extendedLength = CadGeometryGuard.Finite(length + before + after, element.Id + "/beam extended length");
                    var midpointShift = (after - before) / 2d;
                    var midX = CadGeometryGuard.Finite((start.X + end.X) / 2d + ux * midpointShift, element.Id + "/beam mid X");
                    var midY = CadGeometryGuard.Finite((start.Y + end.Y) / 2d + uy * midpointShift, element.Id + "/beam mid Y");
                    var angle = CadGeometryGuard.Finite(Math.Atan2(dy, dx), element.Id + "/beam angle");

                    var part = new Solid3d();
                    try
                    {
                        part.SetDatabaseDefaults(document.Database);
                        part.CreateBox(extendedLength, width, height);
                        part.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                        part.TransformBy(Matrix3d.Displacement(new Vector3d(midX, midY, vertical.CenterDrawing)));
                        if (result == null)
                        {
                            result = part;
                            part = null!;
                        }
                        else
                        {
                            result.BooleanOperation(BooleanOperationType.BoolUnite, part);
                        }
                    }
                    finally { part?.Dispose(); }
                }

                if (result == null) throw new InvalidOperationException("Không tạo được Beam solid từ curved path: " + element.Id);
                var completed = result;
                result = null;
                return completed;
            }
            finally { result?.Dispose(); }
        }

        private static IReadOnlyList<BeamPathPoint> ReadBeamPath(
            Document document,
            Entity entity,
            string label,
            out double sourceZ,
            out bool closed)
        {
            var maximumSagitta = CadGeometryGuard.Positive(
                CadGeometryGuard.ToDrawingUnits(document, BeamArcSagittaM, label + "/beam arc sagitta"),
                label + "/beam arc sagitta drawing units");

            if (entity is Arc arc)
            {
                EnsureWcsXy(arc.Normal, label + "/arc");
                sourceZ = CadGeometryGuard.Finite(arc.Center.Z, label + "/arc Z");
                closed = false;
                var sweep = NormalizeSweep(arc.EndAngle - arc.StartAngle, label + "/arc sweep");
                return SampleCircularPath(arc.Center.X, arc.Center.Y, arc.Radius, arc.StartAngle, sweep, maximumSagitta, false, label);
            }

            if (entity is Circle circle)
            {
                EnsureWcsXy(circle.Normal, label + "/circle");
                sourceZ = CadGeometryGuard.Finite(circle.Center.Z, label + "/circle Z");
                closed = true;
                return SampleCircularPath(circle.Center.X, circle.Center.Y, circle.Radius, 0d, Math.PI * 2d, maximumSagitta, true, label);
            }

            if (entity is Polyline polyline && !polyline.Closed)
            {
                sourceZ = CadGeometryGuard.Finite(polyline.Elevation, label + "/polyline elevation");
                closed = false;
                var path = CadPolylinePathReader.ReadOpenWcsXy(document, polyline, BeamArcSagittaM, label + "/beam path");
                var result = new List<BeamPathPoint>(path.Count);
                foreach (var point in path)
                {
                    result.Add(new BeamPathPoint(
                        CadGeometryGuard.ToDrawingUnits(document, point.X, label + "/beam path X"),
                        CadGeometryGuard.ToDrawingUnits(document, point.Y, label + "/beam path Y")));
                }
                return result.AsReadOnly();
            }

            throw new InvalidOperationException("Beam element " + label + " cần LINE, ARC, CIRCLE hoặc open POLYLINE để dựng 3D.");
        }

        private static IReadOnlyList<BeamPathPoint> SampleCircularPath(
            double centerX,
            double centerY,
            double radius,
            double startAngle,
            double sweep,
            double maximumSagitta,
            bool closed,
            string label)
        {
            centerX = CadGeometryGuard.Finite(centerX, label + "/center X");
            centerY = CadGeometryGuard.Finite(centerY, label + "/center Y");
            radius = CadGeometryGuard.Positive(radius, label + "/radius");
            startAngle = CadGeometryGuard.Finite(startAngle, label + "/start angle");
            sweep = CadGeometryGuard.Positive(sweep, label + "/sweep");
            maximumSagitta = CadGeometryGuard.Positive(maximumSagitta, label + "/maximum sagitta");

            var cosine = 1d - Math.Min(maximumSagitta, radius) / radius;
            cosine = Math.Max(-1d, Math.Min(1d, cosine));
            var maximumAngle = 2d * Math.Acos(cosine);
            if (double.IsNaN(maximumAngle) || double.IsInfinity(maximumAngle) || maximumAngle <= 1e-6d)
                maximumAngle = Math.PI / 180d;
            var minimumSegments = closed ? 12 : 1;
            var segmentCount = Math.Max(minimumSegments, (int)Math.Ceiling(sweep / maximumAngle));
            if (segmentCount > MaxBeamPathSegments)
                throw new InvalidOperationException(label + " curved Beam cần " + segmentCount + " path segments, vượt giới hạn " + MaxBeamPathSegments + ".");

            var result = new List<BeamPathPoint>(segmentCount + 1);
            for (var index = 0; index <= segmentCount; index++)
            {
                var angle = startAngle + sweep * index / segmentCount;
                result.Add(new BeamPathPoint(
                    CadGeometryGuard.Finite(centerX + radius * Math.Cos(angle), label + "/sample X"),
                    CadGeometryGuard.Finite(centerY + radius * Math.Sin(angle), label + "/sample Y")));
            }
            return result.AsReadOnly();
        }

        private static double NormalizeSweep(double sweep, string label)
        {
            sweep = CadGeometryGuard.Finite(sweep, label);
            var full = Math.PI * 2d;
            while (sweep <= 0d) sweep += full;
            if (sweep > full + 1e-9d) throw new InvalidOperationException(label + " vượt quá một vòng tròn.");
            return sweep;
        }

        private static void EnsureWcsXy(Vector3d normal, string label)
        {
            if (Math.Abs(normal.X) > 1e-9d || Math.Abs(normal.Y) > 1e-9d || Math.Abs(normal.Z - 1d) > 1e-9d)
                throw new InvalidOperationException(label + " hiện phải nằm trong WCS XY với +Z normal; profile/path nghiêng bị fail closed để tránh dựng 3D sai.");
        }

        private static Solid3d BuildLinePrism(
            Document document,
            ProjectState project,
            Line line,
            ProjectElement element,
            ProjectFamily? family,
            ElementCategory category,
            out CadElementVerticalPlacement? vertical)
        {
            double widthM;
            double legacyHeightFallback;
            switch (category)
            {
                case ElementCategory.Beam:
                    widthM = CadGeometryGuard.Number(element, family, "WidthM", .3d);
                    legacyHeightFallback = .5d;
                    break;
                case ElementCategory.StructuralWall:
                    widthM = CadGeometryGuard.Number(element, family, "ThicknessM", .2d);
                    legacyHeightFallback = 3.6d;
                    break;
                case ElementCategory.Railing:
                    widthM = CadGeometryGuard.Number(element, family, "ProfileWidthM", .05d);
                    legacyHeightFallback = 1.1d;
                    break;
                default:
                    throw new InvalidOperationException("Category không hỗ trợ LINE prism: " + category);
            }
            widthM = CadGeometryGuard.Positive(widthM, element.Id + "/3D width");
            vertical = CadElementVerticalPlacement.Resolve(
                document, project, element, family, line.StartPoint.Z, "HeightM", legacyHeightFallback);
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
            var height = vertical.HeightDrawing;
            var angle = CadGeometryGuard.Finite(Math.Atan2(dy, dx), element.Id + "/angle");
            var midX = CadGeometryGuard.Midpoint(line.StartPoint.X, line.EndPoint.X, element.Id + "/mid X");
            var midY = CadGeometryGuard.Midpoint(line.StartPoint.Y, line.EndPoint.Y, element.Id + "/mid Y");
            var midZ = vertical.CenterDrawing;
            var mid = new Point3d(midX, midY, midZ);

            var solid = new Solid3d();
            solid.SetDatabaseDefaults(document.Database);
            solid.CreateBox(length, width, height);
            solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
            solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));
            return solid;
        }

        private static Solid3d BuildClosedProfilePrism(
            Document document,
            ProjectState project,
            Entity profile,
            double sourceElevation,
            ProjectElement element,
            ProjectFamily? family,
            ElementCategory category,
            out CadElementVerticalPlacement? vertical)
        {
            vertical = null;
            var direction = 1d;
            string heightKey;
            double heightFallback;
            switch (category)
            {
                case ElementCategory.Slab: heightKey = "ThicknessM"; heightFallback = .12d; break;
                case ElementCategory.Foundation: heightKey = "ThicknessM"; heightFallback = .5d; break;
                case ElementCategory.Stair: heightKey = "ThicknessM"; heightFallback = .15d; break;
                case ElementCategory.Earthwork: heightKey = "DepthM"; heightFallback = 1d; direction = -1d; break;
                case ElementCategory.Column: heightKey = "HeightM"; heightFallback = 3.6d; break;
                default: throw new InvalidOperationException("Category không hỗ trợ closed profile prism: " + category);
            }
            sourceElevation = CadGeometryGuard.Finite(sourceElevation, element.Id + "/profile elevation");
            double heightMagnitude;
            double offset;
            if (category == ElementCategory.Earthwork)
            {
                var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, heightKey, heightFallback), element.Id + "/extrusion height");
                var offsetM = CadGeometryGuard.Number(element, family, "TopOffsetM", 0d);
                heightMagnitude = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, heightM, element.Id + "/extrusion height"), element.Id + "/extrusion drawing height");
                offset = CadGeometryGuard.ToDrawingUnits(document, offsetM, element.Id + "/TopOffsetM");
            }
            else
            {
                vertical = CadElementVerticalPlacement.Resolve(
                    document, project, element, family, sourceElevation, heightKey, heightFallback);
                heightMagnitude = vertical.HeightDrawing;
                offset = CadGeometryGuard.Subtract(vertical.BottomDrawing, sourceElevation, element.Id + "/resolved source displacement Z");
            }
            var height = CadGeometryGuard.Finite(heightMagnitude * direction, element.Id + "/signed extrusion height");

            var solid = new Solid3d();
            solid.SetDatabaseDefaults(document.Database);
            solid.CreateExtrudedSolid(profile, new Vector3d(0d, 0d, height), new SweepOptions());
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
