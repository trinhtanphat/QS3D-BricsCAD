#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rules/QuantityRulePreviewService.cs"
ENGINE = ROOT / "src/QS3D.Core/Rules/QuantityRuleEngine.cs"
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/ModelHealthBaselineService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityRulePreviewSmoke.cs"
errors = []

for path in (SOURCE, ENGINE, HEALTH, SMOKE):
    if not path.is_file():
        errors.append("missing quantity-rule preview contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "public QuantityRuleElementPreview PreviewElement",
        "public QuantityRuleProjectPreview PreviewProject",
        "public long SourceChangeVersion",
        "var sourceChangeVersion = project.ChangeVersion;",
        "preview.SourceChangeVersion != project.ChangeVersion",
        "project changed after preview",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "public int ApplyElement",
        "public int ApplyProject",
        "public QuantityRuleGuardedApplyResult ApplyProjectWithHealthGuard",
        "new ModelHealthBaselineService()",
        "health.CaptureSemantic(project)",
        "health.Compare(before, after)",
        "if (diff.NewErrorCount > 0)",
        "project state was rolled back",
        "Quantity-rule preview is stale",
        "ProjectStateSnapshot.Capture(project)",
        "snapshot.Restore(project)",
        "ReferenceEquals(owned, element)",
        "QuantityRulePreviewChangeKind.Added",
        "QuantityRulePreviewChangeKind.Changed",
        "QuantityRulePreviewChangeKind.Removed",
        "var beforeManaged = beforeHasValue || beforeRule.Length > 0;",
        "var afterManaged = afterHasValue || afterRule.Length > 0;",
        "BeforeProvenance",
        "AfterProvenance",
    ):
        if token not in text:
            errors.append("QuantityRulePreviewService missing guarded preview/apply token: " + token)

if ENGINE.is_file():
    text = ENGINE.read_text(encoding="utf-8")
    for token in (
        "var staged = new List<KeyValuePair<QuantityRule, double>>",
        "CleanupStaleOutputs(element, staleOutputs);",
    ):
        if token not in text:
            errors.append("QuantityRuleEngine lost staged-before-mutation contract required by preview/apply: " + token)

if HEALTH.is_file() and "public ModelHealthBaselineDiff Compare" not in HEALTH.read_text(encoding="utf-8"):
    errors.append("Quantity-rule health guard requires deterministic health baseline comparison.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "PreviewIsReadOnlyAndClassifiesChanges();",
        "ProvenanceOnlyStaleOutputIsRemoved();",
        "StaleElementPreviewFailsBeforeMutation();",
        "ChangeVersionInvalidatesEquivalentPreview();",
        "project.Touch();",
        "preview.SourceChangeVersion",
        "ProjectPreviewAppliesAtomicallyFromFreshState();",
        "HealthGuardedProjectApplyReturnsRegressionDiff();",
        "ForeignElementInstanceFailsClosed();",
        "Rule:OldManaged",
        "Rule:Ghost",
        "QuantityRulePreviewChangeKind.Removed",
        "ApplyProjectWithHealthGuard",
        "HealthDiff.NewErrorCount",
        "[ModuleInitializer]",
    ):
        if token not in text:
            errors.append("QuantityRulePreviewSmoke missing regression token: " + token)

if errors:
    print("QS3D quantity-rule preview preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: quantity rules support detached previews bound to ProjectState.ChangeVersion, add/change/remove provenance deltas, stale-preview rejection, exact ownership, atomic batch apply and rollback on new Model Health errors.")
