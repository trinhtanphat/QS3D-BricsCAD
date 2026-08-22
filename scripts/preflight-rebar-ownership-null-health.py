#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedRebarOwnershipHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'throw new InvalidOperationException("Generated rebar ownership health cannot inspect a null project element.")',
        '"REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT"',
        "GeneratedHandleOwnershipPolicy.RebarHandleKeys",
    )
    for token in required:
        if token not in text:
            errors.append("missing rebar-ownership null-health contract token: " + token)
    if "if (element == null) continue;" in text:
        errors.append("rebar-ownership health still silently skips null project elements")

if not AGGREGATE.is_file():
    errors.append("missing ComprehensiveModelHealthService source")
else:
    text = AGGREGATE.read_text(encoding="utf-8")
    required = (
        'new DiagnosticProvider("GeneratedRebarOwnershipHealthService", () => new GeneratedRebarOwnershipHealthService().Inspect(project))',
        '"HEALTH_PROVIDER_FAILED"',
        "exception is InvalidOperationException",
        "ExecuteProvider",
    )
    for token in required:
        if token not in text:
            errors.append("missing aggregate rebar-ownership fail-visible provider token: " + token)

print("QS3D rebar-ownership standalone null-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone rebar-ownership health rejects null entries and aggregate health remains fail-visible.")
