using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class BeamStirrupCommands
    {
        private const string OperationFailure = "QS3DREBARSTIRRUP3D lỗi: không thể tạo/cập nhật đai dầm. Kiểm tra selection, project semantic và dữ liệu stirrup rồi thử lại.";
        private const string HealthFailure = "QS3DREBARSTIRRUPHEALTH lỗi: không thể hoàn tất kiểm tra đai dầm. Kiểm tra project/drawing hiện hành rồi thử lại.";
        private const string UiSyncWarning = "UI sync warning: đã cập nhật đai dầm nhưng đồng bộ giao diện chưa hoàn tất. Dữ liệu CAD/project đã được giữ nguyên; hãy refresh giao diện.";

        [CommandMethod("QS3DBEAMSTIRRUP3D", CommandFlags.UsePickSet)]
        public void BuildBeamStirrupsWorkspaceAlias() => BuildBeamStirrups();

        [CommandMethod("QS3DREBARSTIRRUP3D", CommandFlags.UsePickSet)]
        public void BuildBeamStirrups()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);
                if (selectedIds.Length == 0)
                {
                    Report(document, "Beam Stirrup 3D: chọn Beam semantic LINE có RebarStirrupNotation (ví dụ D8@150 hoặc 20D8).");
                    return;
                }

                var selectedHandles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in selectedIds)
                {
                    try { selectedHandles.Add(id.Handle.ToString()); }
                    catch { }
                }
                if (selectedHandles.Count == 0)
                {
                    Report(document, "Beam Stirrup 3D: selection không có source handle hợp lệ.");
                    return;
                }

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject))
                {
                    Report(document, "Beam Stirrup 3D: BLOCKED • chưa có QS3D project hiện hữu; lệnh không tạo project mới từ selection.");
                    return;
                }

                var previewTargets = ResolveBeamTargets(previewProject, selectedHandles);
                if (previewTargets.Count == 0)
                {
                    Report(document, "Beam Stirrup 3D: chọn Beam semantic LINE có RebarStirrupNotation (ví dụ D8@150 hoặc 20D8).");
                    return;
                }

                var expectedProjectId = previewProject.ProjectId;
                var expectedChangeVersion = previewProject.ChangeVersion;
                var expectedTargetIds = new HashSet<string>(previewTargets.Select(x => x.Id), StringComparer.OrdinalIgnoreCase);

                var project = ExistingProjectMutationContext.Require(document, "Beam Stirrup 3D");
                if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase) ||
                    project.ChangeVersion != expectedChangeVersion)
                    throw new InvalidOperationException("Beam Stirrup 3D: QS3D project đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var targets = ResolveBeamTargets(project, selectedHandles);
                if (!expectedTargetIds.SetEquals(targets.Select(x => x.Id)))
                    throw new InvalidOperationException("Beam Stirrup 3D: semantic Beam target set đã thay đổi sau khi đọc selection; hãy chọn lại target.");

                var result = BeamStirrupSolidBuilder.BuildSelected(document, project);
                var message = result.Stirrups == 0
                    ? "Beam Stirrup 3D: chọn Beam semantic LINE có RebarStirrupNotation (ví dụ D8@150 hoặc 20D8)."
                    : "Beam Stirrup 3D: đã tạo/cập nhật " + result.Stirrups + " đai trên " + result.Elements + " dầm.";
                FinalizeUi(document, message);
            }
            catch (Exception)
            {
                Report(document, OperationFailure);
            }
        }

        [CommandMethod("QS3DBEAMSTIRRUPHEALTH", CommandFlags.Modal)]
        public void BeamStirrupHealthWorkspaceAlias() => BeamStirrupHealth();

        [CommandMethod("QS3DREBARSTIRRUPHEALTH", CommandFlags.Modal)]
        public void BeamStirrupHealth()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                {
                    Report(document, "Beam Stirrup Health: BLOCKED • chưa có QS3D project state/sidecar; lệnh kiểm tra không tạo project mới.");
                    return;
                }

                var handles = new List<string>();
                foreach (var element in project.Elements)
                {
                    if (!element.Properties.TryGetValue("GeneratedBeamStirrupHandles", out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                    handles.AddRange(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0));
                }
                var live = CadHandleService.GetLiveSolidHandles(document, handles.Distinct(StringComparer.OrdinalIgnoreCase));
                var issues = new GeneratedBeamStirrupHealthService().Inspect(project, live);
                var summary = new HealthSummary(issues);
                var message = "Beam Stirrup Health: " + summary.Errors + " lỗi • " + summary.Warnings + " cảnh báo • " + summary.Info + " thông tin";
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
                foreach (var issue in issues.Take(50))
                    document.Editor.WriteMessage("\n  [" + issue.Severity + "] " + issue.Code + " • " + issue.ElementId + " • " + issue.Message);
                if (issues.Count > 50) document.Editor.WriteMessage("\n  … health output truncated.");
            }
            catch (Exception)
            {
                Report(document, HealthFailure);
            }
        }

        private static List<ProjectElement> ResolveBeamTargets(ProjectState project, HashSet<string> selectedHandles) =>
            project.Elements
                .Where(x => x.Category == ElementCategory.Beam && x.SourceHandles.Any(selectedHandles.Contains))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static void FinalizeUi(Document document, string message)
        {
            try
            {
                PaletteCoordinator.RefreshProject();
                document.Editor.Regen();
                PaletteCoordinator.SetStatus(message);
                document.Editor.WriteMessage("\nQS3D " + message);
            }
            catch (Exception)
            {
                TryWriteMessage(document, "\nQS3D " + message + " " + UiSyncWarning);
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
