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
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Window authoring reuses the canonical WallOpening host/boolean contract and marks the
    /// instance with OpeningUsage=Window instead of adding a parallel Window category.
    /// </summary>
    public sealed class DirectDrawWindowCommands
    {
        private const double PlanarityToleranceM = 0.005d;
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DDRAWWINDOW", CommandFlags.Modal)]
        public void DrawWindow() => DrawWindowCore(promptParameters: false, operation: "QS3DDRAWWINDOW");

        [CommandMethod("QS3DDRAWWINDOWADV", CommandFlags.Modal)]
        public void DrawWindowAdvanced() => DrawWindowCore(promptParameters: true, operation: "QS3DDRAWWINDOWADV");

        private static void DrawWindowCore(bool promptParameters, string operation)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            Guard(document, operation, () =>
            {
                EnsureActive(document, operation);
                RequireModelSpace(document);
                var promptUnit = CadUnitService.GetLengthUnit(document);
                var promptUcs = document.Editor.CurrentUserCoordinateSystem;
                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasProjectBeforePrompts = projectPreview.HasProject;
                var expectedProjectChangeVersion = hasProjectBeforePrompts ? (long?)defaultsProject!.ChangeVersion : null;

                var points = AcquireTwoPoints(document);
                if (points == null) return;

                var widthDrawing = CadGeometryGuard.Hypot(
                    CadGeometryGuard.Subtract(points[1].X, points[0].X, "Cửa Sổ/dx"),
                    CadGeometryGuard.Subtract(points[1].Y, points[0].Y, "Cửa Sổ/dy"),
                    "Cửa Sổ/plan width");
                var widthM = CadGeometryGuard.Positive(
                    CadGeometryGuard.ToMeters(document, widthDrawing, "Cửa Sổ/width"),
                    "Cửa Sổ/WidthM");

                var heightDefault = hasProjectBeforePrompts ? FamilyWindowNumber(defaultsProject!, "WindowHeightM", 1.2d, positive: true) : 1.2d;
                var sillDefault = hasProjectBeforePrompts ? FamilyWindowNumber(defaultsProject!, "WindowSillHeightM", 0.9d, positive: false) : 0.9d;
                var clearanceDefault = hasProjectBeforePrompts ? FamilyWindowNumber(defaultsProject!, "BooleanClearanceM", 0.01d, positive: false) : 0.01d;

                var heightM = heightDefault;
                var sillM = sillDefault;
                var clearanceM = clearanceDefault;
                if (promptParameters)
                {
                    var promptedHeight = PromptPositiveMeters(document.Editor, "Chiều cao Cửa Sổ (m)", heightDefault);
                    if (!promptedHeight.HasValue) return;
                    heightM = promptedHeight.Value;
                    var promptedSill = PromptNonNegativeMeters(document.Editor, "Cao độ bậu Cửa Sổ so với đáy host (m)", sillDefault);
                    if (!promptedSill.HasValue) return;
                    sillM = promptedSill.Value;
                    var promptedClearance = PromptNonNegativeMeters(document.Editor, "Khe hở boolean (m)", clearanceDefault);
                    if (!promptedClearance.HasValue) return;
                    clearanceM = promptedClearance.Value;
                }
                else
                {
                    document.Editor.WriteMessage(
                        "\nQS3D Cửa Sổ nhanh: width theo 2 điểm, dùng Family WallOpening hiện tại (cao " +
                        heightM.ToString("0.###", CultureInfo.InvariantCulture) + " m, bậu " +
                        sillM.ToString("0.###", CultureInfo.InvariantCulture) + " m, clearance " +
                        clearanceM.ToString("0.###", CultureInfo.InvariantCulture) +
                        " m). Dùng QS3DDRAWWINDOWADV khi cần nhập tham số riêng.");
                }

                EnsureActive(document, operation + " / prompt freshness");
                RequireModelSpace(document);
                if (!document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs))
                    throw new InvalidOperationException("Current UCS đã thay đổi trong lúc nhập Cửa Sổ. Hãy chạy lại lệnh.");
                if (CadUnitService.GetLengthUnit(document) != promptUnit)
                    throw new InvalidOperationException("Drawing unit policy đã thay đổi trong lúc nhập Cửa Sổ. Hãy chạy lại lệnh.");

                var project = BindProjectAfterPrompts(document, projectPreview, expectedProjectChangeVersion, operation);
                Execute(document, project, hasProjectBeforePrompts, points[0], points[1], widthM, heightM, sillM, clearanceM);
            });
        }

        private static void Execute(Document document, ProjectState project, bool projectExistedBeforeAuthoring, Point3d start, Point3d end, double widthM, double heightM, double sillM, double clearanceM)
        {
            EnsureActive(document, "Direct Draw Cửa Sổ");
            RequireExactProject(document, project, "Direct Draw Cửa Sổ");
            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            ProjectElement? createdElement = null;
            var hostId = string.Empty;
            var regenerated = 0;

            try
            {
                sourceId = CreateLine(document, start, end);
                if (sourceId.IsNull || !sourceId.IsValid)
                    throw new InvalidOperationException("Không tạo được CAD source cho Direct Draw Cửa Sổ.");
                var sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, ElementCategory.WallOpening);
                if (captured != 1)
                    throw new InvalidOperationException("Direct Draw Cửa Sổ cần capture đúng một WallOpening semantic, nhận được " + captured + ".");

                createdElement = project.Elements.SingleOrDefault(x =>
                    x.Category == ElementCategory.WallOpening &&
                    x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (createdElement == null)
                    throw new InvalidOperationException("Không tìm thấy WallOpening semantic vừa tạo cho Cửa Sổ source " + sourceHandle + ".");
                var createdElementId = createdElement.Id;

                createdElement.SetProperty("OpeningUsage", "Window");
                createdElement.SetProperty("WidthM", widthM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("HeightM", heightM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("SillHeightM", sillM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("BottomOffsetM", sillM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("BooleanClearanceM", clearanceM.ToString("R", CultureInfo.InvariantCulture));

                var regenerator = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
                regenerated += regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id });

                EnsureActive(document, "Direct Draw Cửa Sổ / Auto Host");
                RequireExactProject(document, project, "Direct Draw Cửa Sổ / Auto Host");
                hostId = AutoHostLinkCommands.LinkSingleOpening(document, project, createdElement.Id);
                EnsureActive(document, "Direct Draw Cửa Sổ / post Auto Host");
                RequireExactProject(document, project, "Direct Draw Cửa Sổ / post Auto Host");

                createdElement = project.Elements.SingleOrDefault(x =>
                    string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase));
                if (createdElement == null)
                    throw new InvalidOperationException("Cửa Sổ vừa tạo không còn tồn tại sau Auto Host; operation được rollback.");
                if (!createdElement.Properties.TryGetValue("HostWallId", out var recordedHostId) ||
                    string.IsNullOrWhiteSpace(recordedHostId) ||
                    !string.Equals(recordedHostId.Trim(), hostId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Cửa Sổ chưa tìm được host duy nhất; operation được rollback để không tạo opening mồ côi.");
                var host = project.FindElement(hostId) ??
                    throw new InvalidOperationException("Host Cửa Sổ vừa liên kết không còn tồn tại: " + hostId + ".");

                regenerated += regenerator.RegenerateDirtySubset(project, new[] { createdElement.Id, host.Id });
                project.Touch();
            }
            catch (Exception operationError)
            {
                Exception? cleanupError = null;
                Exception? restoreError = null;
                try { EraseSource(document, sourceId); }
                catch (Exception ex) { cleanupError = ex; }
                try { rollback.Restore(project); }
                catch (Exception ex) { restoreError = ex; }
                if (!projectExistedBeforeAuthoring) ProjectContextCoordinator.Forget(document);
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
                catch { }

                if (cleanupError != null || restoreError != null)
                {
                    var errors = new List<Exception> { operationError };
                    if (cleanupError != null) errors.Add(cleanupError);
                    if (restoreError != null) errors.Add(restoreError);
                    throw new InvalidOperationException(
                        "Direct Draw Cửa Sổ thất bại và rollback không hoàn tất đầy đủ.",
                        new AggregateException(errors));
                }
                throw;
            }

            FinalizeUi(document, sourceId, widthM, hostId, regenerated);
        }

        private static ProjectState BindProjectAfterPrompts(
            Document document,
            DirectDrawProjectPreviewContext projectPreview,
            long? expectedProjectChangeVersion,
            string operation)
        {
            EnsureActive(document, operation + " / project freshness");
            var project = projectPreview.ResolveForMutation(document, operation);
            if (projectPreview.HasProject)
            {
                if (!expectedProjectChangeVersion.HasValue ||
                    project.ChangeVersion != expectedProjectChangeVersion.Value)
                    throw new InvalidOperationException(
                        "QS3D project đã bị thay thế hoặc thay đổi trong lúc nhập Cửa Sổ. Hãy chạy lại lệnh để dùng đúng project defaults.");
            }
            return project;
        }

        private static void RequireExactProject(Document document, ProjectState expectedProject, string operation)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject) ||
                !ReferenceEquals(currentProject, expectedProject))
                throw new InvalidOperationException(
                    operation + " requires the exact canonical QS3D project authorized after prompts; project state was replaced.");
        }

        private static IReadOnlyList<Point3d>? AcquireTwoPoints(Document document)
        {
            var first = document.Editor.GetPoint(new PromptPointOptions("\nCửa Sổ - chọn mép thứ nhất: "));
            if (first.Status != PromptStatus.OK) return null;
            var second = document.Editor.GetPoint(new PromptPointOptions("\nCửa Sổ - chọn mép thứ hai: ")
            {
                UseBasePoint = true,
                BasePoint = first.Value
            });
            if (second.Status != PromptStatus.OK) return null;

            var points = new[] { first.Value, second.Value };
            ValidatePlanView(document, points);
            var dx = CadGeometryGuard.Subtract(second.Value.X, first.Value.X, "Cửa Sổ/dx");
            var dy = CadGeometryGuard.Subtract(second.Value.Y, first.Value.Y, "Cửa Sổ/dy");
            if (CadGeometryGuard.Hypot(dx, dy, "Cửa Sổ/plan width") <= 1e-9d)
                throw new InvalidOperationException("Cửa Sổ có bề rộng plan bằng 0.");
            return points;
        }

        private static void ValidatePlanView(Document document, IReadOnlyList<Point3d> points)
        {
            var baseZ = CadGeometryGuard.Finite(points[0].Z, "Cửa Sổ/base Z");
            for (var index = 1; index < points.Count; index++)
            {
                var delta = Math.Abs(CadGeometryGuard.Subtract(points[index].Z, baseZ, "Cửa Sổ/delta Z"));
                if (Math.Abs(CadGeometryGuard.ToMeters(document, delta, "Cửa Sổ/delta Z")) > PlanarityToleranceM)
                    throw new InvalidOperationException("Cửa Sổ Direct Draw yêu cầu plan-view |ΔZ| <= 0.005 m.");
            }
        }

        private static ObjectId CreateLine(Document document, Point3d start, Point3d end)
        {
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var line = new Line(start, end);
                line.SetDatabaseDefaults(document.Database);
                line.TransformBy(document.Editor.CurrentUserCoordinateSystem);
                var id = modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line, true);
                transaction.Commit();
                return id;
            }
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
                throw new InvalidOperationException("Rollback còn source CAD Cửa Sổ chưa xóa: " + handle + ".");
        }

        private static double FamilyWindowNumber(ProjectState project, string key, double fallback, bool positive)
        {
            var family = PreferredFamily(project);
            if (family == null || !family.Properties.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw)) return fallback;
            if (!double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException("WallOpening/" + key + " không phải số hữu hạn hợp lệ.");
            if (positive ? value <= 0d : value < 0d)
                throw new InvalidOperationException("WallOpening/" + key + (positive ? " phải > 0." : " phải >= 0."));
            return value;
        }

        private static ProjectFamily? PreferredFamily(ProjectState project)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == ElementCategory.WallOpening) return active;
            }
            return project.Families.FirstOrDefault(x => x.Category == ElementCategory.WallOpening);
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
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new InvalidOperationException(label + " phải là số hữu hạn > 0.");
            return value;
        }

        private static double? PromptNonNegativeMeters(Editor editor, string label, double defaultValue)
        {
            var options = new PromptDoubleOptions("\n" + label + ": ")
            {
                AllowNegative = false,
                AllowZero = true,
                AllowNone = true,
                DefaultValue = defaultValue,
                UseDefaultValue = true
            };
            var result = editor.GetDouble(options);
            if (result.Status == PromptStatus.Cancel) return null;
            if (result.Status != PromptStatus.OK && result.Status != PromptStatus.None) return null;
            var value = result.Status == PromptStatus.OK ? result.Value : defaultValue;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(label + " phải là số hữu hạn >= 0.");
            return value;
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                if (!document.Database.CurrentSpaceId.Equals(blockTable[BlockTableRecord.ModelSpace]))
                    throw new InvalidOperationException("Direct Draw Cửa Sổ hiện chỉ hỗ trợ Model Space.");
                transaction.Commit();
            }

            var coordinateSystem = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
            var zAxis = coordinateSystem.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("Current UCS có Z axis không hợp lệ.");
            var x = zAxis.X / length;
            var y = zAxis.Y / length;
            var z = zAxis.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("Direct Draw Cửa Sổ yêu cầu UCS có XY song song WCS XY.");
        }

        private static void FinalizeUi(Document document, ObjectId sourceId, double widthM, string hostId, int regenerated)
        {
            var status = "Cửa Sổ: width=" + widthM.ToString("0.###", CultureInfo.InvariantCulture) +
                " m • host=" + hostId + " • regen=" + regenerated +
                ". Semantic + Auto Host hoàn tất; dùng QS3DCUTSELECTEDOPENINGS để khoét host khi sẵn sàng.";
            try
            {
                EnsureActive(document, "Direct Draw Cửa Sổ / UI sync");
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
                throw new InvalidOperationException(operation + " yêu cầu đúng DWG đã bắt đầu lệnh vẫn là bản vẽ active.");
        }

        private static void Guard(Document document, string operation, Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                try { document.Editor.WriteMessage("\n" + operation + " lỗi: " + ex.Message); } catch { }
                try { PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); } catch { }
            }
        }
    }
}