#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/GridNamingCommands.cs"
CORE = ROOT / "src/QS3D.Core/Domain/GridNamingService.cs"
DOC = ROOT / "docs/GRID-WORKFLOW.md"
errors = []

for path in (COMMAND, CORE, DOC):
    if not path.is_file():
        errors.append("missing Grid naming command dependency: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DGRIDNUMBER", CommandFlags.Modal)]',
        "AcquireOrderedGridIds(document, project)",
        "new PromptEntityOptions(prompt)",
        "AllowNone = orderedIds.Count > 0",
        "editor.GetEntity(options)",
        "GridNamingService.Renumber(project, orderedIds, options)",
        "ProjectStateSnapshot.Capture(project)",
        "rollback.Restore(project)",
        'AuditTrail.ForProject(project).Record(',
        '"grid.renumber"',
        "FinalizeUi(document, assignments, options)",
        'UI sync warning:',
    ):
        if token not in text:
            errors.append("GridNamingCommands.cs missing explicit-order/rollback contract: " + token)
    if "GetSelection(" in text or "SelectImplied(" in text:
        errors.append("QS3DGRIDNUMBER must not rely on selection-set ordering; explicit GetEntity order is required")
    if text.index("GridNamingService.Renumber(project, orderedIds, options)") > text.index("AuditTrail.ForProject(project).Record("):
        errors.append("Grid semantic renumber must occur before its audit record")
    if text.index("FinalizeUi(document, assignments, options)") < text.index("GridNamingService.Renumber(project, orderedIds, options)"):
        errors.append("Grid UI finalization must happen only after semantic renumber succeeds")

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    for token in (
        'public const string GridLabelKey = "GridLabel"',
        'public const string GridSequenceIndexKey = "GridSequenceIndex"',
        'reservedLabels.Contains(label)',
        'plannedLabels.Add(label)',
        'project.Touch()',
    ):
        if token not in text:
            errors.append("GridNamingService.cs missing whole-batch naming contract: " + token)

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "QS3DGRIDNUMBER",
        "explicit click order",
        "does not infer spatial order",
        "does not move/rotate source CAD",
    ):
        if token not in text:
            errors.append("GRID-WORKFLOW.md missing command/runtime boundary: " + token)

print("QS3D Grid naming command preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DGRIDNUMBER uses explicit per-entity click order, delegates fail-closed naming to Core, rolls project state back on command failure and keeps UI finalization non-fatal.")
