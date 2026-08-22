#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedRebarHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    null_guard = 'throw new InvalidOperationException("Generated rebar health cannot inspect a null project element.")'
    if text.count(null_guard) != 4:
        errors.append("Generated Rebar health must pin exactly four null fail-closed traversals")
    if "if (element == null) continue;" in text:
        errors.append("Generated Rebar health still silently skips null project elements")
    required = (
        "public IReadOnlyList<ModelHealthIssue> Inspect(",
        "public IReadOnlyList<ModelHealthIssue> InspectShape(",
        "public IReadOnlyList<ModelHealthIssue> InspectAll(",
        "BuildOwnershipIndex(project)",
        'CodePrefix = "REBAR"',
        'CodePrefix = "SHAPE_REBAR"',
        'spec.CodePrefix + "_GENERATED_OWNERSHIP_CONFLICT"',
        '"REBAR_GENERATED_DIAMETER_INVALID"',
    )
    for token in required:
        if token not in text:
            errors.append("missing Generated Rebar health contract token: " + token)

if not AGGREGATE.is_file():
    errors.append("missing ComprehensiveModelHealthService source")
else:
    text = AGGREGATE.read_text(encoding="utf-8")
    required = (
        'new DiagnosticProvider("GeneratedRebarHealthService", () => new GeneratedRebarHealthService().InspectAll(project, normalizedLiveGeneratedSolidHandles, normalizedLiveGeneratedSolidHandles))',
        '"HEALTH_PROVIDER_FAILED"',
        "exception is InvalidOperationException",
        "ExecuteProvider",
    )
    for token in required:
        if token not in text:
            errors.append("missing aggregate Generated Rebar fail-visible provider token: " + token)

print("QS3D Generated Rebar standalone null-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: all Generated Rebar health traversals reject null entries, shared code-prefix ownership diagnostics cover longitudinal/shape rebar, and aggregate health remains fail-visible.")
