#!/usr/bin/env python3
# Lane-Key: review-workbook-host-bridge
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = {
    "commands": ROOT / "src/QS3D.BricsCAD.V25/ReviewWorkbookCommands.cs",
    "probe": ROOT / "src/QS3D.BricsCAD.V25/ReviewWorkbookRuntimeProbeCommands.cs",
    "resolver": ROOT / "src/QS3D.BricsCAD.V25/Services/ExcelLocateResolutionService.cs",
    "units": ROOT / "src/QS3D.BricsCAD.V25/Services/DrawingUnitWorkflow.cs",
    "projection": ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.IssueProjection.cs",
    "reader": ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.TraceReader.cs",
    "validator": ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.TraceValidator.cs",
    "smoke": ROOT / "tests/QS3D.Core.SmokeTests/Qs3dReviewWorkbookSmoke.cs",
    "runner": ROOT / "scripts/test-bricscad-review-workbook-roundtrip.ps1",
    "v26": ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj",
    "inbox": ROOT / "docs/LOCAL-AGENT-INBOX.md",
}
errors = []
texts = {}

for name, path in FILES.items():
    if not path.is_file():
        errors.append("missing QS Review host bridge file: " + str(path.relative_to(ROOT)))
    else:
        texts[name] = path.read_text(encoding="utf-8")


def require(name, tokens):
    text = texts.get(name, "")
    for token in tokens:
        if token not in text:
            errors.append(FILES[name].name + " missing host bridge token: " + token)


require("commands", (
    '[CommandMethod("QS3DREVIEWEXPORT", CommandFlags.UsePickSet)]',
    '[CommandMethod("QS3DREVIEWLOCATE", CommandFlags.UsePickSet)]',
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "RegenerateDirty(preview)",
    "ProjectQuantityReportBuilder.Detail(preview)",
    "ProjectQuantityReportBuilder.Group(preview)",
    "CoordinationIssuePersistence.Load(preview)",
    "Qs3dReviewIssueProjection.Build(preview, issueSnapshot)",
    "Qs3dReviewWorkbookExporter.Export(",
    "Qs3dReviewLiveHandleBatchPlanner.Create(expected, LiveHandleBatchSize)",
    "Qs3dReviewWorkbookTraceReader.Read(dialog.FileName, sheet, row.Value)",
    "ReviewWorkbookHostService.ResolveTrace(document, currentProject, trace)",
    "document.Editor.SetImpliedSelection(resolution.ObjectIds.ToArray())",
    'SendStringToExecute("QS3DZOOMSELECTED "',
))
require("resolver", (
    "ResolveReviewTrace(",
    "Qs3dReviewTraceValidator.ValidateIdentity",
    "SourceHandleResolver.Resolve(project, trace.ElementIds)",
    "SourceHandleResolver.Resolve(project, new[] { elementId })",
    "resolved.Count == 0",
    "resolved.Count != canonicalHandles.Count",
))
if "SetImpliedSelection" in texts.get("resolver", "") or "SendStringToExecute" in texts.get("resolver", ""):
    errors.append("review resolver must validate every target without mutating PICKFIRST or dispatching Zoom")

require("units", (
    'string.Equals(operation, "QS3DREVIEWEXPORT", StringComparison.OrdinalIgnoreCase)',
    "if (readOnlyReviewPreparation)",
    "if (!readOnlyExportPreparation)",
    "QS3DREVIEWEXPORT: drawing unit is undefined/unsupported",
))

require("projection", (
    "CoordinationIssuePersistenceSnapshot snapshot",
    "CoordinationIssueExcelLifecycle.Project(snapshot)",
    "CoordinationIssueKind.Review",
    "DuplicateMatchKind.None",
    "CoordinationClashExportRow(",
    "CoordinationDuplicateExportRow(",
    "RequireSemanticHandle(",
))
for forbidden in ("DuplicateDetectionService", "DetectExact(", "Detect(", "QS3D_PERSISTED_", "DuplicateMatchKind.SemanticIdentity"):
    if forbidden in texts.get("projection", ""):
        errors.append("persisted issue projection must not re-run a detector: " + forbidden)

require("validator", (
    "public static class Qs3dReviewTraceValidator",
    "ValidateTraceKey(trace)",
    '"QTO", trace.DrawingFingerprint',
    '"CLASH", trace.DrawingFingerprint',
    '"DUPLICATE", trace.DrawingFingerprint',
    "StringComparison.OrdinalIgnoreCase",
    "StringComparison.Ordinal",
    "different drawing fingerprint",
    "model revision is stale",
))
require("reader", (
    "WorksheetRelationshipType",
    "ResolveSheets(archive, out sheetOrder)",
    "ReadSharedStrings(archive)",
    "ReadCells(",
    "workbook.xml.rels",
    "sharedStrings.xml",
    "identity cells must be literal",
    "RequiredColumns(",
))
for forbidden in ('"xl/worksheets/sheet2.xml"', '"xl/worksheets/sheet3.xml"', '"xl/worksheets/sheet4.xml"'):
    if forbidden in texts.get("reader", ""):
        errors.append("review trace reader must resolve worksheet relationships instead of hardcoding package parts: " + forbidden)

