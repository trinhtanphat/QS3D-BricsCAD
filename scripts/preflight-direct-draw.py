#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs": [
        'CommandMethod("QS3DDRAWWALL"',
        'CommandMethod("QS3DDRAWGLASSWALL"',
        'CommandMethod("QS3DDRAWWALLPIER"',
        'CommandMethod("QS3DDRAWBEAM"',
        'CommandMethod("QS3DDRAWCOLUMN"',
        'CommandMethod("QS3DDRAWSLAB"',
        'CommandMethod("QS3DDRAWSTRUCTWALL"',
        'CommandMethod("QS3DDRAWFOUNDATION"',
        "SemanticCaptureService.Capture(document, category)",
        "ProjectStateSnapshot.Capture(project)",
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "GeneratedGeometryService.FindMatchingOwnedHandles",
        "rollback.Restore(project)",
        "EraseHandles(document, cleanupHandles)",
        "RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)",
        "WallSolidBuilder.BuildSelectedLineWalls",
        "PolylineWallSolidBuilder.BuildSelected",
        "StructuralSolidBuilder.BuildSelected",
        "ElementCategory.GlassWall",
        "ElementCategory.WallPier",
        "ElementCategory.StructuralWall",
        "ElementCategory.Foundation",
        "CreateLine(document",
        "CreatePolyline(document",
        "CreateColumnFootprint",
        "PromptPositiveMeters",
        "FamilyNumber",
        "AllowNone = points.Count >= minimumPoints",
        "CadHandleService.GetLiveHandles(document, normalized)",
        "Direct Draw rollback còn CAD handle chưa xóa",
        "QS3DVIEW3D",
    ],
    "src/QS3D.BricsCAD.V25/Build3DCommands.cs": [
        'CommandMethod("QS3DBUILD3D"',
        "unsupported.Count > 0",
        "categories.Count > 1",
        "một category mỗi lần",
        "ValidateWallSourceBatch",
        'string.Equals(x, "Line"',
        'string.Equals(x, "Polyline"',
        "sourceTypes.Count > 1",
        "không build chung LINE và open POLYLINE",
        "RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)",
        "BuildCategory(document, project, category)",
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
        "category == ElementCategory.StructuralWall",
        "category == ElementCategory.Foundation",
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
        "ElementCategory.GlassWall",
        "ElementCategory.WallPier",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
        'CadGeometryGuard.Subtract(line.EndPoint.Z, line.StartPoint.Z',
        "wall planarity tolerance",
        "|ΔZ| <= 0.005 m",
    ],
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs": [
        "BuildSelected",
        "WallPierPathProfilePlanner.Plan",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.CommitReplacement",
    ],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": [
        'RibbonTabSpec("QS3D_AUTHOR", "TẠO MỚI"',
        '"QS3DDRAWWALL"',
        '"QS3DDRAWGLASSWALL"',
        '"QS3DDRAWWALLPIER"',
        '"QS3DDRAWBEAM"',
        '"QS3DDRAWCOLUMN"',
        '"QS3DDRAWSLAB"',
        '"QS3DDRAWSTRUCTWALL"',
        '"QS3DDRAWFOUNDATION"',
        '"QS3DBUILD3D"',
    ],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": [
        'Text="TẠO MỚI / DIRECT DRAW"',
        'Tag="QS3DDRAWWALL"',
        'Tag="QS3DDRAWGLASSWALL"',
        'Tag="QS3DDRAWWALLPIER"',
        'Tag="QS3DDRAWBEAM"',
        'Tag="QS3DDRAWCOLUMN"',
        'Tag="QS3DDRAWSLAB"',
        'Tag="QS3DDRAWSTRUCTWALL"',
        'Tag="QS3DDRAWFOUNDATION"',
        "Capture/Bóc chọn",
    ],
    "docs/DIRECT-DRAW-WORKFLOW.md": [
        "QS3DDRAWWALL",
        "QS3DDRAWBEAM",
        "QS3DDRAWCOLUMN",
        "QS3DDRAWSLAB",
        "P1 candidates",
        "Atomicity and cancellation",
        "Ribbon / discoverability",
    ],
    "docs/DIRECT-DRAW-P1-IMPLEMENTATION.md": [
        "QS3DDRAWGLASSWALL",
        "QS3DDRAWWALLPIER",
        "QS3DDRAWSTRUCTWALL",
        "QS3DDRAWFOUNDATION",
        "WallPierPathProfilePlanner",
        "source-implemented",
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
    "QS3DDRAWWALL", "QS3DDRAWGLASSWALL", "QS3DDRAWWALLPIER", "QS3DDRAWBEAM",
    "QS3DDRAWCOLUMN", "QS3DDRAWSLAB", "QS3DDRAWSTRUCTWALL", "QS3DDRAWFOUNDATION",
    "QS3DWALL", "QS3DGLASSWALL", "QS3DWALLPIER", "QS3DBEAM", "QS3DCOLUMN", "QS3DSLAB",
    "QS3DSTRUCTWALL", "QS3DFOUNDATION", "QS3DBUILD3D",
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
    )
    for token in forbidden:
        if token in text:
            errors.append("DirectDrawCommands must reuse established builders instead of duplicating native geometry: " + token)
    create = text.find("sourceId = createSource();")
    capture = text.find("SemanticCaptureService.Capture(document, category)")
    regenerate = text.find("var regenerated = new RegenerationEngine")
    build = text.find("BuildSelected(document, project, category)")
    restore = text.find("rollback.Restore(project)")
    erase = text.find("EraseHandles(document, cleanupHandles)")
    if min(create, capture, regenerate, build, restore, erase) < 0:
        errors.append("Direct Draw transaction/rollback ordering tokens are incomplete")
    elif not (create < capture < regenerate < build < restore < erase):
        errors.append("Direct Draw must create source -> capture -> semantic regen -> build, then restore semantic state before CAD cleanup")
    if "priorGenerated.Contains(handle)" not in text:
        errors.append("Direct Draw rollback must preserve generated handles that existed before the operation")
    erase_body = text.split("private static void EraseHandles", 1)[-1].split("private static Document? Active", 1)[0]
    if "catch { }" in erase_body or "catch{}" in erase_body.replace(" ", ""):
        errors.append("Direct Draw CAD rollback must not swallow per-entity erase failures")
    if "transaction.Commit();" not in erase_body or "CadHandleService.GetLiveHandles(document, normalized)" not in erase_body:
        errors.append("Direct Draw CAD rollback must commit erase transaction and verify no requested handles remain live")

    wall_pier_match = re.search(r"public void DrawWallPier\(\)(.*?)(?=\n\s*\[CommandMethod|\n\s*private static)", text, re.S)
    if not wall_pier_match:
        errors.append("Direct Draw P1 must declare DrawWallPier")
    else:
        wall_pier_body = wall_pier_match.group(1)
        if "CreatePolyline(document, points, false)" not in wall_pier_body:
            errors.append("WallPier Direct Draw must persist an open POLYLINE so path-profile semantics are preserved")
        if "points.Count == 2 ? CreateLine" in wall_pier_body:
            errors.append("WallPier Direct Draw must not downgrade two-point authoring to the generic LINE box path")

    if "category == ElementCategory.ArchitecturalWall || category == ElementCategory.GlassWall || category == ElementCategory.WallPier" not in text:
        errors.append("Direct Draw P1 wall categories must reuse the established wall builders")
    if "category == ElementCategory.StructuralWall || category == ElementCategory.Foundation" not in text:
        errors.append("Direct Draw P1 structural categories must reuse StructuralSolidBuilder")

build3d = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
if build3d.is_file():
    text = build3d.read_text(encoding="utf-8")
    guard_category = text.find("if (categories.Count > 1)")
    guard_wall_type = text.find("if (sourceTypes.Count > 1)")
    regenerate = text.find("var regenerated = new RegenerationEngine")
    build = text.find("var built = BuildCategory(document, project, category);")
    if min(guard_category, guard_wall_type, regenerate, build) < 0 or not (guard_category < guard_wall_type < regenerate < build):
        errors.append("QS3DBUILD3D must reject mixed batches and regenerate semantic state before the first native builder commit")
    if "foreach (var category in categories)" in text:
        errors.append("QS3DBUILD3D must not commit independent category builders sequentially in one logical operation")

print("QS3D Direct Draw P0/P1 preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Direct Draw P0/P1 validates semantic state before CAD mutation, preserves rollback/ownership contracts, routes WallPier through path-profile semantics, reuses guarded builders and exposes authoring UI.")
