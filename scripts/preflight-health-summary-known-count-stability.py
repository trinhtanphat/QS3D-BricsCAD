from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Diagnostics" / "HealthSummary.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "HealthSummaryKnownCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "health-summary-known-count-stability.md"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"missing {label}: {token}")


def main() -> int:
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    require(source, "expectedKnownCount.HasValue && result.Count >= expectedKnownCount.Value", "early known-Count overrun boundary")
    require(source, "RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);", "repeated Count stability reread")
    require(source, "expectedKnownCountSources != currentKnownCountSources || expectedKnownCount != currentKnownCount", "Count value/source drift rejection")
    require(source, "known issue count changed during traversal", "Count drift rejection")
    require(source, "result.Count >= MaxIssueCount", "independent streaming ceiling")
    require(source, "result.Count != expectedKnownCount.Value", "under-yield rejection")
    require(source, "if (!enumerator.MoveNext())", "explicit MoveNext boundary")
    require(source, "result.Add(enumerator.Current);", "Current observation boundary")

    pre_move = source.index("RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);")
    move = source.index("if (!enumerator.MoveNext())", pre_move)
    post_move = source.index("RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);", move + 1)
    overrun = source.index("if (expectedKnownCount.HasValue && result.Count >= expectedKnownCount.Value)", post_move)
    current = source.index("result.Add(enumerator.Current);", overrun)
    if not (pre_move < move < post_move < overrun < current):
        raise SystemExit("HealthSummary Count stability ordering must be rebind -> MoveNext -> rebind -> overrun admission -> Current")
    if "while (enumerator.MoveNext())" in source:
        raise SystemExit("HealthSummary must not regress to caller-controlled while(MoveNext) traversal")

    require(smoke, "KnownCountOverrunFailsBeforeThrowingTail", "overrun/no-overread regression")
    require(smoke, "GenericCountDriftFailsClosed", "generic Count drift regression")
    require(smoke, "ReadOnlyCountDriftFailsClosed", "read-only Count drift regression")
    require(smoke, "NonGenericCountDriftFailsClosed", "non-generic Count drift regression")
    require(smoke, "PostTraversalNegativeCountFailsClosed", "post-traversal negative regression")
    require(smoke, "KnownCountUnderYieldStillFailsClosed", "under-yield regression")
    require(smoke, "StableCountedAndStreamingInputsRemainSupported", "stable controls")
    require(smoke, "[ModuleInitializer]", "automatic smoke registration")

    require(runbook, "1,000,000", "runbook streaming ceiling")
    require(runbook, "post-traversal", "runbook Count stability contract")

    print("PASS HealthSummary known Count stability preflight")
    return 0


if __name__ == "__main__":
    sys.exit(main())
