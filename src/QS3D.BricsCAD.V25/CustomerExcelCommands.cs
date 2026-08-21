using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CustomerExcelCommands
    {
        [CommandMethod("QS3DEXCEL", CommandFlags.UsePickSet)]
        public void ExportCustomerWorkbook()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Xuất Excel cần một QS3D project hiện hữu; export không tạo project mới.");
                if (project.Elements.Count == 0)
                    throw new InvalidOperationException("Xuất Excel chưa có semantic element để xuất. Capture/dựng mô hình trước.");
                if (!DrawingUnitWorkflow.EnsureResolved(document, "QS3DEXCEL")) return;

                var implied = Cad.EntitySnapshotReader.ReadImpliedSelection(document);
                var defaultScope = implied.Count > 0 ? "Selection" : "All";
                var scopePrompt = document.Editor.GetKeywords(
                    "\nPhạm vi Excel [Selection/Floor/Zone/All] <" + defaultScope + ">: ",
                    "Selection Floor Zone All");
                if (scopePrompt.Status != PromptStatus.OK && scopePrompt.Status != PromptStatus.None) return;
                var scope = scopePrompt.Status == PromptStatus.None ? defaultScope : scopePrompt.StringResult;
                var allScope = string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase);
                var elementIds = allScope ? Array.Empty<string>() : ResolveScope(document, project, scope, implied);

                var preview = ProjectStateSnapshot.CreateDetachedCopy(project);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(preview);
                var details = allScope
                    ? ProjectQuantityReportBuilder.Detail(preview)
                    : ProjectQuantityReportBuilder.Detail(preview, elementIds);
                var summary = allScope
                    ? ProjectQuantityReportBuilder.Group(preview)
                    : ProjectQuantityReportBuilder.Group(preview, elementIds);
                if (details.Count == 0) throw new InvalidOperationException("Phạm vi Excel " + scope + " không có cấu kiện hợp lệ để xuất.");
                EnsureHandlesAreLive(document, details);

                var drawingName = string.IsNullOrWhiteSpace(document.Name) ? "QS3D" : Path.GetFileNameWithoutExtension(document.Name);
                var dialog = new SaveFileDialog
                {
                    Title = "QS3D • Xuất Excel",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = drawingName + "-QS3D.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                QsCustomerWorkbookExporter.Export(dialog.FileName, details, summary);
                var status = "Xuất Excel " + scope + ": " + details.Count + " CHI_TIET • " + summary.Count +
                             " DGKL • preview-regenerate " + regenerated + " • " + dialog.FileName;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + "\nDùng QS3DEXCELTRACE để định vị DGKL/COP_PHA/CHI_TIET ngược về mô hình.");
            }
            catch (Exception error)
            {
                Report(document, "QS3DEXCEL", error);
            }
        }

        [CommandMethod("QS3DEXCELTRACE", CommandFlags.Modal)]
        public void LocateCustomerWorkbookRow()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("Excel → CAD cần một QS3D project hiện hữu; lệnh định vị không tạo project mới.");
                var dialog = new OpenFileDialog
                {
                    Title = "QS3D • Excel → CAD",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var sheetPrompt = document.Editor.GetKeywords("\nSheet [DGKL/COP_PHA/CHI_TIET] <CHI_TIET>: ", "DGKL COP_PHA CHI_TIET");
                if (sheetPrompt.Status != PromptStatus.OK && sheetPrompt.Status != PromptStatus.None) return;
                var sheet = sheetPrompt.Status == PromptStatus.None ? QsCustomerWorkbookExporter.DetailSheet : sheetPrompt.StringResult;
                var rowPrompt = new PromptIntegerOptions("\nNhập số dòng Excel cần định vị: ")
                {
                    AllowNone = false,
                    LowerLimit = 2,
                    UseDefaultValue = true,
                    DefaultValue = 2
                };
                var row = document.Editor.GetInteger(rowPrompt);
                if (row.Status != PromptStatus.OK) return;

                var trace = QsCustomerWorkbookTraceReader.Read(dialog.FileName, sheet, row.Value);
                var resolution = ExcelLocateResolutionService.ResolveCustomerTrace(document, project, trace);
                document.Editor.SetImpliedSelection(resolution.ObjectIds.ToArray());
                var status = "Excel → CAD " + trace.WorksheetName + " dòng " + trace.RowNumber + ": " +
                             trace.ElementIds.Count + " element • " + resolution.ObjectIds.Count + " CAD object";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
                if (resolution.ObjectIds.Count > 0) document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
            }
            catch (Exception error)
            {
                Report(document, "QS3DEXCELTRACE", error);
            }
        }

        private static IReadOnlyList<string> ResolveScope(
            Document document,
            ProjectState project,
            string scope,
            IReadOnlyList<EntitySnapshot> implied)
        {
            if (string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Excel All scope phải dùng quantity report không lọc.");
            if (string.Equals(scope, "Floor", StringComparison.OrdinalIgnoreCase))
            {
                var floor = project.FindFloor(project.ActiveFloorId) ?? throw new InvalidOperationException("Excel Floor cần một Floor/Level active hợp lệ.");
                return project.Elements.Where(element => string.Equals(element.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(element => element.Id).ToList().AsReadOnly();
            }
            if (string.Equals(scope, "Zone", StringComparison.OrdinalIgnoreCase))
            {
                var zone = project.FindZone(project.ActiveZoneId) ?? throw new InvalidOperationException("Excel Zone cần một Zone active hợp lệ.");
                return project.Elements.Where(element => string.Equals(element.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(element => element.Id).ToList().AsReadOnly();
            }
            if (!string.Equals(scope, "Selection", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Excel scope không được hỗ trợ: " + scope + ".");

            var snapshots = implied.Count > 0 ? implied : Cad.EntitySnapshotReader.ReadCurrentSelection(document);
            if (snapshots.Count == 0) throw new InvalidOperationException("Excel Selection cần ít nhất một CAD object đã được QS3D theo dõi.");
            var selectedHandles = new HashSet<string>(
                snapshots.Select(snapshot => (snapshot.Handle ?? string.Empty).Trim()).Where(handle => handle.Length > 0),
                StringComparer.OrdinalIgnoreCase);
            var elements = project.Elements.Where(element => SemanticReferenceHandles.MatchesSelection(element, selectedHandles)).ToList();
            if (elements.Count == 0) throw new InvalidOperationException("Excel Selection không khớp semantic element nào.");
            var aliases = new HashSet<string>(elements.SelectMany(SemanticReferenceHandles.GetSelectionAliases), StringComparer.OrdinalIgnoreCase);
            var untracked = selectedHandles.Where(handle => !aliases.Contains(handle)).OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase).ToList();
            if (untracked.Count > 0)
                throw new InvalidOperationException("Excel Selection trộn CAD object chưa thuộc semantic scope: " + string.Join(", ", untracked) + ".");
            return elements.Select(element => element.Id).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static void EnsureHandlesAreLive(Document document, IReadOnlyList<QuantityReportRow> details)
        {
            var expected = details.SelectMany(row => row.SourceHandles)
                .Select(handle => Cad.CadHandleService.NormalizeHexHandle(handle) ?? throw new InvalidOperationException("Excel contains an invalid CAD Handle: " + handle + "."))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase).ToList();
            if (expected.Count == 0) throw new InvalidOperationException("Excel scope has no CAD Handle provenance.");
            var live = Cad.CadHandleService.GetLiveHandles(document, expected);
            var missing = expected.Where(handle => !live.Contains(handle)).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException("Xuất Excel bị chặn: " + missing.Count + " source Handle stale/missing. Chạy QS3DSYNCSOURCE hoặc recapture trước.");
        }

        private static void Report(Document document, string operation, Exception error)
        {
            var message = error is AggregateException aggregate ? aggregate.GetBaseException().Message : error.Message;
            try { PaletteCoordinator.SetStatus(operation + ": " + message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + operation + ": " + message); } catch { }
        }
    }
}
