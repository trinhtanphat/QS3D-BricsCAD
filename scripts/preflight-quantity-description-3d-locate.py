#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
errors = []

if not TARGET.is_file():
    print("FAIL: missing QuantitySummaryWindow.xaml.cs")
    sys.exit(1)

source = TARGET.read_text(encoding="utf-8")


def method_block(name: str, next_name: str) -> str:
    start = source.find(name)
    end = source.find(next_name, start + len(name)) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append(f"cannot isolate method block: {name}")
        return ""
    return source[start:end]


locate = method_block("private void LocateCurrent()", "private QuantityReportRow ResolveCurrentRow")
resolve = method_block("private QuantityReportRow ResolveCurrentRow", "private QuantityReportRow ResolveSourceHandleRow")
source_fallback = method_block("private QuantityReportRow ResolveSourceHandleRow", "private static bool SameElementIdentity")

for needle in (
    "var displayedHandles = CanonicalIds(row.SourceHandles);",
    "var liveHandles = CanonicalIds(currentRow.SourceHandles);",
    "Cad.CadHandleService.Select(_document, liveHandles)",
    "var expectedCount = displayedHandles.Length > 0 ? displayedHandles.Length : liveHandles.Length;",
    "if (selectedCount <= 0)",
    "selectedCount < expectedCount",
    '_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);',
):
    if needle not in locate:
        errors.append("locate flow missing contract: " + needle)

if locate:
    select_pos = locate.find("Cad.CadHandleService.Select(_document, liveHandles)")
    zero_guard_pos = locate.find("if (selectedCount <= 0)")
    zoom_pos = locate.find('_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);')
    if not (0 <= select_pos < zero_guard_pos < zoom_pos):
        errors.append("zoom must remain after CAD selection and zero-selection guard")

for needle in (
    "var displayedIds = CanonicalIds(displayedRow.ElementIds);",
    "var displayedHandles = CanonicalIds(displayedRow.SourceHandles);",
    "if (displayedIds.Length == 0 && displayedHandles.Length == 0)",
    "if (displayedIds.Length == 0)",
    "return ResolveSourceHandleRow(displayedRow, displayedHandles);",
):
    if needle not in resolve:
        errors.append("row resolution missing identity contract: " + needle)

if resolve:
    semantic_first = resolve.find("var displayedIds = CanonicalIds(displayedRow.ElementIds);")
    fallback = resolve.find("return ResolveSourceHandleRow(displayedRow, displayedHandles);")
    semantic_recalc = resolve.find("var currentRows = _detailMode ? RecalculateDetailRows() : RecalculateSummaryRows(true);")
    if not (0 <= semantic_first < fallback < semantic_recalc):
        errors.append("semantic identity must remain primary and source handles fallback-only")

for needle in (
    "ProjectContextCoordinator.TryGetReadOnly(_document, out var currentProject)",
    "Cad.EntitySnapshotReader.ReadHandles(_document, expectedHandles)",
    "SnapshotQuantityAdapter.Build(snapshots, unit)",
    "current.DrawingFingerprint = currentProject.DrawingFingerprint",
    "SameSourceGroupIdentity(displayedRow, x)",
    "matches.Count != 1",
    "currentHandles.Any(x => !expectedHandles.Contains(x, StringComparer.OrdinalIgnoreCase))",
    "currentHandles.Length == expectedHandles.Length && !SameRow(displayedRow, currentRow)",
):
    if needle not in source_fallback:
        errors.append("source-handle revalidation missing contract: " + needle)

for forbidden in (
    "ReadCurrentSelection(_document)",
    "ProjectContextCoordinator.GetOrCreate",
    "ExistingProjectMutationContext.Require",
    ".Touch()",
    "ProjectContextCoordinator.Save",
):
    if forbidden in source_fallback:
        errors.append("source-handle revalidation must remain stable/read-only: " + forbidden)

if "private static bool SameSourceGroupIdentity" not in source:
    errors.append("missing dedicated source-group identity helper")

if errors:
    for error in errors:
        print("FAIL:", error)
    sys.exit(1)

print(
    "PASS: QS3DBQ description locate keeps semantic-first identity, supports stable source-handle fallback, "
    "revalidates handles without current-pickset dependence, tolerates partial stale handles, and zooms only after a live selection."
)
