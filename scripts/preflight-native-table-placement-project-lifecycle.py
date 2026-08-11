#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CASES = (
    (
        ROOT / "src" / "QS3D.BricsCAD.V25" / "BqNativeTableCommands.cs",
        'CommandMethod("QS3DBQTABLE"',
        'CommandMethod("QS3DBQTABLEREFRESH"',
        "BqNativeTableBuilder.Build(document, project, world)",
        "BQ Table",
        True,
    ),
    (
        ROOT / "src" / "QS3D.BricsCAD.V25" / "BbsNativeTableCommands.cs",
        'CommandMethod("QS3DBBSTABLE"',
        'CommandMethod("QS3DBBSTABLEREFRESH"',
        "BbsNativeTableBuilder.Build(document, project, world)",
        "BBS Table",
        True,
    ),
    (
        ROOT / "src" / "QS3D.BricsCAD.V25" / "DoorOpeningNativeTableCommands.cs",
        'CommandMethod("QS3DDOOROPENINGTABLE"',
        'CommandMethod("QS3DDOOROPENINGTABLEREFRESH"',
        "DoorOpeningNativeTableBuilder.Build(document, project, world)",
        "Door/Opening Table",
        False,
    ),
    (
        ROOT / "src" / "QS3D.BricsCAD.V25" / "MaterialUsageNativeTableCommands.cs",
        'CommandMethod("QS3DMATERIALTABLE"',
        'CommandMethod("QS3DMATERIALTABLEREFRESH"',
        "MaterialUsageNativeTableBuilder.Build(document, project, world)",
        "Material Usage Table",
        False,
    ),
    (
        ROOT / "src" / "QS3D.BricsCAD.V25" / "RoomFinishNativeTableCommands.cs",
        'CommandMethod("QS3DFINISHTABLE"',
        'CommandMethod("QS3DFINISHTABLEREFRESH"',
        "RoomFinishNativeTableBuilder.Build(document, project, world)",
        "Room Finish Table",
        False,
    ),
    (
        ROOT / "src" / "QS3D.BricsCAD.V25" / "SemanticElementTableCommands.cs",
        'CommandMethod("QS3DELEMENTTABLE"',
        'CommandMethod("QS3DELEMENTTABLEREFRESH"',
        "SemanticElementTableBuilder.Build(document, project, world)",
        "Semantic Element Table",
        False,
    ),
)

errors = []
for path, start_token, end_token, build_token, label, requires_regeneration in CASES:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        continue
    text = path.read_text(encoding="utf-8")
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    body = text[start:end] if start >= 0 and end > start else ""
    if not body:
        errors.append(label + ": cannot isolate Build command")
        continue

    required = [
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "var expectedProjectId = previewProject.ProjectId",
        "document.Editor.GetPoint(",
        "if (point.Status != PromptStatus.OK) return;",
        'RequireExistingProject(document, "' + label + '")',
        "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
    ]
    if requires_regeneration:
        required.append("RegenerateSemantic(project)")
    required.append(build_token)

    positions = {}
    for token in required:
        pos = body.find(token)
        positions[token] = pos
        if pos < 0:
            errors.append(label + ": missing lifecycle token: " + token)

    if all(pos >= 0 for pos in positions.values()):
        ordered = [positions[token] for token in required]
        if ordered != sorted(ordered):
            errors.append(label + ": expected read-only probe -> ProjectId snapshot -> point prompt -> cancel guard -> canonical bind -> freshness check -> optional regeneration -> native build ordering")

    prompt = body.find("document.Editor.GetPoint(")
    cancel = body.find("if (point.Status != PromptStatus.OK) return;")
    bind = body.find('RequireExistingProject(document, "' + label + '")')
    if prompt >= 0 and bind >= 0 and bind < prompt:
        errors.append(label + ": canonical project bind occurs before placement prompt/cancel boundary")
    if cancel >= 0 and bind >= 0 and bind < cancel:
        errors.append(label + ": cancelled placement reaches canonical project binding")

    if "ProjectContextCoordinator.GetOrCreate" in body:
        errors.append(label + ": Build must not create a replacement project")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: all six native Table placement commands probe existing state read-only, return on cancelled placement before canonical binding, verify the same ProjectId after binding, then build from canonical state.")
