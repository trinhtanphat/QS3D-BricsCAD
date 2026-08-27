#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.exists():
        print(f"[FAIL] missing {path}")
        sys.exit(1)
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        print(f"[FAIL] {label}: missing {token}")
        sys.exit(1)


builder = read("src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs")
command = read("src/QS3D.BricsCAD.V25/SlabMeshCommands.cs")
planner = read("src/QS3D.Core/Rebar/PolygonalSlabMeshPlanner.cs")
health = read("src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs")
mode_health = read("src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs")
invalidator = read("src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs")

for token in [
    "TryReadRectangle(document, element, polyline)",
    "RectangularSlabMeshPlanner.Plan",
    "PolygonalSlabMeshPlanner.Plan",
    "ReadPolygonFootprint",
    "PolygonGlobalXY",
    "RectangleLocalXY",
    'GeneratedSlabMeshFootprintMode',
    "ValidateCommonFootprint",
    "polygonal Slab mesh chưa hỗ trợ bulge/curved boundary",
    "polygonal Slab mesh hiện yêu cầu plan-view POLYLINE có normal +Z",
    "ProjectStateSnapshot.Capture(project)",
    "GeneratedRebarOwnershipGuard.Build(project)",
    "ErasePrevious(document, transaction, project, element, ownership)",
    "GeneratedRebarNativeOwnershipService.MarkGenerated(document, transaction, bar, update.Project, element, HandlesKey",
    "CommitSemanticUpdate(project, update)",
    "transaction.Commit()",
    "rollback.Restore(project)",
]:
    require(builder, token, "polygonal slab adapter")

if builder.index("RectangularSlabMeshPlanner.Plan") > builder.index("PolygonalSlabMeshPlanner.Plan"):
    print("[FAIL] rectangle compatibility path must remain explicit before polygon fallback")
    sys.exit(1)

for token in [
    "PolygonRegionScanlineClipper.NormalizeAndValidate",
    "MaxBars = 8192",
    "SubtractBoundaryClearance",
]:
    require(planner, token, "Core polygon planner")

require(command, "closed straight-segment plan-view POLYLINE", "command guidance")
require(command, "Rectangle giữ local-axis legacy; polygon dùng drawing X/Y", "axis contract")
require(health, '"GeneratedSlabMeshMode"', "slab health metadata")
require(mode_health, 'RequireExactMode(element, "GeneratedSlabMeshMode", "SlabMeshXY"', "mode compatibility")
require(invalidator, "CoreOwnershipPolicy.RebarHandleKeys", "generated invalidation/ownership")
require(invalidator, "GeneratedRebarNativeOwnershipService.RequireMatchingOwnership", "native rebar ownership validation")

if 'private const string Mode = "SlabMeshXY"' not in builder:
    print("[FAIL] polygon footprint must not invent a new generated rebar mode")
    sys.exit(1)

print("[PASS] QS3DSLABREBAR3D preserves rotated-rectangle local-axis behavior, adds guarded simple-polygon fallback, and verifies project-aware native ownership before replacement")
