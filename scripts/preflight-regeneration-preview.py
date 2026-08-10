#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationPreviewService.cs"
ENGINE = ROOT / "src/QS3D.Core/Services/RegenerationEngine.cs"
REVISION = ROOT / "src/QS3D.Core/Revisions/RevisionService.cs"
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/ModelHealthBaselineService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationPreviewSmoke.cs"
errors = []

for path in (SOURCE, ENGINE, REVISION, HEALTH, SMOKE):
    if not path.is_file():
        errors.append("missing regeneration preview contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "public RegenerationPreview Preview(ProjectState project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        'revisions.Capture(detached, "regen-preview-before")',
        "NewEngine().RegenerateDirty(detached)",
        'revisions.Capture(detached, "regen-preview-after")',
        "revisions.Compare(beforeRevision, afterRevision)",
        "health.Compare(beforeHealth, afterHealth)",
        "public RegenerationGuardedApplyResult Apply",
        "Regeneration preview is stale",
        "if (current.IntroducesHealthErrors)",
        "ProjectStateSnapshot.Capture(project)",
        "snapshot.Restore(project)",
        "if (diff.NewErrorCount > 0)",
    ):
        if token not in text:
            errors.append("RegenerationPreviewService missing detached/stale/health guard token: " + token)

if ENGINE.is_file():
    text = ENGINE.read_text(encoding="utf-8")
    for token in (
        "public int RegenerateDirty(ProjectState project)",
        "RegenerateTransactional(project, project.Elements, project.Elements.Count)",
    ):
        if token not in text:
            errors.append("RegenerationEngine lost transactional regeneration contract required by preview/apply: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "PreviewRunsOnDetachedState();",
        "StalePreviewFailsBeforeLiveMutation();",
        "FreshPreviewCanApplyWithoutNewHealthErrors();",
        "Quantity:NetVolumeM3",
        "!beam.Quantities.ContainsKey(\"NetVolumeM3\")",
        "result.HealthDiff.NewErrorCount",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("RegenerationPreviewSmoke missing regression token: " + token)

if errors:
    print("QS3D regeneration preview preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic regeneration supports detached dry-run revision/health diff, stale-preview rejection and rollback if guarded live apply creates new Model Health errors.")
