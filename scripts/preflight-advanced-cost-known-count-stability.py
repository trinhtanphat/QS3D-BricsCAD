from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADVANCED = ROOT / "src" / "QS3D.Core" / "Cost" / "AdvancedCostManagement.cs"
DEEP = ROOT / "src" / "QS3D.Core" / "Cost" / "DeepCostWorkflows.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AdvancedCostKnownCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "advanced-cost-known-count-stability.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def main() -> int:
    advanced = ADVANCED.read_text(encoding="utf-8")
    deep = DEEP.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

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
    require(runbook, "post-traversal", "runbook two-phase Count contract")

    print("PASS advanced cost known Count stability preflight")
    return 0


if __name__ == "__main__":
    sys.exit(main())
