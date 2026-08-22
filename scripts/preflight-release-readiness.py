#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Diagnostics/BomReleaseGuardService.cs": [
        "BomReleaseGuardService",
        "new RoomFinishHealthService().Inspect(project)",
        "BOM_QUANTITY_DIRTY",
        "BOM_QUANTITY_NONFINITE",
        "BOM_TRACEABILITY_MISSING",
        "BOM_GENERATED_HANDLE_MISSING",
        "BOM_ROW_MISSING",
        "ProjectQuantityReportBuilder.Group",
        "GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element)",
    ],
    "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs": [
        "ROOM_PROVENANCE_CONFLICT",
        "ORPHAN_ROOM_FINISH",
        "INVALID_ROOM_FINISH_PARENT",
        "ROOM_FINISH_SCOPE_MISMATCH",
        "STALE_ROOM_FINISH",
    ],
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs": [
        "public static class GeneratedHandleOwnershipPolicy",
        "RebarHandleKeys",
        "IsRebarOwnerSlot",
        "CanonicalOwnerSlot",
        "AreSameLogicalOwnerSlots",
        "EnumerateOwnerHandles",
        "EnumerateLogicalOwnerHandles",
        "CollectOwnerHandles",
        "TryFindOwner",
        'PhysicalOpeningCutSolidHandle',
        'StartsWith("Generated"',
        'EndsWith("Handle"',
        'EndsWith("Handles"',
    ],
    "src/QS3D.BricsCAD.V25/GeneratedHandleOwnershipPolicy.cs": [
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.RebarHandleKeys",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.IsOwnerSlot(key)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.IsRebarOwnerSlot(key)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.CanonicalOwnerSlot(key)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(element)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles(element)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "QS3D.Core.Diagnostics.GeneratedHandleOwnershipPolicy.TryFindOwner(project, handle",
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs": [
        "GeneratedGridAnnotationRuntimeHealthService.Inspect(document, project)",
        "GeneratedSemanticTagRuntimeHealthService.Inspect(document, project)",
    ],
    "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs": [
        'CommandMethod("QS3DRELEASECHECK"',
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "ModelHealthService().Inspect",
        "GeneratedSolidRuntimeHealthService.Inspect(document, project)",
        "DependencyHealthService().Inspect",
        "SafeGeneratedHandleOwnershipHealthService().Inspect",
        "GeneratedRebarHealthService().InspectAll",
        "GeneratedTieRebarHealthService().Inspect",
        "GeneratedBeamStirrupHealthService().Inspect",
        "GeneratedSlabMeshHealthService().Inspect",
        "GeneratedWallMeshHealthService().Inspect",
        "GeneratedFoundationMeshHealthService().Inspect",
        "GeneratedCurtainFrameHealthService().Inspect",
        "CurtainWallFrameLiveStateService.Inspect",
        "GeneratedGeometryStaleHealthService().Inspect",
        "GeneratedRebarModeHealthService().Inspect",
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
        "ORPHAN_ROOM_FINISH",
        "RoomFinishProvenanceReachesReleaseGuard",
        "PhysicalOpeningCutSolidHandle",
        "GeneratedFuturePanelHandles",
    ],
    "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs": [
        "BomReleaseGuardSmoke.Run();",
        "RoomFinishHealthSmoke.Run();",
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

print("PASS: QS3DRELEASECHECK consumes shared ownership, dependency, HT_Phòng provenance, Foundation/mode, stale/live CAD (including Grid annotation + semantic tag runtime health) and BOM health; runtime/private-DWG remains a separate V25 gate.")