require("smoke", (
    "ExcelResavedRelationshipsAndSharedStringsRoundTrip",
    "TraceKeyTamperFailsClosed",
    "TraceReaderRejectsFormulaIdentityCell",
    "Enumerable.Range(1, 10001)",
    "ReviewOnly",
))
require("probe", (
    '[CommandMethod("QS3DREVIEWROUNDTRIPPROBE", CommandFlags.Modal)]',
    "ReviewWorkbookHostService.Export(document, project, workbookPath",
    "Qs3dReviewWorkbookTraceReader.Read",
    "ReviewWorkbookHostService.ResolveTrace",
    "RequireSelection(document, qto, 1)",
    "RequireSelection(document, clash, 2)",
    "RequireSelection(document, duplicate, 2)",
    "negativeAttempts != 4",
    "negativeSelectionPreserved != 4",
    "negativeSemanticUnchanged != 4",
    "authoritative.RequireUnchanged(project)",
    'DrawingUnitWorkflow.EnsureResolved(document, "QS3DREVIEWEXPORT")',
    "QS3D_REVIEW_ROUNDTRIP_PLUGIN",
    "QS3D_REVIEW_ROUNDTRIP_CORE",
    ".Assembly.Location",
    "SHA256.Create()",
    '"schema=QS3D_REVIEW_HOST_ROUNDTRIP_V1"',
    '"plugin_path_match=true"',
    '"plugin_sha256="',
    '"core_path_match=true"',
    '"core_sha256="',
    '"readonly_unit_resolution=true"',
    '"all_targets_resolved_before_selection=true"',
))
require("runner", (
    '[ValidateSet("V25", "V26")]',
    '$ConfirmDisposableCopy',
    '".review-probe-copy.dwg"',
    "ExpectedSourceSha",
    "Assert-Qs3dExactSourceIdentity",
    "rev-parse '@{u}'",
    "Set-Qs3dDemandLoadControls",
    "Restore-Qs3dDemandLoadControls",
    'Applications\\QS3D"',
    "($demandLoadOriginalControls -band (-bnot 2)) -bor 4",
    "startup_demandload_restored",
    '"NETLOAD"',
    '"QS3DREVIEWROUNDTRIPPROBE"',
    "QS3D_REVIEW_ROUNDTRIP_RESULT",
    "QS3D_REVIEW_ROUNDTRIP_WORKBOOK",
    "QS3D_REVIEW_ROUNDTRIP_NONCE",
    "QS3D_REVIEW_ROUNDTRIP_PLUGIN",
    "QS3D_REVIEW_ROUNDTRIP_CORE",
    "FileMajorPart",
    '$expectedHostMajor = if ($HostMajor -eq "V25") { 25 } else { 26 }',
    'Require-Qs3dValue $marker "plugin_path_match" "true"',
    'Require-Qs3dValue $marker "plugin_sha256" $pluginHash',
    'Require-Qs3dValue $marker "core_path_match" "true"',
    'Require-Qs3dValue $marker "core_sha256" $coreHash',
    'Require-Qs3dValue $marker "readonly_unit_resolution" "true"',
    "Wait-Qs3dNoExactBricsCadProcesses",
    "Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256",
    '"01_TONG_HOP", "02_CHI_TIET_QTO", "03_CLASHES"',
    '"04_DUPLICATES", "05_RULES", "06_MODEL_INFO"',
))
if texts.get("runner", "").count("Get-FileHash -LiteralPath $DrawingCopy -Algorithm SHA256") < 2:
    errors.append("QS Review runner must hash the disposable DWG before and after the host run")

require("v26", (
    r'<Compile Include="..\QS3D.BricsCAD.V25\**\*.cs"',
    '<AssemblyName>QS3D.BricsCAD.V26</AssemblyName>',
    '<TargetFramework>net8.0-windows</TargetFramework>',
))
require("inbox", (
    "## LOCAL-019 — six-sheet QS Review export and Excel-to-Model Locate",
    "issue `#3536`",
    "PENDING_LOCAL until both licensed host-major runs pass",
))

print("QS3D six-sheet Review host bridge preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: V25/V26 shared-source export/trace/resolve wiring and the exact-pushed-SHA local runner are source-guarded; this static gate does not claim licensed runtime PASS.")
