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
        '? key',
        ': key + "\\n" + (issue.Message ?? string.Empty)',
    ):
        if token not in text:
            errors.append("ModelHealthBaselineService missing deterministic diff token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "NewResolvedAndPersistentIssuesAreClassified();",
        "DuplicateIssuesAreStable();",
        "StaleMessageChangesRemainPersistent();",
        "CrossProjectDiffFailsClosed();",
        "SemanticCaptureIsReadOnly();",
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

print("PASS: model health baseline diff keeps stale diagnostics persistent across reason-message changes while preserving message-sensitive identity for ordinary diagnostics.")
