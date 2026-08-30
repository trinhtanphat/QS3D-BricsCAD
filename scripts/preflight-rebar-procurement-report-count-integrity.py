#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Rebar" / "RebarProcurementReport.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RebarProcurementReportCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "rebar-procurement-report-count-integrity.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Rebar procurement report Count-integrity preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

start = source.index("        public static IReadOnlyList<RebarProcurementSummary> Build(")
end = source.index("        private static int CompareRows(", start)
region = source[start:end]

required = (
    "RequireKnownCountWithinLimit(results)",
    "using (var enumerator = results.GetEnumerator())",
    "while (true)",
    "RequireStableKnownCount(results, expectedCount)",
    "if (!enumerator.MoveNext())",
    "var result = enumerator.Current;",
    "if (result == null)",
    "if (!groupIds.Add(result.Demand.GroupId))",
    "rows.Add(new RebarProcurementSummary(result));",
)
for token in required:
    if token not in region:
        raise SystemExit("Rebar procurement report Count-integrity guard missing token: " + token)

if "foreach (var result in results)" in region:
    raise SystemExit("Rebar procurement report must not use caller-controlled foreach traversal.")

rebound = "RequireStableKnownCount(results, expectedCount)"
if region.count(rebound) < 5:
    raise SystemExit("Rebar procurement report must retain pre-move, terminal, post-success, post-Current, and final Count rebounds.")

pre_move_rebound = region.index(rebound)
move_next = region.index("if (!enumerator.MoveNext())", pre_move_rebound)
terminal_rebound = region.index(rebound, move_next)
post_success_rebound = region.index(rebound, terminal_rebound + len(rebound))
known_admission = region.index("observedCount >= expectedCount.Value", post_success_rebound)
hard_cap = region.index("observedCount >= MaxResultCount", known_admission)
current = region.index("var result = enumerator.Current;", hard_cap)
post_current_rebound = region.index(rebound, current)
null_guard = region.index("if (result == null)", post_current_rebound)
duplicate_guard = region.index("if (!groupIds.Add(result.Demand.GroupId))", null_guard)
row_mutation = region.index("rows.Add(new RebarProcurementSummary(result));", duplicate_guard)
observed_mutation = region.index("observedCount++;", row_mutation)
final_rebound = region.rindex(rebound)
cardinality = region.index("observedCount != expectedCount.Value", final_rebound)
if not (
    pre_move_rebound < move_next < terminal_rebound < post_success_rebound <
    known_admission < hard_cap < current < post_current_rebound < null_guard <
    duplicate_guard < row_mutation < observed_mutation < final_rebound < cardinality
):
    raise SystemExit(
        "Rebar procurement report traversal must preserve Count -> MoveNext -> Count -> bounds -> Current -> Count -> semantic acceptance -> final Count ordering."
    )

for token in (
    "KnownCountOverrunRejectsBeforeSecondCurrent",
    "KnownCountUnderYieldFailsClosed",
    "TransientGrowthRejectsBeforeCurrent",
    "TransientShrinkRejectsBeforeCurrent",
    "TransientNegativeRejectsBeforeCurrent",
    "TransientConflictRejectsBeforeCurrent",
    "CurrentInducedCountDriftRejectsBeforeNullAcceptance",
    "CurrentMutatingCountCollection",
    "must be rejected before null-result acceptance",
    "return null!;",
    "StableCountedAndStreamingRemainAccepted",
    "Equal(0, source.CurrentReads)",
):
    if token not in smoke:
        raise SystemExit("Rebar procurement report Count-integrity smoke missing assertion: " + token)

for token in (
    "Count -> MoveNext -> Count -> bounds -> Current -> Count -> semantic acceptance",
    "Current-induced",
    "NOT_APPLICABLE",
):
    if token not in runbook:
        raise SystemExit("Rebar procurement report Count-integrity runbook missing contract: " + token)

print("PASS rebar procurement report known-Count Current-integrity source guard")
