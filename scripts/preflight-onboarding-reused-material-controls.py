#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectOnboardingService.cs"
text = SOURCE.read_text(encoding="utf-8")

method_start = text.find("private static bool HasTrustedMaterial(ProjectFamily family)")
if method_start < 0:
    raise SystemExit("FAIL: HasTrustedMaterial(...) not found")
method_end = text.find("private static string TrustedMaterial(ProjectFamily family)", method_start)
if method_end < 0:
    raise SystemExit("FAIL: cannot bound HasTrustedMaterial(...)")
method = text[method_start:method_end]

required = {
    "raw existing material capture": "var material = raw ?? string.Empty;",
    "raw control rejection": "if (material.Any(char.IsControl)) return false;",
    "ordinary-space normalization": "material = material.Trim();",
    "shared material validation": "ValidateMaterial(material, family.Category);",
}
for label, token in required.items():
    if token not in method:
        raise SystemExit(f"FAIL: reused-material trust guard missing {label}: {token}")

control_index = method.find("if (material.Any(char.IsControl)) return false;")
trim_index = method.find("material = material.Trim();")
if control_index < 0 or trim_index < 0 or control_index >= trim_index:
    raise SystemExit("FAIL: reused Family material must reject raw controls before Trim normalization")

legacy = "var material = (raw ?? string.Empty).Trim();"
if legacy in method:
    raise SystemExit("FAIL: legacy trim-before-trust-validation path has returned")

trusted_start = text.find("private static string TrustedMaterial(ProjectFamily family)")
if trusted_start < 0:
    raise SystemExit("FAIL: TrustedMaterial(...) not found")
trusted_end = text.find("private static FloorDefinition? ResolveExistingFloorActivationPlan", trusted_start)
trusted_method = text[trusted_start:trusted_end]
if "if (!HasTrustedMaterial(family)) return string.Empty;" not in trusted_method:
    raise SystemExit("FAIL: TrustedMaterial must remain gated by HasTrustedMaterial")

print("PASS: reused Family material rejects raw controls before trust normalization")
print("PASS: ordinary surrounding-space normalization and shared validation remain intact")
print("PASS: TrustedMaterial remains gated by HasTrustedMaterial")
print("NOTE: Core/source guard only; no licensed BricsCAD runtime PASS is claimed")
