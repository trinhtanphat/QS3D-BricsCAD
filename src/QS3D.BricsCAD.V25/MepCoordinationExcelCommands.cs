using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Read-only Excel round-trip for exact native clash pairs. The workbook is an export projection,
    /// not project persistence: no CoordinationIssue contract is duplicated here and no DWG/QSDB state
    /// is mutated by export or locate.
    /// </summary>
    public sealed class MepCoordinationExcelCommands
    {
        [CommandMethod("QS3DMEPCLASHEXPORT", CommandFlags.UsePickSet)]
        public void ExportExactClashes()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Clash → Excel cần một QS3D project hiện hữu; export không tạo project mới.");
                if (string.IsNullOrWhiteSpace(project.DrawingFingerprint))
                    throw new InvalidOperationException("QS3D project chưa có Drawing Fingerprint để bảo vệ Excel round-trip.");

                var snapshots = EntitySnapshotReader.ReadCurrentSelection(document);
                if (snapshots.Count < 2)
                    throw new InvalidOperationException("QS3DMEPCLASHEXPORT cần ít nhất hai Solid3d MEP/Structure/Architecture trong selection.");

                var snapshotByHandle = MepExactClashCommands.BuildSnapshotIndex(snapshots);
                var ids = CadHandleService.Resolve(document, snapshotByHandle.Keys);
                var clashes = MepExactClashCommands.DetectExact(
                    document,
                    ids,
                    snapshotByHandle,
                    out var recognizedSolids,
                    out var skipped,
                    out var broadPhasePairs);
                if (clashes.Count == 0)
                    throw new InvalidOperationException("Selection không có exact hard-clash để xuất.");

                var semantic = BuildSemanticIndex(project);
                var rows = new List<CoordinationClashExportRow>(clashes.Count);
                for (var i = 0; i < clashes.Count; i++)
                {
                    var pair = clashes[i];
                    semantic.TryGetValue(pair.LeftHandle, out var left);
                    semantic.TryGetValue(pair.RightHandle, out var right);
                    var floor = CommonFloor(left, right);
                    rows.Add(CoordinationClashExportRow.CreateExactHard(
                        project.DrawingFingerprint,
                        pair.LeftHandle,
                        pair.RightHandle,
                        left == null ? string.Empty : left.ElementId,
                        right == null ? string.Empty : right.ElementId,
                        left == null ? string.Empty : left.Category,
                        right == null ? string.Empty : right.Category,
                        floor));
                }

                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "QS3D • Clash → Excel",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-QS3D-CLASHES.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                CoordinationWorkbookExporter.Export(dialog.FileName, rows);
                var status = "Clash → Excel: " + rows.Count + " exact clash • solids=" + recognizedSolids +
                             " • broad-phase=" + broadPhasePairs + " • skipped=" + skipped + " • " + dialog.FileName;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + "\nDùng QS3DEXCELLOCATECLASH để định vị một dòng CLASHES ngược về mô hình.");
            }
            catch (Exception error)
            {
                Report(document, "QS3DMEPCLASHEXPORT", error);
            }
        }

        [CommandMethod("QS3DEXCELLOCATECLASH", CommandFlags.Modal)]
        public void LocateClashWorkbookRow()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Excel Clash → CAD cần một QS3D project hiện hữu; locate không tạo project mới.");
                if (string.IsNullOrWhiteSpace(project.DrawingFingerprint))
                    throw new InvalidOperationException("QS3D project chưa có Drawing Fingerprint để xác minh workbook.");

                var dialog = new OpenFileDialog
                {
                    Title = "QS3D • Excel Clash → CAD",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var rowPrompt = new PromptIntegerOptions("\nNhập số dòng CLASHES cần định vị: ")
                {
                    AllowNone = false,
                    LowerLimit = 2,
                    UseDefaultValue = true,
                    DefaultValue = 2
                };
                var row = document.Editor.GetInteger(rowPrompt);
                if (row.Status != PromptStatus.OK) return;

                var trace = CoordinationWorkbookTraceReader.Read(dialog.FileName, row.Value);
                if (!string.Equals(project.DrawingFingerprint, trace.DrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Workbook thuộc drawing/project khác (Drawing Fingerprint mismatch). Locate bị chặn để tránh chọn nhầm model.");

                var objectIds = CadHandleService.Resolve(document, new[] { trace.LeftHandle, trace.RightHandle });
                if (objectIds.Count != 2)
                    throw new InvalidOperationException(
                        "Clash pair có Handle stale/missing. Locate yêu cầu cả hai entity còn live; selection hiện tại không bị thay đổi.");

                document.Editor.SetImpliedSelection(objectIds.ToArray());
                var status = "Excel Clash → CAD dòng " + trace.RowNumber + ": " + trace.ClashId + " • 2 CAD object";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
                document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
            }
            catch (Exception error)
            {
                Report(document, "QS3DEXCELLOCATECLASH", error);
            }
        }

        private static Dictionary<string, SemanticProjection> BuildSemanticIndex(ProjectState project)
        {
            var result = new Dictionary<string, SemanticProjection>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                var floor = project.FindFloor(element.FloorId);
                var projection = new SemanticProjection(
                    element.Id,
                    element.Category.ToString(),
                    floor == null ? (element.FloorId ?? string.Empty).Trim() : floor.Name);
                foreach (var rawHandle in SemanticReferenceHandles.GetSelectionAliases(element))
                {
                    var handle = CadHandleService.NormalizeHexHandle(rawHandle);
                    if (string.IsNullOrWhiteSpace(handle)) continue;
                    if (result.TryGetValue(handle, out var existing) &&
                        !string.Equals(existing.ElementId, element.Id, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "Semantic Handle " + handle + " đang map tới nhiều element; clash export fail-closed để không tạo provenance mơ hồ.");
                    result[handle] = projection;
                }
            }
            return result;
        }

        private static string CommonFloor(SemanticProjection? left, SemanticProjection? right)
        {
            if (left == null && right == null) return string.Empty;
            if (left == null) return right!.Floor;
            if (right == null) return left.Floor;
            return string.Equals(left.Floor, right.Floor, StringComparison.OrdinalIgnoreCase) ? left.Floor : string.Empty;
        }

        private static void Report(Document document, string operation, Exception error)
        {
            var message = error is AggregateException aggregate ? aggregate.GetBaseException().Message : error.Message;
            try { PaletteCoordinator.SetStatus(operation + ": " + message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + operation + ": " + message); } catch { }
        }

        private sealed class SemanticProjection
        {
            internal SemanticProjection(string elementId, string category, string floor)
            {
                ElementId = (elementId ?? string.Empty).Trim();
                Category = (category ?? string.Empty).Trim();
                Floor = (floor ?? string.Empty).Trim();
            }
            internal string ElementId { get; }
            internal string Category { get; }
            internal string Floor { get; }
        }
    }
}
