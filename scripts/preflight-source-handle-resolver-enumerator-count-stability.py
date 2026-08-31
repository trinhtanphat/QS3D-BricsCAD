#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Services/SourceHandleResolver.cs"
smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/SourceHandleResolverEnumeratorAcquisitionKnownCountSmoke.cs"
errors = []

for path in (source_path, smoke_path):
    if not path.is_file():
        errors.append("missing Locate enumerator Count-integrity file: " + str(path.relative_to(ROOT)))

source = source_path.read_text(encoding="utf-8") if source_path.is_file() else ""
smoke = smoke_path.read_text(encoding="utf-8") if smoke_path.is_file() else ""

start = source.find("private static IReadOnlyList<string> MaterializeRootElementIds(IEnumerable<string> elementIds)")
end = source.find("private static void RequireStableKnownCountDuringTraversal", start)
method = source[start:end] if start >= 0 and end > start else ""
rebound = "RequireStableKnownCountDuringTraversal(elementIds, knownCount);"
acquisition = "using (var enumerator = elementIds.GetEnumerator())"
move = "if (!enumerator.MoveNext())"
first = method.find(rebound)
acquire = method.find(acquisition, first + len(rebound))
second = method.find(rebound, acquire + len(acquisition))
third = method.find(rebound, second + len(rebound))
move_pos = method.find(move, third + len(rebound))
if min(first, acquire, second, third, move_pos) < 0 or not (first < acquire < second < third < move_pos):
    errors.append("Locate roots must enforce Count rebound -> GetEnumerator -> Count rebound -> loop Count rebound -> MoveNext ordering.")

for token in (
    "[ModuleInitializer]",
    "AcquisitionGrowthRejectsBeforeMoveNext",
    "AcquisitionShrinkRejectsBeforeMoveNext",
    "AcquisitionNegativeRejectsBeforeMoveNext",
    "AcquisitionConflictRejectsBeforeMoveNext",
    "GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0",
    "StableCountStillResolves",
    "StreamingInputStillResolves",
):
    if token not in smoke:
        errors.append("Locate enumerator Count smoke missing token: " + token)

print("QS3D SourceHandleResolver enumerator-acquisition Count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Locate known Count is revalidated immediately before and after enumerator acquisition.")
