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
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Host-aware Direct Draw for Door / WallOpening. The picked LINE is the real DWG source
    /// and its plan length is authoritative WidthM. The command auto-links only the newly
    /// created semantic opening; physical boolean cutting remains an explicit user action.
    /// </summary>
    public sealed class DirectDrawOpeningCommands
    {
        private const double PlanarityToleranceM = 0.005d;
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DDRAWDOOR", CommandFlags.Modal)]
        public void DrawDoor() => DrawOpening(ElementCategory.Door, "Cửa Đi", defaultSillM: 0d, promptParameters: false, operation: "QS3DDRAWDOOR");

        [CommandMethod("QS3DDRAWDOORADV", CommandFlags.Modal)]
        public void DrawDoorAdvanced() => DrawOpening(ElementCategory.Door, "Cửa Đi", defaultSillM: 0d, promptParameters: true, operation: "QS3DDRAWDOORADV");

        [CommandMethod("QS3DDRAWOPENING", CommandFlags.Modal)]
        public void DrawWallOpening() => DrawOpening(ElementCategory.WallOpening, "Lỗ Mở Vách", defaultSillM: 0d, promptParameters: false, operation: "QS3DDRAWOPENING");

        [CommandMethod("QS3DDRAWOPENINGADV", CommandFlags.Modal)]
        public void DrawWallOpeningAdvanced() => DrawOpening(ElementCategory.WallOpening, "Lỗ Mở Vách", defaultSillM: 0d, promptParameters: true, operation: "QS3DDRAWOPENINGADV");

        private static void DrawOpening(ElementCategory category, string label, double defaultSillM, bool promptParameters, string operation)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            Guard(document, operation, () =>
            {
                RequireModelSpace(document);
                var promptUnit = (object)CadUnitService.GetLengthUnit(document);
                var promptUcs = document.Editor.CurrentUserCoordinateSystem;
                var points = AcquireTwoPoints(document, label + (promptParameters ? " tùy chỉnh" : " nhanh"));
                if (points == null) return;
                RequirePromptContextUnchanged(document, promptUnit, promptUcs, operation);

                var widthDrawing = CadGeometryGuard.Hypot(
                    CadGeometryGuard.Subtract(points[1].X, points[0].X, label + "/dx"),
                    CadGeometryGuard.Subtract(points[1].Y, points[0].Y, label + "/dy"),
                    label + "/plan width");
                var widthM = CadGeometryGuard.Positive(CadGeometryGuard.ToMeters(document, widthDrawing, label + "/width"), label + "/WidthM");

                var projectPreview = DirectDrawProjectPreviewContext.Capture(document);
                var defaultsProject = projectPreview.DefaultsProject;
                var hasDefaultsProject = projectPreview.HasProject;
                var heightDefault = hasDefaultsProject
                    ? FamilyPositiveNumber(defaultsProject!, category, "HeightM", 2.2d)
                    : 2.2d;
                var bottomOffsetDefault = hasDefaultsProject
                    ? FamilyNonNegativeNumber(defaultsProject!, category, "BottomOffsetM", defaultSillM)
                    : defaultSillM;
                var sillDefault = hasDefaultsProject
                    ? FamilyNonNegativeNumber(defaultsProject!, category, "SillHeightM", bottomOffsetDefault)
                    : bottomOffsetDefault;
                var clearanceDefault = hasDefaultsProject
                    ? FamilyNonNegativeNumber(defaultsProject!, category, "BooleanClearanceM", 0.01d)
                    : 0.01d;

                var heightM = heightDefault;
                var sillM = sillDefault;
                var clearanceM = clearanceDefault;
                if (promptParameters)
                {
                    var promptedHeight = PromptPositiveMeters(document.Editor, "Chiều cao " + label + " (m)", heightDefault);
                    if (!promptedHeight.HasValue) return;
                    heightM = promptedHeight.Value;

                    var promptedSill = PromptNonNegativeMeters(document.Editor, "Cao độ bậu " + label + " so với đáy host (m)", sillDefault);
                    if (!promptedSill.HasValue) return;
                    sillM = promptedSill.Value;

                    var promptedClearance = PromptNonNegativeMeters(document.Editor, "Khe hở boolean (m)", clearanceDefault);
                    if (!promptedClearance.HasValue) return;
                    clearanceM = promptedClearance.Value;
                }
                else
                {
                    document.Editor.WriteMessage(
                        "\nQS3D " + label + " nhanh: width theo 2 điểm, dùng Family hiện tại (cao " +
                        heightM.ToString("0.###", CultureInfo.InvariantCulture) + " m, bậu " +
                        sillM.ToString("0.###", CultureInfo.InvariantCulture) + " m, clearance " +
                        clearanceM.ToString("0.###", CultureInfo.InvariantCulture) + " m). Dùng " +
                        (category == ElementCategory.Door ? "QS3DDRAWDOORADV" : "QS3DDRAWOPENINGADV") +
                        " khi cần nhập tham số riêng.");
                }

                Execute(document, category, label, points[0], points[1], widthM, heightM, sillM, clearanceM, projectPreview);
            });
        }

        private static void Execute(
            Document document,
            ElementCategory category,
            string label,
            Point3d start,
            Point3d end,
            double widthM,
            double heightM,
            double sillM,
            double clearanceM,
            DirectDrawProjectPreviewContext projectPreview)
        {
            var operation = "Direct Draw " + label;
            EnsureActive(document, operation);
            var project = projectPreview.ResolveForMutation(document, operation);
            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            ProjectElement? createdElement = null;
            var hostId = string.Empty;
            var regenerated = 0;

            try
            {
                sourceId = CreateLine(document, start, end);
                if (sourceId.IsNull || !sourceId.IsValid) throw new InvalidOperationException("Không tạo được CAD source cho Direct Draw " + label + ".");
                var sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, category);
                if (captured != 1) throw new InvalidOperationException("Direct Draw " + label + " cần capture đúng một semantic element, nhận được " + captured + ".");

                createdElement = project.Elements.SingleOrDefault(x =>
                    x.Category == category && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (createdElement == null) throw new InvalidOperationException("Không tìm thấy semantic " + label + " vừa tạo cho source " + sourceHandle + ".");
                var createdElementId = createdElement.Id;

                createdElement.SetProperty("WidthM", widthM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("HeightM", heightM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("SillHeightM", sillM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("BottomOffsetM", sillM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("BooleanClearanceM", clearanceM.ToString("R", CultureInfo.InvariantCulture));

                regenerated += new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                    .RegenerateDirtySubset(project, new[] { createdElementId });

                // QS3DAUTOLINKHOSTS resolves the active document internally. Re-check immediately
                // before delegating and keep only the newly-created source selected so no unrelated
                // Door/Opening can be re-hosted by this Direct Draw operation.
                EnsureActive(document, "Direct Draw " + label + " / Auto Host");
                document.Editor.SetImpliedSelection(new[] { sourceId });
                new AutoHostLinkCommands().AutoLinkHosts();
                EnsureActive(document, "Direct Draw " + label + " / post Auto Host");

                // AutoHost may rollback its ProjectState snapshot and command-surface errors are
                // intentionally swallowed there. Never trust the pre-AutoHost element reference:
                // resolve the canonical element again from the current project by stable Id.
                createdElement = project.Elements.SingleOrDefault(x =>
                    string.Equals(x.Id, createdElementId, StringComparison.OrdinalIgnoreCase));
                if (createdElement == null)
                    throw new InvalidOperationException(label + " vừa tạo không còn tồn tại sau Auto Host; operation được rollback.");

                if (!createdElement.Properties.TryGetValue("HostWallId", out hostId) || string.IsNullOrWhiteSpace(hostId))
                    throw new InvalidOperationException(label + " chưa tìm được host duy nhất trong phạm vi Auto Host; operation được rollback để không tạo opening mồ côi.");

                // AutoHostLinkCommands catches its command-surface failures. Keep the deterministic
                // second pass inside the created opening + resolved host scope so unrelated dirty
                // project elements remain untouched by one Direct Draw operation.
                regenerated += new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                    .RegenerateDirtySubset(project, new[] { createdElementId, hostId });
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
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
                catch { }

                if (cleanupError != null || restoreError != null)
                {
                    var errors = new List<Exception> { operationError };
                    if (cleanupError != null) errors.Add(cleanupError);
                    if (restoreError != null) errors.Add(restoreError);
                    throw new InvalidOperationException("Direct Draw " + label + " thất bại và rollback không hoàn tất đầy đủ.", new AggregateException(errors));
                }
                throw;
            }

            FinalizeUi(document, sourceId, label, widthM, hostId, regenerated);
        }

        private static IReadOnlyList<Point3d>? AcquireTwoPoints(Document document, string label)
        {
            var editor = document.Editor;
            var first = editor.GetPoint(new PromptPointOptions("\n" + label + " - chọn mép thứ nhất: "));
            if (first.Status != PromptStatus.OK) return null;
            var secondOptions = new PromptPointOptions("\n" + label + " - chọn mép thứ hai: ")
            {
                UseBasePoint = true,
                BasePoint = first.Value
            };
            var second = editor.GetPoint(secondOptions);
            if (second.Status != PromptStatus.OK) return null;
            var points = new[] { first.Value, second.Value };
            ValidatePlanView(document, points, label);
            var dx = CadGeometryGuard.Subtract(second.Value.X, first.Value.X, label + "/dx");
            var dy = CadGeometryGuard.Subtract(second.Value.Y, first.Value.Y, label + "/dy");
            if (CadGeometryGuard.Hypot(dx, dy, label + "/plan width") <= 1e-9d)
                throw new InvalidOperationException(label + " có bề rộng plan bằng 0.");
            return points;
        }

        private static void ValidatePlanView(Document document, IReadOnlyList<Point3d> points, string label)
        {
            var baseZ = CadGeometryGuard.Finite(points[0].Z, label + "/base Z");
            for (var index = 1; index < points.Count; index++)
            {
                var deltaDrawing = Math.Abs(CadGeometryGuard.Subtract(points[index].Z, baseZ, label + "/delta Z"));
                var deltaM = Math.Abs(CadGeometryGuard.ToMeters(document, deltaDrawing, label + "/delta Z"));
                if (deltaM > PlanarityToleranceM)
                    throw new InvalidOperationException(label + " Direct Draw yêu cầu plan-view |ΔZ| <= 0.005 m.");
            }
        }

        private static ObjectId CreateLine(Document document, Point3d start, Point3d end)
        {
            var safeStart = new Point3d(
                CadGeometryGuard.Finite(start.X, "Direct Draw opening start X"),
                CadGeometryGuard.Finite(start.Y, "Direct Draw opening start Y"),
                CadGeometryGuard.Finite(start.Z, "Direct Draw opening start Z"));
            var safeEnd = new Point3d(
                CadGeometryGuard.Finite(end.X, "Direct Draw opening end X"),
                CadGeometryGuard.Finite(end.Y, "Direct Draw opening end Y"),
                CadGeometryGuard.Finite(end.Z, "Direct Draw opening end Z"));
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var line = new Line(safeStart, safeEnd);
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
            if (document == null) throw new ArgumentNullException(nameof(document));
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
                throw new InvalidOperationException("Rollback còn source CAD chưa xóa: " + handle + ".");
        }

        private static double? PromptPositiveMeters(Editor editor, string label, double defaultValue)
        {
            if (double.IsNaN(defaultValue) || double.IsInfinity(defaultValue) || defaultValue <= 0d)
                throw new InvalidOperationException(label + " default phải là số hữu hạn > 0.");
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
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d) throw new InvalidOperationException(label + " phải là số hữu hạn > 0.");
            return value;
        }

        private static double? PromptNonNegativeMeters(Editor editor, string label, double defaultValue)
        {
            if (double.IsNaN(defaultValue) || double.IsInfinity(defaultValue) || defaultValue < 0d)
                throw new InvalidOperationException(label + " default phải là số hữu hạn >= 0.");
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
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new InvalidOperationException(label + " phải là số hữu hạn >= 0.");
            return value;
        }

        private static double FamilyPositiveNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            var value = FamilyConfiguredNumber(project, category, key, fallback, out var configured);
            if (value > 0d) return value;
            if (!configured && fallback > 0d && !double.IsNaN(fallback) && !double.IsInfinity(fallback)) return fallback;
            throw new InvalidOperationException(category + "/" + key + " phải là số hữu hạn > 0. Sửa Family trước khi Direct Draw.");
        }

        private static double FamilyNonNegativeNumber(ProjectState project, ElementCategory category, string key, double fallback)
        {
            var value = FamilyConfiguredNumber(project, category, key, fallback, out var configured);
            if (value >= 0d) return value;
            if (!configured && fallback >= 0d && !double.IsNaN(fallback) && !double.IsInfinity(fallback)) return fallback;
            throw new InvalidOperationException(category + "/" + key + " phải là số hữu hạn >= 0. Sửa Family trước khi Direct Draw.");
        }

        private static double FamilyConfiguredNumber(ProjectState project, ElementCategory category, string key, double fallback, out bool configured)
        {
            configured = false;
            var family = PreferredFamily(project, category);
            if (family == null || !family.Properties.TryGetValue(key, out var raw)) return fallback;
            configured = true;
            if (string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(category + "/" + key + " không phải số hữu hạn hợp lệ. Sửa Family trước khi Direct Draw.");
            return value;
        }

        private static ProjectFamily? PreferredFamily(ProjectState project, ElementCategory category)
        {
            if (project.Metadata.TryGetValue("ActiveFamilyId", out var activeId))
            {
                var active = project.FindFamily(activeId);
                if (active != null && active.Category == category) return active;
            }
            return project.Families.FirstOrDefault(x => x.Category == category);
        }

        private static void RequirePromptContextUnchanged(Document document, object promptUnit, Matrix3d promptUcs, string operation)
        {
            EnsureActive(document, operation + " / geometry prompt freshness");
            RequireModelSpace(document);
            if (!Equals(CadUnitService.GetLengthUnit(document), promptUnit))
                throw new InvalidOperationException("Drawing unit policy đã thay đổi trong lúc chọn geometry cho " + operation + ". Hãy chạy lại lệnh.");
            if (!document.Editor.CurrentUserCoordinateSystem.Equals(promptUcs))
                throw new InvalidOperationException("Current UCS đã thay đổi trong lúc chọn geometry cho " + operation + ". Hãy chạy lại lệnh.");
        }

        private static void RequireModelSpace(Document document)
        {
            using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpaceId = blockTable[BlockTableRecord.ModelSpace];
                if (!document.Database.CurrentSpaceId.Equals(modelSpaceId))
                    throw new InvalidOperationException("Direct Draw Cửa/Lỗ mở hiện chỉ hỗ trợ Model Space; chuyển sang Model rồi chạy lại.");
                transaction.Commit();
            }
            RequireSupportedUcs(document);
        }

        private static void RequireSupportedUcs(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var coordinateSystem = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
            var zAxis = coordinateSystem.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("Current UCS có Z axis không hợp lệ.");

            var x = zAxis.X / length;
            var y = zAxis.Y / length;
            var z = zAxis.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("Direct Draw Cửa/Lỗ mở hiện chỉ hỗ trợ UCS có mặt phẳng XY song song WCS XY (có thể xoay/di chuyển trong mặt phẳng). UCS nghiêng/3D chưa được hỗ trợ.");
        }

        private static void FinalizeUi(Document document, ObjectId sourceId, string label, double widthM, string hostId, int regenerated)
        {
            var status = label + ": width=" + widthM.ToString("0.###", CultureInfo.InvariantCulture) +
                " m • host=" + hostId + " • regen=" + regenerated +
                ". Semantic + Auto Host hoàn tất; dùng QS3DCUTSELECTEDOPENINGS khi muốn khoét đúng Cửa/Lỗ đang chọn.";
            try
            {
                EnsureActive(document, "Direct Draw " + label + " / UI sync");
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
                try { document.Editor.WriteMessage("\n" + operation + " lỗi: " + ex.Message); }
                catch { }
                try { PaletteCoordinator.SetStatus(operation + " lỗi: " + ex.Message); }
                catch { }
            }
        }
    }
}