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

first_rebound = region.index("RequireStableKnownCount(results, expectedCount)")
move_next = region.index("if (!enumerator.MoveNext())", first_rebound)
second_rebound = region.index("RequireStableKnownCount(results, expectedCount)", move_next)
known_admission = region.index("observedCount >= expectedCount.Value", second_rebound)
hard_cap = region.index("observedCount >= MaxResultCount", known_admission)
current = region.index("var result = enumerator.Current;", hard_cap)
if not (first_rebound < move_next < second_rebound < known_admission < hard_cap < current):
    raise SystemExit(
        "Rebar procurement report traversal must order Count rebound -> MoveNext -> Count rebound -> known-count/hard-cap admission -> Current."
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
