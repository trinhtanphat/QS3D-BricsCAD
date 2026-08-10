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
            var project = ProjectContextCoordinator.GetOrCreate(document);
            var rollback = ProjectStateSnapshot.Capture(project);
            var sourceHandle = string.Empty;
            try
            {
                var sourceId = CreateLine(document, start, end);
                sourceHandle = sourceId.Handle.ToString();
                document.Editor.SetImpliedSelection(new[] { sourceId });

                var captured = SemanticCaptureService.Capture(document, category);
                if (captured != 1) throw new InvalidOperationException("Direct Draw " + label + " cần capture đúng một semantic element, nhận được " + captured + ".");

                var element = project.Elements.SingleOrDefault(x =>
                    x.Category == category && x.SourceHandles.Any(h => string.Equals(h, sourceHandle, StringComparison.OrdinalIgnoreCase)));
                if (element == null) throw new InvalidOperationException("Không tìm thấy semantic " + label + " vừa tạo cho source " + sourceHandle + ".");

                element.Properties["WidthM"] = widthM.ToString("R", CultureInfo.InvariantCulture);
                element.Properties["HeightM"] = heightM.ToString("R", CultureInfo.InvariantCulture);
                element.Properties["SillHeightM"] = sillM.ToString("R", CultureInfo.InvariantCulture);
                element.Properties["BottomOffsetM"] = sillM.ToString("R", CultureInfo.InvariantCulture);
                element.Properties["BooleanClearanceM"] = clearanceM.ToString("R", CultureInfo.InvariantCulture);
                element.MarkDirty(ElementDirtyFlags.Properties);

                var regeneratedBeforeLink = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                // QS3DAUTOLINKHOSTS is selection-scoped. Keep only this new source selected so the
                // established elevation/scope/ambiguity matcher cannot mutate unrelated openings.
                document.Editor.SetImpliedSelection(new[] { sourceId });
                new AutoHostLinkCommands().AutoLinkHosts();

                if (!element.Properties.TryGetValue("HostWallId", out var hostId) || string.IsNullOrWhiteSpace(hostId))
                    throw new InvalidOperationException(label + " chưa tìm được host duy nhất trong phạm vi Auto Host; operation được rollback để không tạo opening mồ côi.");

                // AutoHostLinkCommands intentionally catches command-surface errors. Re-run the
                // deterministic semantic engine here so a link/regeneration failure propagates into
                // this command's outer snapshot rollback instead of looking like a successful authoring commit.
                var regeneratedAfterLink = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);

                project.Touch();
                PaletteCoordinator.RefreshProject();
                document.Editor.SetImpliedSelection(new[] { sourceId });
                document.Editor.Regen();
                var status = label + ": width=" + widthM.ToString("0.###", CultureInfo.InvariantCulture) +
                    " m • host=" + hostId + " • regen=" + (regeneratedBeforeLink + regeneratedAfterLink) +
                    ". Semantic + Auto Host hoàn tất; dùng QS3DCUTOPENINGS khi muốn khoét physical host.";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status);
            }
            catch (Exception operationError)
            {
                Exception? restoreError = null;
                Exception? cleanupError = null;
                try { rollback.Restore(project); }
                catch (Exception ex) { restoreError = ex; }
                try
                {
                    if (!string.IsNullOrWhiteSpace(sourceHandle)) EraseSource(document, sourceHandle);
                }
                catch (Exception ex) { cleanupError = ex; }
                try { document.Editor.SetImpliedSelection(Array.Empty<ObjectId>()); }
                catch { }

                if (restoreError != null || cleanupError != null)
                {
                    var errors = new List<Exception> { operationError };
                    if (restoreError != null) errors.Add(restoreError);
                    if (cleanupError != null) errors.Add(cleanupError);
                    throw new InvalidOperationException("Direct Draw " + label + " thất bại và rollback không hoàn tất đầy đủ.", new AggregateException(errors));
                }
                throw;
            }
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

        private static void EraseSource(Document document, string handle)
        {
            var normalized = (handle ?? string.Empty).Trim();
            if (normalized.Length == 0) return;
            var ids = CadHandleService.Resolve(document, new[] { normalized });
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    var entity = transaction.GetObject(id, OpenMode.ForWrite, false) as Entity;
                    if (entity == null) throw new InvalidOperationException("Rollback source " + normalized + " không còn là Entity hợp lệ.");
                    if (!entity.IsErased) entity.Erase(true);
                }
                transaction.Commit();
            }
            if (CadHandleService.GetLiveHandles(document, new[] { normalized }).Count > 0)
                throw new InvalidOperationException("Rollback còn source CAD chưa xóa: " + normalized + ".");
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
