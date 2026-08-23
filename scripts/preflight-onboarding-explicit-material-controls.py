#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectOnboardingService.cs"
text = SOURCE.read_text(encoding="utf-8")

required = {
    "category-bound explicit material read":
        "ReadExplicitMaterial(request.StarterMaterials, category)",
    "raw material capture":
        "var material = raw ?? string.Empty;",
    "raw control-character rejection":
        "if (material.Any(char.IsControl))",
    "normal surrounding-space compatibility":
        "return material.Trim();",
}

for label, token in required.items():
    if token not in text:
        raise SystemExit(f"FAIL: onboarding explicit-material guard missing {label}: {token}")

method_start = text.find("private static string ReadExplicitMaterial(")
if method_start < 0:
    raise SystemExit("FAIL: ReadExplicitMaterial method not found")
method_end = text.find("private static void ValidateMaterial", method_start)
if method_end < 0:
    raise SystemExit("FAIL: cannot bound ReadExplicitMaterial method")
method = text[method_start:method_end]

control_index = method.find("if (material.Any(char.IsControl))")
trim_index = method.find("return material.Trim();")
if control_index < 0 or trim_index < 0 or control_index >= trim_index:
    raise SystemExit("FAIL: raw control-character validation must occur before Trim normalization")

legacy = "return (raw ?? string.Empty).Trim();"
if legacy in method:
    raise SystemExit("FAIL: legacy trim-before-validation explicit material path has returned")

print("PASS: explicit onboarding material rejects raw control characters before Trim normalization")
print("PASS: ordinary surrounding-space normalization remains explicit")
print("NOTE: source/static guard only; licensed BricsCAD runtime evidence is not claimed")
