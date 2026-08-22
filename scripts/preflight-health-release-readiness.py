#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SUMMARY = ROOT / "src/QS3D.Core/Diagnostics/HealthSummary.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/HealthSummaryReadinessSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (SUMMARY, SMOKE, REG):
    if not path.is_file():
        errors.append("missing health readiness contract file: " + str(path.relative_to(ROOT)))

if SUMMARY.is_file():
    text = SUMMARY.read_text(encoding="utf-8")
    for token in (
        "public bool IsHealthy => Errors == 0;",
        "public bool IsReleaseReady => Errors == 0 && Warnings == 0;",
        "var normalized = issues.ToList();",
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

if REG.is_file() and "HealthSummaryReadinessSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Health summary readiness smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Core distinguishes health from release readiness, rejects null/undefined diagnostics fail-closed, and warnings block IsReleaseReady. This gate does not inspect V25 runtime/native files.")
