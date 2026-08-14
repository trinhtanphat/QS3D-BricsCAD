#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedBeamStirrupHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'throw new InvalidOperationException("Beam stirrup health cannot inspect a null project element.")',
        "BuildOwnershipIndex(project)",
        '"BEAM_STIRRUP_GENERATED_OWNERSHIP_CONFLICT"',
        '"BEAM_STIRRUP_GENERATED_METADATA_INVALID"',
        '"BEAM_STIRRUP_GENERATED_STALE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing beam-stirrup null-health contract token: " + token)
    if "if (element == null) continue;" in text:
        errors.append("beam-stirrup health still silently skips null project elements")

if not AGGREGATE.is_file():
    errors.append("missing ComprehensiveModelHealthService source")
else:
    text = AGGREGATE.read_text(encoding="utf-8")
    required = (
        'new DiagnosticProvider("GeneratedBeamStirrupHealthService", () => new GeneratedBeamStirrupHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles))',
        '"HEALTH_PROVIDER_FAILED"',
        "exception is InvalidOperationException",
        "ExecuteProvider",
    )
    for token in required:
        if token not in text:
            errors.append("missing aggregate beam-stirrup fail-visible provider token: " + token)

print("QS3D beam-stirrup standalone null-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone beam-stirrup health rejects null entries and aggregate health remains fail-visible.")
