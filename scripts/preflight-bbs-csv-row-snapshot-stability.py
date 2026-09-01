from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "RebarCsvExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "BbsCsvCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var sourceRows = new List<RebarScheduleRow>();",
    "var snapshots = new List<RebarScheduleRow>();",
    "var snapshot = SnapshotRow(sourceRow);",
    "sourceRows.Add(sourceRow);",
    "snapshots.Add(snapshot);",
    "EnsureRowStable(sourceRows[index], snapshots[index], index);",
    "foreach (var row in snapshots)",
    "BBS CSV row values changed during serialization",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing BBS CSV row-snapshot source guard token: {token}")

capture = source.index("var snapshot = SnapshotRow(sourceRow);")
stability = source.index("EnsureRowStable(sourceRows[index], snapshots[index], index);")
projection = source.index("var sb = new StringBuilder();")
if not capture < stability < projection:
    raise SystemExit("BBS CSV must capture rows, validate source-row stability, then project CSV")

if "QIdentity(sourceRow." in source or "Append(Q(sourceRow." in source:
    raise SystemExit("BBS CSV projection must use captured snapshots rather than caller-owned source rows")

required_smoke = [
    "RowMutationRejectsAfterTraversalBeforeProjection",
    "RowMutatingEnumerable",
    "_first.BarMark = \"MUTATED\";",
    "ThrowsRowIntegrity",
    "StableKnownCountPreservesOutput",
    "PureStreamingSourceRemainsSupported",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing BBS CSV row-snapshot smoke token: {token}")

print("PASS BBS CSV row snapshot stability source guard")
