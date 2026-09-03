from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Revisions/SemanticChangeReview.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/SemanticChangeReviewSmoke.cs").read_text(encoding="utf-8")
runbook = (ROOT / "docs/FEATURE-RUNBOOKS/semantic-change-review-generation-stability.md").read_text(encoding="utf-8")

build = source.index("public SemanticChangeReview Build")
detach_before = source.index("RevisionSnapshotDetacher.Capture(before", build)
detach_after = source.index("RevisionSnapshotDetacher.Capture(after", detach_before)
index_before = source.index("Index(beforeSnapshot", detach_after)
compare = source.index("new RevisionService().Compare(beforeSnapshot, afterSnapshot)", index_before)

if not (build < detach_before < detach_after < index_before < compare):
    raise SystemExit("ERROR: semantic review must detach both inputs before indexing/comparison.")

for forbidden in [
    "Index(before, \"before\")",
    "Index(after, \"after\")",
    "new RevisionService().Compare(before, after)",
]:
    if forbidden in source[build:]:
        raise SystemExit("ERROR: semantic review still consults live caller snapshots after boundary admission: " + forbidden)

for token in [
    "ReviewUsesOneDetachedCategoryGeneration",
    "StructuralColumn",
    "StructuralWall",
    "MutatingDictionary",
    "Property:Mark",
]:
    if token not in smoke:
        raise SystemExit("ERROR: semantic review generation regression missing token: " + token)

for token in ["detached generation", "live reference", "category", "RevisionService.Compare", "no retry"]:
    if token not in runbook:
        raise SystemExit("ERROR: semantic review generation runbook missing token: " + token)

print("PASS semantic change review detached generation stability")
