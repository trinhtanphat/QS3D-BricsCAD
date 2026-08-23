#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFloorService.cs"
text = SOURCE.read_text(encoding="utf-8")

method_start = text.find("private static string Required(string value, string parameterName, int maxLength)")
if method_start < 0:
    raise SystemExit("FAIL: ProjectFloorService.Required(...) not found")
method_end = text.find("private static double Finite(double value, string parameterName)", method_start)
if method_end < 0:
    raise SystemExit("FAIL: cannot bound ProjectFloorService.Required(...)")
required = text[method_start:method_end]

raw_capture = "var raw = value ?? string.Empty;"
raw_control = "if (raw.Any(char.IsControl))"
trim = "var text = raw.Trim();"
xml_check = "XmlConvert.VerifyXmlChars(text);"
legacy = "var text = (value ?? string.Empty).Trim();"

for label, token in (
    ("raw token capture", raw_capture),
    ("raw control-character rejection", raw_control),
    ("ordinary whitespace normalization", trim),
    ("XML validation", xml_check),
):
    if token not in required:
        raise SystemExit(f"FAIL: Floor Required guard missing {label}: {token}")

if required.find(raw_control) >= required.find(trim):
    raise SystemExit("FAIL: raw Floor token control validation must occur before Trim normalization")
if legacy in required:
    raise SystemExit("FAIL: legacy trim-before-control-validation Floor Required path has returned")

for public_call in (
    "var normalizedId = Required(id, nameof(id), 64);",
    "var normalizedName = Required(name, nameof(name), MaxNameLength);",
    "var normalized = Required(id, nameof(id), 64);",
):
    if public_call not in text:
        raise SystemExit(f"FAIL: expected Floor service Required call missing: {public_call}")

print("PASS: Floor Required rejects raw control characters before Trim normalization")
print("PASS: ordinary surrounding-space normalization and XML validation remain explicit")
print("PASS: create/name/find service boundaries still route through Required")
print("NOTE: Core/source guard only; no licensed BricsCAD runtime PASS is claimed")
