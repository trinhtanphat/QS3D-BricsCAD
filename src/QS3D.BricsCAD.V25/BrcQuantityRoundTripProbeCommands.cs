using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Export;
using QS3D.Core.Persistence;
using QS3D.Core.Recognition;
using QS3D.Core.Reporting;
using QS3D.Core.Services;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only qualification for the clean-room B4D -> ED2 -> Excel Locate
    /// bridge. The caller must open a disposable reference copy and run QS3DB4D
    /// first. The marker contains aggregate counts only: no paths, handles, element
    /// ids, layers, text, metadata values, or proprietary BLT data are persisted.
    /// </summary>
    public sealed class BrcQuantityRoundTripProbeCommands
    {
        private const string ResultVariable = "QS3D_BRC_ROUNDTRIP_RESULT";
        private const string WorkbookVariable = "QS3D_BRC_ROUNDTRIP_WORKBOOK";
        private const string NonceVariable = "QS3D_BRC_ROUNDTRIP_NONCE";
        private const string ResultFileName = "brc-quantity-roundtrip-result.txt";
        private const string WorkbookFileName = "brc-quantity-roundtrip.xlsx";

        [CommandMethod("QS3DBRCROUNDTRIPPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (string.IsNullOrWhiteSpace(resultPath))
            {
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D BRC quantity round-trip probe skipped: " + ResultVariable + " is not set.");
                return;
            }

            try
            {
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("The BRC quantity round-trip probe nonce is invalid.");
                var workbookPath = RequiredOutputPath(
                    Environment.GetEnvironmentVariable(WorkbookVariable), WorkbookFileName, "workbook");
                var validatedResultPath = RequiredOutputPath(resultPath, ResultFileName, "result");
                if (!string.Equals(Path.GetDirectoryName(validatedResultPath), Path.GetDirectoryName(workbookPath), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The BRC quantity round-trip outputs must use the same qualification directory.");
                if (File.Exists(validatedResultPath) || File.Exists(workbookPath))
                    throw new IOException("The BRC quantity round-trip outputs must not already exist.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available for the BRC quantity round-trip probe.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project) || project.Elements.Count == 0)
                    throw new InvalidOperationException("QS3DB4D did not create any semantic elements for the round-trip probe.");

                var snapshots = EntitySnapshotReader.ReadCurrentSpace(document);
                var proxySnapshots = snapshots
                    .Where(x => string.Equals(x.EntityType, "ProxyEntity", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var proxyBatch = new ProjectRecognitionService().SuggestBatch(project, proxySnapshots);
                var proxyCaptureReadyCount = proxyBatch.Results.Count(x => x.IsCaptureReady);
                var proxyAutoAcceptedCount = proxyBatch.AutoAccepted.Count;
                var proxyHandles = new HashSet<string>(proxySnapshots.Select(x => x.Handle), StringComparer.OrdinalIgnoreCase);
                var proxyCapturedOwnerCount = project.Elements.Count(
                    x => x.SourceHandles.Any(handle => proxyHandles.Contains((handle ?? string.Empty).Trim())));
                if (proxyCaptureReadyCount != 0 || proxyAutoAcceptedCount != 0 || proxyCapturedOwnerCount != 0)
                    throw new InvalidOperationException("Metricless BRC proxy entities were not kept review-only and uncaptured.");

                var preview = ProjectStateSnapshot.CreateDetachedCopy(project);
                var regenerated = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(preview);
                var detailRows = ProjectQuantityReportBuilder.Detail(preview);
                var summaryRows = ProjectQuantityReportBuilder.Group(preview);
                if (detailRows.Count == 0 || summaryRows.Count == 0)
                    throw new InvalidOperationException("QS3DB4D produced no authoritative ED2 rows for the reference copy.");

                var exportHandles = CanonicalHandles(detailRows.SelectMany(x => x.SourceHandles));
                if (exportHandles.Count == 0)
                    throw new InvalidOperationException("ED2 detail rows contain no CAD Handle provenance.");
                var liveIds = CadHandleService.Resolve(document, exportHandles);
                if (liveIds.Count != exportHandles.Count)
                    throw new InvalidOperationException("ED2 contains stale or unresolved CAD Handle provenance.");

                XlsxQuantityExporter.ExportEd2(workbookPath, detailRows, summaryRows);
                var lookup = XlsxHandleReader.ReadHandleLookup(workbookPath, 2);
                if (!lookup.IsModernSchema || !lookup.IsEd2Detail ||
                    !string.Equals(lookup.WorksheetName, "CHI_TIET", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The generated workbook did not expose the modern ED2 CHI_TIET schema.");
                if (lookup.ElementIds.Count != 1)
                    throw new InvalidDataException("The first ED2 CHI_TIET row must identify exactly one semantic element.");
                if (!string.Equals(lookup.DrawingFingerprint, project.DrawingFingerprint, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The generated ED2 workbook fingerprint does not match the active drawing.");
                if (project.FindElement(lookup.ElementIds[0]) == null)
                    throw new InvalidDataException("The generated ED2 workbook references an unknown semantic element.");

                var projectHandles = CanonicalHandles(SourceHandleResolver.Resolve(project, lookup.ElementIds));
                var workbookHandles = CanonicalHandles(lookup.Handles);
                if (!projectHandles.SequenceEqual(workbookHandles, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException("The generated ED2 Element ID and CAD Handle provenance do not round-trip.");
                var locatedIds = CadHandleService.Resolve(document, projectHandles);
                if (locatedIds.Count != projectHandles.Count)
                    throw new InvalidOperationException("Excel Locate could not resolve every Handle without a partial selection.");
                document.Editor.SetImpliedSelection(locatedIds.ToArray());
                var implied = document.Editor.SelectImplied();
                var selectedCount = implied.Value?.Count ?? 0;
                if (selectedCount != locatedIds.Count)
                    throw new InvalidOperationException("Excel Locate did not establish the expected PICKFIRST selection.");

                WriteMarkerAtomic(validatedResultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DBRCROUNDTRIPPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "schema=QS3D_BRC_QUANTITY_ROUNDTRIP_V1",
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "project_element_count=" + project.Elements.Count.ToString(CultureInfo.InvariantCulture),
                    "regenerated_count=" + regenerated.ToString(CultureInfo.InvariantCulture),
                    "detail_row_count=" + detailRows.Count.ToString(CultureInfo.InvariantCulture),
                    "summary_row_count=" + summaryRows.Count.ToString(CultureInfo.InvariantCulture),
                    "live_export_handle_count=" + liveIds.Count.ToString(CultureInfo.InvariantCulture),
                    "located_handle_count=" + locatedIds.Count.ToString(CultureInfo.InvariantCulture),
                    "selected_object_count=" + selectedCount.ToString(CultureInfo.InvariantCulture),
                    "proxy_snapshot_count=" + proxySnapshots.Count.ToString(CultureInfo.InvariantCulture),
                    "proxy_capture_ready_count=" + proxyCaptureReadyCount.ToString(CultureInfo.InvariantCulture),
                    "proxy_autoaccepted_count=" + proxyAutoAcceptedCount.ToString(CultureInfo.InvariantCulture),
                    "proxy_captured_owner_count=" + proxyCapturedOwnerCount.ToString(CultureInfo.InvariantCulture),
                    "modern_ed2_schema=true",
                    "detail_sheet_resolved=true",
                    "drawing_fingerprint_matched=true",
                    "element_handle_provenance_matched=true"
                });
                document.Editor.WriteMessage("\nQS3D BRC quantity round-trip probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D BRC quantity round-trip probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> handles)
        {
            if (handles == null) throw new ArgumentNullException(nameof(handles));
            return handles
                .Select(x => CadHandleService.NormalizeHexHandle(x)
                    ?? throw new InvalidDataException("The BRC quantity round-trip contains invalid CAD Handle provenance."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        private static string RequiredOutputPath(string? value, string expectedFileName, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Probe " + label + " path is required.", label);
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), expectedFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The BRC quantity round-trip " + label + " filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("The BRC quantity round-trip output directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? resultPath)
        {
            try
            {
                var normalized = (resultPath ?? string.Empty).Trim();
                if (normalized.Length > 0 && !File.Exists(normalized))
                    WriteMarkerAtomic(normalized, new[]
                    {
                        "status=FAIL",
                        "command=QS3DBRCROUNDTRIPPROBE",
                        "error_code=ROUNDTRIP_FAILED"
                    });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredOutputPath(resultPath, ResultFileName, "result");
            if (File.Exists(fullPath)) throw new IOException("The BRC quantity round-trip result already exists.");
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(tempPath, fullPath);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string OneLine(string value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }
}
