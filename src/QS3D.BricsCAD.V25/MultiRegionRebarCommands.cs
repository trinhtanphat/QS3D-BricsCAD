using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class MultiRegionRebarCommands
    {
        [CommandMethod("QS3DSLABREBAR3DMULTI", CommandFlags.UsePickSet)]
        public void BuildSlabMultiRegionRebar3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (CadSelectionGuard.AcquireCurrentSelection(document).Length == 0)
                {
                    Report(document, "Slab Multi-Region Rebar 3D: chọn toàn bộ closed POLYLINE source loops của đúng một Slab (outer loops và hole loops). Lệnh fail-closed nếu topology không được hỗ trợ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Slab Multi-Region Rebar 3D: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var project = ExistingProjectMutationContext.Require(document, "Slab Multi-Region Rebar 3D");
                EnsureSameProjectSnapshot(project.ProjectId, project.ChangeVersion, expectedProjectId, expectedChangeVersion, "Slab Multi-Region Rebar 3D");

                var result = SlabFoundationMultiRegionMeshSolidBuilder.BuildSlab(document, project);
                var message = result.Bars == 0
                    ? "Slab Multi-Region Rebar 3D: không có multi-region output được tạo."
                    : "Slab Multi-Region Rebar 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Regions + " region.";
                FinalizeUi(document, "Slab Multi-Region Rebar 3D", message);
            }
            catch (Exception)
            {
                Report(document, "QS3DSLABREBAR3DMULTI không thể hoàn tất. Kiểm tra selection/project/topology và thử lại.");
            }
        }

        [CommandMethod("QS3DFOUNDATIONREBAR3DMULTI", CommandFlags.UsePickSet)]
        public void BuildFoundationMultiRegionRebar3D()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (CadSelectionGuard.AcquireCurrentSelection(document).Length == 0)
                {
                    Report(document, "Foundation Multi-Region Rebar 3D: chọn toàn bộ closed POLYLINE source loops của đúng một Foundation (outer loops và hole loops). Lệnh fail-closed nếu topology không được hỗ trợ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Foundation Multi-Region Rebar 3D: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var project = ExistingProjectMutationContext.Require(document, "Foundation Multi-Region Rebar 3D");
                EnsureSameProjectSnapshot(project.ProjectId, project.ChangeVersion, expectedProjectId, expectedChangeVersion, "Foundation Multi-Region Rebar 3D");

                var result = SlabFoundationMultiRegionMeshSolidBuilder.BuildFoundation(document, project);
                var message = result.Bars == 0
                    ? "Foundation Multi-Region Rebar 3D: không có multi-region output được tạo."
                    : "Foundation Multi-Region Rebar 3D: đã tạo/cập nhật " + result.Bars + " thanh trên " + result.Regions + " region.";
                FinalizeUi(document, "Foundation Multi-Region Rebar 3D", message);
            }
            catch (Exception)
            {
                Report(document, "QS3DFOUNDATIONREBAR3DMULTI không thể hoàn tất. Kiểm tra selection/project/topology và thử lại.");
            }
        }

        [CommandMethod("QS3DMULTIREBARHEALTH", CommandFlags.Modal)]
        public void MultiRegionRebarHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Multi-Region Rebar Health: BLOCKED • chưa có QS3D project state/sidecar; health không tạo project mới.");
                    return;
                }

                var issues = GeneratedMultiRegionRebarRuntimeHealthService.Inspect(document, project);
                var summary = new HealthSummary(issues);
                var message = "Multi-Region Rebar Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                Report(document, message);
                foreach (var issue in issues.Take(50))
                    TryWriteMessage(document, "\n  [" + issue.Severity + "] " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                if (issues.Count > 50) TryWriteMessage(document, "\n  … health output truncated.");
            }
            catch (Exception)
            {
                Report(document, "QS3DMULTIREBARHEALTH không thể hoàn tất kiểm tra. Project/native geometry không bị thay đổi.");
            }
        }

        private static void EnsureSameProjectSnapshot(
            string currentProjectId,
            long currentChangeVersion,
            string expectedProjectId,
            long expectedChangeVersion,
            string operation)
        {
            if (!string.Equals(currentProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                currentChangeVersion != expectedChangeVersion)
                throw new InvalidOperationException(operation + ": QS3D project đã thay đổi sau pha đọc read-only; hãy chọn lại source loops và chạy lại lệnh.");
        }

        private static void FinalizeUi(Document document, string operation, string message)
        {
            var uiSyncFailed = false;
            try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }
            try { document.Editor.Regen(); } catch { uiSyncFailed = true; }
            try { PaletteCoordinator.SetStatus(message); } catch { uiSyncFailed = true; }
            TryWriteMessage(document, "\nQS3D " + message);
            if (uiSyncFailed)
                TryWriteMessage(document, "\nQS3D " + operation + ": native update đã hoàn tất; một phần UI không thể đồng bộ.");
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); }
            catch { }
            TryWriteMessage(document, "\nQS3D " + message);
        }

        private static void TryWriteMessage(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); }
            catch { }
        }
    }
}
