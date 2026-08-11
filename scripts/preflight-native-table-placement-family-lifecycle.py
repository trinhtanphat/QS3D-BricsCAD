#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
errors = []

SPECS = (
    ("BqNativeTableCommands.cs", "QS3DBQTABLE", "QS3DBQTABLEREFRESH", "BQ Table", "BqNativeTableBuilder.Build(document, project, world)"),
    ("BbsNativeTableCommands.cs", "QS3DBBSTABLE", "QS3DBBSTABLEREFRESH", "BBS Table", "BbsNativeTableBuilder.Build(document, project, world)"),
    ("SemanticElementTableCommands.cs", "QS3DELEMENTTABLE", "QS3DELEMENTTABLEREFRESH", "Semantic Element Table", "SemanticElementTableBuilder.Build(document, project, world)"),
    ("MaterialUsageNativeTableCommands.cs", "QS3DMATERIALTABLE", "QS3DMATERIALTABLEREFRESH", "Material Usage Table", "MaterialUsageNativeTableBuilder.Build(document, project, world)"),
    ("RoomFinishNativeTableCommands.cs", "QS3DFINISHTABLE", "QS3DFINISHTABLEREFRESH", "Room Finish Table", "RoomFinishNativeTableBuilder.Build(document, project, world)"),
    ("DoorOpeningNativeTableCommands.cs", "QS3DDOOROPENINGTABLE", "QS3DDOOROPENINGTABLEREFRESH", "Door/Opening Table", "DoorOpeningNativeTableBuilder.Build(document, project, world)"),
)

for filename, build_command, refresh_command, operation, builder_call in SPECS:
    path = SRC / filename
    if not path.is_file():
        errors.append("missing native Table command source: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    start_token = '[CommandMethod("' + build_command + '"'
    end_token = '[CommandMethod("' + refresh_command + '"'
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    body = text[start:end] if start >= 0 and end > start else ""
    if not body:
        errors.append(filename + ": cannot isolate Build command")
        continue

    tokens = (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "var expectedProjectId = previewProject.ProjectId;",
        "document.Editor.GetPoint(",
        "if (point.Status != PromptStatus.OK) return;",
        'RequireExistingProject(document, "' + operation + '")',
        "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
        builder_call,
    )
    positions = []
    for token in tokens:
        position = body.find(token)
        positions.append(position)
        if position < 0:
            errors.append(filename + ": placement lifecycle token missing: " + token)
    if positions and min(positions) >= 0 and positions != sorted(positions):
        errors.append(filename + ": Build must probe read-only state, capture ProjectId, acquire/accept placement, bind canonical state, verify freshness, then build")

    get_point = body.find("document.Editor.GetPoint(")
    bind = body.find('RequireExistingProject(document, "' + operation + '")')
    if get_point >= 0 and bind >= 0 and bind < get_point:
        errors.append(filename + ": canonical project binding occurs before placement input")
    if "ProjectContextCoordinator.GetOrCreate(document)" in body:
        errors.append(filename + ": Build must not directly create a project during native Table placement")
    if "TryGetReadOnly(document, out var previewProject)" not in body:
        errors.append(filename + ": Build must prove an existing project read-only before prompting for placement")

print("QS3D native Table placement family lifecycle preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: all six native Table Build commands prove existing state read-only, allow placement cancellation before canonical binding, then rebind the same ProjectId before native/semantic mutation.")
