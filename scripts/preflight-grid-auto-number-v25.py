#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/GridAutoNumberCommands.cs"
PLANNER = ROOT / "src/QS3D.Core/Geometry/GridSpatialOrderingPlanner.cs"
NAMING = ROOT / "src/QS3D.Core/Domain/GridNamingService.cs"
DOC = ROOT / "docs/GRID-AUTO-NUMBER-V25.md"
errors = []

for path in (COMMAND, PLANNER, NAMING, DOC):
    if not path.is_file():
        errors.append("missing Grid auto-number contract file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DGRIDNUMBERAUTO", CommandFlags.UsePickSet)]',
        'EntitySnapshotReader.ReadCurrentSelection(document)',
        'MaxGridBatch = 2000',
        'x.Category == ElementCategory.Grid',
        'authoritative.Count != 1',
        'transaction.GetObject(objectId, OpenMode.ForRead, false) as Line',
        'GridReferenceCurve.Line(',
        'Math.Abs(line.StartPoint.Z - line.EndPoint.Z) > PlanElevationTolerance',
        'Math.Abs(elevation - planElevation.Value) > PlanElevationTolerance',
        'editor.CurrentUserCoordinateSystem',
        'first.Value.TransformBy(ucs)',
        'second.Value.TransformBy(ucs)',
        'GridSpatialOrderingPlanner.OrderParallelLines(previewExtraction.Curves, orderingAxis.Value)',
        'ConfirmPlan(document.Editor, previewOrdered, namingOptions)',
        'ExistingProjectMutationContext.Require(document, "Grid Auto Number")',
        'GridSpatialOrderingPlanner.OrderParallelLines(currentExtraction.Curves, orderingAxis.Value)',
        'SamePreviewPlan(previewOrdered, currentOrdered)',
        'Áp dụng auto-number theo thứ tự này? [Yes/No] <No>',
        'if (confirm.Status == PromptStatus.None) return false;',
        'confirm.Status == PromptStatus.OK && string.Equals(confirm.StringResult, "Yes"',
        'ProjectStateSnapshot.Capture(project)',
        'GridNamingService.Renumber(project, orderedIds, namingOptions)',
        'rollback.Restore(project)',
        '"grid.autonumber"',
        'document.Editor.SetImpliedSelection(orderedObjectIds)',
    )
    for token in required:
        if token not in text:
            errors.append("GridAutoNumberCommands.cs missing token: " + token)

    forbidden = (
        'OpenMode.ForWrite',
        '.Erase()',
        'AppendEntity(',
        'GridReferenceCurve.Arc(',
        '.OrderBy(',
        '.OrderByDescending(',
        'Áp dụng auto-number theo thứ tự này? [Yes/No] <Yes>',
        'if (confirm.Status == PromptStatus.None) return true;',
    )
    for token in forbidden:
        if token in text:
            errors.append("Grid auto-number must remain read-only CAD + Core-planner-owned ordering + explicit Yes-only confirmation: " + token)

    preview_planner = text.find('GridSpatialOrderingPlanner.OrderParallelLines(previewExtraction.Curves, orderingAxis.Value)')
    confirm = text.find('ConfirmPlan(document.Editor, previewOrdered, namingOptions)')
    bind = text.find('ExistingProjectMutationContext.Require(document, "Grid Auto Number")')
    current_planner = text.find('GridSpatialOrderingPlanner.OrderParallelLines(currentExtraction.Curves, orderingAxis.Value)')
    freshness = text.find('SamePreviewPlan(previewOrdered, currentOrdered)')
    snapshot = text.find('ProjectStateSnapshot.Capture(project)')
    mutate = text.find('GridNamingService.Renumber(project, orderedIds, namingOptions)')
    ordered_steps = (preview_planner, confirm, bind, current_planner, freshness, snapshot, mutate)
    if min(ordered_steps) < 0 or list(ordered_steps) != sorted(ordered_steps):
        errors.append("Grid auto-number must preview, confirm, bind, re-read, freshness-check, snapshot, then mutate in that order")

if PLANNER.is_file():
    text = PLANNER.read_text(encoding="utf-8")
    for token in (
        'OrderParallelLines(',
        'curve.Kind != GridReferenceCurveKind.Line',
        'if (alignment > alignmentTolerance)',
        'Math.Abs(delta) <= coordinateTolerance',
        'if (descending) entries.Reverse()',
        'MaxCurves = 2000',
    ):
        if token not in text:
            errors.append("GridSpatialOrderingPlanner.cs missing fail-closed token: " + token)

if NAMING.is_file():
    text = NAMING.read_text(encoding="utf-8")
    for token in (
        'public static IReadOnlyList<GridLabelAssignment> Renumber(',
        'reservedLabels.Contains(label)',
        'project.Touch()',
    ):
        if token not in text:
            errors.append("GridNamingService.cs missing naming-integrity token: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        'QS3DGRIDNUMBERAUTO',
        'explicit ordering axis',
        'explicit `Yes`',
        'Enter/default means No',
        'UCS → WCS',
        'parallel LINE',
        'ARC/radial',
        'same plan elevation',
        'LOCAL_ONLY',
        'BricsCAD V25',
    ):
        if token not in text:
            errors.append("GRID-AUTO-NUMBER-V25.md missing contract token: " + token)

print("QS3D V25 Grid auto-number preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Grid auto-number uses explicit WCS ordering, confirms a detached preview, then binds and revalidates live source order before rollback-guarded mutation; runtime remains separately qualified.")
