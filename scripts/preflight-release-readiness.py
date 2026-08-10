#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs": [
        "BomReleaseGuardService",
        "BOM_QUANTITY_DIRTY",
        "BOM_QUANTITY_NONFINITE",
        "BOM_TRACEABILITY_MISSING",
        "BOM_GENERATED_HANDLE_MISSING",
        "BOM_ROW_MISSING",
        "ProjectQuantityReportBuilder.Group",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs": [
        "public static class GeneratedHandleOwnershipPolicy",
        "EnumerateOwnerHandles",
        "CollectOwnerHandles",
        "TryFindOwner",
        'PhysicalOpeningCutSolidHandle',
        'StartsWith("Generated"',
        'EndsWith("Handle"',
        'EndsWith("Handles"',
    ],
    "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipPolicy.cs": [
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
    ],
    "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs": [
        'CommandMethod("QS3DRELEASECHECK"',
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "ModelHealthService().Inspect",
        "SafeGeneratedHandleOwnershipHealthService().Inspect",
        "GeneratedRebarHealthService().InspectAll",
        "GeneratedTieRebarHealthService().Inspect",
        "GeneratedBeamStirrupHealthService().Inspect",
        "GeneratedSlabMeshHealthService().Inspect",
        "GeneratedWallMeshHealthService().Inspect",
        "GeneratedFoundationMeshHealthService().Inspect",
        "GeneratedCurtainFrameHealthService().Inspect",
        "GeneratedRebarModeHealthService().Inspect",
        "CurtainWallFrameLiveStateService.Inspect",
        "GeneratedGeometryStaleHealthService().Inspect",
        "BomReleaseGuardService.Inspect",
        "summary.Errors == 0 && summary.Warnings == 0",
        "V25 runtime/private-DWG gate vẫn là bước riêng",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
    ],
    "src/QS3D.BricsCAD.V25/SafeGeneratedHandleOwnershipHealthCommands.cs": [
        'CommandMethod("QS3DOWNERSHIPHEALTH"',
        "SafeGeneratedHandleOwnershipHealthService().Inspect(project)",
    ],
    "tests/QS3D.Core.SmokeTests/BomReleaseGuardSmoke.cs": [
        "BomReleaseGuardService.Inspect",
        "BOM_QUANTITY_DIRTY",
        "BOM_QUANTITY_NONFINITE",
        "BOM_TRACEABILITY_MISSING",
        "BOM_GENERATED_HANDLE_MISSING",
    ],
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs": [
        "BomReleaseGuardSmoke.Run();",
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

adapter_policy = ROOT / "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipPolicy.cs"
if adapter_policy.is_file():
    text = adapter_policy.read_text(encoding="utf-8")
    if 'StartsWith("Generated"' in text or 'PhysicalOpeningCutSolidHandle' in text:
        errors.append("adapter generated ownership policy duplicated Core classification logic")

commands = []
adapter = ROOT / "src/QS3D.BricsCAD.V25"
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in ("QS3DRELEASECHECK", "QS3DOWNERSHIPHEALTH"):
    if commands.count(command) != 1:
        errors.append(command + " must be declared exactly once")

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

print("PASS: QS3DRELEASECHECK consumes the shared ownership registry and gates Foundation mesh, generated mode, stale/live CAD and BOM health; runtime/private-DWG remains a separate V25 gate.")
