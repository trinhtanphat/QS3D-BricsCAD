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
    /// created semantic opening; physical boolean cutting remains an explicit QS3DCUTOPENINGS
    /// operation until a targeted-cut transaction is available.
    /// </summary>
    public sealed class DirectDrawOpeningCommands
    {
        private const double PlanarityToleranceM = 0.005d;

        [CommandMethod("QS3DDRAWDOOR", CommandFlags.Modal)]
        public void DrawDoor() => DrawOpening(ElementCategory.Door, "Cửa Đi", defaultSillM: 0d);

        [CommandMethod("QS3DDRAWOPENING", CommandFlags.Modal)]
        public void DrawWallOpening() => DrawOpening(ElementCategory.WallOpening, "Lỗ Mở Vách", defaultSillM: 0d);

        private static void DrawOpening(ElementCategory category, string label, double defaultSillM)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            Guard(document, "QS3DDRAW" + (category == ElementCategory.Door ? "DOOR" : "OPENING"), () =>
            {
                RequireModelSpace(document);
                var points = AcquireTwoPoints(document, label);
                if (points == null) return;

                var widthDrawing = CadGeometryGuard.Hypot(
                    CadGeometryGuard.Subtract(points[1].X, points[0].X, label + "/dx"),
                    CadGeometryGuard.Subtract(points[1].Y, points[0].Y, label + "/dy"),
                    label + "/plan width");
                var widthM = CadGeometryGuard.Positive(CadGeometryGuard.ToMeters(document, widthDrawing, label + "/width"), label + "/WidthM");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                var heightDefault = FamilyPositiveNumber(project, category, "HeightM", 2.2d);
                var heightM = PromptPositiveMeters(document.Editor, "Chiều cao " + label + " (m)", heightDefault);
                if (!heightM.HasValue) return;

                var bottomOffsetDefault = FamilyNonNegativeNumber(project, category, "BottomOffsetM", defaultSillM);
                var sillDefault = FamilyNonNegativeNumber(project, category, "SillHeightM", bottomOffsetDefault);
                var sillM = PromptNonNegativeMeters(document.Editor, "Cao độ bậu " + label + " so với đáy host (m)", sillDefault);
                if (!sillM.HasValue) return;

                var clearanceDefault = FamilyNonNegativeNumber(project, category, "BooleanClearanceM", 0.01d);
                var clearanceM = PromptNonNegativeMeters(document.Editor, "Khe hở boolean (m)", clearanceDefault);
                if (!clearanceM.HasValue) return;

                Execute(document, category, label, points[0], points[1], widthM, heightM.Value, sillM.Value, clearanceM.Value);
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
            double clearanceM)
        {
            EnsureActive(document, "Direct Draw " + label);
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceId = ObjectId.Null;
            var sourceHandle = string.Empty;
            ProjectElement? createdElement = null;
            var hostId = string.Empty;
            var regeneratedBeforeLink = 0;
            var regeneratedAfterLink = 0;

            try
            {
                sourceId = CreateLine(document, start, end);
                if (sourceId.IsNull || !sourceId.IsValid) throw new InvalidOperationException("Không tạo được CAD source cho Direct Draw " + label + ".");
                sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, category);
                if (captured != 1) throw new InvalidOperationException("Direct Draw " + label + " cần capture đúng một semantic element, nhận được " + captured + ".");

                createdElement = project.Elements.SingleOrDefault(x =>
                    x.Category == category && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (createdElement == null) throw new InvalidOperationException("Không tìm thấy semantic " + label + " vừa tạo cho source " + sourceHandle + ".");

                createdElement.SetProperty("WidthM", widthM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("HeightM", heightM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("SillHeightM", sillM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("BottomOffsetM", sillM.ToString("R", CultureInfo.InvariantCulture));
                createdElement.SetProperty("BooleanClearanceM", clearanceM.ToString("R", CultureInfo.InvariantCulture));

                regeneratedBeforeLink = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                // AutoHostLinkCommands resolves the active document internally. Re-check immediately
                // before delegating and scope implied selection to this newly-created source only.
                EnsureActive(document, "Direct Draw " + label + " / QS3DAUTOLINKHOSTS");
                document.Editor.SetImpliedSelection(new[] { sourceId });
                new AutoHostLinkCommands().AutoLinkHosts();

                if (!createdElement.Properties.TryGetValue("HostWallId", out hostId) || string.IsNullOrWhiteSpace(hostId))
                    throw new InvalidOperationException(label + " chưa tìm được host duy nhất trong phạm vi Auto Host; operation được rollback để không tạo opening mồ côi.");

                // AutoHostLinkCommands intentionally catches command-surface errors. Re-run the
                // deterministic semantic engine so a link/regeneration failure propagates here.
                regeneratedAfterLink = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
                project.Touch();
            }
            catch (Exception operationError)
            {
                Exception? cleanupError = null;
                Exception? restoreError = null;
                try { EraseSource(document, sourceId, sourceHandle); }
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

            FinalizeUi(document, label, sourceId, widthM, hostId, regeneratedBeforeLink + regeneratedAfterLink);
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
            ValidatePlanView(document, new[] { start, end }, "Opening LINE");
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var line = new Line(start, end);
                line.SetDatabaseDefaults(document.Database);
                var id = modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line, true);
                transaction.Commit();
                return id;
            }
        }

        private static void EraseSource(Document document, ObjectId sourceId, string sourceHandle)
        {
            if (sourceId.IsNull || !sourceId.IsValid)
            {
                if (string.IsNullOrWhiteSpace(sourceHandle)) return;
                throw new InvalidOperationException("Rollback mất ObjectId của source " + sourceHandle + "; từ chối erase theo textual handle để tránh xóa nhầm CAD.");
            }

            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var entity = transaction.GetObject(sourceId, OpenMode.ForWrite, true) as Entity;
                if (entity == null) throw new InvalidOperationException("Rollback source " + sourceHandle + " không còn là Entity hợp lệ.");
                if (!entity.IsErased) entity.Erase(true);
                transaction.Commit();
            }

            var handle = string.IsNullOrWhiteSpace(sourceHandle) ? sourceId.Handle.ToString() : sourceHandle.Trim();
            if (CadHandleService.GetLiveHandles(document, new[] { handle }).Count > 0)
                throw new InvalidOperationException("Rollback còn source CAD chưa xóa: " + handle + ".");
        }

        private static void FinalizeUi(Document document, string label, ObjectId sourceId, double widthM, string hostId, int regenerated)
        {
            var status = label + ": width=" + widthM.ToString("0.###", CultureInfo.InvariantCulture) +
                " m • host=" + hostId + " • regen=" + regenerated +
                ". Semantic + Auto Host hoàn tất; dùng QS3DCUTOPENINGS khi muốn khoét physical host.";
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

        private static double? PromptPositiveMeters(Editor editor, string label, double defaultValue)
        {
            if (double.IsNaN(defaultValue) || double.IsInfinity(defaultValue) || defaultValue <= 0d)
                throw new InvalidOperationException(label + " default phải là số hữu hạn > 0.");
            var options = new PromptDoubleOptions("\n" + label + " <" + defaultValue.ToString("0.###", CultureInfo.InvariantCulture) + ">: ")
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
            var options = new PromptDoubleOptions("\n" + label + " <" + defaultValue.ToString("0.###", CultureInfo.InvariantCulture) + ">: ")
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
                var message = operation + " lỗi: " + ex.Message;
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\n" + message);
            }
        }
    }
}
