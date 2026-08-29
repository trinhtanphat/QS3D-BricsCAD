from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/MaterialUsageXlsxExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MaterialUsageXlsxSnapshotStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var sourceRows = new List<MaterialUsageRow>(rowCount);",
    "EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);",
    "EnsureProvenanceStable(source.ElementIds, snapshot.ElementIdValues",
    "EnsureProvenanceStable(source.SourceHandles, snapshot.SourceHandleValues",
    "changed during snapshot traversal",
]
forbidden_source = [
    "var currentSource = rows[rowIndex];",
    "ReferenceEquals(currentSource, sourceRows[rowIndex])",
]
required_smoke = [
    "CountStableScalarMutationFailsBeforePublication();",
    "CountStableProvenanceMutationFailsBeforePublication();",
    "CountStableMutatingRows",
    "single-read outer-row contract",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
forbidden = [token for token in forbidden_source if token in source]
if missing:
    raise SystemExit("Material Usage XLSX snapshot-stability preflight failed; missing: " + ", ".join(missing))
if forbidden:
    raise SystemExit("Material Usage XLSX snapshot-stability preflight failed; caller-owned rows must not be re-read: " + ", ".join(forbidden))

print("PASS Material Usage XLSX single-read snapshot integrity source guard")
