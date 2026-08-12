#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Rules/QuantityRulePreviewService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityRulePreviewSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing quantity-rule preview apply contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Services;",
        'ProjectSemanticMutationExecutor.Execute(project, "quantity-rule-preview.apply-element"',
    ):
        if token not in source:
            errors.append("quantity-rule preview source missing mutation tracking contract: " + token)

    element_start = source.find("public int ApplyElement(ProjectState project, ProjectElement element, QuantityRuleElementPreview preview)")
    project_start = source.find("public int ApplyProject(ProjectState project, QuantityRuleProjectPreview preview)", element_start)
    if element_start < 0 or project_start <= element_start:
        errors.append("cannot isolate QuantityRulePreviewService.ApplyElement")
    else:
        body = source[element_start:project_start]
        equivalent = body.find("if (!Equivalent(preview, current))")
        noop = body.find("if (!preview.HasChanges) return 0;")
        execute = body.find('ProjectSemanticMutationExecutor.Execute(project, "quantity-rule-preview.apply-element"')
        apply = body.find("_engine.ApplyMatching(project, element)", execute)
        touch = body.find("if (applied > 0) project.Touch();", apply)
        if min(equivalent, noop, execute, apply, touch) < 0 or not (equivalent < noop < execute < apply < touch):
            errors.append("element apply must validate freshness, exit semantic no-op, then atomically apply and Touch")
        if body.count("project.Touch();") != 1:
            errors.append("changed element apply must own exactly one project Touch boundary")

    batch_start = source.find("private int ApplyFreshProjectPreview(ProjectState project, QuantityRuleProjectPreview preview)")
    detached_start = source.find("private QuantityRuleElementPreview PreviewDetached", batch_start)
    if batch_start < 0 or detached_start <= batch_start:
        errors.append("cannot isolate QuantityRulePreviewService.ApplyFreshProjectPreview")
    else:
        body = source[batch_start:detached_start]
        loop = body.find("foreach (var item in preview.Elements.Where(x => x.HasChanges)")
        apply = body.find("_engine.ApplyMatching(project, element)", loop)
        touch = body.find("if (applied > 0) project.Touch();", apply)
        ret = body.find("return applied;", touch)
        if min(loop, apply, touch, ret) < 0 or not (loop < apply < touch < ret):
            errors.append("project apply must finish reviewed element batch before one project Touch")
        if body.count("project.Touch();") != 1:
            errors.append("project apply batch must own exactly one project Touch boundary")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ChangedElementApplyAdvancesProjectRevisionOnce();",
        "NoChangeElementApplyIsSideEffectFree();",
        "Equal(beforeVersion + 1L, project.ChangeVersion);",
        "Equal(beforeUpdated, element.UpdatedUtc);",
        "Equal(0, applied);",
    ):
        if token not in smoke:
            errors.append("quantity-rule preview smoke missing revision/no-op assertion: " + token)

print("QS3D quantity-rule preview apply tracking preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: reviewed quantity-rule element/project applies own one rollback-safe project revision boundary and fresh no-change element applies remain side-effect free.")
