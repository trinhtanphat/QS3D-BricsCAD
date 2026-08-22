using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Domain;
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
                var positive = ExcelLocateResolutionService.ResolveModern(document, project, lookup);
                var workbookHandles = positive.Handles;
                var locatedIds = positive.ObjectIds;
                document.Editor.SetImpliedSelection(locatedIds.ToArray());
                var baselineSelection = CurrentImpliedSelection(document);
                var selectedCount = baselineSelection.Count;
                if (selectedCount != locatedIds.Count)
                    throw new InvalidOperationException("Excel Locate did not establish the expected PICKFIRST selection.");

                var projectStamp = ProjectReadOnlyStamp.Capture(project);
                var missingHandle = FindMissingHandle(document, exportHandles);
                var negativeAttempts = 0;
                var negativeRefusals = 0;
                var negativePickfirstPreserved = 0;
                var semanticUnchanged = 0;

                AssertNegative(
                    document, project, projectStamp, baselineSelection,
                    ExcelLocateFailureCode.FingerprintMismatch,
                    () => ExcelLocateResolutionService.ResolveModernRow(
                        document, project, lookup.ElementIds, lookup.Handles, "WRONG-" + nonce),
                    ref negativeAttempts, ref negativeRefusals, ref negativePickfirstPreserved, ref semanticUnchanged);

                AssertNegative(
                    document, project, projectStamp, baselineSelection,
                    ExcelLocateFailureCode.UnknownElementId,
                    () => ExcelLocateResolutionService.ResolveModernRow(
                        document, project, new[] { "UNKNOWN-" + nonce }, lookup.Handles, project.DrawingFingerprint),
                    ref negativeAttempts, ref negativeRefusals, ref negativePickfirstPreserved, ref semanticUnchanged);

                var staleProject = NegativeProject(project.DrawingFingerprint, "NEG-STALE", new[] { missingHandle });
                AssertNegative(
                    document, project, projectStamp, baselineSelection,
                    ExcelLocateFailureCode.NoLiveHandles,
                    () => ExcelLocateResolutionService.ResolveModernRow(
                        document, staleProject, new[] { "NEG-STALE" }, new[] { missingHandle }, staleProject.DrawingFingerprint),
                    ref negativeAttempts, ref negativeRefusals, ref negativePickfirstPreserved, ref semanticUnchanged);

                var liveHandle = workbookHandles[0];
                var partialProject = NegativeProject(project.DrawingFingerprint, "NEG-PARTIAL", new[] { liveHandle, missingHandle });
                AssertNegative(
                    document, project, projectStamp, baselineSelection,
                    ExcelLocateFailureCode.PartialResolution,
                    () => ExcelLocateResolutionService.ResolveModernRow(
                        document, partialProject, new[] { "NEG-PARTIAL" }, new[] { liveHandle, missingHandle }, partialProject.DrawingFingerprint),
                    ref negativeAttempts, ref negativeRefusals, ref negativePickfirstPreserved, ref semanticUnchanged);

                var staleResolvedCount = CadHandleService.Resolve(document, new[] { missingHandle }).Count;
                var partialResolvedCount = CadHandleService.Resolve(document, new[] { liveHandle, missingHandle }).Count;
                if (negativeAttempts != 4 || negativeRefusals != 4 || negativePickfirstPreserved != 4 || semanticUnchanged != 4 ||
                    staleResolvedCount != 0 || partialResolvedCount != 1)
                    throw new InvalidOperationException("Excel Locate negative qualification matrix is incomplete.");

                WriteMarkerAtomic(validatedResultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DBRCROUNDTRIPPROBE",
                    "process=" + OneLine(Process.GetCurrentProcess().ProcessName),
                    "nonce=" + nonce,
                    "schema=QS3D_BRC_QUANTITY_ROUNDTRIP_V2",
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "project_element_count=" + project.Elements.Count.ToString(CultureInfo.InvariantCulture),
                    "regenerated_count=" + regenerated.ToString(CultureInfo.InvariantCulture),
                    "detail_row_count=" + detailRows.Count.ToString(CultureInfo.InvariantCulture),
                    "summary_row_count=" + summaryRows.Count.ToString(CultureInfo.InvariantCulture),
                    "live_export_handle_count=" + liveIds.Count.ToString(CultureInfo.InvariantCulture),
                    "located_handle_count=" + locatedIds.Count.ToString(CultureInfo.InvariantCulture),
                    "selected_object_count=" + selectedCount.ToString(CultureInfo.InvariantCulture),
                    "prior_pickfirst_count=" + baselineSelection.Count.ToString(CultureInfo.InvariantCulture),
                    "negative_attempt_count=" + negativeAttempts.ToString(CultureInfo.InvariantCulture),
                    "negative_refusal_count=" + negativeRefusals.ToString(CultureInfo.InvariantCulture),
                    "negative_pickfirst_preserved_count=" + negativePickfirstPreserved.ToString(CultureInfo.InvariantCulture),
                    "semantic_unchanged_case_count=" + semanticUnchanged.ToString(CultureInfo.InvariantCulture),
                    "stale_requested_handle_count=1",
                    "stale_resolved_handle_count=" + staleResolvedCount.ToString(CultureInfo.InvariantCulture),
                    "partial_requested_handle_count=2",
                    "partial_resolved_handle_count=" + partialResolvedCount.ToString(CultureInfo.InvariantCulture),
                    "eligible_cad_target_count=1",
                    "proxy_locate_attempt_count=0",
                    "wrong_fingerprint_refused=true",
                    "wrong_fingerprint_pickfirst_preserved=true",
                    "unknown_element_refused=true",
                    "unknown_element_pickfirst_preserved=true",
                    "stale_handle_refused=true",
                    "stale_handle_pickfirst_preserved=true",
                    "partial_resolution_refused=true",
                    "partial_resolution_pickfirst_preserved=true",
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

        private static void AssertNegative(
            Document document,
            ProjectState authoritativeProject,
            ProjectReadOnlyStamp authoritativeStamp,
            IReadOnlyList<Teigha.DatabaseServices.ObjectId> baselineSelection,
            ExcelLocateFailureCode expectedCode,
            Action action,
            ref int attemptCount,
            ref int refusalCount,
            ref int selectionPreservedCount,
            ref int semanticUnchangedCount)
        {
            attemptCount = checked(attemptCount + 1);
            try
            {
                action();
                throw new InvalidOperationException("Excel Locate negative case was accepted unexpectedly.");
            }
            catch (ExcelLocateResolutionException ex) when (ex.Code == expectedCode)
            {
                refusalCount = checked(refusalCount + 1);
            }

            var afterSelection = CurrentImpliedSelection(document);
            if (!SameObjectIds(baselineSelection, afterSelection))
                throw new InvalidOperationException("Excel Locate negative case changed PICKFIRST.");
            selectionPreservedCount = checked(selectionPreservedCount + 1);
            authoritativeStamp.RequireUnchanged(authoritativeProject);
            semanticUnchangedCount = checked(semanticUnchangedCount + 1);
        }

        private static ProjectState NegativeProject(string drawingFingerprint, string elementId, IEnumerable<string> handles)
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Excel Locate negative probe")
            {
                DrawingFingerprint = drawingFingerprint ?? string.Empty
            };
            var element = new ProjectElement(elementId, ElementCategory.Beam)
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            foreach (var handle in handles) element.SourceHandles.Add(handle);
            project.Elements.Add(element);
            return project;
        }

        private static string FindMissingHandle(Document document, IEnumerable<string> existing)
        {
            var used = new HashSet<string>(CanonicalHandles(existing), StringComparer.OrdinalIgnoreCase);
            for (var value = long.MaxValue; value > long.MaxValue - 4096L; value--)
            {
                var candidate = value.ToString("X", CultureInfo.InvariantCulture);
                if (used.Contains(candidate)) continue;
                if (CadHandleService.Resolve(document, new[] { candidate }).Count == 0) return candidate;
            }
            throw new InvalidOperationException("Cannot allocate a missing CAD Handle for Excel Locate qualification.");
        }

        private static IReadOnlyList<Teigha.DatabaseServices.ObjectId> CurrentImpliedSelection(Document document)
        {
            var selection = document.Editor.SelectImplied();
            return selection.Value?.GetObjectIds().ToList().AsReadOnly()
                ?? new List<Teigha.DatabaseServices.ObjectId>().AsReadOnly();
        }

        private static bool SameObjectIds(
            IEnumerable<Teigha.DatabaseServices.ObjectId> left,
            IEnumerable<Teigha.DatabaseServices.ObjectId> right)
        {
            var a = left.OrderBy(x => x.Handle.ToString(), StringComparer.OrdinalIgnoreCase).ToList();
            var b = right.OrderBy(x => x.Handle.ToString(), StringComparer.OrdinalIgnoreCase).ToList();
            return a.SequenceEqual(b);
        }

        private sealed class ProjectReadOnlyStamp
        {
            private ProjectReadOnlyStamp(long changeVersion, DateTime updatedUtc, int elementCount, int auditCount, string elementState)
            {
                ChangeVersion = changeVersion;
                UpdatedUtc = updatedUtc;
                ElementCount = elementCount;
                AuditCount = auditCount;
                ElementState = elementState;
            }

            private long ChangeVersion { get; }
            private DateTime UpdatedUtc { get; }
            private int ElementCount { get; }
            private int AuditCount { get; }
            private string ElementState { get; }

            public static ProjectReadOnlyStamp Capture(ProjectState project) => new ProjectReadOnlyStamp(
                project.ChangeVersion,
                project.UpdatedUtc,
                project.Elements.Count,
                project.AuditEvents.Count,
                ElementDigest(project));

            public void RequireUnchanged(ProjectState project)
            {
                if (project.ChangeVersion != ChangeVersion || project.UpdatedUtc != UpdatedUtc ||
                    project.Elements.Count != ElementCount || project.AuditEvents.Count != AuditCount ||
                    !string.Equals(ElementDigest(project), ElementState, StringComparison.Ordinal))
                    throw new InvalidOperationException("Excel Locate negative case mutated the authoritative semantic project.");
            }

            private static string ElementDigest(ProjectState project)
            {
                return string.Join("\u001e", project.Elements
                    .OrderBy(element => element.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(element => string.Join("\u001f", new[]
                    {
                        element.Id,
                        element.Category.ToString(),
                        element.FamilyId,
                        element.FloorId,
                        element.ZoneId,
                        element.DrawingFingerprint,
                        element.Dirty.ToString(),
                        element.UpdatedUtc.ToString("O", CultureInfo.InvariantCulture),
                        string.Join(";", element.SourceHandles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)),
                        string.Join(";", element.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key + "=" + x.Value)),
                        string.Join(";", element.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key + "=" + x.Value.ToString("R", CultureInfo.InvariantCulture))),
                    })));
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
