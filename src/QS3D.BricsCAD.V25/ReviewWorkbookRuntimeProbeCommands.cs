using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Reporting;
using QS3D.Platform.Domain;
using QS3D.Platform.Parity;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only exact-host qualification for #3536. It creates disposable native
    /// entities and semantic review state, exports the production six-sheet workbook, and
    /// drives all three production trace kinds through the same resolver used by the UI.
    /// Result markers contain aggregate evidence only.
    /// </summary>
    public sealed class ReviewWorkbookRuntimeProbeCommands
    {
        private const string ResultVariable = "QS3D_REVIEW_ROUNDTRIP_RESULT";
        private const string WorkbookVariable = "QS3D_REVIEW_ROUNDTRIP_WORKBOOK";
        private const string NonceVariable = "QS3D_REVIEW_ROUNDTRIP_NONCE";
        private const string ResultFileName = "review-workbook-roundtrip-result.txt";
        private const string WorkbookFileName = "review-workbook-roundtrip.xlsx";

        [CommandMethod("QS3DREVIEWROUNDTRIPPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            try
            {
                var validatedResultPath = RequiredOutputPath(resultPath, ResultFileName, "result");
                var workbookPath = RequiredOutputPath(
                    Environment.GetEnvironmentVariable(WorkbookVariable), WorkbookFileName, "workbook");
                if (!string.Equals(Path.GetDirectoryName(validatedResultPath), Path.GetDirectoryName(workbookPath), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("QS3D Review probe outputs must use one qualification directory.");
                if (File.Exists(validatedResultPath) || File.Exists(workbookPath))
                    throw new IOException("QS3D Review probe outputs must not already exist.");
                var nonce = Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty;
                if (!Guid.TryParseExact(nonce, "N", out _))
                    throw new InvalidOperationException("QS3D Review probe nonce is invalid.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active BricsCAD document is available.");
                var project = ProjectContextCoordinator.GetOrCreate(document);
                if (project.Elements.Count != 0 || CoordinationIssuePersistence.Load(project) != null)
                    throw new InvalidOperationException("QS3D Review probe requires a disposable drawing with empty semantic/coordination state.");

                var seeded = Seed(document, project);
                var authoritative = ProjectReadOnlyStamp.Capture(project);
                var export = ReviewWorkbookHostService.Export(document, project, workbookPath, DateTimeOffset.UtcNow);
                if (export.QuantityDetailCount != 3 || export.QuantitySummaryCount != 3 ||
                    export.ClashCount != 1 || export.DuplicateCount != 1)
                    throw new InvalidOperationException("QS3D Review production export did not contain the seeded 3/3/1/1 review scope.");
                authoritative.RequireUnchanged(project);

                var qtoTrace = Qs3dReviewWorkbookTraceReader.Read(workbookPath, Qs3dReviewWorkbookExporter.QuantitySheet, 2);
                var clashTrace = Qs3dReviewWorkbookTraceReader.Read(workbookPath, Qs3dReviewWorkbookExporter.ClashSheet, 2);
                var duplicateTrace = Qs3dReviewWorkbookTraceReader.Read(workbookPath, Qs3dReviewWorkbookExporter.DuplicateSheet, 2);
                var qto = ReviewWorkbookHostService.ResolveTrace(document, project, qtoTrace);
                var clash = ReviewWorkbookHostService.ResolveTrace(document, project, clashTrace);
                var duplicate = ReviewWorkbookHostService.ResolveTrace(document, project, duplicateTrace);
                RequireSelection(document, qto, 1);
                RequireSelection(document, clash, 2);
                RequireSelection(document, duplicate, 2);
                var baselineSelection = CurrentImpliedSelection(document);
                authoritative.RequireUnchanged(project);

                var negativeAttempts = 0;
                var negativeRefusals = 0;
                var negativeSelectionPreserved = 0;
                var negativeSemanticUnchanged = 0;
                var wrongFingerprint = NegativeProject("wrong-" + project.DrawingFingerprint, qtoTrace.ElementIds, qtoTrace.Handles);
                AssertNegative(
                    document, project, authoritative, baselineSelection,
                    () => ReviewWorkbookHostService.ResolveTrace(document, wrongFingerprint, qtoTrace),
                    ref negativeAttempts, ref negativeRefusals, ref negativeSelectionPreserved, ref negativeSemanticUnchanged);

                var directory = Path.GetDirectoryName(workbookPath) ?? throw new InvalidOperationException("Workbook directory is unavailable.");
                var staleRevisionPath = Path.Combine(directory, "review-wrong-revision.xlsx");
                WriteNegativeWorkbook(staleRevisionPath, project.DrawingFingerprint, "STALE-" + nonce, seeded[0], seeded[1], false);
                var staleRevisionTrace = Qs3dReviewWorkbookTraceReader.Read(
                    staleRevisionPath, Qs3dReviewWorkbookExporter.QuantitySheet, 2);
                AssertNegative(
                    document, project, authoritative, baselineSelection,
                    () => ReviewWorkbookHostService.ResolveTrace(document, project, staleRevisionTrace),
                    ref negativeAttempts, ref negativeRefusals, ref negativeSelectionPreserved, ref negativeSemanticUnchanged);

                var missingHandle = FindMissingHandle(document, seeded.Select(x => x.Handle));
                var staleProject = NegativeProject(project.DrawingFingerprint, new[] { "NEG-QTO" }, new[] { missingHandle });
                var stalePath = Path.Combine(directory, "review-stale-handle.xlsx");
                WriteNegativeWorkbook(stalePath, staleProject.DrawingFingerprint, ReviewWorkbookHostService.ModelRevision(staleProject),
                    new SeededElement("NEG-QTO", missingHandle, ElementCategory.Beam), null, false);
                var staleTrace = Qs3dReviewWorkbookTraceReader.Read(stalePath, Qs3dReviewWorkbookExporter.QuantitySheet, 2);
                AssertNegative(
                    document, project, authoritative, baselineSelection,
                    () => ReviewWorkbookHostService.ResolveTrace(document, staleProject, staleTrace),
                    ref negativeAttempts, ref negativeRefusals, ref negativeSelectionPreserved, ref negativeSemanticUnchanged);

                var partialProject = NegativeProject(
                    project.DrawingFingerprint,
                    new[] { "NEG-LIVE", "NEG-MISSING" },
                    new[] { seeded[0].Handle, missingHandle });
                var partialPath = Path.Combine(directory, "review-partial-pair.xlsx");
                WriteNegativeWorkbook(partialPath, partialProject.DrawingFingerprint, ReviewWorkbookHostService.ModelRevision(partialProject),
                    new SeededElement("NEG-LIVE", seeded[0].Handle, ElementCategory.Beam),
                    new SeededElement("NEG-MISSING", missingHandle, ElementCategory.Column), true);
                var partialTrace = Qs3dReviewWorkbookTraceReader.Read(partialPath, Qs3dReviewWorkbookExporter.ClashSheet, 2);
                AssertNegative(
                    document, project, authoritative, baselineSelection,
                    () => ReviewWorkbookHostService.ResolveTrace(document, partialProject, partialTrace),
                    ref negativeAttempts, ref negativeRefusals, ref negativeSelectionPreserved, ref negativeSemanticUnchanged);

                if (negativeAttempts != 4 || negativeRefusals != 4 || negativeSelectionPreserved != 4 || negativeSemanticUnchanged != 4)
                    throw new InvalidOperationException("QS3D Review negative locate matrix is incomplete.");

                WriteMarkerAtomic(validatedResultPath, new[]
                {
                    "status=PASS",
                    "command=QS3DREVIEWROUNDTRIPPROBE",
                    "schema=QS3D_REVIEW_HOST_ROUNDTRIP_V1",
                    "nonce=" + nonce,
                    "process=" + System.Diagnostics.Process.GetCurrentProcess().ProcessName,
                    "plugin_assembly=" + typeof(ReviewWorkbookRuntimeProbeCommands).Assembly.GetName().Name,
                    "is_64bit=" + (Environment.Is64BitProcess ? "true" : "false"),
                    "sheet_count=6",
                    "quantity_detail_count=" + export.QuantityDetailCount.ToString(CultureInfo.InvariantCulture),
                    "quantity_summary_count=" + export.QuantitySummaryCount.ToString(CultureInfo.InvariantCulture),
                    "clash_count=" + export.ClashCount.ToString(CultureInfo.InvariantCulture),
                    "duplicate_count=" + export.DuplicateCount.ToString(CultureInfo.InvariantCulture),
                    "qto_located_count=" + qto.ObjectIds.Count.ToString(CultureInfo.InvariantCulture),
                    "clash_located_count=" + clash.ObjectIds.Count.ToString(CultureInfo.InvariantCulture),
                    "duplicate_located_count=" + duplicate.ObjectIds.Count.ToString(CultureInfo.InvariantCulture),
                    "negative_attempt_count=" + negativeAttempts.ToString(CultureInfo.InvariantCulture),
                    "negative_refusal_count=" + negativeRefusals.ToString(CultureInfo.InvariantCulture),
                    "negative_pickfirst_preserved_count=" + negativeSelectionPreserved.ToString(CultureInfo.InvariantCulture),
                    "negative_semantic_unchanged_count=" + negativeSemanticUnchanged.ToString(CultureInfo.InvariantCulture),
                    "wrong_fingerprint_refused=true",
                    "wrong_revision_refused=true",
                    "stale_handle_refused=true",
                    "partial_resolution_refused=true",
                    "all_targets_resolved_before_selection=true",
                    "production_export_service=true",
                    "production_locate_service=true"
                });
                document.Editor.WriteMessage("\nQS3D Review workbook round-trip probe PASS.");
            }
            catch (Exception)
            {
                TryWriteFailure(resultPath);
                Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                    "\nQS3D Review workbook round-trip probe FAIL. See the local qualification result.");
                throw;
            }
        }

        private static IReadOnlyList<SeededElement> Seed(Document document, ProjectState project)
        {
            var ids = new List<ObjectId>();
            using (document.LockDocument())
            using (var transaction = document.Database.TransactionManager.StartTransaction())
            {
                var modelSpace = (BlockTableRecord)transaction.GetObject(document.Database.CurrentSpaceId, OpenMode.ForWrite);
                ids.Add(AppendLine(document, transaction, modelSpace, 0d));
                ids.Add(AppendLine(document, transaction, modelSpace, 2d));
                ids.Add(AppendLine(document, transaction, modelSpace, 4d));
                transaction.Commit();
            }
            var seeded = new[]
            {
                new SeededElement("REVIEW-ELEMENT-A", ids[0].Handle.ToString(), ElementCategory.Beam),
                new SeededElement("REVIEW-ELEMENT-B", ids[1].Handle.ToString(), ElementCategory.Column),
                new SeededElement("REVIEW-ELEMENT-C", ids[2].Handle.ToString(), ElementCategory.Slab)
            };
            foreach (var item in seeded)
            {
                var element = new ProjectElement(item.ElementId, item.Category) { DrawingFingerprint = project.DrawingFingerprint };
                element.SourceHandles.Add(item.Handle);
                element.Quantities["LengthM"] = 1d;
                element.MarkClean(ElementDirtyFlags.All);
                project.Elements.Add(element);
            }
            project.Touch();

            var drawingFingerprint = Convert.ToString(document.Database.FingerprintGuid)?.Trim();
            if (!Guid.TryParse(drawingFingerprint, out var drawingGuid) || drawingGuid == Guid.Empty)
            {
                drawingGuid = Guid.Parse("1f350b79-cfec-4c90-afce-8b37c16bb796");
            }
            var drawingId = new DrawingId(drawingGuid);
            var created = DateTime.UtcNow;
            var clash = new CoordinationIssue(
                "REVIEW-CLASH-01", CoordinationIssueKind.ExactHardClash, CoordinationIssueSeverity.Critical,
                "Review clash", seeded[0].ElementId, seeded[1].ElementId,
                new CadReference(drawingId, new CadHandle(seeded[0].Handle)),
                new CadReference(drawingId, new CadHandle(seeded[1].Handle)),
                "Structure", "Beam/Column", "Review", "L01", 0d, created);
            var duplicate = new CoordinationIssue(
                "REVIEW-DUPLICATE-01", CoordinationIssueKind.Review, CoordinationIssueSeverity.High,
                "Review duplicate", seeded[1].ElementId, seeded[2].ElementId,
                new CadReference(drawingId, new CadHandle(seeded[1].Handle)),
                new CadReference(drawingId, new CadHandle(seeded[2].Handle)),
                "Structure", "Column/Slab", "Review", "L01", 0d, created.AddSeconds(1));
            CoordinationIssuePersistence.Save(project, new[] { clash, duplicate }, 1L);
            return Array.AsReadOnly(seeded);
        }

        private static ObjectId AppendLine(Document document, Transaction transaction, BlockTableRecord modelSpace, double y)
        {
            var line = new Line(new Point3d(0d, y, 0d), new Point3d(1d, y, 0d));
            try
            {
                line.SetDatabaseDefaults(document.Database);
                var id = modelSpace.AppendEntity(line);
                transaction.AddNewlyCreatedDBObject(line, true);
                line = null!;
                return id;
            }
            finally { line?.Dispose(); }
        }

        private static void RequireSelection(Document document, ExcelLocateResolution resolution, int expected)
        {
            if (resolution.ObjectIds.Count != expected)
                throw new InvalidOperationException("QS3D Review Locate resolved an unexpected target count.");
            document.Editor.SetImpliedSelection(resolution.ObjectIds.ToArray());
            if (CurrentImpliedSelection(document).Count != expected)
                throw new InvalidOperationException("QS3D Review Locate did not establish the expected PICKFIRST selection.");
        }

        private static void AssertNegative(
            Document document,
            ProjectState authoritativeProject,
            ProjectReadOnlyStamp authoritativeStamp,
            IReadOnlyList<ObjectId> baselineSelection,
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
                throw new InvalidOperationException("QS3D Review negative Locate case was accepted unexpectedly.");
            }
            catch (InvalidDataException) { refusalCount = checked(refusalCount + 1); }
            catch (ExcelLocateResolutionException) { refusalCount = checked(refusalCount + 1); }

            if (!SameObjectIds(baselineSelection, CurrentImpliedSelection(document)))
                throw new InvalidOperationException("QS3D Review negative Locate case changed PICKFIRST.");
            selectionPreservedCount = checked(selectionPreservedCount + 1);
            authoritativeStamp.RequireUnchanged(authoritativeProject);
            semanticUnchangedCount = checked(semanticUnchangedCount + 1);
        }

        private static ProjectState NegativeProject(string fingerprint, IEnumerable<string> elementIds, IEnumerable<string> handles)
        {
            var ids = elementIds.ToArray();
            var hs = handles.ToArray();
            if (ids.Length != hs.Length) throw new ArgumentException("Negative project identity cardinality mismatch.");
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "QS3D Review negative") { DrawingFingerprint = fingerprint };
            for (var i = 0; i < ids.Length; i++)
            {
                var element = new ProjectElement(ids[i], ElementCategory.Beam) { DrawingFingerprint = fingerprint };
                element.SourceHandles.Add(hs[i]);
                project.Elements.Add(element);
            }
            return project;
        }

        private static void WriteNegativeWorkbook(
            string path,
            string fingerprint,
            string revision,
            SeededElement qto,
            SeededElement? pair,
            bool includeClash)
        {
            if (File.Exists(path)) throw new IOException("Negative QS3D Review workbook already exists.");
            var detail = QuantityRow(fingerprint, qto.ElementId, qto.Handle);
            var summary = QuantityRow(fingerprint, qto.ElementId, qto.Handle);
            var clashes = includeClash && pair != null
                ? new[] { CoordinationClashExportRow.CreateExactHard(
                    fingerprint, qto.Handle, pair.Handle, qto.ElementId, pair.ElementId,
                    qto.Category.ToString(), pair.Category.ToString(), "L01") }
                : Array.Empty<CoordinationClashExportRow>();
            Qs3dReviewWorkbookExporter.Export(
                path,
                new[] { detail },
                new[] { summary },
                clashes,
                Array.Empty<CoordinationDuplicateExportRow>(),
                null,
                new Qs3dReviewModelInfo("NEGATIVE", "negative.dwg", fingerprint, revision, DateTimeOffset.UtcNow));
        }

        private static QuantityReportRow QuantityRow(string fingerprint, string elementId, string handle)
        {
            var row = new QuantityReportRow
            {
                Category = "Beam",
                ElementName = elementId,
                DrawingFingerprint = fingerprint,
                Count = 1,
                LengthM = 1d,
                HasLengthMEvidence = true
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add(handle);
            return row;
        }

        private static string FindMissingHandle(Document document, IEnumerable<string> existing)
        {
            var used = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
            for (var value = long.MaxValue; value > long.MaxValue - 4096L; value--)
            {
                var candidate = value.ToString("X", CultureInfo.InvariantCulture);
                if (!used.Contains(candidate) && CadHandleService.Resolve(document, new[] { candidate }).Count == 0) return candidate;
            }
            throw new InvalidOperationException("Cannot allocate a missing CAD Handle for QS3D Review qualification.");
        }

        private static IReadOnlyList<ObjectId> CurrentImpliedSelection(Document document)
        {
            var selection = document.Editor.SelectImplied();
            return selection.Value?.GetObjectIds().ToList().AsReadOnly() ?? new List<ObjectId>().AsReadOnly();
        }

        private static bool SameObjectIds(IEnumerable<ObjectId> left, IEnumerable<ObjectId> right)
        {
            var a = left.OrderBy(x => x.Handle.ToString(), StringComparer.OrdinalIgnoreCase).ToArray();
            var b = right.OrderBy(x => x.Handle.ToString(), StringComparer.OrdinalIgnoreCase).ToArray();
            return a.SequenceEqual(b);
        }

        private sealed class ProjectReadOnlyStamp
        {
            private ProjectReadOnlyStamp(long changeVersion, DateTime updatedUtc, int elementCount, int auditCount, string handles)
            {
                ChangeVersion = changeVersion;
                UpdatedUtc = updatedUtc;
                ElementCount = elementCount;
                AuditCount = auditCount;
                Handles = handles;
            }

            private long ChangeVersion { get; }
            private DateTime UpdatedUtc { get; }
            private int ElementCount { get; }
            private int AuditCount { get; }
            private string Handles { get; }

            public static ProjectReadOnlyStamp Capture(ProjectState project) => new ProjectReadOnlyStamp(
                project.ChangeVersion,
                project.UpdatedUtc,
                project.Elements.Count,
                project.AuditEvents.Count,
                string.Join("|", project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Id + ":" + string.Join(";", x.SourceHandles))));

            public void RequireUnchanged(ProjectState project)
            {
                var handles = string.Join("|", project.Elements.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Id + ":" + string.Join(";", x.SourceHandles)));
                if (project.ChangeVersion != ChangeVersion || project.UpdatedUtc != UpdatedUtc ||
                    project.Elements.Count != ElementCount || project.AuditEvents.Count != AuditCount ||
                    !string.Equals(handles, Handles, StringComparison.Ordinal))
                    throw new InvalidOperationException("QS3D Review export/Locate mutated the authoritative semantic project.");
            }
        }

        private sealed class SeededElement
        {
            public SeededElement(string elementId, string handle, ElementCategory category)
            {
                ElementId = elementId;
                Handle = handle;
                Category = category;
            }
            public string ElementId { get; }
            public string Handle { get; }
            public ElementCategory Category { get; }
        }

        private static string RequiredOutputPath(string? value, string expectedFileName, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Probe " + label + " path is required.", label);
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), expectedFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QS3D Review probe " + label + " filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("QS3D Review probe output directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? resultPath)
        {
            try
            {
                var normalized = (resultPath ?? string.Empty).Trim();
                if (normalized.Length > 0 && !File.Exists(normalized))
                    WriteMarkerAtomic(normalized, new[] { "status=FAIL", "command=QS3DREVIEWROUNDTRIPPROBE", "error_code=ROUNDTRIP_FAILED" });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = RequiredOutputPath(resultPath, ResultFileName, "result");
            if (File.Exists(fullPath)) throw new IOException("QS3D Review probe result already exists.");
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine((line ?? string.Empty).Replace('\r', ' ').Replace('\n', ' '));
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
    }
}
