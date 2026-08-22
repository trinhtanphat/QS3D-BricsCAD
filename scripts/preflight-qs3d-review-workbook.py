#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.cs",
    ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.Exporter.cs",
    ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.Xlsx.cs",
    ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.Sheets.cs",
    ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.TraceReader.cs",
]
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/Qs3dReviewWorkbookSmoke.cs"


def fail(message, details=()):
    print("ERROR:", message)
    for detail in details:
        print(" -", detail)
    return 1


def main():
    missing_files = [str(path.relative_to(ROOT)) for path in FILES + [SMOKE] if not path.is_file()]
    if missing_files:
        return fail("missing final QS3D Review workbook source/regression files", missing_files)

    source = "\n".join(path.read_text(encoding="utf-8") for path in FILES)
    smoke = SMOKE.read_text(encoding="utf-8")
    required = [
        'public const string SummarySheet = "01_TONG_HOP";',
        'public const string QuantitySheet = "02_CHI_TIET_QTO";',
        'public const string ClashSheet = "03_CLASHES";',
        'public const string DuplicateSheet = "04_DUPLICATES";',
        'public const string RulesSheet = "05_RULES";',
        'public const string ModelInfoSheet = "06_MODEL_INFO";',
        'IReadOnlyList<QuantityReportRow> quantityDetails',
        'IReadOnlyList<CoordinationClashExportRow> clashes',
        'IReadOnlyList<CoordinationDuplicateExportRow> duplicates',
        'CoordinationRuleProfile? ruleProfile',
        'IReadOnlyDictionary<string, CoordinationIssueExcelRow>? lifecycleByFindingId',
        'IReadOnlyList<Qs3dReviewIssueGeometry>? issueGeometry',
        '"02_CHI_TIET_QTO requires exactly one semantic element per row."',
        '"QS3D Review QTO row belongs to a different DrawingFingerprint."',
        '"QS3D Review clash row belongs to a different DrawingFingerprint."',
        '"QS3D Review duplicate row belongs to a different DrawingFingerprint."',
        '"QS3D Review lifecycle mapping references a finding that is not exported: "',
        '"QS3D Review lifecycle semantic pair does not match exported finding "',
        'if (hasEvidence) Number(sb, cell, value);',
        'AtomicFileCommit.ReplaceWithoutBackup(tempPath, fullPath);',
        'XlsxPackageValidator.Validate(',
        'public static class Qs3dReviewWorkbookTraceReader',
        'DtdProcessing = DtdProcessing.Prohibit',
        'XmlResolver = null',
        'hidden=\"1\"',
        '"CoordinationIssueId"',
        '"IssueRevision"',
        '"Assignee"',
    ]
    missing = [token for token in required if token not in source]
    if missing:
        return fail("six-sheet review workbook contract is incomplete", ["missing: " + token for token in missing])

    forbidden = [
        "Bricscad.", "Teigha.", "Autodesk.", "QS3D.BricsCAD",
        "DuplicateDetectionService()", "DetectExact(", "ProjectQuantityReportBuilder.Detail(",
        "Qs3dReviewIssueMetadata",
    ]
    found = [token for token in forbidden if token in source]
    if found:
        return fail("review workbook must remain a host-neutral composition over canonical projections", ["forbidden: " + token for token in found])

    ordered = [
        '<sheet name=\"01_TONG_HOP\"',
        '<sheet name=\"02_CHI_TIET_QTO\"',
        '<sheet name=\"03_CLASHES\"',
        '<sheet name=\"04_DUPLICATES\"',
        '<sheet name=\"05_RULES\"',
        '<sheet name=\"06_MODEL_INFO\"',
    ]
    positions = [source.find(token) for token in ordered]
    if min(positions) < 0 or positions != sorted(positions):
        return fail("canonical workbook sheet order changed")

    smoke_required = [
        "SixSheetWorkbookRoundTripsAllTraceKinds();",
        "CanonicalLifecyclePairMismatchFailsClosed();",
        "MixedDrawingFailsBeforeReplacingExistingWorkbook();",
        "CoordinationIssueExcelLifecycle.Project",
        "Qs3dReviewIssueGeometry",
        "Qs3dReviewWorkbookTraceReader.Read",
        'qtoXml.Contains("<c r=\\\"O2\\\""',
        '"KEEP-ME"',
    ]
    missing_smoke = [token for token in smoke_required if token not in smoke]
    if missing_smoke:
        return fail("review workbook smoke does not cover lifecycle/trace/blank-evidence/atomic refusal", ["missing: " + token for token in missing_smoke])

    print("PASS: final QS3D Review workbook is six-sheet, host-neutral, traceable, fail-closed, evidence-aware, and consumes the canonical #3496 lifecycle projection.")
    print("NOTE: native Coordination Manager stays exclusively owned by #3494; licensed runtime stays #72 LOCAL_ONLY.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
