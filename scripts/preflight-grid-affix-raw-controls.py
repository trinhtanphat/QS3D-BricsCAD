#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "GridNamingService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "GridNamingXmlFailureAtomicitySmoke.cs"

text = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

optional_start = text.find("private static string Optional(string? value, string name, int maxLength)")
if optional_start < 0:
    raise SystemExit("FAIL: GridNamingService.Optional helper not found")
optional = text[optional_start:]

raw_capture = "var raw = value ?? string.Empty;"
raw_control = "if (char.IsControl(raw[index]))"
trim = "var normalized = raw.Trim();"
legacy = "var normalized = value?.Trim() ?? string.Empty;"

for label, token in (
    ("raw capture", raw_capture),
    ("raw control scan", raw_control),
    ("post-validation Trim", trim),
    ("max affix length", "if (normalized.Length > maxLength)"),
    ("XML validation", "XmlConvert.VerifyXmlChars(normalized);"),
):
    if token not in optional:
        raise SystemExit(f"FAIL: Grid affix guard missing {label}: {token}")

if optional.find(raw_control) >= optional.find(trim):
    raise SystemExit("FAIL: Grid affix control validation must run before Trim normalization")
if legacy in optional:
    raise SystemExit("FAIL: legacy Grid affix trim-before-control path has returned")

required_start = text.find("private static string Required(string value, string name, int maxLength)")
if required_start < 0 or required_start > optional_start:
    raise SystemExit("FAIL: Grid ordered-id Required helper missing")
required = text[required_start:optional_start]
if "var normalized = value.Trim();" not in required:
    raise SystemExit("FAIL: ordered Grid element-ID Required semantics changed in issue-3560")

for marker in (
    'Prefix = "\\tG-"',
    'Prefix = "G-\\n"',
    'Suffix = "\\r-Y"',
    'Suffix = "-Y\\t"',
    "RawControlAffixesFailForFormatLabel();",
    "OrdinarySpaceAffixesStillNormalize();",
    'Require(label == "G-01-Y"',
):
    if marker not in smoke:
        raise SystemExit(f"FAIL: Grid raw-affix regression coverage missing: {marker}")

print("PASS: Grid prefix/suffix raw controls are rejected before Trim normalization")
print("PASS: Renumber atomicity and FormatLabel raw-control regressions are present")
print("PASS: ordinary SPACE normalization remains covered")
print("PASS: ordered Grid element-ID Required semantics remain out of scope")
print("NOTE: Core/source guard only; no licensed BricsCAD runtime PASS is claimed")
