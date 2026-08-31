using System;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Native BricsCAD authoring for Workspace Móng đơn. The Family owns dimensions in meters;
    /// the interactive command acquires center points while the internal one-shot bridge accepts
    /// an already-resolved center. A closed footprint remains the semantic CAD source and the
    /// visible Solid3d is marked with standard QS3D generated-geometry ownership.
    /// </summary>
    public sealed class SingleFootingCommands
    {
        [CommandMethod("QS3DDRAWSINGLEFOOTING", CommandFlags.Modal)]
        public void DrawSingleFooting()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                RequireModelSpace(document);
                var project = ExistingProjectMutationContext.Require(document, "Vẽ Móng đơn");
                var family = ProjectFamilyActivationService.GetActive(project);
                if (!SingleFootingContract.IsSingleFooting(family))
                    throw new InvalidOperationException("Chọn Móng → Móng đơn và một Family Móng đơn trước khi Vẽ.");
                var dimensions = SingleFootingContract.Read(family!);
                var expectedProjectId = project.ProjectId;
                var expectedFamilyId = family!.Id;

                document.Editor.WriteMessage(
                    "\nQS3D Móng đơn: L1×W1=" + Mm(dimensions.L1M) + "×" + Mm(dimensions.W1M) +
                    " mm, L2×W2=" + Mm(dimensions.L2M) + "×" + Mm(dimensions.W2M) +
                    " mm, H1/H2=" + Mm(dimensions.H1M) + "/" + Mm(dimensions.H2M) +
                    " mm. Pick tâm móng; Enter/Esc để kết thúc.");

                while (true)
                {
                    var prompt = new PromptPointOptions("\nMóng đơn - chọn tâm hoặc Enter/Esc để kết thúc: ")
                    {
                        AllowNone = true
                    };
                    var point = document.Editor.GetPoint(prompt);
                    if (point.Status == PromptStatus.None || point.Status == PromptStatus.Cancel) break;
                    if (point.Status != PromptStatus.OK) break;

                    RequireCurrentContext(document, expectedProjectId, expectedFamilyId, dimensions);
                    PlaceOne(document, project, family, dimensions, point.Value);
                }
            }
            catch (Exception ex)
            {
                Report(document, "QS3DDRAWSINGLEFOOTING lỗi: " + ex.Message);
            }
        }

        /// <summary>
        /// Deterministic one-shot bridge for callers that already resolved a center point (for
        /// example a bounded automation surface). It deliberately shares the same Family,
        /// semantic source, generated Solid3d ownership and rollback path as the interactive
        /// command instead of reimplementing Móng đơn geometry.
        /// </summary>
        internal static string PlaceActiveSingleFootingAt(Document document, Point3d center)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            RequireFiniteCenter(center);
            RequireModelSpace(document);

            var project = ExistingProjectMutationContext.Require(document, "Đặt Móng đơn");
            var family = ProjectFamilyActivationService.GetActive(project);
            if (!SingleFootingContract.IsSingleFooting(family))
                throw new InvalidOperationException("Chọn Móng → Móng đơn và một Family Móng đơn trước khi đặt theo tọa độ.");

            var dimensions = SingleFootingContract.Read(family!);
            RequireCurrentContext(document, project.ProjectId, family!.Id, dimensions);
            return PlaceOne(document, project, family, dimensions, center);
        }

        private static string PlaceOne(
            Document document,
            ProjectState project,
            ProjectFamily family,
            SingleFootingDimensions dimensions,
            Point3d center)
        {
            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            var sourceHandle = string.Empty;
            try
            {
                var baseElevationM = ResolveActiveFloorElevation(project);
                var baseElevationDrawing = CadUnitService.MetersToDrawingUnits(document, baseElevationM);
                var l1 = CadUnitService.MetersToDrawingUnits(document, dimensions.L1M);
                var w1 = CadUnitService.MetersToDrawingUnits(document, dimensions.W1M);

                sourceId = CreateFootprint(document, center.X, center.Y, baseElevationDrawing, l1, w1);
                sourceHandle = sourceId.Handle.ToString();
                var snapshots = EntitySnapshotReader.ReadHandles(document, new[] { sourceHandle });
                if (snapshots.Count != 1)
                    throw new InvalidOperationException("Không đọc lại được footprint Móng đơn vừa tạo.");
                if (!SemanticCaptureService.CaptureSnapshot(document, snapshots[0], ElementCategory.Foundation))
                    throw new InvalidOperationException("Không capture được footprint Móng đơn vào semantic project.");

                var liveProject = ExistingProjectMutationContext.Require(document, "Hoàn tất Móng đơn");
                if (!ReferenceEquals(project, liveProject))
                    throw new InvalidOperationException("QS3D project đã thay đổi trong lúc đặt Móng đơn. Hãy Refresh Workspace và thử lại.");
                var activeFamily = ProjectFamilyActivationService.GetActive(project);
                if (activeFamily == null || !string.Equals(activeFamily.Id, family.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Active Family đã thay đổi trong lúc đặt Móng đơn.");

                var matches = project.Elements
                    .Where(x => x.Category == ElementCategory.Foundation &&
                                x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)))
                    .Take(2)
                    .ToList();
                if (matches.Count != 1)
                    throw new InvalidOperationException("Semantic ownership của footprint Móng đơn không duy nhất.");
                var element = matches[0];
                SingleFootingContract.Apply(element, dimensions);
                element.Properties["SingleFootingBaseElevationM"] = baseElevationM.ToString("R", CultureInfo.InvariantCulture);
                element.MarkDirty(ElementDirtyFlags.All);

                string generatedHandle;
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
                {
                    var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                    var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    var source = transaction.GetObject(sourceId, OpenMode.ForRead, false) as Entity;
                    if (source == null || source.IsErased)
                        throw new InvalidOperationException("Footprint Móng đơn đã biến mất trước khi dựng Solid3d.");

                    var solid = BuildSolid(document, dimensions, center.X, center.Y, baseElevationDrawing);
                    try
                    {
                        solid.Layer = source.Layer;
                        var previousHandle = GeneratedGeometryService.PrepareReplacement(document, transaction, project, element);
                        modelSpace.AppendEntity(solid);
                        transaction.AddNewlyCreatedDBObject(solid, true);
                        GeneratedGeometryService.MarkGenerated(
                            document,
                            transaction,
                            solid,
                            project.ProjectId,
                            element.Id,
                            ElementCategory.Foundation);
                        generatedHandle = solid.Handle.ToString();
                        GeneratedGeometryService.CommitReplacement(
                            project,
                            element,
                            previousHandle,
                            generatedHandle,
                            ElementCategory.Foundation);
                        element.Properties["GeneratedSolidMode"] = SingleFootingContract.GeneratedMode;
                        element.Properties[SingleFootingContract.VolumeKey] = dimensions.VolumeM3.ToString("R", CultureInfo.InvariantCulture);
                        element.Properties["VolumeM3"] = dimensions.VolumeM3.ToString("R", CultureInfo.InvariantCulture);
                        element.MarkClean(ElementGeometryPolicy.SemanticCleanFlags(ElementCategory.Foundation));
                        project.Touch();
                        AuditTrail.ForProject(project).Record(
                            "geometry.single-footing.create",
                            element.Id,
                            sourceHandle + " -> " + generatedHandle + " • " + family.Name);
                        transaction.Commit();
                        solid = null!;
                    }
                    finally { solid?.Dispose(); }
                }

                try { document.Editor.SetImpliedSelection(new[] { sourceId }); } catch { }
                try { CadPostCommitUi.TryRegen(document, "Móng đơn"); } catch { }
                Report(document, "Đã tạo Móng đơn " + family.Name + " tại tâm đã chọn • Solid3d " + generatedHandle + ".");
                return generatedHandle;
            }
            catch (Exception operationError)
            {
                Exception? restoreError = null;
                Exception? eraseError = null;
                try { rollback.Restore(project); } catch (Exception ex) { restoreError = ex; }
                try { EraseIfLive(document, sourceId); } catch (Exception ex) { eraseError = ex; }
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); } catch { }

                if (restoreError != null || eraseError != null)
                {
                    var aggregate = restoreError != null && eraseError != null
                        ? new AggregateException(operationError, restoreError, eraseError)
                        : new AggregateException(operationError, restoreError ?? eraseError!);
                    throw new InvalidOperationException("Vẽ Móng đơn thất bại và rollback không hoàn tất đầy đủ.", aggregate);
                }
                throw;
            }
        }

        private static Solid3d BuildSolid(
            Document document,
            SingleFootingDimensions dimensions,
            double centerX,
            double centerY,
            double baseZ)
        {
            var l1 = CadUnitService.MetersToDrawingUnits(document, dimensions.L1M);
            var w1 = CadUnitService.MetersToDrawingUnits(document, dimensions.W1M);
            var l2 = CadUnitService.MetersToDrawingUnits(document, dimensions.L2M);
            var w2 = CadUnitService.MetersToDrawingUnits(document, dimensions.W2M);
            var h1 = CadUnitService.MetersToDrawingUnits(document, dimensions.H1M);
            var h2 = CadUnitService.MetersToDrawingUnits(document, dimensions.H2M);

            var lower = new Solid3d();
            try
            {
                lower.SetDatabaseDefaults(document.Database);
                lower.CreateBox(l1, w1, h1);
                lower.TransformBy(Matrix3d.Displacement(new Vector3d(centerX, centerY, baseZ + h1 / 2d)));
                if (!(h2 > 0d))
                {
                    var completed = lower;
                    lower = null!;
                    return completed;
                }

                Solid3d upper;
                if (Math.Abs(l1 - l2) <= 1e-10d && Math.Abs(w1 - w2) <= 1e-10d)
                {
                    upper = new Solid3d();
                    upper.SetDatabaseDefaults(document.Database);
                    upper.CreateBox(l1, w1, h2);
                    upper.TransformBy(Matrix3d.Displacement(new Vector3d(centerX, centerY, baseZ + h1 + h2 / 2d)));
                }
                else
                {
                    upper = CreateTaperedLoft(document, centerX, centerY, baseZ + h1, l1, w1, baseZ + h1 + h2, l2, w2);
                }

                using (upper)
                    lower.BooleanOperation(BooleanOperationType.BoolUnite, upper);

                var result = lower;
                lower = null!;
                return result;
            }
            finally { lower?.Dispose(); }
        }

        private static Solid3d CreateTaperedLoft(
            Document document,
            double centerX,
            double centerY,
            double bottomZ,
            double bottomLength,
            double bottomWidth,
            double topZ,
            double topLength,
            double topWidth)
        {
            using (var bottom = CreateProfile(centerX, centerY, bottomZ, bottomLength, bottomWidth))
            using (var top = CreateProfile(centerX, centerY, topZ, topLength, topWidth))
            using (var options = new LoftOptions())
            {
                var solid = new Solid3d();
                try
                {
                    solid.SetDatabaseDefaults(document.Database);
                    solid.CreateLoftedSolid(
                        new Entity[] { bottom, top },
                        Array.Empty<Entity>(),
                        null,
                        options);
                    return solid;
                }
                catch
                {
                    solid.Dispose();
                    throw;
                }
            }
        }

        private static Polyline CreateProfile(double centerX, double centerY, double elevation, double length, double width)
        {
            var profile = new Polyline(4) { Closed = true, Elevation = elevation };
            profile.AddVertexAt(0, new Point2d(centerX - length / 2d, centerY - width / 2d), 0d, 0d, 0d);
            profile.AddVertexAt(1, new Point2d(centerX + length / 2d, centerY - width / 2d), 0d, 0d, 0d);
            profile.AddVertexAt(2, new Point2d(centerX + length / 2d, centerY + width / 2d), 0d, 0d, 0d);
            profile.AddVertexAt(3, new Point2d(centerX - length / 2d, centerY + width / 2d), 0d, 0d, 0d);
            return profile;
        }

        private static ObjectId CreateFootprint(
            Document document,
            double centerX,
            double centerY,
            double elevation,
            double length,
            double width)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var polyline = CreateProfile(centerX, centerY, elevation, length, width);
                polyline.SetDatabaseDefaults(document.Database);
                var id = modelSpace.AppendEntity(polyline);
                transaction.AddNewlyCreatedDBObject(polyline, true);
                transaction.Commit();
                return id;
            }
        }

        private static void EraseIfLive(Document document, ObjectId id)
        {
            if (id.IsNull || !id.IsValid || id.IsErased) return;
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var entity = transaction.GetObject(id, OpenMode.ForWrite, true) as Entity;
                if (entity != null && !entity.IsErased) entity.Erase();
                transaction.Commit();
            }
        }

        private static double ResolveActiveFloorElevation(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var floor = project.FindFloor(project.ActiveFloorId);
            if (floor == null) return 0d;
            if (double.IsNaN(floor.ElevationM) || double.IsInfinity(floor.ElevationM))
                throw new InvalidOperationException("Cao độ tầng active không hữu hạn.");
            return floor.ElevationM;
        }

        private static void RequireCurrentContext(
            Document document,
            string expectedProjectId,
            string expectedFamilyId,
            SingleFootingDimensions expectedDimensions)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("DWG active đã thay đổi trong lúc Vẽ Móng đơn.");
            var project = ExistingProjectMutationContext.Require(document, "Vẽ Móng đơn");
            if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QS3D project đã thay đổi trong lúc Vẽ Móng đơn.");
            var family = ProjectFamilyActivationService.GetActive(project);
            if (family == null || !string.Equals(family.Id, expectedFamilyId, StringComparison.OrdinalIgnoreCase) || !SingleFootingContract.IsSingleFooting(family))
                throw new InvalidOperationException("Family Móng đơn active đã thay đổi; chạy lại lệnh để dùng Family hiện hành.");
            var current = SingleFootingContract.Read(family);
            if (!SameDimensions(current, expectedDimensions))
                throw new InvalidOperationException("Kích thước Family Móng đơn đã thay đổi; chạy lại lệnh để dùng thông số mới.");
        }

        private static void RequireFiniteCenter(Point3d center)
        {
            if (!IsFinite(center.X) || !IsFinite(center.Y) || !IsFinite(center.Z))
                throw new InvalidOperationException("Tâm Móng đơn phải có tọa độ hữu hạn.");
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool SameDimensions(SingleFootingDimensions a, SingleFootingDimensions b) =>
            a.L1M == b.L1M && a.W1M == b.W1M && a.L2M == b.L2M && a.W2M == b.W2M && a.H1M == b.H1M && a.H2M == b.H2M;

        private static void RequireModelSpace(Document document)
        {
            if (document.Database.TileMode) return;
            throw new InvalidOperationException("QS3D Móng đơn chỉ author trong Model Space.");
        }

        private static string Mm(double meters) =>
            (meters * 1000d).ToString("0.###", CultureInfo.InvariantCulture);

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
