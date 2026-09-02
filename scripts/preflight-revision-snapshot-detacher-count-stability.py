from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Revisions/RevisionSnapshotDetacher.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/RevisionSnapshotDetacherCountStabilitySmoke.cs").read_text(encoding="utf-8")
runbook = (ROOT / "docs/FEATURE-RUNBOOKS/revision-snapshot-detacher-count-stability.md").read_text(encoding="utf-8")

required_source = [
    "using (var enumerator = source.GetEnumerator())",
    "if (source.Count != expectedCount)",
    "var moved = enumerator.MoveNext();",
    "if (!moved) break;",
    "var pair = enumerator.Current;",
    "destination.Add(pair.Key, pair.Value);",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"ERROR: revision detacher Count-stability source contract missing: {token}")

get_idx = source.index("using (var enumerator = source.GetEnumerator())")
move_idx = source.index("var moved = enumerator.MoveNext();", get_idx)
current_idx = source.index("var pair = enumerator.Current;", move_idx)
add_idx = source.index("destination.Add(pair.Key, pair.Value);", current_idx)
if source.find("if (source.Count != expectedCount)", get_idx, move_idx) < 0:
    raise SystemExit("ERROR: Count is not rebound after enumerator acquisition and before MoveNext.")
if source.find("if (source.Count != expectedCount)", move_idx, current_idx) < 0:
    raise SystemExit("ERROR: Count is not rebound after MoveNext and before Current.")
if source.find("if (source.Count != expectedCount)", current_idx, add_idx) < 0:
    raise SystemExit("ERROR: Count is not rebound after Current and before publication.")

for token in [
    "RejectsEnumeratorAcquisitionCountDriftBeforeTraversal",
    "RejectsMoveNextCountDriftBeforeCurrent",
    "RejectsCurrentCountDriftBeforePublication",
    "StableDictionaryRemainsAccepted",
    "MoveNextCalls != 0 || map.CurrentReads != 0",
    "map.MoveNextCalls != 1 || map.CurrentReads != 0",
]:
    if token not in smoke:
        raise SystemExit(f"ERROR: revision detacher hostile regression missing: {token}")

for token in ["GetEnumerator", "MoveNext", "Current", "before publication", "100,000"]:
    if token not in runbook:
        raise SystemExit(f"ERROR: revision detacher Count-stability runbook missing: {token}")

print("PASS revision snapshot detacher map Count stability")
