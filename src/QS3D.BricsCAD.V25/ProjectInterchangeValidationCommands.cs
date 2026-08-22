using System;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ProjectInterchangeValidationCommands
    {
        private const int MaxDisplayedIssues = 12;

        [CommandMethod("QS3DINTERCHANGEVALIDATE", CommandFlags.Modal)]
        public void ValidateSemanticSnapshot()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Kiểm tra QS3D Semantic Snapshot JSON (chỉ đọc)",
                    Filter = "QS3D Semantic Snapshot (*.qs3d.json)|*.qs3d.json|JSON (*.json)|*.json|All files (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var result = ProjectInterchangeJsonValidator.ValidateFile(dialog.FileName);
                var summary = BuildSummary(result);
                TrySetStatus(summary);
                TryWrite(document, "\nQS3D " + summary);

                foreach (var issue in result.Issues.Take(MaxDisplayedIssues))
                {
                    var severity = issue.Severity == InterchangeValidationSeverity.Error ? "ERROR" : "WARN";
                    TryWrite(document, "\n  [" + severity + "] " + issue.Code + " • " + issue.Path + " • " + issue.Message);
                }
                if (result.Issues.Count > MaxDisplayedIssues)
                    TryWrite(document, "\n  ... còn " + (result.Issues.Count - MaxDisplayedIssues).ToString(CultureInfo.InvariantCulture) + " issue(s); sửa snapshot rồi kiểm tra lại.");

                TryWrite(document, result.IsValid
                    ? "\nQS3D snapshot structurally valid for review. READ-ONLY VALIDATION ONLY — nothing was imported into the current project/DWG."
                    : "\nQS3D snapshot validation FAILED. Nothing was imported or changed in the current project/DWG.");
            }
            catch (Exception ex)
            {
                var message = "QS3DINTERCHANGEVALIDATE lỗi: " + ex.Message;
                TrySetStatus(message);
                TryWrite(document, "\n" + message + " Nothing was imported or changed.");
            }
        }

        private static string BuildSummary(ProjectInterchangeValidationResult result)
        {
            var state = result.IsValid ? "PASS" : "FAIL";
            return "Interchange Validate " + state +
                " • format=" + result.Format + "/v" + result.FormatVersion.ToString(CultureInfo.InvariantCulture) +
                " • Zone=" + result.ZoneCount.ToString(CultureInfo.InvariantCulture) +
                " • Floor=" + result.FloorCount.ToString(CultureInfo.InvariantCulture) +
                " • Family=" + result.FamilyCount.ToString(CultureInfo.InvariantCulture) +
                " • Element=" + result.ElementCount.ToString(CultureInfo.InvariantCulture) +
                " • error=" + result.ErrorCount.ToString(CultureInfo.InvariantCulture) +
                " • warning=" + result.WarningCount.ToString(CultureInfo.InvariantCulture) +
                " • READ-ONLY / NOT IMPORTED";
        }

        private static void TrySetStatus(string status)
        {
            try { PaletteCoordinator.SetStatus(status); } catch { }
        }

        private static void TryWrite(Document document, string message)
        {
            try { document.Editor.WriteMessage(message); } catch { }
        }
    }
}
