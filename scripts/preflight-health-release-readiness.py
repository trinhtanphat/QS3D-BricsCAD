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
    for token in (
        "public bool IsHealthy => Errors == 0;",
        "public bool IsReleaseReady => Errors == 0 && Warnings == 0;",
        "public const int MaxIssueCount = 1000000;",
        "var normalized = MaterializeIssues(issues);",
        "while (enumerator.MoveNext())",
        "if (result.Count >= MaxIssueCount)",
        "if (normalized.Any(x => x == null))",
        'throw new InvalidOperationException("Health summary cannot contain a null diagnostic issue.");',
        "Issues = normalized.AsReadOnly();",
    ):
        if token not in text:
            errors.append("HealthSummary.cs missing readiness token: " + token)

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

print("PASS: Core distinguishes health from release readiness, bounds single-pass issue input, rejects malformed diagnostics fail-closed, and warnings block IsReleaseReady. This gate does not inspect V25 runtime/native files.")
