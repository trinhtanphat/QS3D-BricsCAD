#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs": [
        'using QS3D.Core.Persistence;',
        'CommandMethod("QS3DDRAWGLASSWALL"',
        'CommandMethod("QS3DDRAWWALLPIER"',
        'CommandMethod("QS3DDRAWSTRUCTWALL"',
        'CommandMethod("QS3DDRAWFOUNDATION"',
        'AcquireFixedPath(document, "Trụ Tường", 2)',
        "SemanticCaptureService.Capture(document, category)",
        "ProjectStateSnapshot.Capture(project)",
        "GeneratedHandleOwnershipPolicy.EnumerateOwnerHandles(createdElement)",
        "GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, createdElement.Id, createdElement.Category)",
        "GeneratedGeometryService.RequireMatchingOwnership",
        "new Build3DCommands().Build3D()",
        'createdElement.Properties.TryGetValue("GeneratedSolidHandle"',
        "CadHandleService.GetLiveHandles(document, new[] { generatedHandle })",
        "EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)",
        "rollback.Restore(project)",
        "EnsureActive(document, operation + \" / QS3DBUILD3D\")",
        "FinalizeUi(document, createdElement!, sourceId, generatedHandle)",
        "PlanarityToleranceM = 0.005d",
        "RequireModelSpace(document)",
        "CadGeometryGuard.ToMeters(document, deltaDrawing",
        "CadGeometryGuard.Finite(points[index].X",
        "CadGeometryGuard.Finite(points[index].Y",
        "PreferredFamily",
        "Sửa Family trước khi Direct Draw.",
        "Offset đáy Vách Kính so với Z source (m)",
        "Offset đáy Trụ Tường so với Z source (m)",
        "Offset đáy Vách BTCT so với Z source (m)",
        "Offset đáy Móng so với Z source (m)",
        'element.SetProperty("ThicknessM"',
        'element.SetProperty("HeightM"',
        'element.SetProperty("BottomOffsetM"',
        'ElementCategory.GlassWall',
        'ElementCategory.WallPier',
        'ElementCategory.StructuralWall',
        'ElementCategory.Foundation',
    ],
    "src/QS3D.BricsCAD.V25/Build3DCommands.cs": [
        'CommandMethod("QS3DBUILD3D"',
        "StructuralSolidBuilder.Supports(category)",
        "NativeBuildCapability.IsWallCategory(category)",
        "category == ElementCategory.WallPier",
        "AreAllModelSpaceEntities(document, sourceIds)",
    ],
    "src/QS3D.BricsCAD.V25/Cad/NativeBuildCapability.cs": [
        "IsWallCategory(ElementCategory category)",
        "ElementCategory.GlassWall",
        "ElementCategory.WallPier",
    ],
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs": [
        "case ElementCategory.GlassWall",
        "case ElementCategory.WallPier",
        "case ElementCategory.StructuralWall",
        "case ElementCategory.Foundation",
    ],
    "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs": [
        "category == ElementCategory.StructuralWall",
        "category == ElementCategory.Foundation",
    ],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": [
        '"QS3D_AUTHOR"',
        '"TẠO MỚI"',
        '"QS3DDRAWGLASSWALL"',
        '"QS3DDRAWWALLPIER"',
        '"QS3DDRAWSTRUCTWALL"',
        '"QS3DDRAWFOUNDATION"',
    ],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": [
        'Text="TẠO MỚI / DIRECT DRAW"',
        'Tag="QS3DDRAWGLASSWALL"',
        'Tag="QS3DDRAWWALLPIER"',
        'Tag="QS3DDRAWSTRUCTWALL"',
        'Tag="QS3DDRAWFOUNDATION"',
    ],
    "docs/DIRECT-DRAW-P1-IMPLEMENTATION.md": [
        "QS3DDRAWGLASSWALL",
        "QS3DDRAWWALLPIER",
        "QS3DDRAWSTRUCTWALL",
        "QS3DDRAWFOUNDATION",
        "BricsCAD V25 x64 .NET plugin",
        "QS3DBUILD3D",
        "exactly a two-point **LINE**",
        "WallPierProfileSolidBuilder",
        "Door / Opening Direct Draw extension",
        "QS3DDRAWDOOR",
        "QS3DDRAWOPENING",
        "docs/DIRECT-DRAW-OPENINGS.md",
        "Physical boolean remains explicit",
    ],
    "src/QS3D.BricsCAD.V25/DirectDrawOpeningCommands.cs": [
        'CommandMethod("QS3DDRAWDOOR"',
        'CommandMethod("QS3DDRAWOPENING"',
    ],
    "scripts/preflight-direct-draw-openings.py": [
        "QS3DDRAWDOOR",
        "QS3DDRAWOPENING",
        "never invokes global physical cutting",
    ],
}

for relative, needles in required.items():
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing Direct Draw P1 dependency: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing Direct Draw P1 contract: " + needle)

