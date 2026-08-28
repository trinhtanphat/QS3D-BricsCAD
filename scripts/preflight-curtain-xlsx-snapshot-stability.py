from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CurtainWallXlsxExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallXlsxSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var sourceRows = new List<CurtainWallScheduleRow>(rowCount);",
    "ReferenceEquals(currentSource, sourceRows[rowIndex])",
    "row source changed during snapshot",
    "EnsureRowStable(currentSource, snapshot[rowIndex], rowIndex);",
    "EnsureJoinedCellValuesStable(source, target, label);",
    "values changed during snapshot",
]
required_smoke = [
    "CountStableRowReplacementFailsBeforePublication();",
    "CountStableProvenanceMutationFailsBeforePublication();",
    "RebindingRows",
    "count-stable row replacement must preserve destination",
    "count-stable provenance mutation must preserve destination",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("Curtain XLSX snapshot-stability preflight failed; missing: " + ", ".join(missing))

print("PASS Curtain XLSX count-stable snapshot integrity source guard")
