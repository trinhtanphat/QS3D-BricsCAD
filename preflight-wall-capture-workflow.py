#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

commands = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
build3d = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
selection = ROOT / "src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs"
docs = ROOT / "docs/DIRECT-DRAW-WORKFLOW.md"

for path in (commands, build3d, selection, docs):
    if not path.is_file():
        errors.append("missing wall compatibility workflow dependency: " + str(path.relative_to(ROOT)))

if commands.is_file():
    text = commands.read_text(encoding="utf-8")
    match = re.search(
        r'\[CommandMethod\("QS3DWALL"[^\n]*\)\]\s*public void CaptureWall\(\)\s*\{(?P<body>.*?)\n\s*\}\s*\n\s*\[CommandMethod\("QS3DROOM"',
        text,
        re.S,
    )
    if not match:
        errors.append("QS3DWALL CaptureWall body could not be isolated")
    else:
        body = match.group("body")
        required = (
            "SemanticCaptureService.Capture(doc, ElementCategory.ArchitecturalWall)",
            "PaletteCoordinator.RefreshProject()",
            "Chỉnh Family/Instance",
            "QS3DBUILD3D",
            "LINE/open POLYLINE",
        )
        for token in required:
            if token not in body:
                errors.append("QS3DWALL capture step missing: " + token)
        forbidden = (
            "WallSolidBuilder",
            "PolylineWallSolidBuilder",
            "StructuralSolidBuilder",
            "GeneratedGeometryService",
            "WallRegenerator",
            "CreateBox(",
            "CreateExtrudedSolid(",
        )
        for token in forbidden:
            if token in body:
                errors.append("QS3DWALL must capture only; native build leaked into capture step: " + token)

if build3d.is_file():
    text = build3d.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DBUILD3D"',
        "WallSolidBuilder.BuildSelectedLineWalls",
        "PolylineWallSolidBuilder.BuildSelected",
        "SemanticReferenceHandles.MatchesSelection",
        "CadHandleService.Resolve(document, sourceHandles)",
    ):
        if token not in text:
            errors.append("QS3DBUILD3D compatibility step missing: " + token)

if selection.is_file():
    text = selection.read_text(encoding="utf-8")
    read_selection = text.split("private static IReadOnlyList<EntitySnapshot> ReadSelection", 1)[-1]
    if "editor.SetImpliedSelection(objectIds)" not in read_selection:
        errors.append("Interactive QS3DWALL selection must remain implied/PICKFIRST for the later QS3DBUILD3D step")

if docs.is_file():
    text = docs.read_text(encoding="utf-8")
    normalized = re.sub(r"\s+", " ", text)
    if "LINE -> QS3DWALL -> QS3DBUILD3D" not in normalized:
        errors.append("Direct Draw workflow documentation must retain LINE -> QS3DWALL -> QS3DBUILD3D compatibility flow")

print("QS3D wall capture/build workflow preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: QS3DWALL is capture-only, preserves the selected CAD reference, and QS3DBUILD3D remains the explicit native 3D commit/rebuild step.")
