#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/GridAnnotationCommands.cs"
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs"
RUNTIME_HEALTH = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedGridAnnotationRuntimeHealthService.cs"
RUNTIME_AGGREGATOR = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
COMPREHENSIVE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
POLICY = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedGridAnnotationHealthSmoke.cs"
DOC = ROOT / "docs/GRID-NATIVE-ANNOTATION.md"
errors = []

for path in (BUILDER, COMMANDS, HEALTH, RUNTIME_HEALTH, RUNTIME_AGGREGATOR, COMPREHENSIVE, POLICY, SMOKE, DOC):
    if not path.is_file():
        errors.append("missing Grid annotation contract file: " + str(path.relative_to(ROOT)))

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    required = (
        'GeneratedGridAnnotationHandles',
        'GridNamingService.GridLabelKey',
        'ProjectStateSnapshot.Capture(project)',
        'rollback.Restore(project)',
        'GeneratedGeometryService.MarkGenerated(',
        'GeneratedGeometryService.RequireMatchingOwnership(',
        'new Circle(center, normal, radius)',
        'new DBText',
        'Normal = normal',
        'annotationNormal = arc.Normal',
        'Math.Abs(start.Z - end.Z) > GeometryTolerance',
        'ValidateVector(annotationNormal, element.Id + "/arc normal")',
        'AuditTrail.ForProject(project).Record(',
        'transaction.Commit();',
        'document.Editor.Regen()',
        'source is Line',
        'source is Arc',
    )
    for token in required:
        if token not in text:
            errors.append("GridAnnotationBuilder.cs missing token: " + token)

    commit = text.find('transaction.Commit();')
    regen = text.find('document.Editor.Regen()')
    if 'project.Touch();' in text:
        errors.append("Grid annotation must keep redundant project.Touch removed because AuditTrail.Record owns revision advancement")
    if regen < 0 or commit < 0 or regen < commit:
        errors.append("Grid annotation Regen must be best-effort after CAD commit")
    if 'GeneratedSolidHandle' in text:
        errors.append("Grid annotation must not claim the host GeneratedSolidHandle slot")
    if 'new Circle(center, Vector3d.ZAxis' in text:
        errors.append("Grid annotation must not force ARC/native circles onto WCS-Z plane: new Circle(center, Vector3d.ZAxis")

    endpoint_start = text.find("private static void AddEndpointAnnotation")
    endpoint_end = text.find("private static void PrepareEntity", endpoint_start + 1) if endpoint_start >= 0 else -1
    if endpoint_start < 0 or endpoint_end < 0:
        errors.append("Grid annotation preflight cannot isolate AddEndpointAnnotation")
    else:
        endpoint_body = text[endpoint_start:endpoint_end]
        if 'Normal = Vector3d.ZAxis' in endpoint_body:
            errors.append("Grid annotation DBText must consume the resolved source-plane normal instead of forcing WCS-Z")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DGRIDANNOTATE", CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DGRIDANNOTATEALL", CommandFlags.Modal)]',
        'EntitySnapshotReader.ReadCurrentSelection(document)',
        'x.Category == ElementCategory.Grid',
        'GridNamingService.GridLabelKey',
        'GridAnnotationBuilder.Build(document, project, selected)',
        'GridAnnotationBuilder.Build(document, project, grids)',
    ):
        if token not in text:
            errors.append("GridAnnotationCommands.cs missing token: " + token)

if HEALTH.is_file():
    text = HEALTH.read_text(encoding="utf-8")
    for token in (
        'GeneratedGridAnnotationHandles',
        'GRID_ANNOTATION_LABEL_STALE',
        'GRID_ANNOTATION_PROJECT_MISMATCH',
        'GRID_ANNOTATION_ELEMENT_MISMATCH',
        'GRID_ANNOTATION_HANDLE_INVALID',
        'GRID_ANNOTATION_HANDLE_IN_SOURCE',
        'GRID_ANNOTATION_TEXT_TOO_LARGE',
    ):
        if token not in text:
            errors.append("GeneratedGridAnnotationHealthService.cs missing token: " + token)

if RUNTIME_HEALTH.is_file():
    text = RUNTIME_HEALTH.read_text(encoding="utf-8")
    for token in (
        'GeneratedGridAnnotationHandles',
        'GRID_ANNOTATION_CAD_MISSING',
        'GRID_ANNOTATION_CAD_TYPE_MISMATCH',
        'GRID_ANNOTATION_CAD_OWNERSHIP_MISMATCH',
        'GRID_ANNOTATION_CAD_TEXT_STALE',
        'GeneratedGeometryService.HasMatchingOwnership(',
        'entity is Line',
        'entity is Circle',
        'entity is DBText',
        'GridNamingService.GridLabelKey',
        'StartOpenCloseTransaction()',
    ):
        if token not in text:
            errors.append("GeneratedGridAnnotationRuntimeHealthService.cs missing token: " + token)
    for forbidden in ('Erase()', 'OpenMode.ForWrite'):
        if forbidden in text:
            errors.append("live Grid annotation health must remain read-only: " + forbidden)

if RUNTIME_AGGREGATOR.is_file():
    text = RUNTIME_AGGREGATOR.read_text(encoding="utf-8")
    if 'GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project)' not in text:
        errors.append("GeneratedSolidRuntimeHealthService.cs must aggregate live Grid annotation health into QS3DHEALTH")

if COMPREHENSIVE.is_file():
    text = COMPREHENSIVE.read_text(encoding="utf-8")
    for token in (
        '"GRID_ANNOTATION"',
        'new GeneratedGridAnnotationHealthService().Inspect(project)',
    ):
        if token not in text:
            errors.append("ComprehensiveModelHealthService.cs missing Grid annotation health integration: " + token)

if POLICY.is_file():
    text = POLICY.read_text(encoding="utf-8")
    if 'normalized.StartsWith("Generated"' not in text or 'normalized.EndsWith("Handles"' not in text:
        errors.append("GeneratedHandleOwnershipPolicy must continue discovering generated multi-handle owner slots")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        'GeneratedGridAnnotationHealthService',
        'GRID_ANNOTATION_LABEL_STALE',
        'GRID_ANNOTATION_PROJECT_MISMATCH',
        'GRID_ANNOTATION_HANDLE_INVALID',
        'NoMetadataIsOptional',
    ):
        if token not in text:
            errors.append("GeneratedGridAnnotationHealthSmoke.cs missing scenario: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        'QS3DGRIDANNOTATE',
        'QS3DGRIDANNOTATEALL',
        'GeneratedGridAnnotationHandles',
        'GeneratedGridAnnotationHealthService',
        'GeneratedGridAnnotationRuntimeHealthService',
        'ComprehensiveModelHealthService',
        'XData',
        'ARC uses its native plane normal',
        '3D-sloped LINE',
        'LOCAL_ONLY',
        'BricsCAD V25',
    ):
        if token not in text:
            errors.append("GRID-NATIVE-ANNOTATION.md missing boundary token: " + token)

print("QS3D native Grid annotation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: native Grid annotation has explicit semantic labels, source-plane-aware geometry, generated ownership, replacement guards, AuditTrail-owned revision, persisted + live read-only health integration and cross-layer rollback; runtime remains separately qualified.")