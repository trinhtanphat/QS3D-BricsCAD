#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/GridAnnotationCommands.cs"
POLICY = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
DOC = ROOT / "docs/GRID-NATIVE-ANNOTATION.md"
errors = []

for path in (BUILDER, COMMANDS, POLICY, DOC):
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
        'new Circle(',
        'new DBText',
        'AuditTrail.ForProject(project).Record(',
        'project.Touch();',
        'transaction.Commit();',
        'document.Editor.Regen()',
        'source is Line',
        'source is Arc',
    )
    for token in required:
        if token not in text:
            errors.append("GridAnnotationBuilder.cs missing token: " + token)

    touch = text.find('project.Touch();')
    commit = text.find('transaction.Commit();')
    regen = text.find('document.Editor.Regen()')
    if touch < 0 or commit < 0 or touch > commit:
        errors.append("Grid annotation semantic metadata/project Touch must happen before CAD commit")
    if regen < 0 or commit < 0 or regen < commit:
        errors.append("Grid annotation Regen must be best-effort after CAD commit")
    if 'GeneratedSolidHandle' in text:
        errors.append("Grid annotation must not claim the host GeneratedSolidHandle slot")

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

if POLICY.is_file():
    text = POLICY.read_text(encoding="utf-8")
    if 'normalized.StartsWith("Generated"' not in text or 'normalized.EndsWith("Handles"' not in text:
        errors.append("GeneratedHandleOwnershipPolicy must continue discovering generated multi-handle owner slots")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        'QS3DGRIDANNOTATE',
        'QS3DGRIDANNOTATEALL',
        'GeneratedGridAnnotationHandles',
        'XData',
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
print("PASS: native Grid annotation has explicit semantic labels, generated ownership, replacement guards and cross-layer rollback; runtime remains separately qualified.")
