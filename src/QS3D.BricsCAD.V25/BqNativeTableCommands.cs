using System;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class BqNativeTableCommands
    {
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DBQTABLE", CommandFlags.Modal)]
        public void Build()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                RequireSupportedUcs(document);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "BQ Table: BLOCKED • chưa có QS3D project state/sidecar; đặt Table không tạo project mới.");
                    return;
                }
                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var point = document.Editor.GetPoint("\nChọn điểm đặt QS3D BQ Tổng hợp Table: ");
                if (point.Status != PromptStatus.OK) return;
                var world = point.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem);
                var project = RequireExistingProject(document, "BQ Table");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("BQ Table: QS3D project/state đã thay đổi trong lúc chọn điểm đặt. Hãy chạy lại lệnh.");
                var regenerated = RegenerateSemantic(project);
                var handle = BqNativeTableBuilder.Build(document, project, world);
                FinalizeUi(document, "BQ Table: đã tạo/cập nhật native Table " + handle + " • regen " + regenerated + ".");
            }
            catch (Exception) { Report(document, "QS3DBQTABLE lỗi: thao tác không hoàn tất; kiểm tra project/CAD state và thử lại."); }
        }

        [CommandMethod("QS3DBQTABLEREFRESH", CommandFlags.Modal)]
        public void Refresh()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                var project = RequireExistingProject(document, "BQ Table refresh");
                var position = BqNativeTableBuilder.StoredPosition(project);
                var regenerated = RegenerateSemantic(project);
                var handle = BqNativeTableBuilder.Build(document, project, position);
                FinalizeUi(document, "BQ Table: đã refresh native Table " + handle + " tại WCS position đã lưu • regen " + regenerated + ".");
            }
            catch (Exception) { Report(document, "QS3DBQTABLEREFRESH lỗi: refresh không hoàn tất; kiểm tra project/CAD state và thử lại."); }
        }

        [CommandMethod("QS3DBQTABLEREMOVE", CommandFlags.Modal)]
        public void Remove()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = RequireExistingProject(document, "BQ Table remove");
                BqNativeTableBuilder.Remove(document, project);
                FinalizeUi(document, "BQ Table: đã xóa owned native Table/metadata (nếu có).");
            }
            catch (Exception) { Report(document, "QS3DBQTABLEREMOVE lỗi: remove không hoàn tất; kiểm tra project/CAD state và thử lại."); }
        }

        [CommandMethod("QS3DBQTABLEHEALTH", CommandFlags.Modal)]
        public void Health()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "BQ Table health: BLOCKED • chưa có QS3D project state/sidecar; health check không tạo project mới.");
                    return;
                }

                var issues = BqNativeTableBuilder.Inspect(document, project);
                if (issues.Count == 0)
                {
                    Report(document, "BQ Table health: không phát hiện ownership/stale/live CAD/dirty issue.");
                    return;
                }

                var visible = issues.Take(100).Select(x => x.Code + ": " + x.Message).ToArray();
                var suffix = issues.Count > visible.Length ? "\n- … +" + (issues.Count - visible.Length) + " issue(s)" : string.Empty;
                Report(document, "BQ Table health: " + issues.Count + " issue(s).\n- " + string.Join("\n- ", visible) + suffix);
            }
            catch (Exception) { Report(document, "QS3DBQTABLEHEALTH lỗi: health check không hoàn tất; kiểm tra project/CAD state và thử lại."); }
        }

        private static QS3D.Core.Domain.ProjectState RequireExistingProject(Document document, string operation)
        {
            return ExistingProjectMutationContext.Require(document, operation);
        }

        private static int RegenerateSemantic(QS3D.Core.Domain.ProjectState project)
        {
            return new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
        }

        private static void RequireModelSpace(Document document)
        {
            if (!document.Database.TileMode)
                throw new InvalidOperationException("BQ Table P0 chỉ hỗ trợ ModelSpace.");
        }

        private static void RequireSupportedUcs(Document document)
        {
            var coordinateSystem = document.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
            var zAxis = coordinateSystem.Zaxis;
            var length = zAxis.Length;
            if (double.IsNaN(length) || double.IsInfinity(length) || !(length > 0d))
                throw new InvalidOperationException("Current UCS có Z axis không hợp lệ.");
            var x = zAxis.X / length;
            var y = zAxis.Y / length;
            var z = zAxis.Z / length;
            if (Math.Abs(x) > UcsAxisTolerance || Math.Abs(y) > UcsAxisTolerance || Math.Abs(z - 1d) > UcsAxisTolerance)
                throw new InvalidOperationException("BQ Table P0 chỉ hỗ trợ UCS có mặt phẳng XY song song WCS XY khi chọn điểm đặt mới.");
        }

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                document.Editor.Regen();
                PaletteCoordinator.RefreshProject();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception)
            {
                TryWrite(document, "\nQS3D " + message + " UI sync warning: native Table đã commit nhưng viewport/palette không refresh đầy đủ.");
            }
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            TryWrite(document, "\nQS3D " + message);
        }

        private static void TryWrite(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }
    }
}
