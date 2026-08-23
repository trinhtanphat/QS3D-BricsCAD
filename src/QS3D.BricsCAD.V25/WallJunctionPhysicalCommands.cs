using System;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class WallJunctionPhysicalCommands
    {
        [CommandMethod("QS3DWALLJUNCTION3D", CommandFlags.UsePickSet)]
        public void Build()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                // Selection/cancel is intentionally resolved before project binding so an empty or
                // cancelled pick cannot create project state or touch an existing sidecar.
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, "Wall Junction 3D: chọn semantic wall source LINE/open POLYLINE.");
                    return;
                }

                var project = ExistingProjectMutationContext.Require(document, "Wall Junction 3D");
                var result = WallJunctionSolidBuilder.BuildSelected(document, project, selectedIds);
                Report(document,
                    "Wall Junction 3D: plan=" + result.PlannedOutputs.ToString(CultureInfo.InvariantCulture) +
                    " • tạo=" + result.CreatedOutputs.ToString(CultureInfo.InvariantCulture) +
                    " • giữ=" + result.KeptOutputs.ToString(CultureInfo.InvariantCulture) +
                    " • xóa=" + result.RemovedOutputs.ToString(CultureInfo.InvariantCulture) +
                    " • rebuild group=" + result.RebuiltGroups.ToString(CultureInfo.InvariantCulture) +
                    " • remove group=" + result.RemovedGroups.ToString(CultureInfo.InvariantCulture) + ".");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DWALLJUNCTION3D lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DWALLJUNCTIONHEALTH", CommandFlags.Modal)]
        public void Health()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Wall Junction health: BLOCKED • chưa có QS3D project state/sidecar; health không tạo project mới.");
                    return;
                }

                var issues = GeneratedWallJunctionRuntimeHealthService.Inspect(document, project);
                var summary = new HealthSummary(issues);
                Report(document,
                    "Wall Junction health: " + summary.Errors.ToString(CultureInfo.InvariantCulture) + " lỗi • " +
                    summary.Warnings.ToString(CultureInfo.InvariantCulture) + " cảnh báo • " +
                    summary.Info.ToString(CultureInfo.InvariantCulture) + " thông tin.");
                foreach (var issue in issues.Take(100))
                    document.Editor.WriteMessage("\n  " + issue.Code + " • " + issue.Message);
                if (issues.Count > 100)
                    document.Editor.WriteMessage("\n  … +" + (issues.Count - 100).ToString(CultureInfo.InvariantCulture) + " issue(s).");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DWALLJUNCTIONHEALTH lỗi: " + ex.Message);
            }
        }

        private static void Report(Document document, string message)
        {
            try { PaletteCoordinator.SetStatus(message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
        }
    }
}
