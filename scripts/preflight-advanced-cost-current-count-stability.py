#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/AdvancedCostManagement.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AdvancedCostCurrentCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/advanced-cost-current-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Advanced-cost Current Count stability preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

boundaries = (
    ("var component = componentEnumerator.Current;", "components", "hasKnownComponentCount", "knownComponentCount", "Rate build-up component collection"),
    ("var record = recordEnumerator.Current;", "records", "hasKnownRecordCount", "knownRecordCount", "Historical cost catalog"),
    ("var line = lineEnumerator.Current;", "lines", "hasKnownLineCount", "knownLineCount", "Tender quote line collection"),
    ("var requirement = requirementEnumerator.Current;", "requirements", "hasKnownRequirementCount", "knownRequirementCount", "Tender requirement collection"),
    ("var bid = bidEnumerator.Current;", "bids", "hasKnownBidCount", "knownBidCount", "Tender bid collection"),
    ("var item = contractEnumerator.Current;", "contractItems", "hasKnownContractCount", "knownContractCount", "Progress contract item collection"),
    ("var line = claimEnumerator.Current;", "claimLines", "hasKnownClaimCount", "knownClaimCount", "Progress claim line collection"),
)

for current, collection, has_count, known_count, label in boundaries:
    start = source.find(current)
    if start < 0:
        raise SystemExit("Advanced-cost Current Count source boundary missing: " + current)
    tail = source[start + len(current):]
    required = (
        "AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal(\n"
        f"                        {collection},\n"
        f"                        {has_count},\n"
        f"                        {known_count},\n"
        f"                        \"{label}\");"
    )
    if not tail.lstrip().startswith(required):
        raise SystemExit("Advanced-cost Current Count rebound is not immediate after: " + current)

for token in (
    "RateBuildUpRejectsCurrentInducedDriftBeforeNullValidation();",
    "HistoricalCatalogRejectsCurrentInducedDriftBeforeNullValidation();",
    "TenderQuoteLinesRejectCurrentInducedDriftBeforeNullValidation();",
    "TenderRequirementsRejectCurrentInducedDriftBeforeNullValidation();",
    "TenderBidsRejectCurrentInducedDriftBeforeNullValidation();",
    "ProgressContractsRejectCurrentInducedDriftBeforeNullValidation();",
    "ProgressClaimsRejectCurrentInducedDriftBeforeNullValidation();",
    "StableCountedControlsRemainAccepted();",
    "StreamingControlsRemainAccepted();",
    "known count changed during traversal",
    "CurrentReads == 1",
):
    if token not in smoke:
        raise SystemExit("Advanced-cost Current Count smoke missing contract: " + token)

for phrase in (
    "Lane-Key: `issue-4966`",
    "Current-induced Count drift",
    "immediately after `Current`",
    "before null, duplicate, reference, or retention semantics",
    "stable counted and streaming controls",
    "No licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Advanced-cost Current Count runbook missing boundary: " + phrase)

print("PASS advanced cost Current-induced Count stability contract")
