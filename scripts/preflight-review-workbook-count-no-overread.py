#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/Qs3dReviewWorkbook.Exporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/Qs3dReviewWorkbookCountNoOverreadSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/review-workbook-count-no-overread.md"


def fail(message: str) -> None:
    print(f"FAIL review workbook Count no-overread: {message}", file=sys.stderr)
    raise SystemExit(1)


for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.exists():
        fail(f"missing required artifact: {path.relative_to(ROOT)}")

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

helper_marker = "private static List<T> SnapshotCounted<T>(IReadOnlyList<T> source, int expectedCount, string label)"
quantity_marker = "private static List<QuantityReportRow> Quantity"
if helper_marker not in source:
    fail("shared SnapshotCounted<T> boundary is missing")
helper_start = source.index(helper_marker)
helper_end = source.index(quantity_marker, helper_start)
helper = source[helper_start:helper_end]

required_helper = (
    "while (enumerator.MoveNext())",
    "if (result.Count >= expectedCount)",
    "result.Add(enumerator.Current);",
    "if (result.Count != expectedCount)",
    "if (source.Count != expectedCount)",
)
for token in required_helper:
    if token not in helper:
        fail(f"SnapshotCounted<T> is missing token: {token}")

if helper.index("if (result.Count >= expectedCount)") > helper.index("result.Add(enumerator.Current);"):
    fail("SnapshotCounted<T> observes Current before known-Count admission")
if helper.index("if (result.Count != expectedCount)") > helper.index("if (source.Count != expectedCount)"):
    fail("SnapshotCounted<T> must establish traversal cardinality before post-traversal Count rebind")

for token in (
    "var detailCount = quantityDetails.Count;",
    "var summaryCount = quantitySummary.Count;",
    "var clashCount = clashes.Count;",
    "var duplicateCount = duplicates.Count;",
    "var geometryCount = issueGeometry == null ? (int?)null : issueGeometry.Count;",
    "Limit(detailCount, QuantitySheet)",
    "Limit(summaryCount + 16, SummarySheet)",
    "Limit(clashCount, ClashSheet)",
    "Limit(duplicateCount, DuplicateSheet)",
    'SnapshotCounted(quantityDetails, detailCount, "QTO detail")',
    'SnapshotCounted(quantitySummary, summaryCount, "QTO summary")',
    'SnapshotCounted(clashes, clashCount, "clash")',
    'SnapshotCounted(duplicates, duplicateCount, "duplicate")',
    'SnapshotCounted(issueGeometry, geometryCount!.Value, "issue geometry")',
    "Quantity(detailInput, true, modelInfo.DrawingFingerprint)",
    "Quantity(summaryInput, false, modelInfo.DrawingFingerprint)",
    "Clash(clashInput, modelInfo.DrawingFingerprint)",
    "Duplicate(duplicateInput, modelInfo.DrawingFingerprint)",
    "Geometry(geometryInput, clashRows, duplicateRows)",
):
    if token not in source:
        fail(f"Export is missing bound-count routing token: {token}")

for unsafe in (
    "Quantity(quantityDetails, true",
    "Quantity(quantitySummary, false",
    "Clash(clashes, modelInfo.DrawingFingerprint)",
    "Duplicate(duplicates, modelInfo.DrawingFingerprint)",
    "Geometry(issueGeometry, clashRows, duplicateRows)",
):
    if unsafe in source:
        fail(f"Export bypasses counted-snapshot boundary: {unsafe}")

for token in (
    "KnownCountOverrunStopsBeforeUnexpectedCurrent",
    "ZeroCountOverrunNeverReadsCurrent",
    "UnderYieldFailsExactCardinality",
    "PostTraversalCountDriftFailsClosed",
    "StableCountedSnapshotReadsEachAdmittedCurrentExactlyOnce",
    "var admittedCount = source.Count;",
    "MoveNextCalls",
    "CurrentReads",
    "CountReads",
    "[ModuleInitializer]",
):
    if token not in smoke:
        fail(f"adversarial smoke is missing token: {token}")

for token in ("Issue: #4492", "Lane-Key: `issue-4492`", "MoveNext", "Current", "Count", "QTO", "clash", "duplicate", "geometry"):
    if token not in runbook:
        fail(f"runbook is missing token: {token}")

print("PASS review workbook IReadOnlyList Count boundary Current no-overread source guard")
