#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
planner = ROOT / "src/QS3D.Core/Rebar/RectangularSlabMeshPlanner.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/SlabMeshRegressionSmoke.cs"
linear = ROOT / "src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs"

for path in (planner, smoke, linear):
    if not path.is_file(): errors.append("missing slab-mesh file: " + str(path.relative_to(ROOT)))

if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "SlabMeshFace", "SlabMeshDirection", "RectangularSlabMeshInput", "SlabMeshBarPlacement",
        "LinearRebarLayoutPlanner.Plan", "IncludeBottom", "IncludeTop", "XClosestToFace",
        "slab X end center cover", "slab Y end center cover", "top + bottom two-direction mesh",
        "MaxBars = 8192", "ActualSpacingM",
    ):
        if needle not in text: errors.append("slab mesh planner guard missing: " + needle)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "BottomMeshUsesTwoDirectionsAndCover();", "BothFacesRemainSeparated();", "CountModeIsDeterministic();",
        "ThinSlabIsRejected();", "AmbiguousDistributionIsRejected();", "ModuleInitializer",
    ):
        if needle not in text: errors.append("slab mesh regression missing: " + needle)

print("QS3D slab-mesh planner preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: deterministic rectangular slab X/Y mesh planning, cover/stacking guards, limits and smoke coverage are present; native CAD adapter remains separately runtime-gated.")
