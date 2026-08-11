#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

command = ROOT / "src/QS3D.BricsCAD.V25/HealthAllCommands.cs"
services = [
    ROOT / "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedGeometryStaleHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedFoundationMeshHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipHealthService.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs",
    ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarModeHealthService.cs",
]
for path in [command] + services:
    if not path.is_file(): errors.append("missing unified-health file: " + str(path.relative_to(ROOT)))

owners = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    if re.search(r'\[CommandMethod\("QS3DHEALTHALL"', text, re.IGNORECASE): owners.append(str(path.relative_to(ROOT)))
if len(owners) != 1: errors.append("QS3DHEALTHALL must have exactly one CommandMethod owner; found: " + ", ".join(owners))

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DHEALTHALL"',
        "new ModelHealthService().Inspect",
        "new RoomFinishHealthService().Inspect(project)",
        "new GeneratedGeometryStaleHealthService().Inspect",
        "new GeneratedRebarHealthService().InspectAll",
        "new GeneratedTieRebarHealthService().Inspect",
        "new GeneratedBeamStirrupHealthService().Inspect",
        "new GeneratedSlabMeshHealthService().Inspect",
        "new GeneratedWallMeshHealthService().Inspect",
        "new GeneratedFoundationMeshHealthService().Inspect",
        "new GeneratedCurtainFrameHealthService().Inspect",
        "new GeneratedRebarOwnershipHealthService().Inspect",
        "new GeneratedHandleOwnershipHealthService().Inspect",
        "new GeneratedRebarModeHealthService().Inspect",
        'PropertyHandles(project, "GeneratedSolidHandle")',
        'PropertyHandles(project, "GeneratedRebarHandles")',
        'PropertyHandles(project, "GeneratedShapeRebarHandles")',
        'PropertyHandles(project, "GeneratedTieRebarHandles")',
        'PropertyHandles(project, "GeneratedBeamStirrupHandles")',
        'PropertyHandles(project, "GeneratedSlabMeshHandles")',
        'PropertyHandles(project, "GeneratedWallMeshHandles")',
        'FoundationMeshSolidBuilder.HandlesKey',
        'PropertyHandles(project, "GeneratedCurtainFrameHandles")',
        'normalized.Contains("SLAB_MESH")',
        'normalized.Contains("WALL_MESH")',
        'normalized.Contains("FOUNDATION_MESH")',
        'normalized.Contains("CURTAIN_FRAME")',
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })",
        "GroupBy(x => x.Severity +",
        "LocateHandles",
        "QS3DZOOMSELECTED",
        "ModelHealthWindow",
    ):
        if needle not in text: errors.append("unified health command missing: " + needle)
    if "SourceHandleResolver.Resolve(project, new[] { element.Id })" in text:
        errors.append("unified Health All modeless Locate must not use the project snapshot captured when the window opened")

room_health = ROOT / "src/QS3D.Core/Diagnostics/RoomFinishHealthService.cs"
if room_health.is_file():
    text = room_health.read_text(encoding="utf-8")
    for needle in ("ROOM_PROVENANCE_CONFLICT", "ORPHAN_ROOM_FINISH", "ROOM_FINISH_SCOPE_MISMATCH", "STALE_ROOM_FINISH"):
        if needle not in text: errors.append("room-finish health missing unified diagnostic code: " + needle)

ownership = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarOwnershipHealthService.cs"
if ownership.is_file():
    text = ownership.read_text(encoding="utf-8")
    for needle in ("GeneratedHandleOwnershipPolicy.RebarHandleKeys", "REBAR_GENERATED_CROSS_KEY_OWNERSHIP_CONFLICT"):
        if needle not in text: errors.append("cross-family ownership health missing shared policy contract: " + needle)

policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
if policy.is_file():
    text = policy.read_text(encoding="utf-8")
    for needle in (
        "RebarHandleKeys", "GeneratedRebarHandles", "GeneratedShapeRebarHandles", "GeneratedTieRebarHandles",
        "GeneratedBeamStirrupHandles", "GeneratedSlabMeshHandles", "GeneratedWallMeshHandles", "GeneratedFoundationMeshHandles"):
        if needle not in text: errors.append("generated ownership policy missing unified-health channel: " + needle)

for relative in (
    "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedBeamStirrupHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedWallMeshHealthService.cs",
    "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
):
    path = ROOT / relative
    if path.is_file() and "GeneratedHandleOwnershipPolicy.IsOwnerSlot" not in path.read_text(encoding="utf-8"):
        errors.append(relative + " must use shared owner-slot policy for dedicated ownership health")

print("QS3D unified full-health preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: full health aggregates HT_Phòng provenance plus generated/rebar/curtain ownership/stale checks and modeless Locate re-resolves current project state before dependency-aware CAD selection.")
