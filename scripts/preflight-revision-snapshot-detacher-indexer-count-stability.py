from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Revisions/RevisionSnapshotDetacher.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/RevisionSnapshotDetacherIndexerCountStabilitySmoke.cs").read_text(encoding="utf-8")
runbook = (ROOT / "docs/FEATURE-RUNBOOKS/revision-snapshot-detacher-indexer-count-stability.md").read_text(encoding="utf-8")

capture_index = source.index("var element = elements[index];")
capture_nested = source.index("CopyMap(element.Properties", capture_index)
if source.find("if (elements.Count != elementCount)", capture_index, capture_nested) < 0:
    raise SystemExit("ERROR: element Count is not rebound after indexer read before nested copying.")

copy_list = source.index("private static void CopyList<T>")
item_index = source.index("var item = source[index];", copy_list)
publish_index = source.index("destination.Add(item);", item_index)
if source.find("if (source.Count != expectedCount)", item_index, publish_index) < 0:
    raise SystemExit("ERROR: list Count is not rebound after indexer read before publication.")

for token in [
    "RejectsNestedListIndexerDriftBeforeDestinationPublication",
    "RejectsElementIndexerDriftBeforeNestedCopy",
    "StableListCopyRemainsAccepted",
    "destination.AddCalls != 0",
    "nestedSourceHandles.CountReads != 0",
]:
    if token not in smoke:
        raise SystemExit(f"ERROR: revision indexer Count regression missing: {token}")

for token in ["indexer", "before publication", "nested copying", "100,000", "#5359"]:
    if token not in runbook:
        raise SystemExit(f"ERROR: revision indexer Count runbook missing: {token}")

print("PASS revision snapshot detacher indexer Count stability")
