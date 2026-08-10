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
        'CommandMethod("QS3DDRAWSTRUCTWALL"',
        'CommandMethod("QS3DDRAWCOLUMN"',
        'CommandMethod("QS3DDRAWSLAB"',
        'CommandMethod("QS3DDRAWFOUNDATION"',
        "SemanticCaptureService.Capture(document, category)",
        "ProjectStateSnapshot.Capture(project)",
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, elementId, category)",
        "ownershipDiscoveryError",
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
        "PromptFiniteMeters",
        "FamilyNumber",
        "FamilyFiniteNumber",
        "PreferredFamily",
        'element.Properties["ThicknessM"]',
        'element.Properties["WidthM"]',
        'element.Properties["DepthM"]',
        'element.Properties["HeightM"]',
        'element.Properties["BottomOffsetM"]',
        "AllowNone = points.Count >= minimumPoints",
        "PlanarityToleranceM = 0.005d",
        "CadGeometryGuard.ToMeters(document, deltaDrawingUnits",
        "RequireModelSpace(document)",
        "document.Database.CurrentSpaceId.Equals(modelSpaceId)",
        "CadHandleService.GetLiveHandles(document, normalized)",
        "Direct Draw rollback còn CAD handle chưa xóa",
        'element.Properties.TryGetValue("GeneratedSolidHandle"',
        "QS3DVIEW3D",
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs": [
        "FindMatchingOwnedHandles",
        "GetXDataForApplication(RegAppName)",
        "BlockTableRecord.ModelSpace",
        "HasMatchingOwnership(entity, normalizedProjectId, normalizedElementId, category)",
    ],
    "src/QS3D.BricsCAD.V25/Build3DCommands.cs": [
        'CommandMethod("QS3DBUILD3D"',
        "SemanticReferenceHandles.MatchesSelection(x, handles)",
        "unsupported.Count > 0",
        "categories.Count > 1",
        "một category mỗi lần",
        "CadHandleService.Resolve(document, sourceHandles)",
        "sourceIds.Count != sourceHandles.Count",
        "AreAllModelSpaceEntities(document, sourceIds)",
        "entity.OwnerId.Equals(modelSpaceId)",
        "document.Editor.SetImpliedSelection(sourceIds.ToArray())",
        "EntitySnapshotReader.ReadImpliedSelection(document)",
        "ValidateWallSourceBatch",
        'string.Equals(x, "Line"',
        'string.Equals(x, "Polyline"',
        "sourceTypes.Count > 1",
        "không build chung LINE và open POLYLINE",
        "RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project)",
        "BuildCategory(document, project, category)",
        'x.Properties.TryGetValue("GeneratedSolidHandle"',
    ],
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs": [
        "SelectInspectionSemanticSourcesForBuild()",
        "SemanticReferenceHandles.MatchesSelection(x, handles)",
        ".SelectMany(x => x.SourceHandles)",
        ".Distinct(StringComparer.OrdinalIgnoreCase)",
        "Cad.CadHandleService.Select(doc, sourceHandles)",
        'Send("QS3DBUILD3D")',
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
    discover = text.find("GeneratedGeometryService.FindMatchingOwnedHandles")
    restore = text.find("rollback.Restore(project)")
    erase = text.find("EraseHandles(document, cleanupHandles)")
    if min(create, capture, regenerate, build, discover, restore, erase) < 0:
        errors.append("Direct Draw transaction/rollback ordering tokens are incomplete")
    elif not (create < capture < regenerate < build < discover < restore < erase):
        errors.append("Direct Draw must create source -> capture -> semantic regen -> build; failure discovers tagged output before semantic restore and CAD cleanup")
    if "priorGenerated.Contains(handle)" not in text:
        errors.append("Direct Draw rollback must preserve generated handles that existed before the operation")
    if text.count("RequireModelSpace(document);") < 8:
        errors.append("Every current P0/P1 Direct Draw command must fail closed outside Model Space")
    if "Math.Abs(points[index].Z - z) > 1e-6d" in text:
        errors.append("Direct Draw planarity must be unit-aware rather than using raw drawing-unit tolerance")
    if text.count('element.Properties["BottomOffsetM"]') < 8:
        errors.append("All current P0/P1 Direct Draw commands must persist the prompted base offset")
    if text.count("PromptPositiveMeters(document.Editor") < 13:
        errors.append("Direct Draw P0/P1 must prompt key positive dimensions instead of silently using all Family defaults")
    if "var family = PreferredFamily(project, category);" not in text or "Sửa Family trước khi Direct Draw" not in text:
        errors.append("Direct Draw must preserve fail-closed invalid Family numeric validation")
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
    resolve_sources = text.find("var sourceIds = CadHandleService.Resolve(document, sourceHandles);")
    source_space = text.find("if (!AreAllModelSpaceEntities(document, sourceIds))")
    select_sources = text.find("document.Editor.SetImpliedSelection(sourceIds.ToArray())")
    source_snapshots = text.find("var sourceSnapshots = EntitySnapshotReader.ReadImpliedSelection(document);")
    validate_call = text.find("if (!ValidateWallSourceBatch(selectedElements, sourceSnapshots, category, out var wallSourceError))")
    regenerate = text.find("var regenerated = new RegenerationEngine")
    build = text.find("var built = BuildCategory(document, project, category);")
    if min(guard_category, resolve_sources, source_space, select_sources, source_snapshots, validate_call, regenerate, build) < 0 or not (
        guard_category < resolve_sources < source_space < select_sources < source_snapshots < validate_call < regenerate < build
    ):
        errors.append("QS3DBUILD3D must reject mixed categories, resolve complete live Model-Space source CAD, validate the source batch and regenerate semantic state before the native builder commit")
    if "if (sourceTypes.Count > 1)" not in text:
        errors.append("QS3DBUILD3D wall validation must reject mixed LINE/open POLYLINE source batches")
    if "foreach (var category in categories)" in text:
        errors.append("QS3DBUILD3D must not commit independent category builders sequentially in one logical operation")

workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
if workspace.is_file():
    text = workspace.read_text(encoding="utf-8")
    helper = text.split("private int SelectInspectionSemanticSourcesForBuild()", 1)[-1].split("private void ApplyFamilyFilter()", 1)[0]
    if ".Take(2)" in helper or "matches.Count != 1" in helper:
        errors.append("Workspace Vẽ/Cập nhật 3D must restore the full selected semantic batch, not only a single element")
    if ".SelectMany(x => x.SourceHandles)" not in helper or "Cad.CadHandleService.Select(doc, sourceHandles)" not in helper:
        errors.append("Workspace Vẽ/Cập nhật 3D must resolve selected semantic/generated aliases back to all distinct source handles")

print("QS3D Direct Draw P0/P1 preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Direct Draw P0/P1 prompts key dimensions, preserves fail-closed Family/Model-Space/unit-aware guards and rollback cleanup, routes WallPier through path-profile semantics, and QS3DBUILD3D/Workspace resolve semantic/generated selections back to complete live Model-Space source batches before rebuilding.")
