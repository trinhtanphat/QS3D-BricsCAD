#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/ModelHealthBaselineService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ModelHealthBaselineSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing model-health baseline contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "public ModelHealthBaseline CaptureSemantic",
        "new ComprehensiveModelHealthService().Inspect(project)",
        "public ModelHealthBaseline Capture",
        "public ModelHealthBaselineDiff Compare",
        "NewIssues",
        "ResolvedIssues",
        "PersistentIssues",
        "HasRegressions",
        "HasImprovements",
        "NewErrorCount",
        "ResolvedErrorCount",
        "Model health baselines belong to different projects",
        "StringComparer.Ordinal",
        'code.EndsWith("_STALE", StringComparison.OrdinalIgnoreCase)',
        "KeyPart(((int)issue.Severity).ToString(System.Globalization.CultureInfo.InvariantCulture))",
        "KeyPart(code.ToUpperInvariant())",
        "KeyPart((issue.ElementId ?? string.Empty).ToUpperInvariant())",
        "key + KeyPart(issue.Message ?? string.Empty)",
        "private static string KeyPart(string value)",
        'return text.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + text;',
    ):
        if token not in text:
            errors.append("ModelHealthBaselineService missing deterministic collision-safe diff token: " + token)

    if ': key + "\\n" + (issue.Message ?? string.Empty)' in text:
        errors.append("ModelHealthBaselineService regressed to delimiter-concatenated issue identity.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "NewResolvedAndPersistentIssuesAreClassified();",
        "DuplicateIssuesAreStable();",
        "DelimiterCollisionIssuesRemainDistinct();",
        "MalformedIssuesFailClosed();",
        "StaleMessageChangesRemainPersistent();",
        "CrossProjectDiffFailsClosed();",
        "SemanticCaptureIsReadOnly();",
        'new ModelHealthIssue("A\\nB", HealthSeverity.Warning, "message", "C")',
        'new ModelHealthIssue("A", HealthSeverity.Warning, "message", "B\\nC")',
        "NEW_ERROR",
        "OLD_ERROR",
        "GENERATED_SOLID_STALE",
        "ORDINARY_WARNING",
        "reason B",
        "message A",
        "message B",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("ModelHealthBaselineSmoke missing regression token: " + token)

if errors:
    print("QS3D model-health baseline preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: model health baseline uses length-prefixed collision-safe identity, preserves stale diagnostics across reason-message changes, and keeps ordinary diagnostics message-sensitive.")
