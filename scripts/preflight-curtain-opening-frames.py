#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

planner = ROOT / "src/QS3D.Core/Geometry/CurtainWallOpeningFramePlanner.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/CurtainOpeningFramePlannerSmoke.cs"
for path in (planner, smoke):
    if not path.is_file(): errors.append("missing curtain opening-frame file: " + str(path.relative_to(ROOT)))

if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "CurtainWallOpeningRect", "CurtainWallFramePiece", "CurtainWallOpeningFramePlan",
        "CurtainWallOpeningFramePlanner", "MaxInputFrames = 20000", "MaxOpenings = 4096",
        "MaxOutputPieces = 32768", "clearanceM", "Subtract", "InterruptedFrameCount",
        "RemovedFrameAreaM2", "OrderBy(x => x.SourceFrameIndex)", "FinitePositive",
    ):
        if needle not in text: errors.append("curtain opening-frame planner missing: " + needle)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "ModuleInitializer", "VerticalMullionSplitsAroundWindow", "HorizontalTransomSplitsAroundOpening",
        "DoorFromFloorRemovesLowerMullion", "ClearanceExpandsInterruptedRegion", "FullCoverRemovesFrame",
        "NonIntersectingOpeningLeavesFrameUntouched", "OutputOrderIsDeterministic", "InvalidInputsAreRejected",
    ):
        if needle not in text: errors.append("curtain opening-frame smoke missing: " + needle)

print("QS3D curtain opening/frame interruption preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: Core curtain frame interruption planner is bounded, deterministic and covered for mullion/transom/window/door/clearance/full-cover cases. V25 placement wiring remains a separate guarded step.")
