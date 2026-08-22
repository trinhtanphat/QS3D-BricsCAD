#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedTieRebarHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'throw new InvalidOperationException("Tie rebar health cannot inspect a null project element.")',
        "BuildOwnershipIndex(project)",
        '"TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT"',
        '"TIE_REBAR_GENERATED_COUNT_MISMATCH"',
        '"TIE_REBAR_GENERATED_STALE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing tie-rebar null-health contract token: " + token)
    if "if (element == null) continue;" in text:
        errors.append("tie-rebar health still silently skips null project elements")

if not AGGREGATE.is_file():
    errors.append("missing ComprehensiveModelHealthService source")
else:
    text = AGGREGATE.read_text(encoding="utf-8")
    required = (
        'new DiagnosticProvider("GeneratedTieRebarHealthService", () => new GeneratedTieRebarHealthService().Inspect(project, normalizedLiveGeneratedSolidHandles))',
        '"HEALTH_PROVIDER_FAILED"',
        "exception is InvalidOperationException",
        "ExecuteProvider",
    )
    for token in required:
        if token not in text:
            errors.append("missing aggregate tie-rebar fail-visible provider token: " + token)

print("QS3D tie-rebar standalone null-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone tie-rebar health rejects null entries and aggregate health remains fail-visible.")
