#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "XlsxHandleReader.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "XlsxHandleLookupResultBoundSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
materializer_start = source.index("        private static IReadOnlyList<string> MaterializeIdentityValues")
materializer_end = source.index("    public static class XlsxHandleReader", materializer_start)
materializer = source[materializer_start:materializer_end]

stable = "RequireKnownCountStable(values, admittedCount, label);"
move = "var moved = enumerator.MoveNext();"
overrun = "admittedCount.HasValue && observed >= admittedCount.Value"
current = "var value = enumerator.Current;"
retain = "observed++;"

for token in (
    "ReadKnownCount(values, label)",
    "private static void RequireKnownCountStable(",
    "values is ICollection<string> collection",
    "values is IReadOnlyCollection<string> readOnlyCollection",
    "values is ICollection nonGenericCollection",
    "reported conflicting identity Count values",
    "Count changed during materialization",
    move,
    overrun,
    current,
):
    if token not in materializer:
        raise SystemExit("XLSX handle lookup Count-integrity guard missing source token: " + token)

pre_move = materializer.index(stable)
move_pos = materializer.index(move, pre_move)
post_move = materializer.index(stable, move_pos + len(move))
overrun_pos = materializer.index(overrun, post_move)
current_pos = materializer.index(current, overrun_pos)
post_current = materializer.index(stable, current_pos + len(current))
retain_pos = materializer.index(retain, post_current)
if not (pre_move < move_pos < post_move < overrun_pos < current_pos < post_current < retain_pos):
    raise SystemExit(
        "XLSX identity traversal must be rebound -> MoveNext -> rebound -> overrun/bound -> Current -> rebound -> retention"
    )
if "foreach (var value in values)" in materializer:
    raise SystemExit("XLSX identity materialization must not regress to unguarded foreach traversal")

for token in (
    "KnownOverBoundRejectsBeforeEnumeration",
    "ConflictingKnownCountsRejectBeforeEnumeration",
    "KnownOverYieldRejectsBeforeUnexpectedCurrent",
    "KnownUnderYieldRejectsAtTraversalEnd",
    "TransientMoveNextCountDriftRejectsBeforeCurrent",
    "TransientCurrentCountDriftRejectsBeforeRetention",
    "HandlesRejectFirstStreamingOverBoundObservationBeforeCurrent",
    "ElementIdsRejectFirstStreamingOverBoundObservationBeforeCurrent",
    "StableInputsPreserveCanonicalizationAndDeduplication",
    "[ModuleInitializer]",
):
    if token not in smoke:
        raise SystemExit("XLSX handle lookup Count-integrity smoke missing scenario/token: " + token)

print("PASS XLSX handle lookup known-Count traversal integrity source guard")
