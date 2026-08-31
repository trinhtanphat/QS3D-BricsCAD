from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Geometry/WallJunctionPlanner.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/WallJunctionKnownCountIntegritySmoke.cs").read_text(encoding="utf-8")

required_source = [
    "var raw = MaterializeSegments(source);",
    "private static List<WallAxisSegment> MaterializeSegments",
    "var knownCount = ReadKnownCount(source);",
    "var moved = enumerator.MoveNext();",
    "EnsureKnownCountStable(source, knownCount);",
    "if (knownCount.HasValue && raw.Count >= knownCount.Value)",
    "var current = enumerator.Current;",
    "raw.Add(current);",
    "if (source is ICollection<WallAxisSegment> collection)",
    "if (source is IReadOnlyCollection<WallAxisSegment> readOnlyCollection)",
    "if (source is ICollection nonGenericCollection)",
    "reported conflicting Count values",
]
for needle in required_source:
    if needle not in source:
        raise SystemExit(f"wall-junction known-count preflight missing production contract: {needle}")

if "source.Take(MaxSegments + 1).ToList()" in source:
    raise SystemExit("wall-junction known-count preflight rejects the legacy LINQ materialization boundary")

move_index = source.index("var moved = enumerator.MoveNext();")
post_move_index = source.index("EnsureKnownCountStable(source, knownCount);", move_index)
overrun_index = source.index("if (knownCount.HasValue && raw.Count >= knownCount.Value)", post_move_index)
current_index = source.index("var current = enumerator.Current;", overrun_index)
post_current_index = source.index("EnsureKnownCountStable(source, knownCount);", current_index)
retain_index = source.index("raw.Add(current);", post_current_index)
if not (move_index < post_move_index < overrun_index < current_index < post_current_index < retain_index):
    raise SystemExit("wall-junction known-count traversal ordering is not fail-closed")

required_smoke = [
    "RejectZeroCountOverYieldBeforeCurrent",
    "RejectTransientMoveNextCountDrift",
    "RejectTransientCurrentCountDrift",
    "AcceptStableCountedSource",
    "AcceptPureStreamingSource",
    "CurrentReads != 0",
]
for needle in required_smoke:
    if needle not in smoke:
        raise SystemExit(f"wall-junction known-count preflight missing regression coverage: {needle}")

print("PASS wall junction known-Count integrity")
