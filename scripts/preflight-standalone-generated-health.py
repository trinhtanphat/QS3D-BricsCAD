#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROVIDERS = [
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs",
]
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/StandaloneGeneratedHealthNullSafetySmoke.cs"
errors = []

for path in PROVIDERS + [SMOKE]:
    if not path.is_file():
        errors.append("missing standalone generated health contract file: " + str(path.relative_to(ROOT)))

for path in PROVIDERS:
    if path.is_file() and "if (element == null) continue;" in path.read_text(encoding="utf-8"):
        errors.append(path.name + " must fail visible instead of silently skipping null semantic entries.")

curtain = PROVIDERS[1]
if curtain.is_file():
    text = curtain.read_text(encoding="utf-8")
    guarded = "try\n            {\n                var family = project.FindFamily(element.FamilyId);"
    if guarded not in text:
        errors.append("GeneratedCurtainFrameHealthService must resolve Family inside config-validation try/catch.")
    marker = "CURTAIN_FRAME_CONFIG_INVALID"
    if marker not in text:
        errors.append("GeneratedCurtainFrameHealthService must report invalid/ambiguous config as a health issue.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    required = (
        "RequireFailVisible",
        "catch (InvalidOperationException)",
        "must reject a null semantic entry",
    )
    for token in required:
        if token not in text:
            errors.append("StandaloneGeneratedHealthNullSafetySmoke.cs missing fail-visible contract: " + token)
    for provider in (
        "GeneratedFoundationMeshHealthService",
        "GeneratedCurtainFrameHealthService",
        "GeneratedSemanticTagHealthService",
        "GeneratedGridAnnotationHealthService",
        "GeneratedRebarOwnershipHealthService",
    ):
        if provider not in text:
            errors.append("StandaloneGeneratedHealthNullSafetySmoke.cs missing provider: " + provider)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: standalone generated Core health providers reject corrupt null semantic entries fail visibly while curtain Family ambiguity remains contained as a diagnostic issue.")
