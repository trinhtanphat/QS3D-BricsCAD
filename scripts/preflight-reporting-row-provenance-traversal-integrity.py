#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/ReportingRowProvenance.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ReportingRowProvenanceTraversalIntegritySmoke.cs"
CURRENT_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ReportingRowProvenanceCurrentCountSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/reporting-row-provenance-traversal-integrity.md"

for path in (SOURCE, SMOKE, CURRENT_SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("reporting provenance traversal-integrity file missing: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
current_smoke = CURRENT_SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required_source = (
    "private const int MaxSourceHandleEntries = 10000;",
    "var knownCount = ResolveKnownCount(sourceHandles);",
    "var staged = new List<string>();",
    "using (var enumerator = sourceHandles.GetEnumerator())",
    "while (true)",
    "RequireStableKnownCount(sourceHandles, knownCount);",
    "var moved = enumerator.MoveNext();",
    "if (!moved) break;",
    "if (knownCount.HasValue && index >= knownCount.Value)",
    "if (index >= MaxSourceHandleEntries)",
    "var raw = enumerator.Current;",
    "if (knownCount.HasValue && index != knownCount.Value)",
    "foreach (var handle in staged) target.Add(handle);",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("reporting provenance traversal-integrity source token(s) missing: " + repr(missing))

append_start = source.index("internal static void AppendSourceHandles")
helper_start = source.index("private static string[] SnapshotTargetValues", append_start)
append = source[append_start:helper_start]
pre_move = append.index("RequireStableKnownCount(sourceHandles, knownCount);")
move_next = append.index("var moved = enumerator.MoveNext();", pre_move)
post_move = append.index("RequireStableKnownCount(sourceHandles, knownCount);", pre_move + 1)
break_guard = append.index("if (!moved) break;", post_move)
known_guard = append.index("if (knownCount.HasValue && index >= knownCount.Value)", break_guard)
cap_guard = append.index("if (index >= MaxSourceHandleEntries)", known_guard)
current = append.index("var raw = enumerator.Current;", cap_guard)
post_current_target = append.index("RequireStableTarget(target, targetSnapshot);", current)
post_current_count = append.index("RequireStableKnownCount(sourceHandles, knownCount);", current)
handle = append.index("var handle = raw ?? string.Empty;", current)
stage = append.index("staged.Add(handle);", handle)
final_stability = append.rindex("RequireStableKnownCount(sourceHandles, knownCount);")
cardinality = append.index("if (knownCount.HasValue && index != knownCount.Value)", final_stability)
publish = append.index("foreach (var handle in staged) target.Add(handle);", cardinality)
if not (
    pre_move < move_next < post_move < break_guard < known_guard < cap_guard < current <
    post_current_target < post_current_count < handle < stage < final_stability < cardinality < publish
):
    raise SystemExit("reporting provenance traversal/publication ordering changed")
if append.count("var moved = enumerator.MoveNext();") != 1 or append.count("if (!moved) break;") != 1:
    raise SystemExit("reporting provenance MoveNext admission contract changed")
if "foreach (var raw in sourceHandles)" in append:
    raise SystemExit("reporting provenance regressed to direct foreach traversal/publication")
if "target.Add(handle);\n                index++;" in append:
    raise SystemExit("reporting provenance regressed to per-entry target publication during traversal")

required_smoke = (
    "LateMalformedEntryPublishesNothing",
    "LateDuplicatePublishesNothing",
    "EnumeratorFailurePublishesNothing",
    "KnownCountOverrunRejectsBeforeExtraCurrent",
    "KnownCountUnderYieldPublishesNothing",
    "CountDriftAfterCurrentFailsBeforeNextMoveNext",
    "MoveNextInducedCountDriftFailsBeforeCurrent",
    "StableCountedSourcePublishesAtomically",
    "StreamingSourceRemainsSupported",
    "StreamingHardCapRejectsBeforeExtraCurrent",
    "[ModuleInitializer]",
)
missing = [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("reporting provenance traversal-integrity smoke token(s) missing: " + repr(missing))

required_current_smoke = (
    "CurrentInducedCountDriftPreemptsMalformedItemValidation",
    '"   "',
    '"known Count changed during traversal from 1 to 2"',
    'Equal(1, source.MoveNextCalls, "MoveNext calls")',
    'Equal(1, source.CurrentReads, "Current reads")',
    "[ModuleInitializer]",
)
missing = [token for token in required_current_smoke if token not in current_smoke]
if missing:
    raise SystemExit("reporting provenance Current-count smoke token(s) missing: " + repr(missing))

for token in ("atomic", "known Count", "before `Current`", "post-`Current`", "zero partial"):
    if token not in runbook:
        raise SystemExit("reporting provenance traversal-integrity runbook token missing: " + token)

print("PASS reporting row provenance hostile traversal atomicity, bounds, and post-Current Count stability")
