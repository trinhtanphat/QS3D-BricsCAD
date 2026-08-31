#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SUMMARY = ROOT / "src/QS3D.Core/Diagnostics/HealthSummary.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HealthSummaryReadinessSmoke.cs"
BOUNDED_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HealthSummaryBoundedInputSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (SUMMARY, SMOKE, BOUNDED_SMOKE, REG):
    if not path.is_file():
        errors.append("missing health readiness contract file: " + str(path.relative_to(ROOT)))

if SUMMARY.is_file():
    text = SUMMARY.read_text(encoding="utf-8")
    rebound = "RequireKnownCountStable(issues, expectedKnownCount, expectedKnownCountSources);"
    current_capture = "var issue = enumerator.Current;"
    retain = "result.Add(issue);"
    for token in (
        "public bool IsHealthy => Errors == 0;",
        "public bool IsReleaseReady => Errors == 0 && Warnings == 0;",
        "public const int MaxIssueCount = 1000000;",
        "var normalized = MaterializeIssues(issues);",
        "var expectedKnownCount = RequireKnownCountsWithinLimit(issues, out var expectedKnownCountSources);",
        "private static int? RequireKnownCountsWithinLimit",
        "private static void RequireKnownCountStable",
        "issues is ICollection<ModelHealthIssue> collection",
        "issues is IReadOnlyCollection<ModelHealthIssue> readOnlyCollection",
        "issues is System.Collections.ICollection nonGenericCollection",
        "while (true)",
        "if (!enumerator.MoveNext())",
        "if (result.Count >= MaxIssueCount)",
        "if (expectedKnownCount.HasValue && result.Count != expectedKnownCount.Value)",
        'throw new InvalidOperationException("Health summary known issue count does not match enumerated issue count.");',
        'throw new InvalidOperationException("Health summary received an invalid negative known issue count.");',
        'throw new InvalidOperationException("Health summary received conflicting known issue counts.");',
        current_capture,
        rebound,
        retain,
        "if (normalized.Any(x => x == null))",
        'throw new InvalidOperationException("Health summary cannot contain a null diagnostic issue.");',
        "Issues = normalized.AsReadOnly();",
    ):
        if token not in text:
            errors.append("HealthSummary.cs missing readiness token: " + token)

    pre_move = text.find(rebound)
    move = text.find("if (!enumerator.MoveNext())", pre_move)
    post_move = text.find(rebound, move + 1)
    overrun = text.find("if (expectedKnownCount.HasValue && result.Count >= expectedKnownCount.Value)", post_move)
    current = text.find(current_capture, overrun)
    post_current = text.find(rebound, current + len(current_capture))
    retention = text.find(retain, post_current + len(rebound))
    if min(pre_move, move, post_move, overrun, current, post_current, retention) < 0 or not (
        pre_move < move < post_move < overrun < current < post_current < retention
    ):
        errors.append(
            "HealthSummary must rebind Count around MoveNext, reject known-Count overrun before IEnumerator.Current, "
            "then rebind Count after Current before retention"
        )
    if "result.Add(enumerator.Current);" in text:
        errors.append("HealthSummary must not retain caller-controlled Current before its post-Current Count rebound")
    if "while (enumerator.MoveNext())" in text:
        errors.append("HealthSummary must not regress to caller-controlled while(MoveNext) traversal")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "WarningIsHealthyButNotReleaseReady",
        "ErrorBlocksHealthAndRelease",
        "InfoOnlyIsReleaseReady",
        "NullIssueEntriesFailClosed",
        "UndefinedSeverityFailsAtIssueBoundary",
    ):
        if token not in text:
            errors.append("HealthSummaryReadinessSmoke.cs missing regression scenario: " + token)

if BOUNDED_SMOKE.is_file():
    text = BOUNDED_SMOKE.read_text(encoding="utf-8")
    for token in (
        "ExactCapIsAcceptedInOnePass",
        "FirstIssueBeyondCapIsRejectedInOnePass",
        "ThrowingInputPropagatesWithoutAResult",
        "OversizedKnownCountIsRejectedBeforeEnumeration",
        "NegativeKnownCountIsRejectedBeforeEnumeration",
        "ConflictingKnownCountsAreRejectedBeforeEnumeration",
        "KnownCountUnderEnumerationIsRejected",
        "KnownCountOverEnumerationIsRejected",
        "HonestKnownCountIsAccepted",
        "StreamingInputWithoutKnownCountRemainsAccepted",
        "AdversarialKnownCountCollection",
        "KnownCountTraversalCollection",
        "HealthSummary.MaxIssueCount + 1",
        "source.EnumerationCount",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("HealthSummaryBoundedInputSmoke.cs missing bounded-input scenario: " + token)

if REG.is_file() and "HealthSummaryReadinessSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Health summary readiness smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Core distinguishes health from release readiness, rejects invalid/unstable known issue-count contracts around MoveNext and Current before retention, rejects Count/traversal mismatches after bounded single-pass traversal, rejects malformed diagnostics fail-closed, and warnings block IsReleaseReady. This gate does not inspect V25 runtime/native files.")
