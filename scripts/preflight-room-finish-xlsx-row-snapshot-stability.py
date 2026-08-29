from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/RoomFinishXlsxExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RoomFinishXlsxRowSnapshotSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var sourceRows = new List<RoomFinishScheduleRow>(rowCount);",
    "sourceRows.Add(sourceRow);",
    "EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);",
    "EnsureJoinedCellValuesStable(source.ElementIds, snapshot.ElementIds, rowIndex, \"ElementIds\");",
    "!source.LengthM.Equals(snapshot.LengthM)",
    "values changed during snapshot",
]
required_smoke = [
    "CrossRowTextMutationFailsBeforeIo();",
    "CrossRowProvenanceMutationFailsBeforeIo();",
    "CrossRowMutatingList",
    "single-read outer-row contract",
    "must preserve the existing destination",
]
forbidden_source = [
    "var currentSource = rows[rowIndex];",
    "ReferenceEquals(rows[rowIndex]",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
forbidden = [token for token in forbidden_source if token in source]
if missing:
    raise SystemExit("Room-finish XLSX snapshot-stability preflight failed; missing: " + ", ".join(missing))
if forbidden:
    raise SystemExit("Room-finish XLSX snapshot-stability preflight failed; caller-owned rows must not be re-read: " + ", ".join(forbidden))

count_check = source.find('if (rows.Count != rowCount)')
stability_check = source.find('EnsureRowStable(sourceRows[rowIndex], snapshot[rowIndex], rowIndex);')
io_boundary = source.find('var fullPath = Path.GetFullPath(path);')
if min(count_check, stability_check, io_boundary) < 0 or not (count_check < stability_check < io_boundary):
    raise SystemExit("Room-finish XLSX snapshot-stability preflight failed; stability verification must follow the outer count check and precede filesystem work.")

print("PASS Room-finish XLSX single-read cross-row snapshot integrity source guard")
