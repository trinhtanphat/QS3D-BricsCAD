#!/usr/bin/env python3
from pathlib import Path
import math
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedBeamStirrupHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "var expected = centerline * validCount;",
        "if (double.IsNaN(expected) || double.IsInfinity(expected))",
        'InvalidMetadata(element, TotalCenterlineKey + " expected value overflowed the finite numeric range.")',
        "var tolerance = Math.Max(1e-9d, Math.Abs(expected) * 1e-9d);",
    )
    for token in required:
        if token not in text:
            errors.append("missing beam-stirrup overflow guard token: " + token)

    expected_pos = text.find("var expected = centerline * validCount;")
    guard_pos = text.find("if (double.IsNaN(expected) || double.IsInfinity(expected))")
    tolerance_pos = text.find("var tolerance = Math.Max(1e-9d, Math.Abs(expected) * 1e-9d);")
    if expected_pos < 0 or guard_pos < expected_pos or tolerance_pos < guard_pos:
        errors.append("beam-stirrup overflow guard must run before tolerance comparison")

fixture = 1e308 * 2
if not math.isinf(fixture):
    errors.append("overflow regression fixture no longer produces infinity")

print("QS3D beam-stirrup length-overflow preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: beam-stirrup health rejects non-finite derived expected lengths before tolerance comparison.")
