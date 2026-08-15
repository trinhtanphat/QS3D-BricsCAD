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
                'Add(bottom, -clearance, "cutter bottom")',
                'Finite(-cutterHeight, "negative-Z extrusion")',
                "ExtrusionZM = extrusionZ",
            ),
        )
        boolean = require(
            ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabOpeningBooleanService.cs",
            (
                "SlabOpeningContract.IsSlabOpening",
                "host.IsGeneratedSolidStale()",
                "Build 3D again before slabOpen subtraction.",
                "SlabOpeningCutPlanner.Plan",
                "var sourceGeometryFingerprint = PolylineFingerprint(openingSource);",
                "sourceGeometryFingerprint,",
                '"slabOpen-v2"',
                '"polyline-v1"',
                "polyline.NumberOfVertices",
                "polyline.GetPoint2dAt(index)",
                "polyline.GetBulgeAt(index)",
                "polyline.Elevation",
                "polyline.Normal.X",
                "polyline.Normal.Y",
                "polyline.Normal.Z",
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
                "EnsureFirstUseHostSolid(document, project, host, selectedHostHandle);",
                'host.Properties.TryGetValue("GeneratedSolidHandle", out var existingHandle)',
                "StructuralSolidBuilder.BuildSelected(document, project, ElementCategory.Slab)",
                'host.Properties.TryGetValue("GeneratedSolidHandle", out var generatedHandle)',
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
        require(
            ROOT / "src/QS3D.Core/Diagnostics/QsHostOpeningIntegrityRuleFamily.cs",
            (
                '"MISSING_HOST"',
                '"INVALID_HOST"',
                '"INVALID_HOST_CATEGORY"',
                "semantic contract",
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
    if "Finite(cutterHeight" in planner or "ExtrusionZM = cutterHeight" in planner:
        return fail("slabOpen planner must not regress to positive-Z extrusion")
    if '"slabOpen-v1"' in boolean:
        return fail("slabOpen applied fingerprint must stay on the live-footprint-aware v2 contract")

    geometry = boolean.find("var sourceGeometryFingerprint = PolylineFingerprint(openingSource);")
    fingerprint = boolean.find("var fingerprint = Fingerprint(", geometry)
    same_solid = boolean.find("AppliedSolidHandleKey", fingerprint)
    no_op = boolean.find("return 0;", same_solid)
    if min(geometry, fingerprint, same_solid, no_op) < 0 or not (geometry < fingerprint < same_solid < no_op):
        return fail("slabOpen must fingerprint live footprint geometry before the same-solid idempotence decision")

    vertex = boolean.find("polyline.GetPoint2dAt(index)")
    bulge = boolean.find("polyline.GetBulgeAt(index)", vertex)
    if vertex < 0 or bulge < 0 or vertex > bulge:
        return fail("slabOpen footprint fingerprint must include live vertices and bulges")

    auto_build = direct.find("EnsureFirstUseHostSolid(document, project, host, selectedHostHandle);")
    rollback = direct.find("var rollback = ProjectStateSnapshot.Capture(project);")
    if auto_build < 0 or rollback < 0 or auto_build > rollback:
        return fail("slabOpen first-use host auto-build must happen before the opening rollback snapshot")
    existing_guard = direct.find('host.Properties.TryGetValue("GeneratedSolidHandle", out var existingHandle)')
    builder = direct.find("StructuralSolidBuilder.BuildSelected(document, project, ElementCategory.Slab)")
    if existing_guard < 0 or builder < 0 or existing_guard > builder:
        return fail("slabOpen must preserve an existing generated host and auto-build only the first-use missing-solid case")

    print(
        "PASS: slabOpen exact-family routing, first-use host auto-build, HostSlabId semantics, live footprint-aware "
        "physical-cut freshness, negative-Z cutter, automatic BoolSubtract, stale-existing-host fail-closed behavior, "
        "and ordinary wall-host preservation are pinned. NATIVE_RUNTIME=LOCAL_ONLY"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
