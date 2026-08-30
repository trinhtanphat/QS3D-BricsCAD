#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Rebar" / "RebarProcurementReport.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RebarProcurementReportCountIntegritySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

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
)
for token in required:
    if token not in region:
        raise SystemExit("Rebar procurement report Count-integrity guard missing token: " + token)

if "foreach (var result in results)" in region:
    raise SystemExit("Rebar procurement report must not use caller-controlled foreach traversal.")

rebound = "RequireStableKnownCount(results, expectedCount)"
if region.count(rebound) < 4:
    raise SystemExit("Rebar procurement report must retain pre-move, terminal, post-success, and final Count rebounds.")

pre_move_rebound = region.index(rebound)
move_next = region.index("if (!enumerator.MoveNext())", pre_move_rebound)
terminal_rebound = region.index(rebound, move_next)
post_success_rebound = region.index(rebound, terminal_rebound + len(rebound))
known_admission = region.index("observedCount >= expectedCount.Value", post_success_rebound)
hard_cap = region.index("observedCount >= MaxResultCount", known_admission)
current = region.index("var result = enumerator.Current;", hard_cap)
final_rebound = region.index(rebound, current)
if not (
    pre_move_rebound < move_next < terminal_rebound < post_success_rebound <
    known_admission < hard_cap < current < final_rebound
):
    raise SystemExit(
        "Rebar procurement report traversal must preserve pre-move, terminal, post-success, admission, Current, and final Count-rebound ordering."
    )

for token in (
    "KnownCountOverrunRejectsBeforeSecondCurrent",
    "KnownCountUnderYieldFailsClosed",
    "TransientGrowthRejectsBeforeCurrent",
    "TransientShrinkRejectsBeforeCurrent",
    "TransientNegativeRejectsBeforeCurrent",
    "TransientConflictRejectsBeforeCurrent",
    "StableCountedAndStreamingRemainAccepted",
    "Equal(0, source.CurrentReads)",
):
    if token not in smoke:
        raise SystemExit("Rebar procurement report Count-integrity smoke missing assertion: " + token)

print("PASS rebar procurement report known-Count Current-integrity source guard")
