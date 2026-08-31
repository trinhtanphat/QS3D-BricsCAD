#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source_path = ROOT / "src/QS3D.Core/Services/DependencyImpactPlanner.cs"
smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/DependencyImpactPlannerTransientKnownCountSmoke.cs"
legacy_guard_path = ROOT / "scripts/preflight-dependency-impact-known-count-stability.py"
errors = []

for path in (source_path, smoke_path, legacy_guard_path):
    if not path.is_file():
        errors.append("missing dependency-impact transient Count-stability file: " + str(path.relative_to(ROOT)))

source = source_path.read_text(encoding="utf-8") if source_path.is_file() else ""
smoke = smoke_path.read_text(encoding="utf-8") if smoke_path.is_file() else ""
legacy = legacy_guard_path.read_text(encoding="utf-8") if legacy_guard_path.is_file() else ""

for token in (
    "using (var enumerator = sourceElementIds.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount, nameof(sourceElementIds));",
    "invalid negative known Count during traversal",
    "conflicting known Count values during traversal",
    "known Count changed while its roots were being traversed",
):
    if token not in source:
        errors.append("DependencyImpactPlanner missing transient Count token: " + token)

for token in (
    "[ModuleInitializer]",
    "TransientGrowthRejectsBeforeCurrent",
    "TransientShrinkRejectsBeforeCurrent",
    "TransientNegativeRejectsBeforeCurrent",
    "TransientConflictRejectsBeforeCurrent",
    "CurrentGrowthRejectsImmediatelyAfterCurrent",
    "CurrentShrinkRejectsImmediatelyAfterCurrent",
    "CurrentNegativeRejectsImmediatelyAfterCurrent",
    "CountReadsAfterCurrent == 1",
    "StableCountedInputStillPlans",
    "StreamingInputStillPlans",
    "MoveNextCalls == 1 && source.CurrentReads == 0",
):
    if token not in smoke:
        errors.append("Dependency-impact transient Count smoke missing token: " + token)

start = source.find("private static IReadOnlyList<string> CanonicalRoots")
end = source.find("private static void RequireKnownCountStableAfterTraversal", start)
method = source[start:end] if start >= 0 and end > start else ""
ordered = (
    "while (enumerator.MoveNext())",
    "RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount, nameof(sourceElementIds));",
    "index >= knownCount.Value",
    "index >= maxRootCount",
    "var value = enumerator.Current;",
    "RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount, nameof(sourceElementIds));",
    "var raw = value ?? string.Empty;",
    "result.Add(raw);",
)
search_from = 0
positions = []
for token in ordered:
    pos = method.find(token, search_from)
    positions.append(pos)
    if pos >= 0:
        search_from = pos + len(token)
if not method or any(pos < 0 for pos in positions):
    errors.append("Dependency-impact traversal must enforce MoveNext -> Count rebound -> cardinality gates -> Current -> Count rebound -> semantic retention.")
if "foreach (var value in sourceElementIds)" in method:
    errors.append("Dependency-impact caller-controlled roots must not use foreach before Count revalidation.")

if "MoveNext -> traversal Count rebound -> advertised-count guard -> Current" not in legacy:
    errors.append("Historical dependency-impact known-count guard must pin the stronger pre-Current ordering.")

print("QS3D dependency-impact transient known-count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: dependency-impact transient Count drift rejects around MoveNext/Current before semantic retention.")
