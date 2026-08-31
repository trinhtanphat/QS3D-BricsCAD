#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs",
    "src/QS3D.Core/Rebar/BeamLongitudinalRebarPlanner.cs",
    "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs",
    "tests/QS3D.Core.SmokeTests/BeamRebarRegressionSmoke.cs",
]
for rel in required:
    if not (ROOT / rel).is_file(): errors.append("missing beam-rebar file: " + rel)

planner = ROOT / "src/QS3D.Core/Rebar/BeamLongitudinalRebarPlanner.cs"
if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in ("LinearRebarLayoutPlanner.Plan", "TopCount", "BottomCount", "ActualSpacingM", "TopElevationM", "BottomElevationM"):
        if needle not in text: errors.append("beam longitudinal planner missing: " + needle)

builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs"
if builder.is_file():
    text = builder.read_text(encoding="utf-8")
    for needle in (
        "ElementCategory.Beam", "BeamLongitudinalRebarPlanner.Plan", "RebarBeamTopCount", "RebarBeamBottomCount",
        "RebarBeamEndCoverM", "RebarBeamDiameterMm", "Matrix3d.Rotation(Math.PI / 2d", '"BeamLongitudinalBars"',
        "GeneratedRebarOwnershipGuard.Build(project)", 'ownership.EnsureOwned(handle, element, "GeneratedRebarHandles")',
        "MaxBarsPerElement = 1024", "MaxBarsPerBatch = 4096",
        "BuildSelected(Document document, ProjectState project, ObjectId[] selectedIds)",
        "if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));",
        "if (selectedIds.Length == 0) return 0;", "var ids = (ObjectId[])selectedIds.Clone();",
        "CadElementVerticalPlacement.Resolve(", "vertical.CenterDrawing", "GeneratedRebar", "CadGeometryGuard.Finite(nx * localX",
        "var halfBarLength = CadGeometryGuard.Finite(barLength / 2d", "var longitudinalCenterX = CadGeometryGuard.Add(startX",
        "var longitudinalCenterY = CadGeometryGuard.Add(startY", "CadGeometryGuard.Add(longitudinalCenterX",
        "CadGeometryGuard.Add(longitudinalCenterY",
    ):
        if needle not in text: errors.append("beam rebar solid builder missing: " + needle)
    for forbidden in ("document.Editor.SelectImplied()", "document.Editor.GetSelection()", "document.Editor.SetImpliedSelection", "PromptStatus"):
        if forbidden in text:
            errors.append("beam rebar builder must consume the admitted selection snapshot without editor re-selection: " + forbidden)
    if "CadGeometryGuard.Multiply" in text: errors.append("beam rebar builder references nonexistent CadGeometryGuard.Multiply")
    if "var x = CadGeometryGuard.Add(startX" in text or "var y = CadGeometryGuard.Add(startY" in text:
        errors.append("beam rebar builder must translate centered frustums to the usable-bar midpoint before transverse placement")

command = ROOT / "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs"
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in ('[CommandMethod("QS3DBEAMREBAR3D"', "CadSelectionGuard.AcquireCurrentSelection(document)", "BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds)", "RebarBeamTopCount", "RebarBeamBottomCount"):
        if needle not in text: errors.append("beam rebar command missing: " + needle)

smoke = ROOT / "tests/QS3D.Core.SmokeTests/BeamRebarRegressionSmoke.cs"
if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in ("SymmetricFourBarLayout();", "AsymmetricLayerCounts();", "RejectsOvercrowdedLayer();", "RejectsCollapsedVerticalEnvelope();"):
        if needle not in text: errors.append("beam rebar regression missing: " + needle)

print("QS3D beam-rebar preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: deterministic Beam top/bottom longitudinal layout, guarded ownership/caps, one admitted selection snapshot through the Solid3d adapter, dedicated command and regression source are present.")
