#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
command = ROOT / "src/QS3D.BricsCAD.V25/SourceEditCommands.cs"
reconcile = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
doc = ROOT / "docs/SOURCE-EDIT-MOVE-ROTATE.md"
errors = []

checks = {
    command: [
        'CommandMethod("QS3DEDITSOURCE", CommandFlags.UsePickSet)',
        'options.Keywords.Add("Move")',
        'options.Keywords.Add("Rotate")',
        'GeneratedHandleOwnershipIndex.Build(project)',
        'SemanticHandleOwnershipResolver.Resolve(project, handles)',
        'Select the authoritative source CAD instead.',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'project.ChangeVersion != expected.ProjectChangeVersion',
        'entity.TransformBy(transform)',
        'SourceReconcileService.ReconcileSelection(document)',
        'ApplyTransform(document, selection, transform.Value.Inverse)',
        'reconcile failed; the authoritative CAD transform was reversed',
        'document.Editor.SetImpliedSelection(selection.ObjectIds)',
        'Matrix3d.Displacement(displacement)',
        'Matrix3d.Rotation(angleResult.Value, rotationAxis, rotationBaseWcs)',
        'Generated dependents stale đã được invalidate/remove theo ownership',
    ],
    reconcile: [
        'GeneratedDependentGeometryInvalidator.Prepare(document, transaction, project, invalidationTargets)',
        'RefreshSourceDerivedState(project, target.Element, target.Snapshot, units)',
        'RegenerateAffectedToStable(project, invalidationTargets)',
        'rollback.Restore(project)',
    ],
    doc: [
        '`QS3DEDITSOURCE`',
        'MOVE',
        'ROTATE',
        '`QS3DSYNCSOURCE`',
        'LOCAL_ONLY',
        'STRETCH',
        'không',
    ],
}

for path, needles in checks.items():
    if not path.is_file():
        errors.append("missing source-edit dependency: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing source-edit contract: " + needle)

if command.is_file():
    text = command.read_text(encoding="utf-8")
    validate = text.find("ValidateAuthoritativeOwnership(project, handles)")
    prompt = text.find("var operation = PromptOperation(document)")
    freshness = text.find("RequireFreshSelection(document, selection)")
    mutate = text.find("ApplyTransform(document, selection, transform.Value.Forward)")
    reconcile_call = text.find("SourceReconcileService.ReconcileSelection(document)")
    reverse = text.find("ApplyTransform(document, selection, transform.Value.Inverse)")
    if min(validate, prompt, freshness, mutate, reconcile_call, reverse) < 0 or not (
        validate < prompt < freshness < mutate < reconcile_call < reverse
    ):
        errors.append("QS3DEDITSOURCE must validate ownership -> prompt -> revalidate freshness -> mutate -> reconcile -> reverse on reconcile failure")

    generated = text.find("GeneratedHandleOwnershipIndex.Build(project)")
    generated_reject = text.find("generatedOwners.TryFindOwner(handle", generated)
    source_resolve = text.find("SemanticHandleOwnershipResolver.Resolve(project, handles)", generated_reject)
    if min(generated, generated_reject, source_resolve) < 0 or not generated < generated_reject < source_resolve:
        errors.append("QS3DEDITSOURCE must reject generated ownership before canonical source ownership resolution")

    if "Matrix3d.Scaling" in text or 'Keywords.Add("Stretch")' in text or 'CommandMethod("QS3DSTRETCH' in text:
        errors.append("MOVE/ROTATE slice must not fake STRETCH with scale or expose an unimplemented STRETCH command")
    if "GetOrCreate(document)" in text:
        errors.append("QS3DEDITSOURCE must not bootstrap project state")
    if "SourceReconcileService.ReconcileSelection(document)" not in text:
        errors.append("QS3DEDITSOURCE must reuse the canonical source reconcile path")

if errors:
    print("Source edit preflight FAILED:")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: guarded QS3DEDITSOURCE MOVE/ROTATE source edit contract")
