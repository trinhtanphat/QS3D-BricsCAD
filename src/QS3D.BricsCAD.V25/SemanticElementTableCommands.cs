using System;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class SemanticElementTableCommands
    {
        private const double UcsAxisTolerance = 1e-9d;

        [CommandMethod("QS3DELEMENTTABLE", CommandFlags.Modal)]
        public void BuildElementTable()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                RequireSupportedUcs(document);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Semantic Element Table: BLOCKED • chưa có QS3D project state/sidecar; đặt Table không tạo project mới.");
                    return;
                }
                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var point = document.Editor.GetPoint("\nChọn điểm đặt QS3D Semantic Element Table: ");
                if (point.Status != PromptStatus.OK) return;
                var world = point.Value.TransformBy(document.Editor.CurrentUserCoordinateSystem);
                var project = RequireExistingProject(document, "Semantic Element Table");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Semantic Element Table: QS3D project/state đã thay đổi trong lúc chọn điểm đặt. Hãy chạy lại lệnh.");
                var handle = SemanticElementTableBuilder.Build(document, project, world);
                FinalizeUi(document, "Semantic Element Table: đã tạo/cập nhật native Table " + handle + ".");
            }
            catch (Exception ex) { Report(document, "QS3DELEMENTTABLE lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DELEMENTTABLEREFRESH", CommandFlags.Modal)]
        public void RefreshElementTable()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                RequireModelSpace(document);
                var project = RequireExistingProject(document, "Semantic Element Table refresh");
                var position = SemanticElementTableBuilder.StoredPosition(project);
                var handle = SemanticElementTableBuilder.Build(document, project, position);
                FinalizeUi(document, "Semantic Element Table: đã refresh native Table " + handle + " tại vị trí WCS đã lưu.");
            }
            catch (Exception ex) { Report(document, "QS3DELEMENTTABLEREFRESH lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DELEMENTTABLEREMOVE", CommandFlags.Modal)]
        public void RemoveElementTable()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var project = RequireExistingProject(document, "Semantic Element Table remove");
                SemanticElementTableBuilder.Remove(document, project);
                FinalizeUi(document, "Semantic Element Table: đã xóa generated Table/metadata thuộc project (nếu có).");
            }
            catch (Exception ex) { Report(document, "QS3DELEMENTTABLEREMOVE lỗi: " + ex.Message); }
        }

        [CommandMethod("QS3DELEMENTTABLEHEALTH", CommandFlags.Modal)]
        public void CheckElementTableHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Semantic Element Table health: BLOCKED • chưa có QS3D project state/sidecar; health check không tạo project mới.");
                    return;
                }

                var issues = GeneratedSemanticElementTableRuntimeHealthService.Inspect(document, project);
                if (issues.Count == 0)
                {
                    Report(document, "Semantic Element Table health: không phát hiện persisted/native ownership/content drift issue.");
                    return;
                }

                var visible = issues.Take(100)
                    .Select(x => x.Code + ": " + x.Message)
                    .ToArray();
                var suffix = issues.Count > visible.Length
                    ? "\n- … +" + (issues.Count - visible.Length) + " issue(s)"
                    : string.Empty;
                Report(document, "Semantic Element Table health: " + issues.Count + " issue(s).\n- " + string.Join("\n- ", visible) + suffix);
            }
            catch (Exception ex) { Report(document, "QS3DELEMENTTABLEHEALTH lỗi: " + ex.Message); }
        }

        private static QS3D.Core.Domain.ProjectState RequireExistingProject(Document document, string operation)
        {
            return ExistingProjectMutationContext.Require(document, operation);
        }

        private static void RequireModelSpace(Document document)
        {
            if (!document.Database.TileMode)
                throw new InvalidOperationException("Semantic Element Table P0 chỉ hỗ trợ ModelSpace. PaperSpace/Layout sẽ được triển khai theo sheet lifecycle riêng.");
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
                throw new InvalidOperationException("Semantic Element Table P0 chỉ hỗ trợ UCS có mặt phẳng XY song song WCS XY.");
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
            catch (Exception ex) { TryWrite(document, "\nQS3D " + message + " UI sync warning: " + ex.Message); }
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
