using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal sealed class CurtainFrameBuildResult
    {
        public int Elements { get; set; }
        public int Frames { get; set; }
    }

    internal static class CurtainWallFrameSolidBuilder
    {
        private const string HandlesKey = "GeneratedCurtainFrameHandles";
        private const string Mode = "LineFrameOverlay";
        private const int MaxFramesPerElement = 4096;
        private const int MaxFramesPerBatch = 8192;

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public List<string> Handles { get; } = new List<string>();
            public int Columns { get; set; }
            public int Rows { get; set; }
            public double FrameDepthM { get; set; }
            public double SourceLengthM { get; set; }
            public double HeightM { get; set; }
        }

        public static CurtainFrameBuildResult BuildSelectedLineWalls(Document document, ProjectState project)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return new CurtainFrameBuildResult();
            var ids = selection.Value.GetObjectIds();
            if (ids.Length == 0) return new CurtainFrameBuildResult();

            var ownership = GeneratedCurtainFrameOwnershipGuard.Build(project);
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pending = new List<PendingUpdate>();
            var batchFrames = 0;

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                foreach (var id in ids)
                {
                    var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                    if (line == null || line.IsErased) continue;
                    var sourceHandle = line.Handle.ToString();
                    var matches = project.Elements
                        .Where(x => x.Category == ElementCategory.GlassWall && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)))
                        .Take(2)
                        .ToList();
                    if (matches.Count == 0) continue;
                    if (matches.Count > 1) throw new InvalidOperationException("GlassWall source " + sourceHandle + " đang thuộc nhiều semantic element.");
                    var element = matches[0];
                    if (!processed.Add(element.Id)) throw new InvalidOperationException("GlassWall " + element.Id + " có nhiều source LINE đang được chọn. Tách/capture từng source trước khi dựng curtain frame 3D.");

                    var family = project.FindFamily(element.FamilyId);
                    var dx = CadGeometryGuard.Finite(line.EndPoint.X - line.StartPoint.X, element.Id + "/curtain dx");
                    var dy = CadGeometryGuard.Finite(line.EndPoint.Y - line.StartPoint.Y, element.Id + "/curtain dy");
                    var dz = CadGeometryGuard.Finite(line.EndPoint.Z - line.StartPoint.Z, element.Id + "/curtain dz");
                    var lengthDrawing = CadGeometryGuard.Hypot(dx, dy, element.Id + "/curtain length");
                    if (lengthDrawing <= 1e-8d) throw new InvalidOperationException("GlassWall source LINE quá ngắn: " + element.Id);
                    var dzM = Math.Abs(CadGeometryGuard.ToMeters(document, dz, element.Id + "/curtain dz"));
                    if (dzM > 1e-6d) throw new InvalidOperationException("Curtain frame 3D hiện yêu cầu GlassWall LINE nằm ngang: " + element.Id);

                    var lengthM = CadGeometryGuard.Positive(CadGeometryGuard.ToMeters(document, lengthDrawing, element.Id + "/LengthM"), element.Id + "/LengthM");
                    var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
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
                    var detail = CurtainWallDetailPlanner.Plan(input);
                    var frameCount = checked(detail.VerticalFrames.Count + detail.HorizontalFrames.Count);
                    if (frameCount > MaxFramesPerElement) throw new InvalidOperationException(element.Id + " cần " + frameCount + " curtain frame solids, vượt giới hạn native " + MaxFramesPerElement + ". Tăng panel size hoặc chia vách.");
                    if (batchFrames > MaxFramesPerBatch - frameCount) throw new InvalidOperationException("Curtain frame batch vượt giới hạn " + MaxFramesPerBatch + " solid.");

                    ErasePrevious(document, transaction, element, ownership);
                    var ux = dx / lengthDrawing;
                    var uy = dy / lengthDrawing;
                    var angle = CadGeometryGuard.Finite(Math.Atan2(uy, ux), element.Id + "/curtain angle");
                    var baseZ = CadGeometryGuard.Add(line.StartPoint.Z, CadGeometryGuard.ToDrawingUnits(document, bottomOffsetM, element.Id + "/BottomOffsetM"), element.Id + "/curtain base Z");
                    var update = new PendingUpdate
                    {
                        Element = element,
                        Columns = detail.Layout.Columns,
                        Rows = detail.Layout.Rows,
                        FrameDepthM = frameDepthM,
                        SourceLengthM = lengthM,
                        HeightM = heightM
                    };

                    foreach (var frame in detail.VerticalFrames.Concat(detail.HorizontalFrames))
                    {
                        Solid3d? solid = CreateFrame(document, line, frame, frameDepthM, baseZ, angle, ux, uy, element.Id);
                        try
                        {
                            solid.Layer = line.Layer;
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            update.Handles.Add(solid.Handle.ToString());
                            solid = null;
                        }
                        finally { solid?.Dispose(); }
                    }
                    pending.Add(update);
                    batchFrames = checked(batchFrames + update.Handles.Count);
                }
                transaction.Commit();
            }

            foreach (var update in pending)
            {
                update.Element.Properties[HandlesKey] = string.Join(";", update.Handles);
                update.Element.Properties["GeneratedCurtainFrameCount"] = update.Handles.Count.ToString(CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedCurtainFrameColumns"] = update.Columns.ToString(CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedCurtainFrameRows"] = update.Rows.ToString(CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedCurtainFrameDepthM"] = update.FrameDepthM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedCurtainFrameSourceLengthM"] = update.SourceLengthM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedCurtainFrameHeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
                update.Element.Properties["GeneratedCurtainFrameMode"] = Mode;
                update.Element.ClearGeneratedCurtainFrameStale();
                AuditTrail.ForProject(project).Record("geometry.curtain.frames", update.Element.Id, update.Handles.Count.ToString(CultureInfo.InvariantCulture) + " frame solids");
            }
            if (pending.Count > 0)
            {
                project.Touch();
                document.Editor.Regen();
            }
            return new CurtainFrameBuildResult { Elements = pending.Count, Frames = pending.Sum(x => x.Handles.Count) };
        }

        private static Solid3d CreateFrame(Document document, Line line, CurtainWallRect frame, double depthM, double baseZ, double angle, double ux, double uy, string label)
        {
            var width = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, frame.WidthM, label + "/frame width"), label + "/frame width drawing");
            var depth = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, depthM, label + "/frame depth"), label + "/frame depth drawing");
            var height = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, frame.HeightM, label + "/frame height"), label + "/frame height drawing");
            var centerStation = CadGeometryGuard.ToDrawingUnits(document, frame.X_M + frame.WidthM / 2d, label + "/frame station");
            var centerZOffset = CadGeometryGuard.ToDrawingUnits(document, frame.Z_M + frame.HeightM / 2d, label + "/frame Z offset");
            var centerX = CadGeometryGuard.Add(line.StartPoint.X, CadGeometryGuard.Finite(ux * centerStation, label + "/frame center dx"), label + "/frame center X");
            var centerY = CadGeometryGuard.Add(line.StartPoint.Y, CadGeometryGuard.Finite(uy * centerStation, label + "/frame center dy"), label + "/frame center Y");
            var centerZ = CadGeometryGuard.Add(baseZ, centerZOffset, label + "/frame center Z");
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

        private static void ErasePrevious(Document document, Transaction transaction, ProjectElement element, GeneratedCurtainFrameOwnershipGuard.OwnershipIndex ownership)
        {
            if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return;
            foreach (var handle in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                ownership.EnsureOwned(handle, element);
                var ids = CadHandleService.Resolve(document, new[] { handle });
                if (ids.Count == 0) continue;
                if (ids.Count > 1) throw new InvalidOperationException("Generated curtain frame handle " + handle + " resolves to multiple live CAD objects.");
                var entity = transaction.GetObject(ids[0], OpenMode.ForWrite, false) as Entity;
                if (entity == null || entity.IsErased) continue;
                if (!(entity is Solid3d solid)) throw new InvalidOperationException("Generated curtain frame handle " + handle + " is live but is not a Solid3d. Refusing destructive erase.");
                solid.Erase();
            }
        }

        private static double NonNegative(double value, string label)
        {
            value = CadGeometryGuard.Finite(value, label);
            if (value < 0d) throw new InvalidOperationException(label + " phải >= 0.");
            return value;
        }
    }
}
