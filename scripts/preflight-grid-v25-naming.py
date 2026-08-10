#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/GridNamingCommands.cs"
CORE = ROOT / "src/QS3D.Core/Domain/GridNamingService.cs"
DOC = ROOT / "docs/GRID-NAMING-V25.md"
errors = []

for path in (COMMAND, CORE, DOC):
    if not path.is_file():
        errors.append("missing Grid V25 naming contract file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DGRIDRENUMBER", CommandFlags.Modal)]',
        'editor.GetEntity(',
        'x.Category == ElementCategory.Grid',
        'x.SourceHandles.Any(',
        'matches.Count > 1',
        'seenElementIds.Add(',
        'GridLabelSequence.Alphabetic',
        'GridLabelSequence.Numeric',
        'GridNamingService.Renumber(project, orderedIds, options)',
        'ProjectStateSnapshot.Capture(project)',
        'rollback.Restore(project)',
        'MaxGridBatch = 2000',
        'MaxSequenceIndex = 999999',
        'MaxNumericPadding = 6',
    )
    for token in required:
        if token not in text:
            errors.append("GridNamingCommands.cs missing guarded interaction token: " + token)

    forbidden = (
        'ReadCurrentSelection(document)',
        'OrderBy(x => x.',
        'OrderByDescending(x => x.',
    )
    for token in forbidden:
        if token in text:
            errors.append("GridNamingCommands.cs must not infer renumber order from PICKFIRST/spatial sorting: " + token)

    if text.find('GridNamingService.Renumber(project, orderedIds, options)') > text.find('FinalizeUi(document, assignments, pickedObjectIds)'):
        errors.append("Grid semantic mutation must complete before best-effort UI finalization")

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    for token in (
        'public const string GridLabelKey = "GridLabel"',
        'public const string GridSequenceIndexKey = "GridSequenceIndex"',
        'reservedLabels.Contains(label)',
        'project.Touch()',
    ):
        if token not in text:
            errors.append("GridNamingService.cs missing Core naming token: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DGRIDRENUMBER",
        "click",
        "không dùng thứ tự PICKFIRST",
        "không suy đoán spatial order",
        "LOCAL_ONLY",
    ):
        if token not in text:
            errors.append("GRID-NAMING-V25.md missing interaction/runtime boundary: " + token)

print("QS3D V25 Grid naming preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: V25 Grid renumber uses explicit click order, exact semantic ownership, bounded options and semantic rollback; native runtime remains separately qualified.")
