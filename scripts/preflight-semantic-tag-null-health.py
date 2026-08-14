#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedSemanticTagHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'throw new InvalidOperationException("Semantic tag health cannot inspect a null project element.")',
        '"SEMANTIC_TAG_RENDER_INVALID"',
        "catch (Exception ex) when (IsDiagnosticDataFailure(ex))",
        '"SEMANTIC_TAG_HANDLE_INVALID"',
        '"SEMANTIC_TAG_POSITION_INVALID"',
    )
    for token in required:
        if token not in text:
            errors.append("missing semantic-tag null-health contract token: " + token)
    if "if (element == null) continue;" in text:
        errors.append("semantic-tag health still silently skips null project elements")

if not AGGREGATE.is_file():
    errors.append("missing ComprehensiveModelHealthService source")
else:
    text = AGGREGATE.read_text(encoding="utf-8")
    required = (
        'new DiagnosticProvider("GeneratedSemanticTagHealthService", () => new GeneratedSemanticTagHealthService().Inspect(project))',
        '"HEALTH_PROVIDER_FAILED"',
        "exception is InvalidOperationException",
        "ExecuteProvider",
    )
    for token in required:
        if token not in text:
            errors.append("missing aggregate fail-visible provider token: " + token)

print("QS3D semantic-tag standalone null-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone semantic-tag health rejects null entries and aggregate health remains fail-visible.")
