#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/ReportingRowProvenance.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ReportingRowProvenanceTargetStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/reporting-row-provenance-target-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("reporting target-stability file missing: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required_source = (
    "var targetSnapshot = SnapshotTargetValues(target);",
    "var existingIdentities = SnapshotTargetIdentities(targetSnapshot);",
    "RequireStableTarget(target, targetSnapshot);",
    "var moved = enumerator.MoveNext();",
    "if (!moved) break;",
    "var raw = enumerator.Current;",
    "foreach (var handle in staged) target.Add(handle);",
    "private static string[] SnapshotTargetValues(IList<string> target)",
    "private static void RequireStableTarget(IList<string> target, string[] expected)",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("reporting target-stability source token(s) missing: " + repr(missing))

append_start = source.index("internal static void AppendSourceHandles")
helper_start = source.index("private static string[] SnapshotTargetValues", append_start)
append = source[append_start:helper_start]

snapshot = append.index("var targetSnapshot = SnapshotTargetValues(target);")
identity_snapshot = append.index("var existingIdentities = SnapshotTargetIdentities(targetSnapshot);", snapshot)
pre_move_target = append.index("RequireStableTarget(target, targetSnapshot);", identity_snapshot)
pre_move_count = append.index("RequireStableKnownCount(sourceHandles, knownCount);", pre_move_target)
move = append.index("var moved = enumerator.MoveNext();", pre_move_count)
post_move_target = append.index("RequireStableTarget(target, targetSnapshot);", move)
post_move_count = append.index("RequireStableKnownCount(sourceHandles, knownCount);", post_move_target)
break_guard = append.index("if (!moved) break;", post_move_count)
current = append.index("var raw = enumerator.Current;", break_guard)
post_current_target = append.index("RequireStableTarget(target, targetSnapshot);", current)
stage = append.index("staged.Add(handle);", post_current_target)
post_traversal_target = append.index("RequireStableTarget(target, targetSnapshot);", stage)
final_count = append.index("RequireStableKnownCount(sourceHandles, knownCount);", post_traversal_target)
cardinality = append.index("if (knownCount.HasValue && index != knownCount.Value)", final_count)
prepublish_target = append.index("RequireStableTarget(target, targetSnapshot);", cardinality)
publish = append.index("foreach (var handle in staged) target.Add(handle);", prepublish_target)

if not (
    snapshot < identity_snapshot < pre_move_target < pre_move_count < move <
    post_move_target < post_move_count < break_guard < current < post_current_target <
    stage < post_traversal_target < final_count < cardinality < prepublish_target < publish
):
    raise SystemExit("reporting target-stability traversal/publication ordering changed")

if append.count("var raw = enumerator.Current;") != 1:
    raise SystemExit("reporting target-stability Current read contract changed")
if append.count("foreach (var handle in staged) target.Add(handle);") != 1:
    raise SystemExit("reporting target-stability atomic publication contract changed")

required_smoke = (
    "MoveNextAppendMutationFailsBeforeCurrentAndPublishesNothing",
    "MoveNextRemoveMutationFailsBeforeCurrentAndPublishesNothing",
    "CurrentReplaceMutationFailsClosedAndPublishesNothing",
    "CurrentReorderMutationFailsClosedAndPublishesNothing",
    "StableTargetStillPublishesAtomically",
    "PureStreamingStableTargetRemainsAccepted",
    "Equal(0, source.CurrentReads",
    "[ModuleInitializer]",
)
missing = [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("reporting target-stability smoke token(s) missing: " + repr(missing))

for token in (
    "target snapshot",
    "MoveNext",
    "Current",
    "zero C03-owned publication",
    "known-Count",
    "10,000",
    "pure streaming",
):
    if token not in runbook:
        raise SystemExit("reporting target-stability runbook token missing: " + token)

print("PASS reporting row provenance target stability across hostile source traversal")
