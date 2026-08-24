using System;
using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Export;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CadToExcelCommands
    {
        [CommandMethod("QS3DCADTOEXCEL", CommandFlags.UsePickSet)]
        public void ActivateExcelDetailRow()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("CAD → Excel cần một QS3D project hiện hữu.");
                if (project.Elements.Count == 0)
                    throw new InvalidOperationException("CAD → Excel chưa có semantic element để truy vết.");

                var snapshots = EntitySnapshotReader.ReadImpliedSelection(document);
                if (snapshots.Count == 0) snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count == 0)
                    throw new InvalidOperationException("Hãy chọn đúng một cấu kiện QS3D trước khi chạy CAD → Excel.");

                var selectedHandles = new HashSet<string>(
                    snapshots.Select(snapshot => (snapshot.Handle ?? string.Empty).Trim())
                        .Where(handle => handle.Length > 0),
                    StringComparer.OrdinalIgnoreCase);
                if (selectedHandles.Count == 0)
                    throw new InvalidOperationException("Selection không có CAD Handle hợp lệ để map về semantic element.");

                var elements = project.Elements
                    .Where(element => SemanticReferenceHandles.MatchesSelection(element, selectedHandles))
                    .ToList();
                if (elements.Count != 1)
                    throw new InvalidOperationException(
                        elements.Count == 0
                            ? "Selection không khớp semantic element QS3D nào."
                            : "CAD → Excel chỉ nhận đúng một semantic element; selection hiện khớp " + elements.Count + " element.");

                var element = elements[0];
                var aliases = new HashSet<string>(SemanticReferenceHandles.GetSelectionAliases(element), StringComparer.OrdinalIgnoreCase);
                var untracked = selectedHandles.Where(handle => !aliases.Contains(handle)).OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase).ToList();
                if (untracked.Count > 0)
                    throw new InvalidOperationException("Selection trộn CAD object ngoài cấu kiện đang truy vết: " + string.Join(", ", untracked) + ".");

                var reviewedVersion = project.ChangeVersion;
                if (!ExcelModelRowActivationService.TryFindActiveWorkbookRow(
                        element.Id,
                        project.DrawingFingerprint,
                        out var candidate,
                        out var discoveryError) || candidate == null)
                    throw new InvalidOperationException(discoveryError);

                // COM is discovery-only. Re-read the exact candidate from the saved XLSX through
                // the existing hardened provenance reader and resolve every current CAD Handle
                // before Excel selection is allowed to move.
                if (candidate.WorkbookKind == ExcelModelRowWorkbookKind.Customer)
                {
                    var trace = QsCustomerWorkbookTraceReader.Read(
                        candidate.WorkbookPath,
                        QsCustomerWorkbookExporter.DetailSheet,
                        candidate.RowNumber);
                    if (trace.ElementIds.Count != 1 ||
                        !string.Equals(trace.ElementIds[0], element.Id, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Customer workbook CHI_TIET candidate không còn khớp cấu kiện CAD đang chọn.");
                    ExcelLocateResolutionService.ResolveCustomerTrace(document, project, trace);
                }
                else
                {
                    var lookup = XlsxHandleReader.ReadHandleLookup(candidate.WorkbookPath, candidate.RowNumber);
                    if (lookup.ElementIds.Count != 1 ||
                        !string.Equals(lookup.ElementIds[0], element.Id, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("ED2 CHI_TIET candidate không còn khớp cấu kiện CAD đang chọn.");
                    ExcelLocateResolutionService.ResolveModern(document, project, lookup);
                }

                if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                    throw new InvalidOperationException("DWG đang hoạt động đã đổi sau bước kiểm tra; không thay đổi selection Excel.");
                if (project.ChangeVersion != reviewedVersion)
                    throw new InvalidOperationException("Project đã thay đổi sau bước kiểm tra; không thay đổi selection Excel.");

                if (!ExcelModelRowActivationService.TryActivateValidatedRow(candidate, out var activationError))
                    throw new InvalidOperationException(activationError);

                var status = "CAD → Excel: " + element.Id + " → CHI_TIET dòng " + candidate.RowNumber +
                             " (" + candidate.WorkbookKind + ")";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
            }
            catch (Exception error)
            {
                Report(document, error);
            }
        }

        private static void Report(Document document, Exception error)
        {
            var message = error is AggregateException aggregate ? aggregate.GetBaseException().Message : error.Message;
            try { PaletteCoordinator.SetStatus("QS3DCADTOEXCEL: " + message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D QS3DCADTOEXCEL: " + message); } catch { }
        }
    }
}
