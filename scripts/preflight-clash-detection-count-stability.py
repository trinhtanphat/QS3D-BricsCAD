#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Coordination/ClashDetection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ClashDetectionCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/clash-detection-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Clash detection Count-stability preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "var expectedCount = RequireKnownCountWithinLimit(elements);",
    "using (var enumerator = elements.GetEnumerator())",
    "RequireStableKnownCount(elements, expectedCount);",
    "if (!enumerator.MoveNext()) break;",
    "var element = enumerator.Current;",
    "Coordination input known element Count changed during snapshot.",
    "if (expectedCount.HasValue && snapshot.Count != expectedCount.Value)",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Clash detection Count-stability source contract missing: " + repr(missing))

method_start = source.index("public IReadOnlyList<ClashResult> Detect(")
capacity_start = source.index("private static void EnsureResultCapacity", method_start)
contract = source[method_start:capacity_start]
if "foreach (var element in elements)" in contract:
    raise SystemExit("Clash detection known-count snapshot must not use outer foreach traversal.")

admission = contract.index("var expectedCount = RequireKnownCountWithinLimit(elements);")
enumerator = contract.index("using (var enumerator = elements.GetEnumerator())", admission)
pre_move = contract.index("RequireStableKnownCount(elements, expectedCount);", enumerator)
move = contract.index("if (!enumerator.MoveNext()) break;", pre_move)
pre_current = contract.index("RequireStableKnownCount(elements, expectedCount);", pre_move + 1)
current = contract.index("var element = enumerator.Current;", pre_current)
post_current = contract.index("RequireStableKnownCount(elements, expectedCount);", current)
null_validation = contract.index("if (element == null)", post_current)
duplicate_validation = contract.index("if (!ids.Add(element.ElementId))", null_validation)
snapshot_acceptance = contract.index("snapshot.Add(element);", duplicate_validation)
final_rebound = contract.rindex("RequireStableKnownCount(elements, expectedCount);")
cardinality = contract.index("if (expectedCount.HasValue && snapshot.Count != expectedCount.Value)", final_rebound)
if not (
    admission < enumerator < pre_move < move < pre_current < current < post_current <
    null_validation < duplicate_validation < snapshot_acceptance < final_rebound < cardinality
):
    raise SystemExit("Clash detection Count-stability ordering changed.")

if contract.count("var element = enumerator.Current;") != 1:
    raise SystemExit("Clash detection must read accepted Current exactly once per traversal body.")

required_smoke = (
    "GrowthRejectsBeforeSecondAdvance",
    "ShrinkRejectsBeforeFirstCurrentRead",
    "CurrentCountDriftWinsBeforeNullValidation",
    "CurrentCountDriftEnumerable",
    "_owner._count = 2;",
    "FinalReboundRejectsAfterEnumerationEnds",
    "StableKnownCountProducesExpectedClash",
    "PureStreamingEnumerableRemainsSupported",
    "Equal(1, source.MoveNextCalls",
    "Equal(0, source.CurrentReads",
    "Equal(1, source.CurrentReads, \"Current-induced drift Current reads\")",
    "Equal(4, source.CountReads, \"Current-induced drift Count reads\")",
    "Equal(9, source.CountReads, \"stable Count reads\")",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Clash detection Count-stability smoke contract missing: " + repr(missing_smoke))

print("PASS clash detection Count-stability source guard")
