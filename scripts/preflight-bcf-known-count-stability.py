from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "BcfIssueExchange.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "BcfIssueExchangeKnownCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "bcf-known-count-stability.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(source, "out var knownCountSources", "initial deterministic Count-surface snapshot")
    require(source, "private static void RequireStableKnownCounts<T>(", "central Count-stability rebound contract")
    require(source, "out var currentKnownCountSources", "Count-surface rebinding")
    require(source, "expectedKnownCountSources != currentKnownCountSources", "Count-source stability comparison")
    require(source, "expectedCorroboratedKnownCount != currentCorroboratedKnownCount", "Count-corroboration stability comparison")
    require(source, "expectedKnownCount != currentKnownCount", "Count-value stability comparison")
    require(source, "BCF collection Count changed during enumeration.", "Count drift fail-closed diagnostic")
    require(source, "corroboratedKnownCount && knownCount.HasValue && observedCount >= knownCount.Value", "#4349 corroborated early-overrun precedence")
    require(smoke, "TopicCountDriftFailsAfterExactTraversal", "topic-level Count drift regression")
    require(smoke, "ViewpointCountDriftFailsAfterExactTraversal", "viewpoint-level Count drift regression")
    require(smoke, "CommentCountDriftFailsAfterExactTraversal", "comment-level Count drift regression")
    require(smoke, "ComponentCountDriftFailsAfterExactTraversal", "component-level Count drift regression")
    require(smoke, "PostTraversalNegativeCountFailsClosed", "post-traversal negative Count regression")
    require(smoke, "PostTraversalConflictingCountsFailClosed", "post-traversal Count conflict regression")
    require(smoke, "StableCountedAndStreamingInputsRemainAccepted", "stable counted and streaming controls")
    require(runbook, "post-traversal", "two-phase Count contract documentation")
    require(runbook, "#4349", "early-overrun compatibility boundary")

    print("PASS BCF known Count stability preflight")
    return 0


if __name__ == "__main__":
    sys.exit(main())
