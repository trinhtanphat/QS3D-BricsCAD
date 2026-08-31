#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/DeepCostWorkflows.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DeepCostCurrentCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/deep-cost-current-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Deep Cost Current-count preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

contracts = (
    ("var edge = edgeEnumerator.Current;", "RequireKnownCountStableDuringTraversal(edges, knownCount.Value);", "if (edge == null)"),
    ("var rate = rateEnumerator.Current;", "AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(", "if (rate == null)"),
    ("var item = itemEnumerator.Current;", "AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(", "if (item == null)"),
    ("var entry = entryEnumerator.Current;", "AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(", "if (entry == null)"),
    ("var entry = projectEntryEnumerator.Current;", "AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(", "if (entry == null)"),
)

for current_token, rebound_token, acceptance_token in contracts:
    current = source.find(current_token)
    if current < 0:
        raise SystemExit("Deep Cost Current-count preflight missing Current boundary: " + current_token)
    acceptance = source.find(acceptance_token, current)
    if acceptance < 0:
        raise SystemExit("Deep Cost Current-count preflight missing acceptance boundary: " + acceptance_token)
    window = source[current + len(current_token):acceptance]
    if rebound_token not in window:
        raise SystemExit("Deep Cost traversal must rebind admitted Count immediately after Current before acceptance: " + current_token)

for token in (
    "RateReferenceRejectsCurrentInducedDriftBeforeNullAcceptance();",
    "BqLibraryRejectsCurrentInducedDriftBeforeNullAcceptance();",
    "known count changed during traversal",
    "CurrentReads == 1",
):
    if token not in smoke:
        raise SystemExit("Deep Cost Current-count smoke missing contract: " + token)

for phrase in (
    "after Current",
    "before null, identity, grouping, reference, snapshot, or import acceptance",
    "five caller-controlled traversals",
    "pure streaming",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Deep Cost Current-count runbook missing boundary: " + phrase)

print("PASS Deep Cost Current-induced Count stability")
