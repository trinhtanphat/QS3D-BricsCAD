#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFamilyService.cs"
text = SOURCE.read_text(encoding="utf-8")

method_start = text.find("private static string Required(string value, string parameterName, int maxLength)")
if method_start < 0:
    raise SystemExit("FAIL: ProjectFamilyService.Required(...) not found")
method_end = text.find("private static string Value(string value, string parameterName, int maxLength)", method_start)
if method_end < 0:
    raise SystemExit("FAIL: cannot bound ProjectFamilyService.Required(...)")
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
        raise SystemExit(f"FAIL: Family Required guard missing {label}: {token}")

if required.find(raw_control) >= required.find(trim):
    raise SystemExit("FAIL: raw Family token control validation must occur before Trim normalization")
if legacy in required:
    raise SystemExit("FAIL: legacy trim-before-control-validation Family Required path has returned")

value_start = text.find("private static string Value(string value, string parameterName, int maxLength)")
if value_start < 0:
    raise SystemExit("FAIL: ProjectFamilyService.Value(...) not found")
value_method = text[value_start:]
if "var text = value ?? string.Empty;" not in value_method:
    raise SystemExit("FAIL: Family property-value boundary changed unexpectedly")
if "var text = (value ?? string.Empty).Trim();" in value_method:
    raise SystemExit("FAIL: Family property-value boundary must not gain Trim normalization")

print("PASS: Family Required rejects raw control characters before Trim normalization")
print("PASS: ordinary surrounding-space normalization and XML validation remain explicit")
print("PASS: Family property-value boundary remains untrimmed")
print("NOTE: Core/source guard only; no licensed BricsCAD runtime PASS is claimed")
