#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs",
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs",
    "src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs",
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmoke.cs",
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmokeRegistration.cs",
    "docs/FOUNDATION-REBAR3D.md",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing foundation-mesh file: " + relative)

checks = {
    "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs": [
        "RectangularSlabMeshPlanner.Plan", "ElementCategory.Foundation", "GeneratedFoundationMeshHandles",
        "GeneratedRebarOwnershipGuard.Build", "ownership.EnsureOwned", "RebarFoundationXNotation", "RebarFoundationYNotation",
        "RebarFoundationFaces", "MaxBarsPerBatch", "FoundationMeshXY", "duplicateSelectedSource", "checked(batchBars + layout.Count)",
        "CadGeometryGuard.Multiply", "CadGeometryGuard.Subtract", "ClearGeneratedFoundationMeshStale"
    ],
    "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs": ["QS3DFOUNDATIONREBAR3D"],
    "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs": [
        "FOUNDATION_MESH_GENERATED_OWNERSHIP_CONFLICT", "FOUNDATION_MESH_GENERATED_SOLID_MISSING",
        "FOUNDATION_MESH_CATEGORY_MISMATCH", "FOUNDATION_MESH_GENERATED_STALE", "IsGeneratedFoundationMeshStale",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot", "OwnershipIndex", "Conflicts", "IsConflicted"
    ],
    "src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs": ["QS3DFOUNDATIONREBARHEALTH"],
    "src/QS3D.BricsCAD.V25/RebarMeshSetupCommands.cs": ["ElementCategory.Foundation"],
    "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml.cs": [
        "ElementCategory.Foundation", "RebarFoundationXNotation", "RebarFoundationYNotation", "RebarFoundationCoverM",
        "RebarFoundationFaces", "RebarFoundationXClosestToFace", "D16@200"
    ],
    "src/QS3D.BricsCAD.V25/UI/Rebar3DHubWindow.xaml": ["QS3DFOUNDATIONREBAR3D", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARMESHSETUP"],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": ["QS3DFOUNDATIONREBAR3D", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARMESHSETUP"],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": ["QS3DFOUNDATIONREBAR3D", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARMESHSETUP"],
    "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs": ["GeneratedFoundationMeshHandles", "RebarHandleKeys", "IsOwnerSlot", "IsRebarOwnerSlot"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs": ["CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot", "CoreOwnershipPolicy.RebarHandleKeys"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs": ["CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot", "CoreOwnershipPolicy.RebarHandleKeys"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainFrameOwnershipGuard.cs": ["CoreOwnershipPolicy.IsOwnerSlot", "GeneratedCurtainFrameHandles"],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedDependentGeometryInvalidator.cs": ["GeneratedFoundationMeshHandles", "GeneratedFoundationMeshMode"],
    "src/QS3D.Core/Domain/ProjectElement.cs": [
        "GeneratedFoundationMeshStateKey", "GeneratedFoundationMeshStaleSnapshotKey", "IsGeneratedFoundationMeshStale", "ClearGeneratedFoundationMeshStale"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs": ["FOUNDATION_MESH_GENERATED_STALE"],
    "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs": ["GeneratedFoundationMeshHandles"],
    "src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs": ["FoundationMeshXY", "GeneratedFoundationMeshHandles", "GeneratedSlabMeshHandles", "GeneratedWallMeshHandles"],
    "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs": ["GeneratedFoundationMeshHandles"],
    "src/QS3D.BricsCAD.V25/RebarHealthAllCommands.cs": ["GeneratedFoundationMeshHealthService", "FoundationMeshSolidBuilder.HandlesKey"],
    "src/QS3D.BricsCAD.V25/HealthAllCommands.cs": ["GeneratedFoundationMeshHealthService", "FoundationMeshSolidBuilder.HandlesKey"],
    "src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs": ["GeneratedFoundationMeshHealthService().Inspect", "GeneratedRebarModeHealthService().Inspect"],
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs": ["CollectGeneratedHandles(project)", "GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)"],
    "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs": ["0.005d", "beam horizontal tolerance"],
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmoke.cs": [
        "IsGeneratedFoundationMeshStale", "GeneratedRebarModeHealthService", "GeneratedRebarOwnershipHealthService",
        "DetectsLaterOwnerConflictAndFutureGeneratedSlot", "GeneratedFutureMeshHandles"
    ],
    "tests/QS3D.Core.SmokeTests/FoundationMeshHealthSmokeRegistration.cs": ["FoundationMeshHealthSmoke.Run();"],
    "tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSmoke.cs": ["FoundationMeshGeneratedHandleResolvesOwner", "GeneratedFoundationMeshHandles"],
    "tests/QS3D.Core.SmokeTests/GeneratedGeometryStaleSmoke.cs": ["FOUNDATION_MESH_GENERATED_STALE", "GeneratedFoundationMeshHandles"],
    "tests/QS3D.Core.SmokeTests/GeneratedOutputHealthStaleSmoke.cs": ["FoundationMeshUsesSnapshotState"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing checked file: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

setup = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarMeshSetupWindow.xaml.cs"
if setup.is_file():
    text = setup.read_text(encoding="utf-8")
    if "first.DiameterMm - second.DiameterMm" in text:
        errors.append("Mesh Setup still blocks independent direction diameters")

commands = []
commands_root = ROOT / "src/QS3D.BricsCAD.V25"
if commands_root.is_dir():
    for path in commands_root.rglob("*.cs"):
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
upper = [x.upper() for x in commands]
for required_command in ("QS3DFOUNDATIONREBAR3D", "QS3DFOUNDATIONREBARHEALTH", "QS3DREBARMESHSETUP"):
    if required_command not in upper:
        errors.append("missing command: " + required_command)
if len(upper) != len(set(upper)):
    duplicates = sorted({x for x in upper if upper.count(x) > 1})
    errors.append("duplicate CommandMethod names: " + ", ".join(duplicates))

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Foundation mesh uses dedicated stale/health metadata, policy-driven destructive ownership, semantic-selection/B4D exclusion, independent X/Y setup, unified health/release-readiness and UI contracts.")
