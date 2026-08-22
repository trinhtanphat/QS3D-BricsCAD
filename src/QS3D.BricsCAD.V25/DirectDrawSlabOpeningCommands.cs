using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Direct authoring for the exact slabOpen Family. The user preselects exactly one semantic
    /// Slab source, draws one closed footprint, then the command records HostSlabId and applies
    /// the dedicated negative-Z Boolean subtraction in the same guarded operation.
    /// </summary>
    public sealed class DirectDrawSlabOpeningCommands
    {
        private const double PlanarityToleranceM = 0.005d;
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DDRAWSLABOPEN", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void DrawSlabOpening() => Draw(promptClearance: false, operation: "QS3DDRAWSLABOPEN");

        [CommandMethod("QS3DDRAWSLABOPENADV", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void DrawSlabOpeningAdvanced() => Draw(promptClearance: true, operation: "QS3DDRAWSLABOPENADV");

        private static void Draw(bool promptClearance, string operation)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            Guard(document, operation, () =>
            {
                RequireModelSpace(document);
                var selectedHostIds = CadSelectionGuard.ReadImpliedSelection(document);
                if (selectedHostIds.Length != 1)
                    throw new InvalidOperationException(
                        "slabOpen yêu cầu PICKFIRST đúng một source Slab semantic trước khi chạy lệnh; hiện có " +
                        selectedHostIds.Length + ".");
                var selectedHostHandle = selectedHostIds[0].Handle.ToString();

                var points = AcquirePath(document, promptClearance ? "slabOpen tùy chỉnh" : "slabOpen nhanh");
                if (points == null) return;

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                if (!projectPreview.HasProject || defaultsProject == null)
                    throw new InvalidOperationException("slabOpen cần QS3D project hiện hành và Family slabOpen đang active.");

                var activeFamily = ProjectFamilyActivationService.GetActive(defaultsProject);
                if (!SlabOpeningContract.IsSlabOpenFamily(activeFamily))
                    throw new InvalidOperationException("Active Family phải là exact slabOpen trước khi Direct Draw.");

                var clearanceDefault = FamilyPositiveNumber(activeFamily!, SlabOpeningContract.BooleanClearanceMKey, 0.01d);
                var clearanceM = clearanceDefault;
                if (promptClearance)
                {
                    var prompted = PromptPositiveMeters(document.Editor, "Khe hở Boolean slabOpen (m)", clearanceDefault);
                    if (!prompted.HasValue) return;
                    clearanceM = prompted.Value;
                }
                else
                {
                    document.Editor.WriteMessage(
                        "\nQS3D slabOpen nhanh: chọn footprint kín, clearance " +
                        clearanceM.ToString("0.###", CultureInfo.InvariantCulture) +
                        " m; cutter sẽ đi xuyên Sàn theo -Z và tự BoolSubtract.");
                }

                Execute(document, projectPreview, selectedHostHandle, points, clearanceM, operation);
            });
        }

        private static void Execute(
            Document document,
            DirectDrawProjectPreviewContext projectPreview,
            string selectedHostHandle,
            IReadOnlyList<Point3d> points,
            double clearanceM,
            string operation)
        {
            EnsureActive(document, operation + " / mutation");
            var project = projectPreview.ResolveForMutation(document, operation);
            var family = ProjectFamilyActivationService.GetActive(project);
            if (!SlabOpeningContract.IsSlabOpenFamily(family))
                throw new InvalidOperationException("Active Family/routing đã đổi; slabOpen operation bị hủy.");

            var hostMatches = project.Elements
                .Where(element => element.Category == ElementCategory.Slab &&
                    element.SourceHandles.Any(handle =>
                        string.Equals((handle ?? string.Empty).Trim(), selectedHostHandle, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (hostMatches.Count != 1)
                throw new InvalidOperationException(
                    "PICKFIRST phải trỏ tới đúng một source của semantic Slab; tìm được " + hostMatches.Count + ".");
            var host = hostMatches[0];

            // Direct Draw promises first-use auto subtraction. If this Slab has never had native 3D,
            // materialize exactly the selected host before taking the opening rollback snapshot. A later
            // opening failure may then roll back only the opening operation without orphaning that committed
            // host Solid3d. Existing/stale generated hosts remain fail-closed in SlabOpeningBooleanService.
            EnsureFirstUseHostSolid(document, project, host, selectedHostHandle);

            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            var booleanCommitted = false;
            try
            {
                sourceId = CreatePolyline(document, points);
                if (sourceId.IsNull || !sourceId.IsValid)
                    throw new InvalidOperationException("Không tạo được closed POLYLINE source cho slabOpen.");

                var bounds = PlanBoundsMeters(document, points);
                var opening = new ProjectElement(
                    Guid.NewGuid().ToString("N"),
                    ElementCategory.WallOpening,
                    family!.Id,
                    project.ActiveFloorId,
                    project.ActiveZoneId);
                opening.SourceHandles.Add(sourceId.Handle.ToString());
                opening.SetProperty("WidthM", bounds.WidthM.ToString("R", CultureInfo.InvariantCulture));
                opening.SetProperty("HeightM", bounds.DepthM.ToString("R", CultureInfo.InvariantCulture));
                opening.SetProperty(SlabOpeningContract.BooleanClearanceMKey, clearanceM.ToString("R", CultureInfo.InvariantCulture));
                project.Elements.Add(opening);
                SlabOpeningContract.Bind(project, opening, host);
                project.Touch();

                EnsureActive(document, operation + " / before BoolSubtract");
                SlabOpeningBooleanService.CutLinkedOpening(document, project, opening);
                booleanCommitted = true;

                FinalizeUi(document, sourceId, opening, host, clearanceM);
            }
            catch (Exception operationError)
            {
                if (booleanCommitted) throw;

                Exception? cleanupError = null;
                Exception? restoreError = null;
                try { EraseSource(document, sourceId); }
                catch (Exception ex) { cleanupError = ex; }
                try { rollback.Restore(project); }
                catch (Exception ex) { restoreError = ex; }
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
                catch { }

                if (cleanupError != null || restoreError != null)
                {
                    var errors = new List<Exception> { operationError };
                    if (cleanupError != null) errors.Add(cleanupError);
                    if (restoreError != null) errors.Add(restoreError);
                    throw new InvalidOperationException(
                        "slabOpen thất bại và rollback không hoàn tất đầy đủ.",
                        new AggregateException(errors));
                }
                throw;
            }
        }

        private static void EnsureFirstUseHostSolid(
            Document document,
            ProjectState project,
            ProjectElement host,
            string selectedHostHandle)
        {
            if (host.Properties.TryGetValue("GeneratedSolidHandle", out var existingHandle) &&
                !string.IsNullOrWhiteSpace(existingHandle))
                return;

            var hostSourceIds = CadHandleService.Resolve(document, new[] { selectedHostHandle }).Distinct().ToArray();
            if (hostSourceIds.Length != 1)
                throw new InvalidOperationException(
                    "slabOpen không thể tự dựng host Slab lần đầu: source handle " + selectedHostHandle +
                    " resolve thành " + hostSourceIds.Length + " CAD entity.");

            document.Editor.SetImpliedSelection(hostSourceIds);
            var built = StructuralSolidBuilder.BuildSelected(document, project, ElementCategory.Slab);
            if (built != 1)
                throw new InvalidOperationException(
                    "slabOpen không thể tự dựng đúng một host Slab lần đầu; đã dựng " + built + ".");

            if (!host.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle) ||
                string.IsNullOrWhiteSpace(generatedHandle))
                throw new InvalidOperationException("slabOpen auto-build không tạo GeneratedSolidHandle cho host Slab " + host.Id + ".");
            if (host.IsGeneratedSolidStale())
                throw new InvalidOperationException("slabOpen auto-build tạo host Slab nhưng geometry vẫn stale: " + host.Id + ".");
        }

        private static IReadOnlyList<Point3d>? AcquirePath(Document document, string label)
        {
            var editor = document.Editor;
            var promptUnit = (object)CadUnitService.GetLengthUnit(document);
            var promptUcs = editor.CurrentUserCoordinateSystem;
            var points = new List<Point3d>();
            while (true)
            {
                var prompt = points.Count == 0
                    ? "\n" + label + " - chọn điểm đầu footprint: "
                    : "\n" + label + " - chọn điểm tiếp theo" + (points.Count >= 3 ? " hoặc Enter để đóng" : string.Empty) + ": ";
                var options = new PromptPointOptions(prompt) { AllowNone = points.Count >= 3 };
                if (points.Count > 0)
                {
                    options.UseBasePoint = true;
                    options.BasePoint = points[points.Count - 1];
                }

                var result = editor.GetPoint(options);
                if (result.Status == PromptStatus.None && points.Count >= 3) break;
                if (result.Status != PromptStatus.OK) return null;
                if (points.Count > 0 && result.Value.DistanceTo(points[points.Count - 1]) <= 1e-9d) continue;
                points.Add(result.Value);
            }

            if (points.Count >= 3 && points[0].DistanceTo(points[points.Count - 1]) <= 1e-9d)
                points.RemoveAt(points.Count - 1);
            if (points.Count < 3) return null;
            RequirePromptContextUnchanged(document, promptUnit, promptUcs, label);
            ValidatePlanView(document, points, label);
            PlanBoundsMeters(document, points);
            return points;
        }

        private static ObjectId CreatePolyline(Document document, IReadOnlyList<Point3d> points)
        {
            ValidatePlanView(document, points, "slabOpen source");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var polyline = new Polyline();
                polyline.SetDatabaseDefaults(document.Database);
                polyline.Elevation = points[0].Z;
                for (var index = 0; index < points.Count; index++)
                    polyline.AddVertexAt(index, new Point2d(points[index].X, points[index].Y), 0d, 0d, 0d);
                polyline.Closed = true;
                polyline.TransformBy(document.Editor.CurrentUserCoordinateSystem);
                var id = modelSpace.AppendEntity(polyline);
                transaction.AddNewlyCreatedDBObject(polyline, true);
                transaction.Commit();
                return id;
            }
        }

        private static (double WidthM, double DepthM) PlanBoundsMeters(Document document, IReadOnlyList<Point3d> points)
        {
            var minX = points.Min(point => point.X);
            var maxX = points.Max(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxY = points.Max(point => point.Y);
            var widthDrawing = CadGeometryGuard.Subtract(maxX, minX, "slabOpen/bounds width");
            var depthDrawing = CadGeometryGuard.Subtract(maxY, minY, "slabOpen/bounds depth");
            var widthM = CadGeometryGuard.Positive(
                CadGeometryGuard.ToMeters(document, widthDrawing, "slabOpen/WidthM"),
                "slabOpen/WidthM");
            var depthM = CadGeometryGuard.Positive(
                CadGeometryGuard.ToMeters(document, depthDrawing, "slabOpen/DepthM"),
                "slabOpen/DepthM");
            return (widthM, depthM);
        }

        private static void ValidatePlanView(Document document, IReadOnlyList<Point3d> points, string label)
        {
            if (points == null || points.Count == 0) throw new InvalidOperationException(label + " không có điểm.");
            var baseZ = CadGeometryGuard.Finite(points[0].Z, label + "/base Z");
            for (var index = 1; index < points.Count; index++)
            {
                var deltaDrawing = Math.Abs(CadGeometryGuard.Subtract(points[index].Z, baseZ, label + "/delta Z"));
                var deltaM = Math.Abs(CadGeometryGuard.ToMeters(document, deltaDrawing, label + "/delta Z"));
                if (deltaM > PlanarityToleranceM)
                    throw new InvalidOperationException(label + " yêu cầu plan-view |ΔZ| <= 0.005 m.");
            }
        }

        private static double FamilyPositiveNumber(ProjectFamily family, string key, double fallback)
        {
            if (family.Properties.TryGetValue(key, out var raw))
            {
                if (string.IsNullOrWhiteSpace(raw) ||
                    !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                    double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                    throw new InvalidOperationException("Family slabOpen/" + key + " phải là số hữu hạn > 0.");
                return value;
            }
            return fallback;
        }

        private static double? PromptPositiveMeters(Editor editor, string label, double defaultValue)
        {
            var options = new PromptDoubleOptions("\n" + label + ": ")
            {
                AllowNegative = false,
                AllowZero = false,
                AllowNone = true,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK && result.Status != PromptStatus.None) return null;
            var value = result.Status == PromptStatus.OK ? result.Value : defaultValue;
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new InvalidOperationException(label + " phải là số hữu hạn > 0.");
            return value;
        }

        private static void RequirePromptContextUnchanged(Document document, object promptUnit, Matrix3d promptUcs, string operation)
        {
            EnsureActive(document, operation + " / prompt freshness");
            RequireModelSpace(document);
            if (!Equals(CadUnitService.GetLengthUnit(document), promptUnit))
                throw new InvalidOperationException("Drawing unit policy đã thay đổi trong lúc chọn slabOpen footprint. Hãy chạy lại.");
            if (!document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs))
                throw new InvalidOperationException("Current UCS đã thay đổi trong lúc chọn slabOpen footprint. Hãy chạy lại.");
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                if (!document.Database.CurrentSpaceId.Equals(modelSpaceId))
                    throw new InvalidOperationException("slabOpen Direct Draw chỉ hỗ trợ Model Space.");
                transaction.Commit();
            }
            RequireSupportedUcs(document);
        }

        private static void RequireSupportedUcs(Document document)
        {
            var zAxis = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("Current UCS có Z axis không hợp lệ.");
            var x = zAxis.X / length;
            var y = zAxis.Y / length;
            var z = zAxis.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("slabOpen chỉ hỗ trợ UCS có XY song song WCS XY; UCS nghiêng/3D chưa được hỗ trợ.");
        }

        private static void EraseSource(Document document, ObjectId sourceId)
        {
            if (sourceId.IsNull || !sourceId.IsValid) return;
            var handle = sourceId.Handle.ToString();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var source = transaction.GetObject(sourceId, OpenMode.ForWrite, true) as Entity;
                if (source != null && !source.IsErased) source.Erase(true);
                transaction.Commit();
            }
            if (CadHandleService.GetLiveHandles(document, new[] { handle }).Count > 0)
                throw new InvalidOperationException("Rollback còn slabOpen source CAD chưa xóa: " + handle + ".");
        }

        private static void FinalizeUi(
            Document document,
            ObjectId sourceId,
            ProjectElement opening,
            ProjectElement host,
            double clearanceM)
        {
            var status = "slabOpen: host=" + host.Id + " • clearance=" +
                clearanceM.ToString("0.###", CultureInfo.InvariantCulture) +
                " m • negative-Z BoolSubtract applied • semantic=" + opening.Id + ".";
            try
            {
                PaletteCoordinator.RefreshProject();
                if (!sourceId.IsNull && sourceId.IsValid) document.Editor.SetImpliedSelection(new[] { sourceId });
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\nQS3D " + status + " UI sync warning: " + ex.Message); }
                catch { }
            }
        }

        private static void EnsureActive(Document document, string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException(operation + " yêu cầu DWG bắt đầu lệnh vẫn là bản vẽ active.");
        }

        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\n" + operation + " lỗi: " + ex.Message); }
                catch { }
                try { PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); }
                catch { }
            }
        }
    }
}
