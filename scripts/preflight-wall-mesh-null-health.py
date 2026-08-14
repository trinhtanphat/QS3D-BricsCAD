#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedWallMeshHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'throw new InvalidOperationException("Wall mesh health cannot inspect a null project element.")',
        "BuildOwnershipIndex(project)",
        '"WALL_MESH_GENERATED_OWNERSHIP_CONFLICT"',
        '"WALL_MESH_GENERATED_COUNT_MISMATCH"',
        '"WALL_MESH_GENERATED_STALE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing wall-mesh null-health contract token: " + token)
    if "if (element == null) continue;" in text:
        errors.append("wall-mesh health still silently skips null project elements")

if not AGGREGATE.is_file():
    errors.append("missing ComprehensiveModelHealthService source")
else:
    text = AGGREGATE.read_text(encoding="utf-8")
    required = (
        'new DiagnosticProvider("GeneratedWallMeshHealthService", () => new GeneratedWallMeshHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles))',
        '"HEALTH_PROVIDER_FAILED"',
        "exception is InvalidOperationException",
        "ExecuteProvider",
    )
    for token in required:
        if token not in text:
            errors.append("missing aggregate wall-mesh fail-visible provider token: " + token)

print("QS3D wall-mesh standalone null-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone wall-mesh health rejects null entries and aggregate health remains fail-visible.")
