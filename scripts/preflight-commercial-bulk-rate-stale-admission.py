from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Commercial" / "EstimatingWorkflow.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "BulkRateAssignmentStaleAdmissionSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SmokeTestRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

for anchor in [
    "if (line.IsBlocked || line.IsStale)",
    "blocked.Add(line.LineId);",
    "if (!preview.CanCommit)",
    "if (!SourceLinesMatch(preview.SourceLines, portfolio, request))",
    "expected.IsStale != current.IsStale",
    "!string.Equals(expected.StaleReason, current.StaleReason, StringComparison.Ordinal)",
]:
    if anchor not in source:
        raise SystemExit(f"commercial bulk-rate stale admission preflight failed: missing production anchor {anchor!r}")

for anchor in [
    "AlreadyStaleLineIsBlockedAtPreview();",
    "StaleAfterPreviewIsRejectedWithoutMutation();",
    "ActiveLineStillCommits();",
    "Require(!preview.CanCommit",
    "service.MarkQuantitySourceStale(",
    "audit.Events.Count == 0",
    "line.ReferencedRate == 5m && line.Amount == 20m",
]:
    if anchor not in smoke:
        raise SystemExit(f"commercial bulk-rate stale admission preflight failed: missing smoke anchor {anchor!r}")

if "BulkRateAssignmentStaleAdmissionSmoke.Run();" not in registration:
    raise SystemExit("commercial bulk-rate stale admission preflight failed: smoke is not registered")

print("PASS commercial bulk-rate stale admission source guard")
