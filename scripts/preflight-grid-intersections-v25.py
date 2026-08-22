#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/GridIntersectionCommands.cs"
PLANNER = ROOT / "src/QS3D.Core/Geometry/GridIntersectionPlanner.cs"
DOC = ROOT / "docs/GRID-INTERSECTIONS-V25.md"
errors = []

for path in (COMMAND, PLANNER, DOC):
    if not path.is_file():
        errors.append("missing V25 Grid intersection contract file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DGRIDINTERSECTIONS", CommandFlags.UsePickSet)]',
        'EntitySnapshotReader.ReadCurrentSelection(document)',
        'selected.Count < 2',
        'MaxGridBatch = 2000',
        'MaxPrintedIntersections = 100',
        'x.Category == ElementCategory.Grid',
        'authoritative.Count != 1',
        'transaction.GetObject(objectId, OpenMode.ForRead, false) as Entity',
        'entity is Line line',
        'entity is Arc arc',
        'GridReferenceCurve.Line(',
        'GridReferenceCurve.Arc(',
        'Math.Abs(line.StartPoint.Z - line.EndPoint.Z) > PlanElevationTolerance',
        'Math.Abs(elevation - planElevation.Value) > PlanElevationTolerance',
        'ValidatePositive(arc.TotalAngle',
        'arc.StartAngle',
        'var normal = arc.Normal',
        'nz < 1d - NormalTolerance',
        'Math.Abs(arc.StartPoint.Z - arc.Center.Z) > PlanElevationTolerance',
        'GridIntersectionPlanner.FindIntersections(extraction.Curves)',
        'Math.Min(intersections.Count, MaxPrintedIntersections)',
        'ToString("G17", CultureInfo.InvariantCulture)',
    )
    for token in required:
        if token not in text:
            errors.append("GridIntersectionCommands.cs missing token: " + token)

    for forbidden in (
        'OpenMode.ForWrite',
        '.Erase()',
        'AppendEntity(',
        'AddNewlyCreatedDBObject(',
        'ProjectStateSnapshot',
        'project.Touch()',
        'AuditTrail.',
    ):
        if forbidden in text:
            errors.append("QS3DGRIDINTERSECTIONS must remain read-only CAD + semantic state: " + forbidden)

if PLANNER.is_file():
    text = PLANNER.read_text(encoding="utf-8")
    for token in (
        'private const int MaxCurves = 2000',
        'private const int MaxIntersections = 100000',
        'IntersectLines(first, second, tolerance)',
        'IntersectLineArc(first, second, tolerance)',
        'IntersectArcs(first, second, tolerance)',
        'collinear/overlapping LINE references do not define one unique Grid intersection',
        'coincident ARC support circles are intentionally rejected',
    ):
        if token not in text:
            errors.append("GridIntersectionPlanner.cs missing bounded/fail-closed token: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        'QS3DGRIDINTERSECTIONS',
        'read-only',
        'WCS-XY',
        'normal +Z',
        'Arc.TotalAngle',
        'same plan elevation',
        'does not create markers',
        'pair ownership',
        'LOCAL_ONLY',
        'BricsCAD V25',
    ):
        if token not in text:
            errors.append("GRID-INTERSECTIONS-V25.md missing adapter/runtime boundary: " + token)

print("QS3D V25 Grid intersection preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: V25 Grid intersection inspection is bounded, read-only, ownership-aware, same-plane WCS-XY LINE/+Z ARC extraction feeding the fail-closed Core planner; runtime remains separately qualified.")
