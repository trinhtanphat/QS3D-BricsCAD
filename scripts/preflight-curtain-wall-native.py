#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "planner": ROOT / "src/QS3D.Core/Geometry/CurtainWallLayoutPlanner.cs",
    "detail": ROOT / "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs",
    "builder": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "owner": ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs",
    "invalidator": ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs",
    "health": ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
    "frame_command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs",
    "health_command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs",
    "build_command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs",
    "ui": ROOT / "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml",
    "ui_code": ROOT / "src/QS3D.BricsCAD.V25/UI/CurtainWallWindow.xaml.cs",
    "defaults": ROOT / "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
}
for path in files.values():
    if not path.is_file(): errors.append("missing native curtain file: " + str(path.relative_to(ROOT)))

if files["ui"].is_file():
    try: ET.parse(files["ui"])
    except ET.ParseError as exc: errors.append("CurtainWallWindow.xaml is not well-formed: " + str(exc))

checks = {
    "detail": ["VerticalFrames", "HorizontalFrames", "MaxDetailSolids", "PanelAreaM2"],
    "builder": [
        'HandlesKey = "GeneratedCurtainFrameHandles"', 'Mode = "LineFrameOverlay"',
        "CurtainWallDetailPlanner.Plan", "MaxFramesPerElement = 4096", "MaxFramesPerBatch = 8192",
        "GeneratedCurtainFrameOwnershipGuard.Build", "ownership.EnsureOwned", "CurtainFrameDepthM",
        "GeneratedCurtainFrameColumns", "GeneratedCurtainFrameRows", "GeneratedCurtainFrameSourceLengthM",
        "GeneratedCurtainFrameHeightM", "CreateBox", "GlassWall", "LINE nằm ngang",
    ],
    "owner": [
        'GeneratedCurtainFrameHandles', 'GeneratedSolidHandle', 'PhysicalOpeningCutSolidHandle',
        'GeneratedSlabMeshHandles', 'GeneratedWallMeshHandles',
    ],
    "invalidator": [
        'GeneratedCurtainFrameHandles', 'GeneratedCurtainFrameCount', 'GeneratedCurtainFrameMode',
        'GeneratedCurtainFrameOwnershipGuard.Build', 'EraseCurtainFrames',
    ],
    "health": [
        'GeneratedCurtainFrameHandles', 'CURTAIN_FRAME_GENERATED_SOLID_MISSING',
        'CURTAIN_FRAME_GRID_COUNT_MISMATCH', 'GeneratedCurtainFrameDepthM',
        'GeneratedCurtainFrameSourceLengthM', 'GeneratedCurtainFrameHeightM', 'ElementCategory.GlassWall',
    ],
    "frame_command": ['CommandMethod("QS3DCURTAINFRAMES3D"', 'CurtainWallFrameSolidBuilder.BuildSelectedLineWalls'],
    "health_command": ['CommandMethod("QS3DCURTAINFRAMEHEALTH"', 'GeneratedCurtainFrameHealthService().Inspect'],
    "build_command": [
        'CommandMethod("QS3DCURTAIN3D"', 'WallSolidBuilder.BuildSelectedLineWalls',
        'PolylineWallSolidBuilder.BuildSelected', 'CurtainWallFrameSolidBuilder.BuildSelectedLineWalls',
    ],
    "ui": ['x:Name="FrameDepthBox"', 'Tag="QS3DCURTAINFRAMES3D"', 'Tag="QS3DCURTAINFRAMEHEALTH"'],
    "ui_code": ['CurtainFrameDepthM', 'FrameDepthBox.Text', 'yield return FrameDepthBox'],
    "defaults": ['CurtainFrameDepthM'],
}
for key, needles in checks.items():
    path = files[key]
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(str(path.relative_to(ROOT)) + " missing curtain native token: " + needle)

owners = {"QS3DCURTAIN3D": [], "QS3DCURTAINFRAMES3D": [], "QS3DCURTAINFRAMEHEALTH": []}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for command in owners:
        if re.search(r'\[CommandMethod\("' + re.escape(command) + r'"', text, re.IGNORECASE):
            owners[command].append(str(path.relative_to(ROOT)))
for command, found in owners.items():
    if len(found) != 1: errors.append(command + " must have exactly one CommandMethod owner; found: " + ", ".join(found))

if files["builder"].is_file():
    text = files["builder"].read_text(encoding="utf-8")
    for forbidden in ('GeneratedSolidHandle"] = string.Join', 'GeneratedSolidHandle"] = string.Join(";"', "PolylineWallSolidBuilder"):
        if forbidden in text: errors.append("curtain frame builder must not replace backing host ownership: " + forbidden)

print("QS3D native curtain-wall preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: GlassWall LINE keeps one backing host for opening booleans and adds guarded dedicated curtain-frame overlays with Family depth, ownership, invalidation and health. Curved frame overlay remains intentionally unsupported/runtime-gated.")