commands = []
command_root = ROOT / "src/QS3D.BricsCAD.V25"
if command_root.is_dir():
    for path in command_root.rglob("*.cs"):
        commands.extend(re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8")))
for name in (
    "QS3DDRAWWALL", "QS3DDRAWGLASSWALL", "QS3DDRAWWALLPIER", "QS3DDRAWBEAM",
    "QS3DDRAWSTRUCTWALL", "QS3DDRAWCOLUMN", "QS3DDRAWSLAB", "QS3DDRAWFOUNDATION",
    "QS3DDRAWDOOR", "QS3DDRAWOPENING",
):
    if commands.count(name) != 1:
        errors.append(name + " must be declared exactly once, found " + str(commands.count(name)))

source = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs"
if source.is_file():
    text = source.read_text(encoding="utf-8")
    if "new Build3DCommands().Build3D()" not in text:
        errors.append("Direct Draw P1 must reuse canonical QS3DBUILD3D rather than fork native category builders")
    for forbidden in ("new WallFootprintEngine()", "CreateBox(", "CreateExtrudedSolid(", "return value > 0d ? value : fallback;"):
        if forbidden in text:
            errors.append("Direct Draw P1 contains stale/duplicated authoring behavior: " + forbidden)
    for key in ("ThicknessM", "HeightM", "BottomOffsetM"):
        if 'element.Properties["' + key + '"]' in text:
            errors.append("Direct Draw P1 must not bypass ProjectElement.SetProperty for geometry parameter " + key)
    if text.count("element.SetProperty(") < 11:
        errors.append("Direct Draw P1 parameter writes must flow through canonical ProjectElement.SetProperty dirty/stale semantics")
    if text.count("RequireModelSpace(document);") < 4:
        errors.append("Every Direct Draw P1 native command must fail closed outside Model Space")
    if text.count('element.SetProperty("BottomOffsetM"') < 4:
        errors.append("All Direct Draw P1 native commands must persist source-relative BottomOffsetM through ProjectElement.SetProperty")
    if "Sửa Family trước khi Direct Draw." not in text or "if (!(value > 0d))" not in text:
        errors.append("Direct Draw P1 must fail closed on invalid configured Family numerics instead of silently substituting fallback values")
    if "CadHandleService.GetLiveHandles(document, new[] { generatedHandle })" not in text:
        errors.append("Direct Draw P1 must verify QS3DBUILD3D produced a live generated solid")
    if "CadGeometryGuard.Finite(points[index].X" not in text or "CadGeometryGuard.Finite(points[index].Y" not in text:
        errors.append("Direct Draw P1 POLYLINE persistence must finite-check every X/Y coordinate")
    if 'CommandMethod("QS3DDRAWOPENING"' in text or 'CommandMethod("QS3DDRAWDOOR"' in text:
        errors.append("Door/Opening Direct Draw belongs in the separate host-aware DirectDrawOpeningCommands lifecycle, not the native P1 builder wrapper")

    wallpier_body = text.split('[CommandMethod("QS3DDRAWWALLPIER"', 1)[-1].split('[CommandMethod("QS3DDRAWSTRUCTWALL"', 1)[0]
    if 'AcquireFixedPath(document, "Trụ Tường", 2)' not in wallpier_body:
        errors.append("WallPier Direct Draw must acquire exactly two points")
    if "() => CreateLine(document, points[0], points[1])" not in wallpier_body:
        errors.append("WallPier Direct Draw must persist a LINE source")
    if 'AcquirePath(document, "Trụ Tường"' in wallpier_body or "CreatePolyline(document, points, false)" in wallpier_body:
        errors.append("WallPier Direct Draw must not accept open-POLYLINE paths until a deterministic profile-around-corners contract exists")

    create = text.find("sourceId = createSource();")
    capture = text.find("SemanticCaptureService.Capture(document, category)")
    active_check = text.find('EnsureActive(document, operation + " / QS3DBUILD3D")')
    build = text.find("new Build3DCommands().Build3D()")
    verify = text.find('createdElement.Properties.TryGetValue("GeneratedSolidHandle"')
    discover = text.find("GeneratedGeometryService.FindMatchingOwnedHandles")
    erase = text.find("EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)")
    restore = text.find("rollback.Restore(project)")
    finalize = text.find("FinalizeUi(document, createdElement!, sourceId, generatedHandle)")
    if min(create, capture, active_check, build, verify, discover, erase, restore, finalize) < 0:
        errors.append("Direct Draw P1 lifecycle ordering tokens are incomplete")
    elif not (create < capture < active_check < build < verify < discover < erase < restore < finalize):
        errors.append("Direct Draw P1 must create/capture -> re-check active DWG -> canonical build/live verify; failure must discover ownership -> erase CAD -> restore project; UI finalization must run after rollback-critical scope")

    erase_body = text.split("private static void EraseDirectDrawCad", 1)[-1].split("private static void FinalizeUi", 1)[0]
    if "GeneratedGeometryService.RequireMatchingOwnership" not in erase_body:
        errors.append("Direct Draw P1 generated rollback must validate XData ownership before erasing generated CAD")
    if "source.Erase(true)" not in erase_body or "remainingSource" not in erase_body or "remainingGenerated" not in erase_body:
        errors.append("Direct Draw P1 rollback must atomically remove and verify both operation source and generated CAD")

    finalize_body = text.split("private static void FinalizeUi", 1)[-1].split("private static void EnsureActive", 1)[0]
    if "try" not in finalize_body or "UI sync warning" not in finalize_body:
        errors.append("Direct Draw P1 UI finalization must be best-effort and must not convert a successful CAD/project commit into rollback")

print("QS3D Direct Draw P1 preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Native Direct Draw P1 compiles its project snapshot rollback contract, keeps WallPier Direct Draw on the two-point specialized LINE profile path, reuses canonical QS3DBUILD3D and preserves ownership-safe lifecycle invariants; the separately implemented Door/Opening extension is present, uniquely registered, host-aware and guarded with physical boolean kept explicit.")
