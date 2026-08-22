using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class WallSolidBuilder
    {
        private enum SourceBatchKind
        {
            Line,
            OpenPolyline
        }

        private sealed class PendingUpdate
        {
            public ProjectElement Element { get; set; } = null!;
            public string PreviousHandle { get; set; } = string.Empty;
            public string GeneratedHandle { get; set; } = string.Empty;
            public double LengthM { get; set; }
            public double ThicknessM { get; set; }
            public double HeightM { get; set; }
        }

        public static int BuildSelectedLineWalls(Document document, ProjectState project) =>
            BuildSelectedLineWalls(document, project, ElementCategory.ArchitecturalWall);

        public static int BuildSelectedLineWalls(Document document, ProjectState project, ElementCategory category)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!IsSupportedWall(category)) throw new ArgumentOutOfRangeException(nameof(category), "Unsupported architectural wall category: " + category);
            var selection = document.Editor.SelectImplied();
            if (selection.Status != PromptStatus.OK || selection.Value == null) return 0;
            var sourceIds = selection.Value.GetObjectIds();
            if (sourceIds.Length == 0) return 0;

            // This LINE builder is often called immediately before the open-POLYLINE builder.
            // Validate the whole logical wall batch before either builder is allowed to commit,
            // otherwise a mixed selection could commit LINE solids and then fail on POLYLINE.
            if (ValidateSourceBatch(document, sourceIds) != SourceBatchKind.Line) return 0;

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
                    foreach (var id in sourceIds)
                    {
                        var line = transaction.GetObject(id, OpenMode.ForRead, false) as Line;
                        if (line == null) continue;
                        if (!line.OwnerId.Equals(modelSpace.ObjectId))
                            throw new InvalidOperationException("Wall source phải nằm trong Model Space trước khi tạo native 3D: " + line.Handle + ".");
                        var sourceHandle = line.Handle.ToString();
                        var matches = project.Elements
                            .Where(x => x.Category == category && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)))
                            .Take(2)
                            .ToList();
                        if (matches.Count == 0) continue;
                        if (matches.Count > 1) throw new InvalidOperationException("CAD source handle " + sourceHandle + " đang thuộc nhiều QS3D wall element.");
                        var element = matches[0];
                        if (!processedElements.Add(element.Id)) throw new InvalidOperationException("Wall element " + element.Id + " có nhiều source đang được chọn. Tách/capture từng source thành element riêng trước khi Vẽ 3D.");

                        var family = project.FindFamily(element.FamilyId);
                        var thicknessM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "ThicknessM", .2d), element.Id + "/ThicknessM");
                        var heightM = CadGeometryGuard.Positive(CadGeometryGuard.Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
                        var bottomOffsetM = CadGeometryGuard.Number(element, family, "BottomOffsetM", 0d);
                        var placement = CadVerticalPlacementResolver.Resolve(
                            document,
                            project,
                            element,
                            line.StartPoint.Z,
                            heightM,
                            bottomOffsetM);
                        var dx = CadGeometryGuard.Subtract(line.EndPoint.X, line.StartPoint.X, element.Id + "/dx");
                        var dy = CadGeometryGuard.Subtract(line.EndPoint.Y, line.StartPoint.Y, element.Id + "/dy");
                        var dz = CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z, element.Id + "/dz");
                        var planTolerance = CadGeometryGuard.Positive(
                            CadGeometryGuard.ToDrawingUnits(document, .005d, element.Id + "/wall planarity tolerance"),
                            element.Id + "/wall planarity tolerance drawing units");
                        if (Math.Abs(dz) > planTolerance)
                            throw new InvalidOperationException("Wall source LINE hiện yêu cầu gần ngang (|ΔZ| <= 0.005 m): " + element.Id);
                        var length = CadGeometryGuard.Hypot(dx, dy, element.Id + "/source length");
                        if (length <= 1e-6) throw new InvalidOperationException("Wall source LINE quá ngắn: " + element.Id);

                        var thickness = CadGeometryGuard.Positive(CadGeometryGuard.ToDrawingUnits(document, thicknessM, element.Id + "/ThicknessM"), element.Id + "/Thickness drawing units");
                        var height = placement.HeightDrawingUnits;
                        var angle = CadGeometryGuard.Finite(Math.Atan2(dy, dx), element.Id + "/angle");
                        var midX = CadGeometryGuard.Midpoint(line.StartPoint.X, line.EndPoint.X, element.Id + "/mid X");
                        var midY = CadGeometryGuard.Midpoint(line.StartPoint.Y, line.EndPoint.Y, element.Id + "/mid Y");
                        var midZ = CadGeometryGuard.Add(placement.BottomDrawingUnits, height / 2d, element.Id + "/mid Z");
                        var mid = new Point3d(midX, midY, midZ);

                        var solid = new Solid3d();
                        try
                        {
                            solid.SetDatabaseDefaults(document.Database);
                            solid.CreateBox(length, thickness, height);
                            solid.TransformBy(Matrix3d.Displacement(new Vector3d(-length / 2d, -thickness / 2d, -height / 2d)));
                            solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));
                            solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));
                            solid.Layer = line.Layer;

                            var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                            modelSpace.AppendEntity(solid);
                            transaction.AddNewlyCreatedDBObject(solid, true);
                            GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, category);
                            pending.Add(new PendingUpdate
                            {
                                Element = element,
                                PreviousHandle = previousHandle,
                                GeneratedHandle = solid.Handle.ToString(),
                                LengthM = CadGeometryGuard.ToMeters(document, length, element.Id + "/source length"),
                                ThicknessM = thicknessM,
                                HeightM = heightM
                            });
                        }
                        catch
                        {
                            solid.Dispose();
                            throw;
                        }
                    }

                    // Commit semantic ownership while the CAD transaction is still rollback-capable.
                    // If this phase fails, the transaction is aborted and the project snapshot is
                    // restored, so a new Solid3d can never survive without matching semantic state.
                    foreach (var update in pending)
                    {
                        GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, category);
                        update.Element.Properties["LengthM"] = update.LengthM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["ThicknessM"] = update.ThicknessM.ToString("R", CultureInfo.InvariantCulture);
                        update.Element.Properties["HeightM"] = update.HeightM.ToString("R", CultureInfo.InvariantCulture);
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
                            "LINE wall replacement failed before CAD commit and project rollback also failed.",
                            new AggregateException(operationError, restoreError));
                    }
                }
                throw;
            }

            if (pending.Count > 0)
                CadPostCommitUi.TryRegen(document, "LINE wall native 3D");
            return pending.Count;
        }

        private static SourceBatchKind ValidateSourceBatch(Document document, IReadOnlyCollection<ObjectId> sourceIds)
        {
            var sawLine = false;
            var sawPolyline = false;
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                foreach (var id in sourceIds)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null || entity.IsErased)
                        throw new InvalidOperationException("Wall source selection chứa entity không còn hợp lệ.");
                    if (!entity.OwnerId.Equals(modelSpaceId))
                        throw new InvalidOperationException("Wall source phải nằm trong Model Space trước khi tạo native 3D: " + entity.Handle + ".");

                    if (entity is Line)
                    {
                        sawLine = true;
                        continue;
                    }
                    if (entity is Polyline polyline)
                    {
                        if (polyline.Closed)
                            throw new InvalidOperationException("Tường KT centerline POLYLINE phải open. Closed wall loop cần tách thành các wall centerline trước khi Build 3D.");
                        if (polyline.NumberOfVertices < 2)
                            throw new InvalidOperationException("Tường KT centerline POLYLINE cần ít nhất 2 đỉnh: " + polyline.Handle + ".");
                        sawPolyline = true;
                        continue;
                    }

                    throw new InvalidOperationException("Tường KT native 3D chỉ hỗ trợ source LINE hoặc open POLYLINE; nhận " + entity.GetType().Name + " (" + entity.Handle + ").");
                }
                transaction.Commit();
            }

            if (sawLine && sawPolyline)
                throw new InvalidOperationException("Không build chung LINE và open POLYLINE trong một wall batch vì hai builder có transaction riêng. Chọn một source type mỗi lần.");
            if (sawLine) return SourceBatchKind.Line;
            if (sawPolyline) return SourceBatchKind.OpenPolyline;
            throw new InvalidOperationException("Wall source selection không có LINE/open POLYLINE hợp lệ.");
        }

        private static bool IsSupportedWall(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier;
    }
}
