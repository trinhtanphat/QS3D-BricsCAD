using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CurtainWallPathFrameSolidBuilder
    {
        private const string HandlesKey = "GeneratedCurtainFrameHandles";
        private const string Mode = "PathFrameOverlay";
        private const string OpeningAwareMode = "PathFrameOverlay.OpeningAware";
        private const int MaxFramesPerElement = 4096;
        private const int MaxFramesPerBatch = 8192;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public int Columns { get; set; }
            public int Rows { get; set; }
            public int BaseFrameCount { get; set; }
            public int OpeningCount { get; set; }
            public int PathSegmentCount { get; set; }
            public int MappedFrameCount { get; set; }
            public double FrameDepthM { get; set; }
            public double SourceLengthM { get; set; }
            public double HeightM { get; set; }
            public string ConfigFingerprint { get; set; } = string.Empty;
        }

        public static CurtainFrameBuildResult BuildSelectedOpenPolylines(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null)
            {
                selection = document.Editor.GetSelection();
                if (selection.Status != PromptStatus.OK || selection.Value == null) return new CurtainFrameBuildResult();
                document.Editor.SetImpliedSelection(selection.Value.GetObjectIds());
            }
            var ids = selection.Value.GetObjectIds();
            if (ids.Length == 0) return new CurtainFrameBuildResult();

            var ownership = GeneratedCurtainFrameOwnershipGuard.Build(project);
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new List<PendingUpdate>();
            var batchFrames = 0;
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
                        var sourceHandle = polyline.Handle.ToString();
                        var matches = project.Elements
                            .Where(x => x.Category == ElementCategory.GlassWall && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)))
                            .Take(2)
                            .ToList();
                        if (matches.Count == 0) continue;
                        if (matches.Count > 1) throw new InvalidOperationException("GlassWall source " + sourceHandle + " đang thuộc nhiều semantic element.");
                        var element = matches[0];
                        if (!processed.Add(element.Id)) throw new InvalidOperationException("GlassWall " + element.Id + " có nhiều source POLYLINE đang được chọn. Tách/capture từng source trước khi dựng curtain frame 3D.");

                        var family = project.FindFamily(element.FamilyId);
                        var sagittaM = ProjectNumber(project, "WallArcSagittaM", 0.002d, 1e-6d);
                        var centerline = CadPolylinePathReader.ReadOpenWcsXy(document, polyline, sagittaM, element.Id + "/curtain path");
                        var lengthM = CadGeometryGuard.Positive(CurtainPathFramePlanner.Length(centerline), element.Id + "/LengthM");
                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
                        var hostThicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", 0.012d), element.Id + "/ThicknessM");
                        var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                        var frameDepthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainFrameDepthM", 0.05d), element.Id + "/CurtainFrameDepthM");
                        var input = new CurtainWallLayoutInput
                        {
                            LengthM = lengthM,
                            HeightM = heightM,
                            MaxPanelWidthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelWidthM", 1.2d), element.Id + "/CurtainMaxPanelWidthM"),
                            MaxPanelHeightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "CurtainMaxPanelHeightM", 1.5d), element.Id + "/CurtainMaxPanelHeightM"),
                            PerimeterFrameWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainPerimeterFrameWidthM", 0.05d), element.Id + "/CurtainPerimeterFrameWidthM"),
                            MullionWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainMullionWidthM", 0.05d), element.Id + "/CurtainMullionWidthM"),
                            TransomWidthM = NonNegative(CadGeometryGuard.Number(element, family, "CurtainTransomWidthM", 0.05d), element.Id + "/CurtainTransomWidthM")
                        };
                        var configFingerprint = CurtainWallFrameFingerprint.Compute(new CurtainWallFrameFingerprintInput
                        {
                            LengthM = lengthM,
                            HeightM = heightM,
                            BottomOffsetM = bottomOffsetM,
                            MaxPanelWidthM = input.MaxPanelWidthM,
                            MaxPanelHeightM = input.MaxPanelHeightM,
                            PerimeterFrameWidthM = input.PerimeterFrameWidthM,
                            MullionWidthM = input.MullionWidthM,
                            TransomWidthM = input.TransomWidthM,
                            FrameDepthM = frameDepthM
                        });
                        var detail = CurtainWallDetailPlanner.Plan(input);
                        var baseFrames = detail.VerticalFrames.Concat(detail.HorizontalFrames).ToList();
                        var openingRects = ReadLinkedOpenings(document, transaction, project, element, centerline, lengthM, heightM, hostThicknessM);
                        var frames = CurtainFrameOpeningPlanner.Interrupt(baseFrames, openingRects).ToList();
                        var pathPlan = CurtainPathFramePlanner.Plan(centerline, frames);
                        var frameCount = pathPlan.Pieces.Count;
                        if (frameCount > MaxFramesPerElement) throw new InvalidOperationException(element.Id + " cần " + frameCount + " curtain path frame fragment solids, vượt giới hạn native " + MaxFramesPerElement + ". Tăng panel size, giảm arc tessellation/opening hoặc chia vách.");
                        if (batchFrames > MaxFramesPerBatch - frameCount) throw new InvalidOperationException("Curtain path frame batch vượt giới hạn " + MaxFramesPerBatch + " solid.");

                        var previous = ValidatePrevious(document, transaction, project, element, ownership);
                        ErasePrevious(transaction, project, element, previous);
                        var baseZ = CadGeometryGuard.Add(polyline.Elevation, CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM"), element.Id + "/curtain path base Z");
                        var update = new PendingUpdate
                        {
                            Element = element,
                            Columns = detail.Layout.Columns,
                            Rows = detail.Layout.Rows,
                            BaseFrameCount = baseFrames.Count,
                            OpeningCount = openingRects.Count,
                            PathSegmentCount = pathPlan.PathSegmentCount,
                            MappedFrameCount = frames.Count,
                            FrameDepthM = frameDepthM,
                            SourceLengthM = lengthM,
                            HeightM = heightM,
                            ConfigFingerprint = configFingerprint
                        };

                        foreach (var piece in pathPlan.Pieces)
                        {
                            Solid3d? solid = CreateFrame(document, piece, frameDepthM, baseZ, element.Id);
                            try
                            {
                                solid.Layer = polyline.Layer;
                                modelSpace.AppendEntity(solid);
                                transaction.AddNewlyCreatedDBObject(solid, true);
                                GeneratedCurtainFrameNativeOwnershipService.MarkGenerated(document, transaction, solid, project, element);
                                update.Handles.Add(solid.Handle.ToString());
                                solid = null;
                            }
                            finally { solid?.Dispose(); }
                        }
                        pending.Add(update);
                        batchFrames = checked(batchFrames + update.Handles.Count);
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
                            "Curtain path frame replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            return new CurtainFrameBuildResult { Elements = pending.Count, Frames = pending.Sum(x => x.Handles.Count) };
        }

        private static void CommitSemanticUpdate(ProjectState project, PendingUpdate update)
        {
            update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
            update.Element.Properties["GeneratedCurtainFrameCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameBaseCount"] = update.BaseFrameCount.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameOpeningCount"] = update.OpeningCount.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameColumns"] = update.Columns.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameRows"] = update.Rows.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameDepthM"] = update.FrameDepthM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameSourceLengthM"] = update.SourceLengthM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameHeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameConfigFingerprint"] = update.ConfigFingerprint;
            update.Element.Properties["GeneratedCurtainFrameMode"] = update.OpeningCount > 0 ? OpeningAwareMode : Mode;
            update.Element.Properties["GeneratedCurtainFrameSourceKind"] = "OpenPolyline";
            update.Element.Properties["GeneratedCurtainFramePathSegmentCount"] = update.PathSegmentCount.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["GeneratedCurtainFrameMappedFrameCount"] = update.MappedFrameCount.ToString(CultureInfo.InvariantCulture);
            update.Element.Properties["LengthM"] = update.SourceLengthM.ToString("R", CultureInfo.InvariantCulture);
            update.Element.ClearGeneratedCurtainFrameStale();
            AuditTrail.ForProject(project).Record("geometry.curtain.path.frames", update.Element.Id,
                update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " path frame fragments • base=" + update.BaseFrameCount.ToString(CultureInfo.InvariantCulture) + " • mapped=" + update.MappedFrameCount.ToString(CultureInfo.InvariantCulture) + " • segments=" + update.PathSegmentCount.ToString(CultureInfo.InvariantCulture) + " • openings=" + update.OpeningCount.ToString(CultureInfo.InvariantCulture));
        }

        private static IReadOnlyList<CurtainOpeningRect> ReadLinkedOpenings(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement host,
            IReadOnlyList<Point2> centerline,
            double hostLengthM,
            double hostHeightM,
            double hostThicknessM)
        {
            var result = new List<CurtainOpeningRect>();
            var maximumOffsetM = hostThicknessM / 2d + 0.25d;
            foreach (var opening in project.Elements
                .Where(x => (x.Category == ElementCategory.Door || x.Category == ElementCategory.WallOpening) &&
                            x.Properties.TryGetValue("HostWallId", out var hostId) &&
                            string.Equals(hostId, host.Id, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                var openingFamily = project.FindFamily(opening.FamilyId);
                var widthM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "WidthM", 0d), opening.Id + "/WidthM");
                var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(opening, openingFamily, "HeightM", 0d), opening.Id + "/HeightM");
                var sillM = NonNegative(CadGeometryGuard.Number(opening, openingFamily, "SillHeightM", opening.Category == ElementCategory.Door ? 0d : 0.9d), opening.Id + "/SillHeightM");
                var clearanceM = NonNegative(CadGeometryGuard.Number(opening, openingFamily, "BooleanClearanceM", 0.01d), opening.Id + "/BooleanClearanceM");
                var sourceIds = CadHandleService.Resolve(document, opening.SourceHandles);
                if (sourceIds.Count == 0) throw new InvalidOperationException("Linked opening " + opening.Id + " không còn live CAD source để ngắt curtain path frame an toàn.");
                if (sourceIds.Count > 1) throw new InvalidOperationException("Linked opening " + opening.Id + " có nhiều live CAD source; cần một source duy nhất để ngắt curtain path frame.");
                var entity = transaction.GetObject(sourceIds[0], OpenMode.ForRead, false) as Entity;
                if (entity == null || entity.IsErased) throw new InvalidOperationException("Linked opening source không còn live: " + opening.Id);
                Extents3d extents;
                try { extents = entity.GeometricExtents; }
                catch (Exception ex) { throw new InvalidOperationException("Không đọc được extents cho linked opening " + opening.Id + ".", ex); }
                var centerXDrawing = CadGeometryGuard.Midpoint(extents.MinPoint.X, extents.MaxPoint.X, opening.Id + "/opening center X");
                var centerYDrawing = CadGeometryGuard.Midpoint(extents.MinPoint.Y, extents.MaxPoint.Y, opening.Id + "/opening center Y");
                var center = new Point2(
                    CadGeometryGuard.ToMeters(document, centerXDrawing, opening.Id + "/opening center X meters"),
                    CadGeometryGuard.ToMeters(document, centerYDrawing, opening.Id + "/opening center Y meters"));
                var projection = CurtainPathFramePlanner.ProjectPoint(centerline, center);
                if (projection.DistanceM > maximumOffsetM)
                    throw new InvalidOperationException("Linked opening " + opening.Id + " nằm quá xa GlassWall path centerline để ngắt curtain frame an toàn.");
                var plan = OpeningCutPlanner.Plan(new OpeningCutInput
                {
                    HostLengthM = hostLengthM,
                    HostThicknessM = hostThicknessM,
                    HostHeightM = hostHeightM,
                    OpeningWidthM = widthM,
                    OpeningHeightM = heightM,
                    SillHeightM = sillM,
                    CenterAlongHostM = projection.StationM,
                    ClearanceM = clearanceM
                });
                result.Add(new CurtainOpeningRect(
                    plan.CenterAlongHostM - plan.CutterWidthM / 2d,
                    plan.BaseElevationM,
                    plan.CutterWidthM,
                    plan.CutterHeightM));
            }
            return result.AsReadOnly();
        }

        private static Solid3d CreateFrame(Document document, CurtainPathFramePiece piece, double depthM, double baseZ, string label)
        {
            var width = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, piece.WidthM, label + "/path frame width"), label + "/path frame width drawing");
            var depth = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, depthM, label + "/path frame depth"), label + "/path frame depth drawing");
            var height = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, piece.HeightM, label + "/path frame height"), label + "/path frame height drawing");
            var centerX = CadGeometryGuard.ToDrawingUnits(document, piece.CenterX_M, label + "/path frame center X");
            var centerY = CadGeometryGuard.ToDrawingUnits(document, piece.CenterY_M, label + "/path frame center Y");
            var centerZOffset = CadGeometryGuard.ToDrawingUnits(document, piece.Z_M + piece.HeightM / 2d, label + "/path frame center Z offset");
            var centerZ = CadGeometryGuard.Add(baseZ, centerZOffset, label + "/path frame center Z");
            var angle = CadGeometryGuard.Finite(piece.AngleRadians, label + "/path frame angle");
            var solid = new Solid3d();
            try
            {
                solid.SetDatabaseDefaults(document.Database);
                solid.CreateBox(width, depth, height);
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(-width / 2d, -depth / 2d, -height / 2d)));
                solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                solid.TransformBy(Matrix3d.Displacement(new Vector3d(centerX, centerY, centerZ)));
                var complete = solid;
                solid = null!;
                return complete;
            }
            finally { solid?.Dispose(); }
        }

        private static IReadOnlyList<KeyValuePair<string, ObjectId>> ValidatePrevious(
            Document document,
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            GeneratedCurtainFrameOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw))
                return Array.Empty<KeyValuePair<string, ObjectId>>();

            var expected = new List<KeyValuePair<string, string>>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var original = token.Trim();
                if (original.Length == 0) continue;
                ownership.EnsureOwned(original, element);
                var canonical = CadHandleService.NormalizeHexHandle(original);
                if (canonical == null)
                    throw new InvalidOperationException("Generated curtain PATH frame metadata contains an invalid handle. Refusing destructive replacement before any frame is erased: " + original + ".");
                if (seen.Add(canonical)) expected.Add(new KeyValuePair<string, string>(canonical, original));
            }
            if (expected.Count == 0)
                throw new InvalidOperationException("Generated curtain PATH frame metadata contains no valid handles. Refusing destructive replacement before any frame is erased.");

            var ids = CadHandleService.Resolve(document, expected.Select(x => x.Key));
            if (ids.Count != expected.Count)
                throw new InvalidOperationException("Generated curtain PATH frame live-handle set is incomplete. Refusing destructive replacement before any frame is erased.");

            var result = new List<KeyValuePair<string, ObjectId>>(expected.Count);
            for (var i = 0; i < expected.Count; i++)
            {
                var entity = transaction.GetObject(ids[i], OpenMode.ForRead, false) as Entity;
                if (!(entity is Solid3d solid) || solid.IsErased)
                    throw new InvalidOperationException("Generated curtain PATH frame is missing, erased, or not a Solid3d. Refusing destructive replacement before any frame is erased: " + expected[i].Key + ".");
                GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(solid, project, element, "validate generated curtain PATH frame " + expected[i].Key);
                result.Add(new KeyValuePair<string, ObjectId>(expected[i].Key, ids[i]));
            }
            return result;
        }

        private static void ErasePrevious(
            Transaction transaction,
            ProjectState project,
            ProjectElement element,
            IReadOnlyList<KeyValuePair<string, ObjectId>> previous)
        {
            foreach (var item in previous)
            {
                var entity = transaction.GetObject(item.Value, OpenMode.ForWrite, false) as Entity;
                if (!(entity is Solid3d solid) || solid.IsErased)
                    throw new InvalidOperationException("Generated curtain PATH frame changed after validation. Refusing partial destructive replacement: " + item.Key + ".");
                GeneratedCurtainFrameNativeOwnershipService.RequireMatchingOwnership(solid, project, element, "erase generated curtain PATH frame " + item.Key);
                solid.Erase();
            }
        }

        private static double ProjectNumber(ProjectState project, string key, double fallback, double minimum)
        {
            if (!project.Metadata.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text)) return fallback;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value) || value < minimum)
                throw new InvalidOperationException("Project metadata " + key + " không hợp lệ: " + text);
            return value;
        }

        private static double NonNegative(double value, string label)
        {
            value = CadGeometryGuard.Finite(value, label);
            if (value < 0d) throw new InvalidOperationException(label + " phải >= 0.");
            return value;
        }
    }
}
