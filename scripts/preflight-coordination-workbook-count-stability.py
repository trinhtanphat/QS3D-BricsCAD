#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CoordinationWorkbook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CoordinationWorkbookCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/coordination-workbook-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Coordination workbook Count-stability preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "var admittedRowCount = rows.Count;",
    "RequireCoordinationRowCountAdmission(admittedRowCount);",
    "var snapshot = Snapshot(rows, admittedRowCount);",
    "private static List<CoordinationClashExportRow> Snapshot(IReadOnlyList<CoordinationClashExportRow> source, int admittedRowCount)",
    "RequireStableCoordinationRowCount(source, admittedRowCount);",
    "var row = source[index];",
    "Coordination workbook row Count changed during snapshot.",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Coordination workbook Count-stability source contract missing: " + repr(missing))

export_start = source.index("public static void Export(string path, IReadOnlyList<CoordinationClashExportRow> rows)")
snapshot_start = source.index("private static List<CoordinationClashExportRow> Snapshot", export_start)
export = source[export_start:snapshot_start]

admission = export.index("var admittedRowCount = rows.Count;")
admission_guard = export.index("RequireCoordinationRowCountAdmission(admittedRowCount);", admission)
snapshot_call = export.index("var snapshot = Snapshot(rows, admittedRowCount);", admission_guard)
if not (admission < admission_guard < snapshot_call):
    raise SystemExit("Coordination workbook Count admission ordering changed.")
if "rows.Count == 0" in export or "rows.Count > MaxRows" in export:
    raise SystemExit("Coordination workbook Export must not reread live Count outside the admitted Count contract.")

build_start = source.index("private static string BuildClashSheet", snapshot_start)
snapshot = source[snapshot_start:build_start]
loop = snapshot.index("for (var index = 0; index < admittedRowCount; index++)")
pre_index = snapshot.index("RequireStableCoordinationRowCount(source, admittedRowCount);", loop)
current = snapshot.index("var row = source[index];", pre_index)
final_rebound = snapshot.index("RequireStableCoordinationRowCount(source, admittedRowCount);", current)
sort = snapshot.index("result.Sort", final_rebound)
if not (loop < pre_index < current < final_rebound < sort):
    raise SystemExit("Coordination workbook Count-stability snapshot ordering changed.")
if "foreach (var row in source)" in snapshot:
    raise SystemExit("Coordination workbook caller-controlled snapshot must not regress to foreach traversal.")

required_smoke = (
    "GrowthAfterFirstRowRejectsBeforeUnexpectedIndexerRead",
    "ShrinkAfterFirstRowRejectsBeforeMissingIndexerRead",
    "PostTraversalCountDriftRejects",
    "StableRowsExportDeterministically",
    "source.IndexerReads == 1",
    "source.IndexerReads == 2",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Coordination workbook Count-stability smoke contract missing: " + repr(missing_smoke))

print("PASS coordination workbook Count-stability source guard")
