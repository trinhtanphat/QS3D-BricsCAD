#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs",
    "src/QS3D.Core/Rebar/BeamLongitudinalRebarPlanner.cs",
    "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs",
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
        "Refusing to orphan or overwrite rebar ownership", "CadGeometryGuard.Midpoint(line.StartPoint.Z, line.EndPoint.Z",
    ):
        if needle not in text: errors.append("beam rebar solid builder missing: " + needle)
    if "CadGeometryGuard.Multiply" in text: errors.append("beam rebar builder references nonexistent CadGeometryGuard.Multiply")

command = ROOT / "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs"
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in ('[CommandMethod("QS3DBEAMREBAR3D"', "BeamRebarSolidBuilder.BuildSelected", "RebarBeamTopCount", "RebarBeamBottomCount"):
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
print("PASS: deterministic Beam top/bottom longitudinal layout, guarded LINE Solid3d adapter, dedicated command and regression source are present.")
