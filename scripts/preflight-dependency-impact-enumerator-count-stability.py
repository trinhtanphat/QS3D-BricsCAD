#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Services/DependencyImpactPlanner.cs"
smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/DependencyImpactPlannerEnumeratorAcquisitionKnownCountSmoke.cs"
errors = []

for path in (source_path, smoke_path):
    if not path.is_file():
        errors.append("missing dependency-impact enumerator Count-integrity file: " + str(path.relative_to(ROOT)))

source = source_path.read_text(encoding="utf-8") if source_path.is_file() else ""
smoke = smoke_path.read_text(encoding="utf-8") if smoke_path.is_file() else ""

start = source.find("private static IReadOnlyList<string> CanonicalRoots")
end = source.find("private static void RequireKnownCountStableDuringTraversal", start)
method = source[start:end] if start >= 0 and end > start else ""
ordered = (
    "RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount, nameof(sourceElementIds));",
    "sourceElementIds.GetEnumerator()",
    "RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount, nameof(sourceElementIds));",
    "while (enumerator.MoveNext())",
)
search_from = 0
positions = []
for token in ordered:
    pos = method.find(token, search_from)
    positions.append(pos)
    if pos >= 0:
        search_from = pos + len(token)
if not method or any(pos < 0 for pos in positions):
    errors.append("Dependency-impact roots must enforce Count rebound -> GetEnumerator -> Count rebound -> first MoveNext ordering.")

for token in (
    "[ModuleInitializer]",
    "AcquisitionGrowthRejectsBeforeMoveNext",
    "AcquisitionShrinkRejectsBeforeMoveNext",
    "AcquisitionNegativeRejectsBeforeMoveNext",
    "AcquisitionConflictRejectsBeforeMoveNext",
    "GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0",
    "StableCountedInputStillPlans",
    "StreamingInputStillPlans",
):
    if token not in smoke:
        errors.append("Dependency-impact enumerator Count smoke missing token: " + token)

print("QS3D dependency-impact enumerator-acquisition Count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: dependency-impact known Count is revalidated immediately around enumerator acquisition.")
