#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectZoneService.cs"
text = SOURCE.read_text(encoding="utf-8")

required_start = text.find("private static string Required(string value, string parameterName, int maxLength)")
if required_start < 0:
    raise SystemExit("FAIL: ProjectZoneService.Required(...) not found")
required = text[required_start:]

raw_capture = "var raw = value ?? string.Empty;"
raw_control = "if (raw.Any(char.IsControl))"
trim = "var text = raw.Trim();"
legacy = "var text = (value ?? string.Empty).Trim();"
xml_check = "XmlConvert.VerifyXmlChars(text);"

for label, token in (
    ("raw capture", raw_capture),
    ("raw control rejection", raw_control),
    ("ordinary-space normalization", trim),
    ("XML validation", xml_check),
):
    if token not in required:
        raise SystemExit(f"FAIL: Zone Required guard missing {label}: {token}")

if required.find(raw_control) >= required.find(trim):
    raise SystemExit("FAIL: Zone raw control validation must occur before Trim normalization")
if legacy in required:
    raise SystemExit("FAIL: legacy Zone trim-before-control-validation path has returned")

if "var canonical = Required(value, parameterName, maxLength);" not in text:
    raise SystemExit("FAIL: RequiredIdentity must continue to route through Required")
if "if (!string.Equals(value, canonical, StringComparison.Ordinal))" not in text:
    raise SystemExit("FAIL: RequiredIdentity canonical exactness guard missing")

for caller in (
    "var normalizedName = Required(name, nameof(name), MaxNameLength);",
    "var canonicalId = RequiredIdentity(id, nameof(id), 64);",
):
    if caller not in text:
        raise SystemExit(f"FAIL: expected Zone validation caller missing: {caller}")

print("PASS: Zone names reject raw control characters before Trim normalization")
print("PASS: Zone semantic identities retain RequiredIdentity canonical exactness")
print("PASS: ordinary surrounding-space name normalization and XML validation remain explicit")
print("NOTE: Core/source guard only; no licensed BricsCAD runtime PASS is claimed")
