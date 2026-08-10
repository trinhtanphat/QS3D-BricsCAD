#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs",
    "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs",
    "src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs",
    "src/QS3D.Core/Export/CurtainWallXlsxExporter.cs",
    "tests/QS3D.Core.SmokeTests/CurtainWallDetailSmoke.cs",
    "tests/QS3D.Core.SmokeTests/GeneratedCurtainFrameHealthSmoke.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing curtain native file: " + relative)

planner = ROOT / "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs"
if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in ("MaxDetailSolids = 20000", "projectedDetailSolids", "layout.PanelCount", "BuildPanelCells", "solidCount != projectedDetailSolids"):
        if needle not in text: errors.append("curtain detail planner missing: " + needle)
    projection = text.find("projectedDetailSolids")
    panel_build = text.find("BuildPanelCells(verticalFrames, horizontalFrames)")
    if projection < 0 or panel_build < 0 or projection > panel_build:
        errors.append("Curtain detail solid budget must be checked before panel allocation.")

builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs"
if builder.is_file():
    text = builder.read_text(encoding="utf-8")
    for needle in (
        'HandlesKey = "GeneratedCurtainFrameHandles"', "GeneratedCurtainFrameOwnershipGuard.Build(project)",
        "ownership.EnsureOwned(handle, element)", "MaxFramesPerElement = 4096", "MaxFramesPerBatch = 8192",
        "document.Editor.GetSelection()", "document.Editor.SetImpliedSelection", "CurtainWallDetailPlanner.Plan",
        "CadGeometryGuard.Subtract", "CadGeometryGuard.Multiply", "CadGeometryGuard.Add", "CadGeometryGuard.Hypot",
        "GeneratedCurtainFrameSourceLengthM", "GeneratedCurtainFrameHeightM", "ClearGeneratedCurtainFrameStale",
        "Refusing destructive erase", "geometry.curtain.frames",
    ):
        if needle not in text: errors.append("curtain frame builder missing: " + needle)

health = ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs"
if health.is_file():
    text = health.read_text(encoding="utf-8")
    for needle in (
        "class OwnershipIndex", "HashSet<string> Conflicts", "ownership.Conflicts.Contains(handle)",
        "CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT", "CURTAIN_FRAME_GENERATED_SOLID_MISSING",
        "CURTAIN_FRAME_GRID_COUNT_MISMATCH", "CURTAIN_FRAME_GENERATED_STALE", "IsGeneratedCurtainFrameStale",
    ):
        if needle not in text: errors.append("curtain frame health missing: " + needle)

ownership = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs"
if ownership.is_file():
    text = ownership.read_text(encoding="utf-8")
    for needle in (
        "SourceHandles", "GeneratedSolidHandle", "PhysicalOpeningCutSolidHandle", "GeneratedRebarHandles",
        "GeneratedShapeRebarHandles", "GeneratedTieRebarHandles", "GeneratedBeamStirrupHandles",
        "GeneratedSlabMeshHandles", "GeneratedWallMeshHandles", "GeneratedCurtainFrameHandles", "EnsureOwned",
    ):
        if needle not in text: errors.append("curtain ownership guard missing: " + needle)

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
for command in ("QS3DCURTAINFRAMES3D", "QS3DCURTAINFRAMEHEALTH"):
    if commands.count(command) != 1:
        errors.append(command + " must have exactly one CommandMethod owner; found " + str(commands.count(command)))

exporter = ROOT / "src/QS3D.Core/Export/CurtainWallXlsxExporter.cs"
if exporter.is_file():
    text = exporter.read_text(encoding="utf-8")
    for needle in ("AtomicFileCommit.Replace", "inlineStr", "CurtainWallScheduleBuilder.Build"):
        if needle not in text: errors.append("curtain XLSX exporter missing: " + needle)

smoke = ROOT / "tests/QS3D.Core.SmokeTests/GeneratedCurtainFrameHealthSmoke.cs"
if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in ("ModuleInitializer", "LaterGeneratedOwnerStillConflictsWithCurtainFrames", "CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT"):
        if needle not in text: errors.append("curtain ownership regression missing: " + needle)

print("QS3D Curtain native safety preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: curtain native detail budget, finite frame geometry, selection fallback, cross-slot ownership health, atomic XLSX export and command wiring are present; BricsCAD V25 runtime remains separately gated.")
