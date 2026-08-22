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
        'does not silently rebuild destructive downstream native output',
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

    # EditSource executes ownership validation through CaptureAuthoritativeSelection.
    # Do not compare the textual location of the validation statement inside its helper
    # with the caller's PromptOperation line: helper definitions naturally appear later
    # in the file even though the helper executes first.
    capture = text.find("var selection = CaptureAuthoritativeSelection(document)")
    prompt = text.find("var operation = PromptOperation(document)", capture)
    freshness = text.find("RequireFreshSelection(document, selection)", prompt)
    mutate = text.find("ApplyTransform(document, selection, transform.Value.Forward)", freshness)
    reconcile_call = text.find("SourceReconcileService.ReconcileSelection(document)", mutate)
    reverse = text.find("ApplyTransform(document, selection, transform.Value.Inverse)", reconcile_call)
    if min(capture, prompt, freshness, mutate, reconcile_call, reverse) < 0 or not (
        capture < prompt < freshness < mutate < reconcile_call < reverse
    ):
        errors.append("QS3DEDITSOURCE must capture/validate ownership -> prompt -> revalidate freshness -> mutate -> reconcile -> reverse on reconcile failure")

    capture_helper = text.find("private static SourceEditSelection? CaptureAuthoritativeSelection(Document document)")
    capture_validate = text.find("ValidateAuthoritativeOwnership(project, handles)", capture_helper)
    capture_return = text.find("return new SourceEditSelection(", capture_validate)
    if min(capture_helper, capture_validate, capture_return) < 0 or not capture_helper < capture_validate < capture_return:
        errors.append("QS3DEDITSOURCE capture must validate authoritative ownership before returning a mutable selection")

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
