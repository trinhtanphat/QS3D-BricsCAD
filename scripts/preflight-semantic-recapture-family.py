#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
service = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs"
health = ROOT / "src/QS3D.Core/Diagnostics/ModelHealthService.cs"
errors = []

if not service.is_file():
    errors.append("missing SemanticCaptureService.cs")
else:
    text = service.read_text(encoding="utf-8")
    required = (
        "family = project.FindFamily(element.FamilyId);",
        "if (family == null || family.Category != category)",
        "family = ResolveFamily(project, category);",
        "element.FamilyId = family.Id;",
    )
    for token in required:
        if token not in text:
            errors.append("semantic recapture missing Family repair contract: " + token)

    unsafe = "family = project.FindFamily(element.FamilyId) ?? ResolveFamily(project, category);"
    if unsafe in text:
        errors.append("semantic recapture must not preserve dangling or wrong-category FamilyId via null-coalescing fallback")

if not health.is_file():
    errors.append("missing ModelHealthService.cs")
else:
    text = health.read_text(encoding="utf-8")
    for token in ('"MISSING_FAMILY"', '"FAMILY_CATEGORY_MISMATCH"'):
        if token not in text:
            errors.append("Model Health missing Family integrity signal: " + token)

print("QS3D semantic recapture Family preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: recapturing an existing semantic source repairs dangling or wrong-category Family references while preserving the existing element/instance state.")
