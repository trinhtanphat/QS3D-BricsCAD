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
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
        'CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z',
        "line planarity tolerance",
        "|ΔZ| <= 0.005 m",
    ],
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs": [
        "BuildSelectedLineWalls",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
        'CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z',
        "wall planarity tolerance",
        "|ΔZ| <= 0.005 m",
    ],
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs": [
        "BuildSelected",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
    ],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": [
        'RibbonTabSpec("QS3D_AUTHOR", "TẠO MỚI"',
        '"QS3DDRAWWALL"',
        '"QS3DDRAWBEAM"',
        '"QS3DDRAWCOLUMN"',
        '"QS3DDRAWSLAB"',
        '"QS3DBUILD3D"',
    ],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": [
        'Text="TẠO MỚI / DIRECT DRAW"',
        'Tag="QS3DDRAWWALL"',
        'Tag="QS3DDRAWBEAM"',
        'Tag="QS3DDRAWCOLUMN"',
        'Tag="QS3DDRAWSLAB"',
        "Capture/Bóc chọn",
    ],
    "docs/DIRECT-DRAW-WORKFLOW.md": [
        "QS3DDRAWWALL",
        "QS3DDRAWBEAM",
        "QS3DDRAWCOLUMN",
        "QS3DDRAWSLAB",
        "Atomicity and cancellation",
        "Ribbon / discoverability",
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
for name in (
    "QS3DDRAWWALL", "QS3DDRAWBEAM", "QS3DDRAWCOLUMN", "QS3DDRAWSLAB",
    "QS3DWALL", "QS3DBEAM", "QS3DCOLUMN", "QS3DSLAB", "QS3DBUILD3D",
):
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
        "priorGenerated.Contains(handle)",
        "Math.Abs(points[index].Z - z) > 1e-6d",
    )
    for token in forbidden:
        if token in text:
            errors.append("DirectDrawCommands contains unsafe/duplicated legacy behavior: " + token)

    create = text.find("sourceId = createSource();")
    capture = text.find("SemanticCaptureService.Capture(document, category)")
    build = text.find("BuildSelected(document, project, category)")
    catch_pos = text.find("catch (Exception operationError)")
    cleanup_call = text.find("EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)", catch_pos)
    restore_call = text.find("rollback.Restore(project)", catch_pos)
    finalize_call = text.find("FinalizeUi(document", catch_pos)
    if min(create, capture, build, catch_pos, cleanup_call, restore_call, finalize_call) < 0:
        errors.append("Direct Draw transaction/rollback/UI ordering tokens are incomplete")
    else:
        if not (create < capture < build < catch_pos):
            errors.append("Direct Draw must create source -> capture -> native build before its rollback boundary")
        if cleanup_call > restore_call:
            errors.append("Direct Draw must clean scoped CAD while semantic ownership metadata is still available, before project restore")
        if finalize_call < restore_call:
            errors.append("Direct Draw UI/View3D sync must stay outside the atomic model mutation/rollback block")

    cleanup_start = text.find("private static void EraseDirectDrawCad(")
    cleanup_end = text.find("private static void FinalizeUi(", cleanup_start)
    if cleanup_start < 0 or cleanup_end < 0:
        errors.append("Direct Draw ownership-scoped CAD rollback helper is missing")
    else:
        cleanup_body = text[cleanup_start:cleanup_end]
        if "catch { }" in cleanup_body:
            errors.append("Direct Draw CAD rollback must not swallow per-entity erase failures")
        if "GeneratedGeometryService.RequireMatchingOwnership" not in cleanup_body:
            errors.append("Direct Draw generated CAD rollback must verify QS3D XData ownership before erase")

print("QS3D Direct Draw P0 preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Direct Draw preserves legacy capture, uses persistence snapshots, scopes CAD rollback to the new semantic owner with ownership verification, keeps UI sync outside the atomic mutation boundary, reuses guarded builders, rejects sloped LINE flattening and remains exposed in Ribbon/Domain Hub.")
