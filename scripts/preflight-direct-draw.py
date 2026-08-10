#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs": [
        'CommandMethod("QS3DDRAWWALL"',
        'CommandMethod("QS3DDRAWBEAM"',
        'CommandMethod("QS3DDRAWCOLUMN"',
        'CommandMethod("QS3DDRAWSLAB"',
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
        "CreateLine(document",
        "CreatePolyline(document",
        "CreateColumnFootprint",
        "PromptPositiveMeters",
        "PromptFiniteMeters",
        "FamilyNumber",
        "FamilyFiniteNumber",
        "PreferredFamily",
        "Sửa Family trước khi Direct Draw.",
        "Offset đáy Tường so với Z source (m)",
        "Offset đáy Dầm so với Z source (m)",
        "Offset đáy Sàn so với Z source (m)",
        "Offset đáy Cột so với Z source (m)",
        'element.SetProperty("ThicknessM"',
        'element.SetProperty("WidthM"',
        'element.SetProperty("DepthM"',
        'element.SetProperty("HeightM"',
        'element.SetProperty("BottomOffsetM"',
        "AllowNone = points.Count >= minimumPoints",
        "PlanarityToleranceM = 0.005d",
        "CadGeometryGuard.ToMeters(document, deltaDrawingUnits",
        "CadGeometryGuard.Finite(points[index].X",
        "CadGeometryGuard.Finite(points[index].Y",
        "CadGeometryGuard.Multiply(width, 0.5d",
        "CadGeometryGuard.Multiply(depth, 0.5d",
        "CadGeometryGuard.Finite(center.X",
        "CadGeometryGuard.Finite(center.Y",
        "CadGeometryGuard.Finite(center.Z",
        "CadGeometryGuard.Subtract(centerX, halfWidth",
        "CadGeometryGuard.Add(centerX, halfWidth",
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
        "ValidateSourceBatch(document, sourceIds)",
        "SourceBatchKind",
        "entity.OwnerId.Equals(modelSpaceId)",
        "Tường KT native 3D chỉ hỗ trợ source LINE hoặc open POLYLINE",
        "Không build chung LINE và open POLYLINE trong một wall batch",
        "polyline.Closed",
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
        "Cao độ đáy Tường (m)",
        "Cao độ đáy Dầm (m)",
        "Cao độ đáy Sàn (m)",
        "Cao độ đáy Cột (m)",
        "return value > 0d ? value : fallback;",
    )
    for token in forbidden:
        if token in text:
            errors.append("DirectDrawCommands contains stale/unsafe authoring behavior: " + token)
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
    if text.count("RequireModelSpace(document);") < 4:
        errors.append("Every P0 Direct Draw command must fail closed outside Model Space")
    if "Math.Abs(points[index].Z - z) > 1e-6d" in text:
        errors.append("Direct Draw planarity must be unit-aware rather than using raw drawing-unit tolerance")
    if text.count('element.SetProperty("BottomOffsetM"') < 4:
        errors.append("All P0 Direct Draw commands must persist the prompted source-relative bottom offset through ProjectElement.SetProperty")
    if text.count("element.SetProperty(") < 12:
        errors.append("P0 Direct Draw parameter writes must flow through canonical ProjectElement.SetProperty dirty/stale semantics")
    for key in ("ThicknessM", "WidthM", "DepthM", "HeightM", "BottomOffsetM"):
        if 'element.Properties["' + key + '"]' in text:
            errors.append("Direct Draw must not bypass ProjectElement.SetProperty for geometry parameter " + key)
    if text.count("PromptPositiveMeters(document.Editor") < 7:
        errors.append("P0 Direct Draw must prompt key positive dimensions instead of silently using all Family defaults")
    if "Sửa Family trước khi Direct Draw." not in text or "if (!(value > 0d))" not in text:
        errors.append("Existing invalid Family numeric values must fail closed rather than being silently replaced by Direct Draw fallbacks")
    if "CadGeometryGuard.Multiply(width, 0.5d" not in text or "CadGeometryGuard.Multiply(depth, 0.5d" not in text:
        errors.append("Direct Draw Column footprint must compute half-dimensions with finite-safe CAD arithmetic")
    if "CadGeometryGuard.Finite(center.X" not in text or "CadGeometryGuard.Finite(center.Y" not in text or "CadGeometryGuard.Finite(center.Z" not in text:
        errors.append("Direct Draw Column footprint must finite-check insertion coordinates before offset arithmetic")
    if "CadGeometryGuard.Finite(points[index].X" not in text or "CadGeometryGuard.Finite(points[index].Y" not in text:
        errors.append("Direct Draw POLYLINE creation must finite-check vertex coordinates before persistence")
    erase_body = text.split("private static void EraseHandles", 1)[-1].split("private static Document? Active", 1)[0]
    if "catch { }" in erase_body or "catch{}" in erase_body.replace(" ", ""):
        errors.append("Direct Draw CAD rollback must not swallow per-entity erase failures")
    if "transaction.Commit();" not in erase_body or "CadHandleService.GetLiveHandles(document, normalized)" not in erase_body:
        errors.append("Direct Draw CAD rollback must commit erase transaction and verify no requested handles remain live")

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

wall_builder = ROOT / "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs"
if wall_builder.is_file():
    text = wall_builder.read_text(encoding="utf-8")
    validate = text.find("ValidateSourceBatch(document, sourceIds)")
    pending = text.find("var pending = new List<PendingUpdate>();")
    native_transaction = text.find("using (document.LockDocument())", pending)
    if min(validate, pending, native_transaction) < 0 or not (validate < pending < native_transaction):
        errors.append("WallSolidBuilder must validate the entire source batch before any native Solid3d transaction can commit")
    if "sawLine && sawPolyline" not in text:
        errors.append("WallSolidBuilder must reject mixed LINE/open POLYLINE batches before the first builder commits")
    if "!entity.OwnerId.Equals(modelSpaceId)" not in text:
        errors.append("WallSolidBuilder must reject non-Model-Space source provenance before native generation")

print("QS3D Direct Draw P0 preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Direct Draw P0 uses source-relative offsets, rejects invalid configured Family numerics, routes geometry parameters through canonical SetProperty dirty/stale semantics, finite-checks persisted authoring coordinates, validates semantic state before CAD mutation, is Model-Space/unit aware, verifies rollback cleanup, and wall/QS3DBUILD3D/Workspace source batches fail closed before partial native rebuilds.")
