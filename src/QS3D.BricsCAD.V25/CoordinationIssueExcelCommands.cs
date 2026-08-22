using System;
using System.IO;
using Bricscad.ApplicationServices;
using Microsoft.Win32;
using QS3D.Core.Coordination;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Host wiring for the canonical CoordinationIssue lifecycle workbook.
    /// Export is read-only. Import parses and validates the complete workbook before
    /// mutating ProjectState, then persists the accepted batch through the canonical
    /// QSDB save boundary.
    /// </summary>
    public sealed class CoordinationIssueExcelCommands
    {
        private const string ImportOperation = "Coordination Issue Excel Import";

        [CommandMethod("QS3DISSUEEXPORT", CommandFlags.Modal)]
        public void ExportIssues()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Coordination Issue → Excel cần một QS3D project hiện hữu; export không tạo project mới.");

                var snapshot = CoordinationIssuePersistence.Load(project);
                if (snapshot == null || snapshot.Issues.Count == 0)
                    throw new InvalidOperationException("QS3D project chưa có canonical CoordinationIssue để xuất.");

                var drawingStem = SafeDrawingStem(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "QS3D • Coordination Issues → Excel",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingStem + "-QS3D-ISSUES.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                CoordinationIssueExcelWorkbook.Export(dialog.FileName, snapshot);
                var status = "Coordination Issues → Excel: " + snapshot.Issues.Count +
                             " issue • revision=" + snapshot.Revision + " • " + dialog.FileName;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage(
                    "\nQS3D " + status +
                    "\nCó thể sửa STATUS, SEVERITY, ASSIGNEE, COMMENT_AUTHOR và COMMENT; các cột trace/identity là bất biến.");
            }
            catch (Exception error)
            {
                Report(document, "QS3DISSUEEXPORT", error);
            }
        }

        [CommandMethod("QS3DISSUEIMPORT", CommandFlags.Modal)]
        public void ImportIssues()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "QS3D • Excel → Coordination Issues",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                // Bind only an existing canonical project. The workbook must never
                // bootstrap a new QS3D project as a side effect of import.
                var project = ExistingProjectMutationContext.Require(document, ImportOperation);
                var current = CoordinationIssuePersistence.Load(project)
                    ?? throw new InvalidOperationException("QS3D project chưa có canonical CoordinationIssue state để import.");

                // ReadAndPlanImport performs the complete workbook/provenance/revision/
                // immutable-trace validation against the current snapshot and returns
                // cloned issues. No ProjectState mutation has occurred at this point.
                var plan = CoordinationIssueExcelWorkbook.ReadAndPlanImport(
                    dialog.FileName,
                    current,
                    DateTime.UtcNow);

                // The file picker and XLSX parse may take time. Re-check the canonical
                // .qsdb/.bak generation immediately before the first mutation so an
                // external writer cannot be silently overwritten.
                ProjectContextCoordinator.RequireBackingStoreUnchanged(document, project, ImportOperation);

                if (plan.ChangedIssueCount == 0)
                {
                    var noChange = "Excel → Coordination Issues: workbook hợp lệ, không có thay đổi • revision=" + current.Revision;
                    PaletteCoordinator.SetStatus(noChange);
                    document.Editor.WriteMessage("\nQS3D " + noChange + ".");
                    return;
                }

                CoordinationIssuePersistence.Save(project, plan.Issues, plan.NextRevision);
                var projectPath = ProjectContextCoordinator.Save(document);

                var persisted = CoordinationIssuePersistence.Load(project)
                    ?? throw new InvalidOperationException("CoordinationIssue state vừa lưu không thể đọc lại để xác nhận.");
                if (persisted.Revision != plan.NextRevision)
                    throw new InvalidOperationException("CoordinationIssue revision sau khi lưu không khớp import plan.");

                var status = "Excel → Coordination Issues: " + plan.ChangedIssueCount +
                             " issue changed • revision=" + persisted.Revision + " • " + projectPath;
                try { PaletteCoordinator.RefreshProject(); } catch { }
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage(
                    "\nQS3D " + status +
                    "\nWorkbook cũ hiện stale; hãy QS3DISSUEEXPORT lại trước vòng chỉnh sửa tiếp theo.");
            }
            catch (Exception error)
            {
                Report(document, "QS3DISSUEIMPORT", error);
            }
        }

        private static string SafeDrawingStem(string drawingPath)
        {
            try
            {
                var stem = Path.GetFileNameWithoutExtension(drawingPath ?? string.Empty);
                return string.IsNullOrWhiteSpace(stem) ? "QS3D" : stem;
            }
            catch
            {
                return "QS3D";
            }
        }

        private static void Report(Document document, string operation, Exception error)
        {
            var message = error is AggregateException aggregate ? aggregate.GetBaseException().Message : error.Message;
            try { PaletteCoordinator.SetStatus(operation + ": " + message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + operation + ": " + message); } catch { }
        }
    }
}
