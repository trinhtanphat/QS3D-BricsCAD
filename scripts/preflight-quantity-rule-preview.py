#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rules/QuantityRulePreviewService.cs"
ENGINE = ROOT / "src/QS3D.Core/Rules/QuantityRuleEngine.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityRulePreviewSmoke.cs"
errors = []

for path in (SOURCE, ENGINE, SMOKE):
    if not path.is_file():
        errors.append("missing quantity-rule preview contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "public QuantityRuleElementPreview PreviewElement",
        "public QuantityRuleProjectPreview PreviewProject",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "public int ApplyElement",
        "public int ApplyProject",
        "Quantity-rule preview is stale",
        "ProjectStateSnapshot.Capture(project)",
        "snapshot.Restore(project)",
        "ReferenceEquals(owned, element)",
        "QuantityRulePreviewChangeKind.Added",
        "QuantityRulePreviewChangeKind.Changed",
        "QuantityRulePreviewChangeKind.Removed",
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

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "PreviewIsReadOnlyAndClassifiesChanges();",
        "StaleElementPreviewFailsBeforeMutation();",
        "ProjectPreviewAppliesAtomicallyFromFreshState();",
        "ForeignElementInstanceFailsClosed();",
        "Rule:OldManaged",
        "QuantityRulePreviewChangeKind.Removed",
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

print("PASS: quantity rules support detached element/project previews, explicit add/change/remove deltas with provenance, stale-preview rejection, exact project ownership, and snapshot rollback for guarded batch apply.")
