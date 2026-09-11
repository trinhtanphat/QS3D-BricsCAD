from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "Qs3dReviewWorkbook.Exporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "Qs3dReviewWorkbookSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var row = SnapshotQuantityRow(sourceRow);",
    "EnsureQuantityRowStable(sourceRow, row);",
    "private static QuantityReportRow SnapshotQuantityRow(QuantityReportRow source)",
    "CopyStableValues(source.ElementIds, snapshot.ElementIds, \"QTO ElementIds\");",
    "CopyStableValues(source.SourceHandles, snapshot.SourceHandles, \"QTO SourceHandles\");",
    "QTO row changed during detached snapshot capture.",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing QS3D Review QTO snapshot source guard token: {token}")

capture = source.index("var row = SnapshotQuantityRow(sourceRow);")
stability = source.index("EnsureQuantityRowStable(sourceRow, row);")
validation = source.index("if (row.Count <= 0")
if not capture < stability < validation:
    raise SystemExit("QS3D Review QTO must detach and stability-check each row before semantic validation/publication")
required_smoke = [
    "QuantityRowsAreDetachedBeforeWorkbookPublication",
    "EL-MUTATED",
    "QTO row must publish the admitted detached semantic id",
    "QTO row must publish the admitted detached CAD handle",
    "QTO row must publish the admitted detached NetConcreteM3 value",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing QS3D Review QTO detached-snapshot smoke token: {token}")

if "sourceRow.ElementIds" in source or "sourceRow.SourceHandles" in source:
    raise SystemExit("QS3D Review QTO publication must not project caller-owned provenance lists")

print("PASS QS3D Review QTO detached snapshot source guard")
