#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Coordination/DuplicateDetection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DuplicateDetectionCountStabilitySmoke.cs"
CURRENT_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DuplicateDetectionCurrentCountAcceptanceSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/duplicate-detection-count-stability.md"

for path in (SOURCE, SMOKE, CURRENT_SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Duplicate detection Count-stability preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
current_smoke = CURRENT_SMOKE.read_text(encoding="utf-8")

required_source = (
    "return DetectSnapshot(MaterializeElements(elements), effective);",
    "return DetectSnapshot(MaterializeCandidates(candidates), effective);",
    "private static List<DuplicateCandidate> MaterializeElements",
    "private static List<DuplicateCandidate> MaterializeCandidates",
    "RequireStableKnownCount(elements, expectedCount);",
    "RequireStableKnownCount(candidates, expectedCount);",
    "if (!enumerator.MoveNext()) break;",
    "Duplicate-detection input known element Count changed during snapshot.",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Duplicate detection Count-stability source contract missing: " + repr(missing))

if "return Detect(ProjectCandidates(elements), options);" in source or "private static IEnumerable<DuplicateCandidate> ProjectCandidates" in source:
    raise SystemExit("Duplicate detection element overload must not erase known Count evidence through generator projection.")

for method_name, source_name, semantic_guard in (
    ("MaterializeElements", "elements", "if (element == null)"),
    ("MaterializeCandidates", "candidates", "if (candidate == null)"),
):
    start = source.index("private static List<DuplicateCandidate> " + method_name)
    next_method = source.index("private static ", start + 20)
    contract = source[start:next_method]
    admission = contract.index("var expectedCount = RequireKnownCountWithinLimit(" + source_name + ");")
    enumerator = contract.index("using (var enumerator = " + source_name + ".GetEnumerator())")
    pre_move = contract.index("RequireStableKnownCount(" + source_name + ", expectedCount);", enumerator)
    move = contract.index("if (!enumerator.MoveNext()) break;", pre_move)
    pre_current = contract.index("RequireStableKnownCount(" + source_name + ", expectedCount);", pre_move + 1)
    current = contract.index("enumerator.Current", pre_current)
    post_current = contract.index("RequireStableKnownCount(" + source_name + ", expectedCount);", current)
    semantic_acceptance = contract.index(semantic_guard, post_current)
    final_rebound = contract.rindex("RequireStableKnownCount(" + source_name + ", expectedCount);")
    cardinality = contract.index("RequireExpectedCount(snapshot.Count, expectedCount);", final_rebound)
    if not (admission < enumerator < pre_move < move < pre_current < current < post_current < semantic_acceptance < final_rebound < cardinality):
        raise SystemExit("Duplicate detection " + method_name + " Count-stability ordering changed.")
    if "foreach (" in contract:
        raise SystemExit("Duplicate detection " + method_name + " must not use foreach traversal.")

required_smoke = (
    "CandidateGrowthRejectsBeforeSecondAdvance",
    "CandidateShrinkRejectsBeforeCurrent",
    "CandidateTerminalReboundRejects",
    "ElementProjectionPreservesKnownCountBoundary",
    "StableKnownCountPreservesDuplicateSemantics",
    "PureStreamingOverloadsRemainSupported",
    "Equal(0, source.CurrentReads",
    "Equal(9, source.CountReads",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Duplicate detection Count-stability smoke contract missing: " + repr(missing_smoke))

required_current_smoke = (
    "[ModuleInitializer]",
    "RejectElementCurrentInducedCountDriftBeforeNullAcceptance",
    "RejectCandidateCurrentInducedCountDriftBeforeNullAcceptance",
    "AcceptStableCountAfterElementCurrent",
    "AcceptStableCountAfterCandidateCurrent",
    "known element Count changed during snapshot",
    "must be rejected before null/identity acceptance.",
    "CurrentReads",
)
missing_current_smoke = [token for token in required_current_smoke if token not in current_smoke]
if missing_current_smoke:
    raise SystemExit("Duplicate detection post-Current Count smoke contract missing: " + repr(missing_current_smoke))

print("PASS duplicate detection Count-stability source guard")
