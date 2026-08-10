#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/SemanticElementTableBuilder.cs"
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/SemanticElementTableCommands.cs"
CORE = ROOT / "src/QS3D.Core/Documentation/SemanticDocumentationTableBuilder.cs"
errors = []

for path in (BUILDER, COMMANDS, CORE):
    if not path.is_file():
        errors.append("missing native documentation Table contract file: " + str(path.relative_to(ROOT)))

if BUILDER.is_file():
    text = BUILDER.read_text(encoding="utf-8")
    for token in (
        'RegAppName = "QS3DDOC"',
        'DocumentId = "SemanticElementSchedule"',
        'GeneratedSemanticElementTableHandle',
        'SemanticDocumentationTableBuilder.Build',
        'ProjectStateSnapshot.Capture(project)',
        'ErasePrevious(document, transaction, project)',
        'if (!(entity is Table table))',
        'HasMatchingOwnership(table, project.ProjectId, storedFingerprint)',
        'table.SetSize(',
        'table.SetTextString(',
        'table.GenerateLayout()',
        'CadUnitService.MetersToDrawingUnits',
        'SEMANTIC_ELEMENT_TABLE_STALE',
        'SEMANTIC_ELEMENT_TABLE_WRONG_TYPE',
        'SEMANTIC_ELEMENT_TABLE_OWNERSHIP_MISMATCH',
    ):
        if token not in text:
            errors.append("SemanticElementTableBuilder.cs missing contract token: " + token)
    if 'GeneratedGeometryService.MarkGenerated' in text or 'GeneratedSolidHandle' in text:
        errors.append("project-level documentation Table must not masquerade as element generated-solid ownership")

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DELEMENTTABLE"',
        '[CommandMethod("QS3DELEMENTTABLEREFRESH"',
        '[CommandMethod("QS3DELEMENTTABLEREMOVE"',
        '[CommandMethod("QS3DELEMENTTABLEHEALTH"',
        'if (!document.Database.TileMode)',
        'CurrentUserCoordinateSystem',
        'SemanticElementTableBuilder.StoredPosition(project)',
    ):
        if token not in text:
            errors.append("SemanticElementTableCommands.cs missing command/scope token: " + token)

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    for token in ('MaxRows', 'MaxColumns', 'SemanticTagRenderer.Render'):
        if token not in text:
            errors.append("Core semantic documentation table lost bounded renderer contract: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: native Semantic Element Table uses bounded Core rendering, project-level QS3DDOC ownership, ModelSpace P0 scope, unit-aware sizing, rollback, replacement and health contracts. Runtime V25 qualification is still required.")
