#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectMaterialCatalog.cs"
text = SOURCE.read_text(encoding="utf-8")

required_start = text.find("private static string Required(string value, string name, int max)")
optional_start = text.find("private static string Optional(string value, string name, int max)")
if required_start < 0 or optional_start < 0 or optional_start <= required_start:
    raise SystemExit("FAIL: ProjectMaterial Required/Optional validation helpers not found")
required = text[required_start:optional_start]

raw_capture = "var raw = value ?? string.Empty;"
raw_control = "if (raw.Any(char.IsControl))"
trim = "var text = raw.Trim();"
legacy = "var text = (value ?? string.Empty).Trim();"

for label, token in (
    ("raw capture", raw_capture),
    ("raw control rejection", raw_control),
    ("ordinary-space normalization", trim),
    ("Unicode validation", "RequireWellFormedUnicode(text, name);"),
    ("XML validation", "RequireXmlText(text, name);"),
):
    if token not in required:
        raise SystemExit(f"FAIL: Material Required guard missing {label}: {token}")

if required.find(raw_control) >= required.find(trim):
    raise SystemExit("FAIL: Material name raw control validation must occur before Trim normalization")
if legacy in required:
    raise SystemExit("FAIL: legacy Material Required trim-before-control-validation path has returned")

for caller in (
    "Name = Required(name, nameof(name), 120);",
    "Id = RequireMaterialId(id, nameof(id));",
    "Unit = Optional(unit, nameof(unit), 24);",
    "Description = Optional(description, nameof(description), 240);",
):
    if caller not in text:
        raise SystemExit(f"FAIL: expected Material validation caller missing: {caller}")

id_start = text.find("internal static string RequireMaterialId(string value, string name)")
if id_start < 0:
    raise SystemExit("FAIL: RequireMaterialId contract missing")
id_guard = text[id_start:required_start]
if raw_capture not in id_guard or raw_control not in id_guard or "return Required(raw, name, 64);" not in id_guard:
    raise SystemExit("FAIL: Material ID raw-control/canonical validation contract changed")

optional = text[optional_start:text.find("private static void RequireWellFormedUnicode", optional_start)]
if "var text = (value ?? string.Empty).Trim();" not in optional:
    raise SystemExit("FAIL: Material Unit/Description Optional semantics changed unexpectedly")

print("PASS: Material names reject raw control characters before Trim normalization")
print("PASS: Material ID raw-control contract remains explicit")
print("PASS: Unit/Description Optional normalization remains unchanged")
print("NOTE: Core/source guard only; no licensed BricsCAD runtime PASS is claimed")
