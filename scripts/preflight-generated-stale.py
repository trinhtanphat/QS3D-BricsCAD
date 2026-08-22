#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Domain/ProjectElement.cs": [
        "GeneratedSlabMeshStateKey",
        "GeneratedWallMeshStateKey",
        "GeneratedCurtainFrameStateKey",
        "GeneratedSlabMeshStaleSnapshotKey",
        "GeneratedWallMeshStaleSnapshotKey",
        "GeneratedCurtainFrameStaleSnapshotKey",
        "GeneratedSlabMeshHandles",
        "GeneratedWallMeshHandles",
        "GeneratedCurtainFrameHandles",
        "IsGeneratedSlabMeshStale",
        "IsGeneratedWallMeshStale",
        "IsGeneratedCurtainFrameStale",
        "ClearGeneratedSlabMeshStale",
        "ClearGeneratedWallMeshStale",
        "ClearGeneratedCurtainFrameStale",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs": [
        "SLAB_MESH_GENERATED_STALE",
        "WALL_MESH_GENERATED_STALE",
        "CURTAIN_FRAME_GENERATED_STALE",
    ],
    "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs": [
        'element.Properties["GeneratedSlabMeshHandles"]',
        'element.Properties["GeneratedWallMeshHandles"]',
        'element.Properties["GeneratedCurtainFrameHandles"]',
        "IsGeneratedSlabMeshStale",
        "IsGeneratedWallMeshStale",
        "IsGeneratedCurtainFrameStale",
        "Equal(8, issues.Count)",
        'Contains(issues, "SLAB_MESH_GENERATED_STALE")',
        'Contains(issues, "WALL_MESH_GENERATED_STALE")',
        'Contains(issues, "CURTAIN_FRAME_GENERATED_STALE")',
    ],
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs": [
        "GeneratedGeometryStaleSmoke.Run();",
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

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: host, rebar, tie/stirrup, slab/wall mesh and curtain-frame outputs all participate in stale lifecycle, health and registered smoke coverage.")
