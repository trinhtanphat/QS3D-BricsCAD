from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "DependencyGraph.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DependencyOrderSemanticSnapshotStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
errors = []

start = source.find("public IReadOnlyList<ProjectElement> TopologicalDirtyOrder")
end = source.find("private static int? RejectKnownOversizedInput", start)
method = source[start:end] if start >= 0 and end >= 0 else ""
if not method:
    errors.append("cannot locate TopologicalDirtyOrder boundary")

required_source = [
    "OrderingSnapshot",
    "CaptureOrderingSnapshot(element)",
    "RequireStableOrderingSnapshot",
    "snapshot.Dirty",
    "snapshot.Dependencies",
]
for token in required_source:
    if token not in source:
        errors.append("missing dependency-order snapshot token: " + token)

if "frame.Element.DependsOn" in method:
    errors.append("dependency DFS regressed to live DependsOn reads")
if "element.Dirty != ElementDirtyFlags.None" in method:
    errors.append("dependency ordering regressed to post-enumeration live Dirty read")

required_smoke = [
    "DirtyMutationFromLaterMoveNextFailsClosed",
    "DependencyMutationFromLaterMoveNextFailsClosed",
    "StableSemanticStatePreservesTopologicalOrder",
    "MoveNextCalls",
    "CurrentReads",
]
for token in required_smoke:
    if token not in smoke:
        errors.append("missing deterministic smoke token: " + token)

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS dependency-order semantic snapshot stability source guard")
