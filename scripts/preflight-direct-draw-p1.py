#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = {
    "src/QS3D.BricsCAD.V25/DirectDrawP1Commands.cs": [
        'CommandMethod("QS3DDRAWGLASSWALL"',
        'CommandMethod("QS3DDRAWWALLPIER"',
        'CommandMethod("QS3DDRAWSTRUCTWALL"',
        'CommandMethod("QS3DDRAWFOUNDATION"',
        "SemanticCaptureService.Capture(document, category)",
        "ProjectStateSnapshot.Capture(project)",
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "GeneratedGeometryService.FindMatchingOwnedHandles(document, project.ProjectId, elementId, category)",
        "new Build3DCommands().Build3D()",
        'element.Properties.TryGetValue("GeneratedSolidHandle"',
        "CadHandleService.GetLiveHandles(document, new[] { generatedHandle })",
        "rollback.Restore(project)",
        "EraseHandles(document, cleanupHandles)",
        "PlanarityToleranceM = 0.005d",
        "RequireModelSpace(document)",
        "CadGeometryGuard.ToMeters(document, deltaDrawing",
        "PreferredFamily",
        "Sửa Family trước khi Direct Draw.",
        "Offset đáy Vách Kính so với Z source (m)",
        "Offset đáy Trụ Tường so với Z source (m)",
        "Offset đáy Vách BTCT so với Z source (m)",
        "Offset đáy Móng so với Z source (m)",
        'ElementCategory.GlassWall',
        'ElementCategory.WallPier',
        'ElementCategory.StructuralWall',
        'ElementCategory.Foundation',
    ],
    "src/QS3D.BricsCAD.V25/Build3DCommands.cs": [
        'CommandMethod("QS3DBUILD3D"',
        "StructuralSolidBuilder.Supports(category)",
        "category == ElementCategory.GlassWall",
        "category == ElementCategory.WallPier",
        "AreAllModelSpaceEntities(document, sourceIds)",
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
        'RibbonTabSpec("QS3D_AUTHOR", "TẠO MỚI"',
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
        "Door / Opening Direct Draw is not included",
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
    if "priorGenerated.Contains(handle)" not in text:
        errors.append("Direct Draw P1 rollback must preserve generated handles that existed before the operation")
    if text.count("RequireModelSpace(document);") < 4:
        errors.append("Every Direct Draw P1 command must fail closed outside Model Space")
    if text.count('element.Properties["BottomOffsetM"]') < 4:
        errors.append("All Direct Draw P1 commands must persist source-relative BottomOffsetM")
    if "Sửa Family trước khi Direct Draw." not in text or "if (!(value > 0d))" not in text:
        errors.append("Direct Draw P1 must fail closed on invalid configured Family numerics instead of silently substituting fallback values")
    if "CadHandleService.GetLiveHandles(document, new[] { generatedHandle })" not in text:
        errors.append("Direct Draw P1 must verify QS3DBUILD3D produced a live generated solid")
    if 'CommandMethod("QS3DDRAWOPENING"' in text or 'CommandMethod("QS3DDRAWDOOR"' in text:
        errors.append("Door/Opening Direct Draw must not be introduced without explicit host/link/boolean authoring contract")
    create = text.find("var sourceId = createSource();")
    capture = text.find("SemanticCaptureService.Capture(document, category)")
    build = text.find("new Build3DCommands().Build3D()")
    verify = text.find('element.Properties.TryGetValue("GeneratedSolidHandle"')
    discover = text.find("GeneratedGeometryService.FindMatchingOwnedHandles")
    restore = text.find("rollback.Restore(project)")
    erase = text.find("EraseHandles(document, cleanupHandles)")
    if min(create, capture, build, verify, discover, restore, erase) < 0 or not (create < capture < build < verify < discover < restore < erase):
        errors.append("Direct Draw P1 ordering must be source -> capture -> canonical build -> live verify; failure discovers tagged output before project restore/CAD cleanup")

print("QS3D Direct Draw P1 preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Direct Draw P1 is BricsCAD-hosted, Model-Space/unit-aware, uses source-relative offsets, rejects invalid Family numerics, reuses canonical QS3DBUILD3D, and keeps ownership-aware rollback without guessed Door/Opening authoring.")
