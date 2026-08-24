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
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DSLABREBAR3DMULTI lỗi: " + ex.Message);
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
                FinalizeUi(document, message);
            }
            catch (Exception ex)
            {
                Report(document, "QS3DFOUNDATIONREBAR3DMULTI lỗi: " + ex.Message);
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
                    document.Editor.WriteMessage("\n  [" + issue.Severity + "] " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                if (issues.Count > 50) document.Editor.WriteMessage("\n  … health output truncated.");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DMULTIREBARHEALTH lỗi: " + ex.Message);
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

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                Report(document, message);
            }
            catch (Exception ex)
            {
                TryWriteMessage(document, "\nQS3D " + message + " UI sync warning: " + ex.Message);
            }
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
