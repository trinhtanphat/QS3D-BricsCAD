using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Microsoft.Win32;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    internal sealed class ReviewWorkbookExportResult
    {
        public ReviewWorkbookExportResult(
            int quantityDetailCount,
            int quantitySummaryCount,
            int clashCount,
            int duplicateCount,
            int regeneratedCount,
            string modelRevision)
        {
            QuantityDetailCount = quantityDetailCount;
            QuantitySummaryCount = quantitySummaryCount;
            ClashCount = clashCount;
            DuplicateCount = duplicateCount;
            RegeneratedCount = regeneratedCount;
            ModelRevision = modelRevision;
        }

        public int QuantityDetailCount { get; }
        public int QuantitySummaryCount { get; }
        public int ClashCount { get; }
        public int DuplicateCount { get; }
        public int RegeneratedCount { get; }
        public string ModelRevision { get; }
    }

    /// <summary>
    /// Read-only host boundary for the canonical six-sheet review workbook. Quantity
    /// regeneration runs only against a detached ProjectState copy. Coordination rows
    /// project the persisted issue snapshot and never re-run clash/duplicate detectors.
    /// </summary>
    internal static class ReviewWorkbookHostService
    {
        private const int LiveHandleBatchSize = 5000;

        public static string ModelRevision(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return "CV:" + project.ChangeVersion.ToString(CultureInfo.InvariantCulture);
        }

        public static ReviewWorkbookExportResult Export(
            Document document,
            ProjectState project,
            string path,
            DateTimeOffset exportedAtUtc)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document))
                throw new InvalidOperationException("QS3D Review export requires the source DWG to remain active.");
            if (project.Elements.Count == 0)
                throw new InvalidOperationException("QS3D Review export requires at least one semantic element.");
            if (string.IsNullOrWhiteSpace(project.DrawingFingerprint))
                throw new InvalidOperationException("QS3D Review export requires a canonical drawing fingerprint.");

            var revision = ModelRevision(project);
            var preview = ProjectStateSnapshot.CreateDetachedCopy(project);
            var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault())
                .RegenerateDirty(preview);
            var details = ProjectQuantityReportBuilder.Detail(preview);
            var summary = ProjectQuantityReportBuilder.Group(preview);
            if (details.Count == 0 || summary.Count == 0)
                throw new InvalidOperationException("QS3D Review export has no canonical QTO rows.");

            var issueSnapshot = CoordinationIssuePersistence.Load(preview);
            var issueProjection = issueSnapshot == null
                ? null
                : Qs3dReviewIssueProjection.Build(preview, issueSnapshot);
            var clashes = issueProjection == null
                ? Array.Empty<CoordinationClashExportRow>()
                : issueProjection.Clashes;
            var duplicates = issueProjection == null
                ? Array.Empty<CoordinationDuplicateExportRow>()
                : issueProjection.Duplicates;
            IReadOnlyDictionary<string, CoordinationIssueExcelRow>? lifecycle =
                issueProjection == null ? null : issueProjection.LifecycleByFindingId;

            EnsureCanonicalLiveTargets(document, preview, details, clashes, duplicates);
            var drawingName = SafeDrawingName(document.Name);
            var model = new Qs3dReviewModelInfo(
                project.ProjectId,
                drawingName,
                project.DrawingFingerprint,
                revision,
                exportedAtUtc);
            Qs3dReviewWorkbookExporter.Export(
                path,
                details,
                summary,
                clashes,
                duplicates,
                null,
                model,
                lifecycle);
            return new ReviewWorkbookExportResult(
                details.Count,
                summary.Count,
                clashes.Count,
                duplicates.Count,
                regenerated,
                revision);
        }

        public static ExcelLocateResolution ResolveTrace(
            Document document,
            ProjectState project,
            Qs3dReviewTrace trace)
        {
            return ExcelLocateResolutionService.ResolveReviewTrace(
                document,
                project,
                trace,
                ModelRevision(project));
        }

        private static void EnsureCanonicalLiveTargets(
            Document document,
            ProjectState project,
            IReadOnlyList<QuantityReportRow> details,
            IReadOnlyList<CoordinationClashExportRow> clashes,
            IReadOnlyList<CoordinationDuplicateExportRow> duplicates)
        {
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in details)
            {
                var rowHandles = CanonicalHandles(row.SourceHandles, "QTO row");
                var projectHandles = CanonicalHandles(
                    SourceHandleResolver.Resolve(project, row.ElementIds),
                    "semantic project");
                if (rowHandles.Count == 0 ||
                    !rowHandles.SequenceEqual(projectHandles, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "QS3D Review QTO semantic Element ID to CAD Handle provenance is inconsistent.");
                foreach (var handle in rowHandles) expected.Add(handle);
            }
            foreach (var row in clashes)
            {
                expected.Add(CanonicalHandle(row.LeftHandle, "clash left"));
                expected.Add(CanonicalHandle(row.RightHandle, "clash right"));
            }
            foreach (var row in duplicates)
            {
                expected.Add(CanonicalHandle(row.LeftHandle, "duplicate left"));
                expected.Add(CanonicalHandle(row.RightHandle, "duplicate right"));
            }
            if (expected.Count == 0)
                throw new InvalidOperationException("QS3D Review export has no CAD Handle provenance.");

            var live = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var batch in Qs3dReviewLiveHandleBatchPlanner.Create(expected, LiveHandleBatchSize))
                live.UnionWith(CadHandleService.GetLiveHandles(document, batch));
            var missing = expected.Where(handle => !live.Contains(handle)).ToList();
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    "QS3D Review export blocked: " + missing.Count.ToString(CultureInfo.InvariantCulture) +
                    " CAD Handle(s) are stale or missing. Selection and project state were not changed.");
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> handles, string label)
        {
            return (handles ?? throw new ArgumentNullException(nameof(handles)))
                .Select(handle => CanonicalHandle(handle, label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(handle => handle, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        private static string CanonicalHandle(string handle, string label)
        {
            return CadHandleService.NormalizeHexHandle(handle)
                ?? throw new InvalidOperationException("QS3D Review " + label + " contains an invalid CAD Handle.");
        }

        private static string SafeDrawingName(string drawingPath)
        {
            try
            {
                var name = Path.GetFileName(drawingPath ?? string.Empty);
                return string.IsNullOrWhiteSpace(name) ? "QS3D.dwg" : name;
            }
            catch { return "QS3D.dwg"; }
        }
    }

    public sealed class ReviewWorkbookCommands
    {
        [CommandMethod("QS3DREVIEWEXPORT", CommandFlags.UsePickSet)]
        public void ExportReviewWorkbook()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("QS3D Review export cần một QS3D project hiện hữu; export không tạo project mới.");
                if (project.Elements.Count == 0)
                    throw new InvalidOperationException("QS3D Review export chưa có semantic element để xuất.");
                if (!DrawingUnitWorkflow.EnsureResolved(document, "QS3DREVIEWEXPORT")) return;

                var dialog = new SaveFileDialog
                {
                    Title = "QS3D • Six-sheet QS Review",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    AddExtension = true,
                    OverwritePrompt = true,
                    FileName = SafeDrawingStem(document.Name) + "-QS3D-REVIEW.xlsx"
                };
                if (dialog.ShowDialog() != true) return;

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))
                    throw new InvalidOperationException("QS3D project không còn khả dụng sau khi chọn file export.");
                var result = ReviewWorkbookHostService.Export(
                    document,
                    currentProject,
                    dialog.FileName,
                    DateTimeOffset.UtcNow);
                var status = "QS Review: " + result.QuantityDetailCount + " QTO • " +
                             result.ClashCount + " clash • " + result.DuplicateCount + " duplicate • " +
                             result.ModelRevision + " • " + dialog.FileName;
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage(
                    "\nQS3D " + status +
                    "\nDùng QS3DREVIEWLOCATE để định vị 02_CHI_TIET_QTO / 03_CLASHES / 04_DUPLICATES về model.");
            }
            catch (Exception error) { Report(document, "QS3DREVIEWEXPORT", error); }
        }

        [CommandMethod("QS3DREVIEWLOCATE", CommandFlags.UsePickSet)]
        public void LocateReviewWorkbookRow()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out _))
                    throw new InvalidOperationException("QS Review Excel → Model cần một QS3D project hiện hữu; locate không tạo project mới.");
                var dialog = new OpenFileDialog
                {
                    Title = "QS3D • QS Review Excel → Model",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (dialog.ShowDialog() != true) return;

                var sheetPrompt = document.Editor.GetKeywords(
                    "\nSheet [QTO/CLASHES/DUPLICATES] <QTO>: ",
                    "QTO CLASHES DUPLICATES");
                if (sheetPrompt.Status != PromptStatus.OK && sheetPrompt.Status != PromptStatus.None) return;
                var sheet = SheetName(sheetPrompt.Status == PromptStatus.None ? "QTO" : sheetPrompt.StringResult);
                var rowPrompt = new PromptIntegerOptions("\nNhập số dòng QS Review cần định vị: ")
                {
                    AllowNone = false,
                    LowerLimit = 2,
                    UseDefaultValue = true,
                    DefaultValue = 2
                };
                var row = document.Editor.GetInteger(rowPrompt);
                if (row.Status != PromptStatus.OK) return;

                var trace = Qs3dReviewWorkbookTraceReader.Read(dialog.FileName, sheet, row.Value);
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))
                    throw new InvalidOperationException("QS3D project không còn khả dụng trước khi Model Locate.");
                var resolution = ReviewWorkbookHostService.ResolveTrace(document, currentProject, trace);

                // The resolver completes identity, provenance and every Handle lookup first.
                // Only a fully successful result may replace the current native PICKFIRST set.
                document.Editor.SetImpliedSelection(resolution.ObjectIds.ToArray());
                var status = "QS Review Excel → Model " + trace.SheetName + " dòng " + trace.RowNumber +
                             ": " + trace.ElementIds.Count + " element • " + resolution.ObjectIds.Count + " CAD object";
                PaletteCoordinator.SetStatus(status);
                document.Editor.WriteMessage("\nQS3D " + status + ".");
                document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);
            }
            catch (Exception error) { Report(document, "QS3DREVIEWLOCATE", error); }
        }

        private static string SheetName(string keyword)
        {
            if (string.Equals(keyword, "QTO", StringComparison.OrdinalIgnoreCase))
                return Qs3dReviewWorkbookExporter.QuantitySheet;
            if (string.Equals(keyword, "CLASHES", StringComparison.OrdinalIgnoreCase))
                return Qs3dReviewWorkbookExporter.ClashSheet;
            if (string.Equals(keyword, "DUPLICATES", StringComparison.OrdinalIgnoreCase))
                return Qs3dReviewWorkbookExporter.DuplicateSheet;
            throw new InvalidOperationException("QS Review sheet không được hỗ trợ: " + keyword + ".");
        }

        private static string SafeDrawingStem(string drawingPath)
        {
            try
            {
                var stem = Path.GetFileNameWithoutExtension(drawingPath ?? string.Empty);
                return string.IsNullOrWhiteSpace(stem) ? "QS3D" : stem;
            }
            catch { return "QS3D"; }
        }

        private static void Report(Document document, string operation, Exception error)
        {
            var message = error is AggregateException aggregate ? aggregate.GetBaseException().Message : error.Message;
            try { PaletteCoordinator.SetStatus(operation + ": " + message); } catch { }
            try { document.Editor.WriteMessage("\nQS3D " + operation + ": " + message); } catch { }
        }
    }
}
