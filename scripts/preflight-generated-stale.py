#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Domain/ProjectElement.cs": [
        "GeneratedSlabMeshStateKey",
        "GeneratedWallMeshStateKey",
        "GeneratedFoundationMeshStateKey",
        "GeneratedCurtainFrameStateKey",
        "GeneratedSlabMeshStaleSnapshotKey",
        "GeneratedWallMeshStaleSnapshotKey",
        "GeneratedFoundationMeshStaleSnapshotKey",
        "GeneratedCurtainFrameStaleSnapshotKey",
        "GeneratedSlabMeshHandles",
        "GeneratedWallMeshHandles",
        "GeneratedFoundationMeshHandles",
        "GeneratedCurtainFrameHandles",
        "IsGeneratedTieRebarStale",
        "IsGeneratedBeamStirrupStale",
        "IsGeneratedSlabMeshStale",
        "IsGeneratedWallMeshStale",
        "IsGeneratedFoundationMeshStale",
        "IsGeneratedCurtainFrameStale",
        "ClearGeneratedSlabMeshStale",
        "ClearGeneratedWallMeshStale",
        "ClearGeneratedFoundationMeshStale",
        "ClearGeneratedCurtainFrameStale",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs": [
        "TIE_REBAR_GENERATED_STALE",
        "BEAM_STIRRUP_GENERATED_STALE",
        "SLAB_MESH_GENERATED_STALE",
        "WALL_MESH_GENERATED_STALE",
        "FOUNDATION_MESH_GENERATED_STALE",
        "CURTAIN_FRAME_GENERATED_STALE",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs": [
        "element.IsGeneratedTieRebarStale()",
        "TIE_REBAR_GENERATED_STALE",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs": [
        "element.IsGeneratedBeamStirrupStale()",
        "BEAM_STIRRUP_GENERATED_STALE",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs": [
        "element.IsGeneratedSlabMeshStale()",
        "SLAB_MESH_GENERATED_STALE",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs": [
        "element.IsGeneratedWallMeshStale()",
        "WALL_MESH_GENERATED_STALE",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs": [
        "element.IsGeneratedFoundationMeshStale()",
        "FOUNDATION_MESH_GENERATED_STALE",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs": [
        "element.IsGeneratedCurtainFrameStale()",
        "CURTAIN_FRAME_GENERATED_STALE",
    ],
    "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs": [
        'element.Properties["GeneratedSlabMeshHandles"]',
        'element.Properties["GeneratedWallMeshHandles"]',
        'element.Properties["GeneratedFoundationMeshHandles"]',
        'element.Properties["GeneratedCurtainFrameHandles"]',
        "IsGeneratedSlabMeshStale",
        "IsGeneratedWallMeshStale",
        "IsGeneratedFoundationMeshStale",
        "IsGeneratedCurtainFrameStale",
        "Equal(9, issues.Count)",
        'Contains(issues, "SLAB_MESH_GENERATED_STALE")',
        'Contains(issues, "WALL_MESH_GENERATED_STALE")',
        'Contains(issues, "FOUNDATION_MESH_GENERATED_STALE")',
        'Contains(issues, "CURTAIN_FRAME_GENERATED_STALE")',
    ],
    "tests/QS3D.Core.SmokeTests/GeneratedOutputHealthStaleSmoke.cs": [
        "CurtainFramesUseSnapshotState",
        "ColumnTiesUseSnapshotState",
        "BeamStirrupsUseSnapshotState",
        "SlabMeshUsesSnapshotState",
        "WallMeshUsesSnapshotState",
        "FoundationMeshUsesSnapshotState",
        "NotContains(inspect(), code)",
        "element.MarkGeneratedGeometryStale",
        "Contains(inspect(), code)",
    ],
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs": [
        "GeneratedGeometryStaleSmoke.Run();",
        "GeneratedOutputHealthStaleSmoke.Run();",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing generated-stale file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing generated-stale guard/token: " + needle)

for relative in (
    "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
):
    path = ROOT / relative
    if path.is_file() and "element.Dirty != ElementDirtyFlags.None" in path.read_text(encoding="utf-8"):
        errors.append(relative + " still uses dirty-only stale detection instead of output snapshot state")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: host, rebar, tie/stirrup, slab/wall/foundation mesh and curtain-frame outputs use per-output stale snapshots; dirty-only health false positives are regression-gated.")
