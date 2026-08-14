#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedCurtainFrameHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'throw new InvalidOperationException("Curtain-frame diagnostics cannot inspect a project containing a null semantic element.")',
        'throw new InvalidOperationException("Curtain-frame diagnostics cannot build ownership for a project containing a null semantic element.")',
        "BuildOwnershipIndex(project)",
        '"CURTAIN_FRAME_GENERATED_OWNERSHIP_CONFLICT"',
        '"CURTAIN_FRAME_COUNT_MISMATCH"',
        '"CURTAIN_FRAME_GENERATED_STALE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing curtain-frame null-health contract token: " + token)
    if "if (element == null) continue;" in text:
        errors.append("curtain-frame health still silently skips null project elements")

if not AGGREGATE.is_file():
    errors.append("missing ComprehensiveModelHealthService source")
else:
    text = AGGREGATE.read_text(encoding="utf-8")
    required = (
        'new DiagnosticProvider("GeneratedCurtainFrameHealthService", () => new GeneratedCurtainFrameHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles))',
        '"HEALTH_PROVIDER_FAILED"',
        "exception is InvalidOperationException",
        "ExecuteProvider",
    )
    for token in required:
        if token not in text:
            errors.append("missing aggregate curtain-frame fail-visible provider token: " + token)

print("QS3D curtain-frame standalone null-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone curtain-frame health rejects null entries while valid diagnostics and aggregate fail-visible handling stay pinned.")
