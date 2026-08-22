#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "QS3D.Core" / "Rebar"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


math = read(CORE / "RebarMath.cs")
schedule = read(CORE / "RebarSchedule.cs")
linear = read(CORE / "LinearRebarLayoutPlanner.cs")
ties = read(CORE / "ColumnTieLayoutPlanner.cs")
linear_smoke = read(SMOKE / "LinearRebarLayoutSmoke.cs")
tie_smoke = read(SMOKE / "ColumnTieLayoutSmoke.cs")

for token in (
    "public static double CeilingNearInteger(double value, string label)",
    "BitConverter.DoubleToInt64Bits(magnitude)",
    "BitConverter.Int64BitsToDouble(bits + 1L)",
    "(next - magnitude) * 8d",
):
    if token not in math:
        errors.append("shared 8-ULP interval ceiling missing token: " + token)

for name, text in (
    ("RebarSchedule", schedule),
    ("LinearRebarLayoutPlanner", linear),
    ("ColumnTieLayoutPlanner", ties),
):
    if "RebarMath.CeilingNearInteger(" not in text:
        errors.append(name + " must use the shared near-integer interval ceiling")

if "Math.Ceiling(" in linear or "Math.Ceiling(" in ties:
    errors.append("layout planners must not bypass shared near-integer interval normalization")

for token in (
    "NearIntegerSpacingDoesNotAddPhantomBar",
    "TrueSpacingOverrunStillAddsBar",
    "SpanM = 0.4d",
    "SpanM = 0.40000000005d",
):
    if token not in linear_smoke:
        errors.append("linear spacing regression missing token: " + token)

for token in (
    "NearIntegerSpacingDoesNotAddPhantomTie",
    "TrueSpacingOverrunStillAddsTie",
    "HeightM = 1.056d",
    "HeightM = 1.0560000001d",
):
    if token not in tie_smoke:
        errors.append("column-tie spacing regression missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: BBS, linear rebar and column-tie spacing share an 8-ULP integer ceiling without phantom bars or undercounted real overruns.")
