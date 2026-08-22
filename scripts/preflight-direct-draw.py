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
        "GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, createdElement.Id, createdElement.Category)",
        "GeneratedGeometryService.RequireMatchingOwnership",
        "ownershipDiscoveryError",
        "EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)",
        "rollback.Restore(project)",
        "FinalizeUi(document, createdElement!",
        "EnsureActive(document",
        ".RegenerateDirtySubset(project, new[] { createdElement.Id })",
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
        "RequireModelSpace(document)",
        "document.Database.CurrentSpaceId.Equals(modelSpaceId)",
        "CadGeometryGuard.Subtract(center.X, halfWidth",
        "CadGeometryGuard.Add(center.X, halfWidth",
        "CadHandleService.GetLiveHandles(document, normalized)",
        "Direct Draw rollback còn generated CAD handle chưa xóa",
        "Direct Draw rollback còn source CAD chưa xóa",
        'element.Properties.TryGetValue("GeneratedSolidHandle"',
        "QS3DVIEW3D",
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedGeometryService.cs": [
        "FindMatchingOwnedHandles",
        "GetXDataForApplication(RegAppName)",
        "BlockTableRecord.ModelSpace",
        "HasMatchingOwnership(entity, normalizedProjectId, normalizedElementId, category)",
        "RequireMatchingOwnership",
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
        ".RegenerateDirtySubset(project, regenerationScope)",
        "BuildCategory(document, project, category, sourceType)",
        'return element.Properties.TryGetValue("GeneratedSolidHandle"',
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
        '"QS3D_AUTHOR"',
        '"TẠO MỚI"',
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
        'Tag="QS3DWALL"',
        'Tag="QS3DBUILD3D"',
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
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "priorGenerated.Contains(handle)",
        "EraseHandles(document, cleanupHandles)",
        "Math.Abs(points[index].Z - z) > 1e-6d",
    )
    for token in forbidden:
        if token in text:
            errors.append("DirectDrawCommands contains stale/unsafe authoring behavior: " + token)

    create = text.find("sourceId = createSource();")
    capture = text.find("SemanticCaptureService.Capture(document, category)")
    regenerate = text.find("regenerated = new RegenerationEngine")
    build = text.find("BuildSelected(document, project, category)")
    catch_pos = text.find("catch (Exception operationError)")
    metadata_discovery = text.find("GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(createdElement)", catch_pos)
    xdata_discovery = text.find("GeneratedGeometryService.FindMatchingOwnedHandles", catch_pos)
    cleanup = text.find("EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)", catch_pos)
    restore = text.find("rollback.Restore(project)", catch_pos)
    finalize = text.find("FinalizeUi(document, createdElement!", catch_pos)
    if min(create, capture, regenerate, build, catch_pos, metadata_discovery, xdata_discovery, cleanup, restore, finalize) < 0:
        errors.append("Direct Draw transaction/rollback ordering tokens are incomplete")
    else:
        if not (create < capture < regenerate < build < catch_pos):
            errors.append("Direct Draw must create source -> capture -> semantic regen -> native build before rollback handling")
        if not (catch_pos < metadata_discovery < xdata_discovery < cleanup < restore < finalize):
            errors.append("Direct Draw failure must discover only the new owner's output, clean ownership-verified CAD before project restore, then run UI sync only after the atomic boundary")

    if text.count("RequireModelSpace(document);") < 4:
        errors.append("Every P0 Direct Draw command must fail closed outside Model Space")
    if text.count('element.SetProperty("BottomOffsetM"') < 8:
        errors.append("All P0 Direct Draw commands must persist the prompted source-relative bottom offset")
    if text.count("PromptPositiveMeters(document.Editor") < 7:
        errors.append("P0 Direct Draw must prompt key positive dimensions instead of silently using all Family defaults")
    if "Sửa Family trước khi Direct Draw." not in text or "if (!(value > 0d))" not in text:
        errors.append("Existing invalid Family numeric values must fail closed rather than being silently replaced by Direct Draw fallbacks")

    cleanup_start = text.find("private static void EraseDirectDrawCad(")
    cleanup_end = text.find("private static void FinalizeUi(", cleanup_start)
    if cleanup_start < 0 or cleanup_end < 0:
        errors.append("Direct Draw ownership-scoped CAD rollback helper is missing")
    else:
        cleanup_body = text[cleanup_start:cleanup_end]
        if "catch { }" in cleanup_body or "catch{}" in cleanup_body.replace(" ", ""):
            errors.append("Direct Draw CAD rollback must not swallow per-entity destructive erase failures")
        for token in (
            "GeneratedGeometryService.RequireMatchingOwnership",
            "CadHandleService.Resolve(document, normalized)",
            "transaction.Commit();",
            "CadHandleService.GetLiveHandles(document, normalized)",
        ):
            if token not in cleanup_body:
                errors.append("Direct Draw CAD rollback missing safety token: " + token)
        resolve_pos = cleanup_body.find("CadHandleService.Resolve(document, normalized)")
        transaction_pos = cleanup_body.find("using (document.LockDocument())")
        if resolve_pos < 0 or transaction_pos < 0 or resolve_pos > transaction_pos:
            errors.append("Direct Draw must resolve generated ObjectIds before opening the destructive write transaction")

build3d = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
if build3d.is_file():
    text = build3d.read_text(encoding="utf-8")
    guard_category = text.find("if (categories.Count > 1)")
    resolve_sources = text.find("var sourceIds = CadHandleService.Resolve(document, sourceHandles);")
    source_space = text.find("if (!AreAllModelSpaceEntities(document, sourceIds))")
    select_sources = text.find("document.Editor.SetImpliedSelection(sourceIds.ToArray())")
    source_snapshots = text.find("var sourceSnapshots = EntitySnapshotReader.ReadImpliedSelection(document);")
    validate_call = text.find("if (!ValidateWallSourceBatch(selectedElements, sourceSnapshots, category, out var wallSourceError))")
    regenerate = text.find("regenerated = new RegenerationEngine")
    build = text.find("built = BuildCategory(document, project, category, sourceType);")
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
print("PASS: Direct Draw P0 uses source-relative offsets, fail-closed Family numerics, semantic validation before CAD mutation, Model-Space/unit-aware authoring, ownership-scoped/XData-complete rollback before project restore, non-destructive UI finalization, and guarded QS3DBUILD3D/Workspace source batches.")
