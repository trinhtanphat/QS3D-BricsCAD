from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/XlsxQuantityExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/XlsxQuantityNullRowSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    'if (rows.Count != count)',
    '"Quantity XLSX export row count changed during snapshot."',
    'sheetLabel + " row count changed during snapshot."',
    'if (source.Count != count)',
    '"Quantity XLSX provenance count changed during snapshot."',
]
required_smoke = [
    "AssertStandardRowCountDriftPreservesDestination",
    "AssertEd2RowCountDriftPreservesDestination",
    "CountDriftingRows",
    "preserve-existing-quantity-xlsx",
    "row count changed during snapshot",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Quantity XLSX snapshot-integrity preflight failed; missing: " + ", ".join(missing))

standard_loop = source.find("private static IReadOnlyList<QuantityReportRow> SnapshotStandardRows")
standard_rebind = source.find('"Quantity XLSX export row count changed during snapshot."')
ed2_loop = source.find("private static IReadOnlyList<QuantityReportRow> SnapshotEd2Rows")
ed2_rebind = source.find('sheetLabel + " row count changed during snapshot."')
if min(standard_loop, standard_rebind, ed2_loop, ed2_rebind) < 0 or not (standard_loop < standard_rebind < ed2_loop < ed2_rebind):
    raise SystemExit("Quantity XLSX snapshot-integrity preflight failed; Count rebind guards are not anchored after their snapshot traversals.")

print("PASS Quantity XLSX standard/ED2 snapshot Count stability source guard")
