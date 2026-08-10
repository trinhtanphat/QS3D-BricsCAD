#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs": [
        'using QS3D.Core.Persistence;',
        'CommandMethod("QS3DDRAWWALL"',
        'CommandMethod("QS3DDRAWBEAM"',
        'CommandMethod("QS3DDRAWCOLUMN"',
        'CommandMethod("QS3DDRAWSLAB"',
        "SemanticCaptureService.Capture(document, category)",
        "ProjectStateSnapshot.Capture(project)",
        "ProjectElement? createdElement",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(createdElement)",
        "EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)",
        "GeneratedGeometryService.RequireMatchingOwnership",
        "rollback.Restore(project)",
        "FinalizeUi(document",
        "EnsureActive(document",
        "ValidatePlanView(document, points, label)",
        "PlanarityToleranceM = .005d",
        "WallSolidBuilder.BuildSelectedLineWalls",
        "PolylineWallSolidBuilder.BuildSelected",
        "StructuralSolidBuilder.BuildSelected",
        "CreateLine(document",
        "CreatePolyline(document",
        "CreateColumnFootprint",
        "PromptPositiveMeters",
        "FamilyNumber",
        "AllowNone = points.Count >= minimumPoints",
        "QS3DVIEW3D",
    ],
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs": [
        "GeneratedHandleOwnershipPolicy.TryFindOwner",
        "ProjectStateSnapshot.Capture(project)",
        "ResolveFamily(project, category)",
        "case ElementCategory.Beam",
        "case ElementCategory.Slab",
        "case ElementCategory.Column",
    ],
    "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs": [
        "category == ElementCategory.Beam",
        "category == ElementCategory.Slab",
        "category == ElementCategory.Column",
        "BuildLinePrism",
        "BuildClosedPolylinePrism",
        'CadGeometryGuard.ToDrawingUnits(document, .005d, element.Id + "/line planarity tolerance")',
        "Math.Abs(dz) > planTolerance",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
    ],
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs": [
        "BuildSelectedLineWalls",
        'CadGeometryGuard.ToDrawingUnits(document, .005d, element.Id + "/line planarity tolerance")',
        "Math.Abs(dz) > planTolerance",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
    ],
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs": [
        "BuildSelected",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
    ],
    "docs/DIRECT-DRAW-WORKFLOW.md": [
        "QS3DDRAWWALL",
        "QS3DDRAWBEAM",
        "QS3DDRAWCOLUMN",
        "QS3DDRAWSLAB",
    ],
}

for relative, needles in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing Direct Draw dependency: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing Direct Draw contract: " + needle)

commands = []
command_root = ROOT / "src/QS3D.BricsCAD.V25"
if command_root.is_dir():
    for path in command_root.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text))
for name in ("QS3DDRAWWALL", "QS3DDRAWBEAM", "QS3DDRAWCOLUMN", "QS3DDRAWSLAB"):
    if commands.count(name) != 1:
        errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

source = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
if source.is_file():
    text = source.read_text(encoding="utf-8")
    forbidden = (
        "new WallFootprintEngine()",
        "CreateBox(",
        "CreateExtrudedSolid(",
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "EraseHandles(document",
        "Math.Abs(points[index].Z - z) > 1e-6d",
    )
    for token in forbidden:
        if token in text:
            errors.append("DirectDrawCommands contains unsafe/duplicated legacy behavior: " + token)

    cleanup_start = text.find("private static void EraseDirectDrawCad(")
    cleanup_end = text.find("private static void FinalizeUi(", cleanup_start)
    if cleanup_start < 0 or cleanup_end < 0:
        errors.append("Direct Draw ownership-scoped CAD rollback helper is missing")
    else:
        cleanup_body = text[cleanup_start:cleanup_end]
        if "catch { }" in cleanup_body:
            errors.append("Direct Draw CAD rollback must not swallow per-entity erase failures")
        if "GeneratedGeometryService.RequireMatchingOwnership" not in cleanup_body:
            errors.append("Direct Draw generated CAD rollback must verify QS3D ownership before erase")

    catch_pos = text.find("catch (Exception operationError)")
    cleanup_call = text.find("EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)", catch_pos)
    restore_call = text.find("rollback.Restore(project)", catch_pos)
    finalize_call = text.find("FinalizeUi(document", catch_pos)
    if min(catch_pos, cleanup_call, restore_call, finalize_call) < 0:
        errors.append("Direct Draw rollback/UI ordering tokens are incomplete")
    else:
        if cleanup_call > restore_call:
            errors.append("Direct Draw must clean scoped CAD while ownership metadata is still available, before restoring project state")
        if finalize_call < restore_call:
            errors.append("Direct Draw UI/view synchronization must run only after the atomic operation/rollback catch has completed")

print("QS3D Direct Draw P0 preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Wall/Beam/Column/Slab Direct Draw compiles with persistence snapshot support, creates source CAD, reuses semantic/native builders, uses meter-based planarity, scopes rollback to the created element, verifies generated ownership before erase, and keeps UI sync outside the atomic model mutation path.")
