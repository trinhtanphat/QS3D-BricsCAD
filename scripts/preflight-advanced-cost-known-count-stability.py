from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
ADVANCED = ROOT / "src" / "QS3D.Core" / "Cost" / "AdvancedCostManagement.cs"
DEEP = ROOT / "src" / "QS3D.Core" / "Cost" / "DeepCostWorkflows.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AdvancedCostKnownCountStabilitySmoke.cs"
TRANSIENT_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AdvancedCostTransientCountSmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "advanced-cost-known-count-stability.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def main() -> int:
    advanced = ADVANCED.read_text(encoding="utf-8")
    deep = DEEP.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    transient_smoke = TRANSIENT_SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    # Preserve the historical final-state contract.
    require(advanced, "RequireKnownCountStableAfterTraversal", "shared post-traversal Count rebinding helper")
    require(advanced, "TryGetKnownCount(items, out var currentKnownCount)", "post-traversal deterministic Count reread")
    require(advanced, "currentKnownCount != initialKnownCount", "Count stability comparison")

    advanced_sources = (
        "components,\n                hasKnownComponentCount",
        "records,\n                hasKnownRecordCount",
        "lines,\n                hasKnownLineCount",
        "requirements,\n                hasKnownRequirementCount",
        "bids,\n                hasKnownBidCount",
        "contractItems,\n                hasKnownContractCount",
        "claimLines,\n                hasKnownClaimCount",
    )
    for token in advanced_sources:
        require(advanced, token, "AdvancedCostManagement consumer rebinding")

    deep_sources = (
        "rates,\n                hasKnownRateCount",
        "items,\n                hasKnownItemCount",
        "entries,\n                hasKnownEntryCount",
        "projectEntries,\n                hasKnownProjectEntryCount",
    )
    for token in deep_sources:
        require(deep, token, "DeepCostWorkflows consumer rebinding")

    require(smoke, "DriftingReadOnlyCollection", "single-interface Count drift regression")
    require(smoke, "DriftingMultiCountCollection", "multi-interface Count drift regression")
    require(smoke, "PureStreamingInputRemainsAccepted", "streaming control")

    # Caller-controlled AdvancedCost materializers must rebind admitted Count
    # immediately before MoveNext and after a successful move, before Current.
    require(
        advanced,
        "RequireKnownCountStableDuringTraversal<T>(",
        "shared traversal-wide Count rebinding helper")

    stale_loops = re.findall(
        r"while\s*\(\s*[A-Za-z_][A-Za-z0-9_]*Enumerator\.MoveNext\(\)\s*\)",
        advanced)
    if stale_loops:
        raise SystemExit(
            "caller-controlled AdvancedCost loops still cross MoveNext without pre/post Count rebound: "
            + ", ".join(sorted(set(stale_loops))))

    required_materializers = (
        ("componentEnumerator", "components"),
        ("recordEnumerator", "records"),
        ("lineEnumerator", "lines"),
        ("requirementEnumerator", "requirements"),
        ("bidEnumerator", "bids"),
        ("contractEnumerator", "contractItems"),
        ("claimEnumerator", "claimLines"),
    )
    for enumerator, source_name in required_materializers:
        using_token = f"using (var {enumerator} = {source_name}.GetEnumerator())"
        start = advanced.find(using_token)
        if start < 0:
            raise SystemExit(f"missing AdvancedCost enumerator: {using_token}")
        segment = advanced[start:start + 2200]
        helper = "AdvancedCostCollectionContract.RequireKnownCountStableDuringTraversal("
        first_rebind = segment.find(helper)
        move = segment.find(f"if (!{enumerator}.MoveNext())")
        second_rebind = segment.find(helper, move + 1) if move >= 0 else -1
        current = segment.find(f"{enumerator}.Current")
        positions = (first_rebind, move, second_rebind, current)
        if any(position < 0 for position in positions) or positions != tuple(sorted(positions)):
            raise SystemExit(
                f"{enumerator} must order Count rebound -> MoveNext -> Count rebound -> Current")

    # Known-count snapshots must also prove semantic generation stability. Count-only
    # fencing cannot detect a source that swaps values while preserving cardinality.
    require(
        advanced,
        "RequireStableKnownGeneration<T>(",
        "shared semantic generation replay helper")
    for token in (
        "SameHistoricalRecordState",
        "SameTenderQuoteLineState",
        "SameTenderRequirementState",
        "SameTenderBidState",
        "SameProgressContractState",
        "SameProgressClaimState",
    ):
        require(advanced, token, "advanced-cost semantic state comparator")

    for source_name in (
        "records",
        "lines",
        "requirements",
        "bids",
        "contractItems",
        "claimLines",
    ):
        require(
            advanced,
            "AdvancedCostCollectionContract.RequireStableKnownGeneration(\n                " + source_name + ",",
            "advanced-cost semantic replay consumer")

    for token in (
        "HistoricalCatalogRejectsStableCountGenerationDrift",
        "TenderQuoteLinesRejectStableCountGenerationDrift",
        "TenderRequirementsRejectStableCountGenerationDrift",
        "TenderBidsRejectStableCountGenerationDrift",
        "ProgressContractsRejectStableCountGenerationDrift",
        "ProgressClaimsRejectStableCountGenerationDrift",
        "GenerationSwitchCollection<T>",
        "AffectedKnownCountControlsRemainAccepted",
        "AffectedStreamingInputRemainsSinglePass",
        "EnumerationCount",
        "semantic generation replay",
    ):
        require(smoke, token, "semantic generation replay regression")

    for token in (
        "[ModuleInitializer]",
        "RateBuildUpRejectsTransientCountBeforeCurrent",
        "HistoricalCatalogRejectsTransientCountBeforeCurrent",
        "TenderBidRejectsTransientCountBeforeCurrent",
        "TransientCountCollection<T>",
        "CurrentReads == 0",
        "StableCountedAndStreamingControlsSucceed",
    ):
        require(transient_smoke, token, "transient Count regression")

    require(runbook, "post-traversal", "runbook final-state Count contract")
    require(runbook, "semantic generation", "runbook semantic generation contract")
    require(runbook, "single-pass", "runbook streaming single-pass contract")

    print("PASS advanced cost traversal-wide known Count and semantic generation stability preflight")
    return 0


if __name__ == "__main__":
    sys.exit(main())
