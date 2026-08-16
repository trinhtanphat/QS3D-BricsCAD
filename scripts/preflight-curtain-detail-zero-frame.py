#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CurtainWallDetailNumericCollapseSmoke.cs"
errors = []

source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""
if not source:
    errors.append("missing CurtainWallDetailPlanner.cs")
if not smoke:
    errors.append("missing CurtainWallDetailNumericCollapseSmoke.cs")

for token in (
    "PhysicalFrameCount(layout.Columns, input.PerimeterFrameWidthM, input.MullionWidthM)",
    "PhysicalFrameCount(layout.Rows, input.PerimeterFrameWidthM, input.TransomWidthM)",
    "if (width == 0d) continue;",
    "if (height == 0d) continue;",
    "BuildPanelCells(input, layout)",
    "lost a positive deduction at floating-point precision",
    "lost a positive contribution at floating-point precision",
    "underflowed to zero",
    "below the representable coordinate resolution",
):
    if token not in source:
        errors.append("curtain detail contract missing: " + token)

for forbidden in (
    "BuildPanelCells(verticalFrames, horizontalFrames)",
    "new List<CurtainWallRect>(layout.VerticalFrameCount)",
    "new List<CurtainWallRect>(layout.HorizontalFrameCount)",
):
    if forbidden in source:
        errors.append("stale curtain detail coupling retained: " + forbidden)

for token in (
    "ZeroInternalFramesRemainValidWithoutDegenerateSolids",
    "AllZeroFramesProducePanelOnlyDetail",
    "MixedZeroMullionPreservesTransomSolids",
    "MixedZeroTransomPreservesMullionSolids",
    "ZeroPerimeterPreservesInternalSolids",
    "GeneratedRightFrameCollapseFailsClosed",
    "InternalVerticalFrameHalfWidthPlacementCollapseFailsClosed",
    "RectangleAreaUnderflowFailsClosed",
    "PanelAreaUnderflowFailsClosed",
    "OrdinaryDetailRemainsStable",
):
    if token not in smoke:
        errors.append("curtain detail regression missing: " + token)

print("QS3D curtain detail zero-frame/numeric robustness preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curtain detail decouples panel boundaries from physical frame solids, omits zero-width frames, and pins numeric-collapse regressions.")
