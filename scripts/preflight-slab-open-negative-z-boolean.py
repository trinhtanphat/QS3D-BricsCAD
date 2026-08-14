#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message):
    print("ERROR:", message)
    return 1


def require(path, tokens):
    if not path.is_file():
        raise RuntimeError(f"missing slabOpen surface: {path.relative_to(ROOT)}")
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            raise RuntimeError(f"{path.relative_to(ROOT)} missing contract token: {token}")
    return text


def main():
    try:
        contract = require(
            ROOT / "src/QS3D.Core/Domain/SlabOpeningContract.cs",
            (
                'FamilyKey = "slabOpen"',
                'HostSlabIdKey = "HostSlabId"',
                "IsSlabOpenFamily",
                "ElementCategory.WallOpening",
                "ElementCategory.Slab",
            ),
        )
        planner = require(
            ROOT / "src/QS3D.Core/Geometry/SlabOpeningCutPlanner.cs",
            (
                "CutterTopM",
                "CutterBottomM",
                "ExtrusionZM = -cutterHeightM",
                "input.HostBottomM - input.ClearanceM",
            ),
        )
        boolean = require(
            ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabOpeningBooleanService.cs",
            (
                "SlabOpeningContract.IsSlabOpening",
                "SlabOpeningCutPlanner.Plan",
                "plan.ExtrusionZM",
                "extrusionDrawing < 0d",
                "new Vector3d(0d, 0d, extrusionDrawing)",
                "BooleanOperationType.BoolSubtract",
                "GeneratedGeometryService.RequireMatchingOwnership",
            ),
        )
        direct = require(
            ROOT / "src/QS3D.BricsCAD.V25/DirectDrawSlabOpeningCommands.cs",
            (
                'CommandMethod("QS3DDRAWSLABOPEN"',
                'CommandMethod("QS3DDRAWSLABOPENADV"',
                "CadSelectionGuard.ReadImpliedSelection",
                "ElementCategory.Slab",
                "SlabOpeningContract.Bind",
                "SlabOpeningBooleanService.CutLinkedOpening",
            ),
        )
        routing = require(
            ROOT / "src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs",
            (
                "SlabOpeningContract.IsSlabOpenFamily(family)",
                "expectedSlabOpenRouting",
                "currentSlabOpenRouting",
                "new DirectDrawSlabOpeningCommands().DrawSlabOpeningAdvanced()",
                "new DirectDrawSlabOpeningCommands().DrawSlabOpening()",
            ),
        )
        health = require(
            ROOT / "src/QS3D.Core/Diagnostics/ModelHealthService.cs",
            (
                "SlabOpeningContract.IsSlabOpenFamily(family)",
                'slabOpening ? SlabOpeningContract.HostSlabIdKey : "HostWallId"',
                "host.Category != ElementCategory.Slab",
                "else if (!IsWall(host.Category))",
            ),
        )
        rules = require(
            ROOT / "src/QS3D.Core/Diagnostics/QsHostOpeningIntegrityRuleFamily.cs",
            (
                '"MISSING_HOST"',
                '"INVALID_HOST"',
                '"INVALID_HOST_CATEGORY"',
            ),
        )
        wall_boolean = require(
            ROOT / "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs",
            ("HostWallId", "BooleanOperationType.BoolSubtract"),
        )
    except RuntimeError as exc:
        return fail(str(exc))

    if "HostWallIdKey" in contract:
        return fail("slabOpen contract must not fake a HostWallId alias")
    if "HostWallId" in boolean:
        return fail("dedicated slabOpen boolean service must not fall back to HostWallId")
    if "HostWallId" in direct:
        return fail("slabOpen Direct Draw must bind HostSlabId, not fake HostWallId")
    if "DirectDrawOpeningCommands" not in routing:
        return fail("ordinary WallOpening routing must remain present")
    if "HostWallId" not in health:
        return fail("ordinary Door/WallOpening HostWallId health path must remain present")
    if "SlabOpening" in wall_boolean or "HostSlabId" in wall_boolean:
        return fail("existing wall OpeningBooleanService must remain slabOpen-independent")
    if "ExtrusionZM = cutterHeightM" in planner:
        return fail("slabOpen planner must not regress to positive-Z extrusion")

    print(
        "PASS: slabOpen exact-family routing, HostSlabId semantics, negative-Z cutter, "
        "automatic BoolSubtract, and ordinary wall-host preservation are pinned. "
        "NATIVE_RUNTIME=LOCAL_ONLY"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
