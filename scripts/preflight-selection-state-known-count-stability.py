from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "SelectionState.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SelectionStateKnownCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "selection-state-known-count-stability.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(source, "knownCount.HasValue && inputCount >= knownCount.Value", "known-Count early-overrun boundary")
    require(source, "var finalKnownCount = ResolveKnownCount(ids);", "post-traversal Count reread")
    require(source, "known Count changed during traversal", "Count-stability failure")
    require(source, "_changeVersion != enumerationVersion", "reentrant selection freshness guard")
    require(source, "inputCount >= MaxInputCount", "independent streaming cap")

    require(smoke, "KnownCountOverrunFailsBeforeCurrentAndThrowingTail", "overrun/no-Current-overread regression")
    require(smoke, "MoveNextCalls", "overrun MoveNext observation")
    require(smoke, "CurrentReads", "overrun Current observation")
    require(smoke, "Equal(2, source.MoveNextCalls);", "overrun MoveNext boundary assertion")
    require(smoke, "Equal(1, source.CurrentReads);", "overrun Current no-overread assertion")
    require(smoke, "GenericCountDriftFailsWithoutPublication", "generic Count drift regression")
    require(smoke, "ReadOnlyCountDriftFailsWithoutPublication", "read-only Count drift regression")
    require(smoke, "NonGenericCountDriftFailsWithoutPublication", "non-generic Count drift regression")
    require(smoke, "KnownCountUnderYieldStillFailsWithoutPublication", "under-yield regression")
    require(smoke, "StableMultiInterfaceCountAndStreamingInputsRemainSupported", "stable counted/streaming controls")

    require(runbook, "post-traversal", "runbook stability contract")
    require(runbook, "10,000", "runbook streaming bound")

    print("PASS SelectionState known Count stability preflight")
    return 0


if __name__ == "__main__":
    sys.exit(main())
