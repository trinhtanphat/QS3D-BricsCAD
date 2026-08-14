#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs"
AGGREGATE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedGridAnnotationHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'throw new InvalidOperationException("Grid annotation health cannot inspect a null project element.")',
        '"GRID_ANNOTATION_HANDLE_INVALID"',
        '"GRID_ANNOTATION_HANDLE_COUNT"',
        '"GRID_ANNOTATION_LABEL_STALE"',
        '"GRID_ANNOTATION_TEXT_TOO_LARGE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing grid-annotation null-health contract token: " + token)
    if "if (element == null) continue;" in text:
        errors.append("grid-annotation health still silently skips null project elements")

if not AGGREGATE.is_file():
    errors.append("missing ComprehensiveModelHealthService source")
else:
    text = AGGREGATE.read_text(encoding="utf-8")
    required = (
        'new DiagnosticProvider("GeneratedGridAnnotationHealthService", () => new GeneratedGridAnnotationHealthService().Inspect(project))',
        '"HEALTH_PROVIDER_FAILED"',
        "exception is InvalidOperationException",
        "ExecuteProvider",
    )
    for token in required:
        if token not in text:
            errors.append("missing aggregate grid-annotation fail-visible provider token: " + token)

print("QS3D grid-annotation standalone null-health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone grid-annotation health rejects null entries and aggregate health remains fail-visible.")
