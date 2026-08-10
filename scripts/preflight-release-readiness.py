#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipPolicy.cs": [
        "GeneratedHandleOwnershipPolicy",
        'PhysicalOpeningCutSolidHandle',
        'StartsWith("Generated"',
        'EndsWith("Handle"',
        'EndsWith("Handles"',
    ],
    "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs": [
        'CommandMethod("QS3DRELEASECHECK"',
        "ModelHealthService().Inspect",
        "SafeGeneratedHandleOwnershipHealthService().Inspect",
        "GeneratedRebarHealthService().InspectAll",
        "GeneratedTieRebarHealthService().Inspect",
        "GeneratedBeamStirrupHealthService().Inspect",
        "GeneratedSlabMeshHealthService().Inspect",
        "GeneratedWallMeshHealthService().Inspect",
        "GeneratedCurtainFrameHealthService().Inspect",
        "CurtainWallFrameLiveStateService.Inspect",
        "GeneratedGeometryStaleHealthService().Inspect",
        "BomReleaseGuardService.Inspect",
        "summary.Errors == 0 && summary.Warnings == 0",
        "V25 runtime/private-DWG gate vẫn là bước riêng",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot",
    ],
    "src/QS3D.BricsCAD.V25/SafeGeneratedHandleOwnershipHealthCommands.cs": [
        'CommandMethod("QS3DOWNERSHIPHEALTH"',
        "SafeGeneratedHandleOwnershipHealthService().Inspect(project)",
    ],
}

for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing release-readiness file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing release-readiness token: " + needle)

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in ("QS3DRELEASECHECK", "QS3DOWNERSHIPHEALTH"):
    if commands.count(command) != 1:
        errors.append(command + " must be declared exactly once")

# The transitional broad scanner command must stay out of primary Ribbon/Hub entry points.
for relative in (
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
):
    path = ROOT / relative
    if path.is_file() and "QS3DHANDLEHEALTH" in path.read_text(encoding="utf-8"):
        errors.append(relative + " must not expose transitional QS3DHANDLEHEALTH; use provenance-safe QS3DOWNERSHIPHEALTH/release check")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: unified QS3DRELEASECHECK uses provenance-safe ownership, generated/live CAD health and BOM guards; runtime/private-DWG remains a separate V25 gate.")
