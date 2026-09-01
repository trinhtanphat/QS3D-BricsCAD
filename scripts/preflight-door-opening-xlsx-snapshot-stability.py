from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/DoorOpeningXlsxExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DoorOpeningXlsxSnapshotStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var sourceRows = new List<DoorOpeningScheduleRow>(rowCount.Value);",
    "var sourceRow = rows[rowIndex];",
    "EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);",
    "EnsureProvenanceStable(source.ElementIds, snapshot.ElementIds",
    "EnsureProvenanceStable(source.HostIds, snapshot.HostIds",
    "EnsureProvenanceStable(source.SourceHandles, snapshot.SourceHandles",
    "changed during snapshot traversal",
]
forbidden_source = [
    "var currentSource = rows[rowIndex];",
    "ReferenceEquals(currentSource, sourceRows[rowIndex])",
]
required_smoke = [
    "CountStableScalarMutationFailsBeforePublication();",
    "CountStableElementProvenanceMutationFailsBeforePublication();",
    "CountStableHostProvenanceMutationFailsBeforePublication();",
    "CountStableSourceHandleMutationFailsBeforePublication();",
    "CountStableMutatingRows",
    "single-read outer-row contract",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
forbidden = [token for token in forbidden_source if token in source]
if missing:
    raise SystemExit("Door/Opening XLSX snapshot-stability preflight failed; missing: " + ", ".join(missing))
if forbidden:
    raise SystemExit("Door/Opening XLSX snapshot-stability preflight failed; caller-owned rows must not be re-read: " + ", ".join(forbidden))
if source.count("var sourceRow = rows[rowIndex];") != 1:
    raise SystemExit("Door/Opening XLSX snapshot-stability preflight failed; caller-owned row indexer must remain single-read per traversal iteration")

before = source.find('rowCount.Revalidate(rows, "before row indexer")')
read = source.find("var sourceRow = rows[rowIndex];")
after = source.find('rowCount.Revalidate(rows, "after row indexer")')
stable = source.find("EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);")
if min(before, read, after, stable) < 0 or not (before < read < after < stable):
    raise SystemExit("Door/Opening XLSX snapshot-stability preflight failed; Count revalidation must wrap the one caller indexer read before detached-row stability checks")

print("PASS Door/Opening XLSX single-read snapshot integrity source guard with bound Count revalidation")
