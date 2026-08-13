#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing Level native host file: " + relative)
        return ""
    return path.read_text(encoding="utf-8")


resolver = read("src/QS3D.BricsCAD.V25/Cad/CadElementVerticalPlacement.cs")
wall = read("src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs")
path_wall = read("src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs")
wall_pier = read("src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs")
structural = read("src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs")
structural_regenerator = read("src/QS3D.Core/Services/StructuralRegenerator.cs")
policy = read("src/QS3D.Core/Diagnostics/LevelReferenceNativeIntegrationPolicy.cs")

for token in (
    'LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Native 3D Level placement")',
    "ElementVerticalPlacementService.Resolve(project, element, sourceBaseM",
    "if (bottomLevelId.Length == 0)",
    "else if (topLevelId.Length == 0)",
    "LegacyHeightM",
    "LegacyBottomOffsetM",
    "BottomDrawing",
    "HeightDrawing",
    "CenterDrawing",
    "FingerprintBottomM",
    "return ElementVerticalPlacementService.HasAnyLevelConfiguration(element);",
    "CommitSnapshot",
    "ClearSnapshot",
):
    if token not in resolver:
        errors.append("shared CAD Level resolver is missing contract token: " + token)

qualification_guard = resolver.find("LevelReferenceNativeIntegrationPolicy.EnsureQualified(element")
level_lookup = resolver.find("var bottomLevelId")
if qualification_guard < 0 or level_lookup < 0 or qualification_guard >= level_lookup:
    errors.append("shared CAD Level resolver must reject unsupported categories before reading Level or legacy placement inputs")

legacy_branch = resolver.find("if (bottomLevelId.Length == 0)")
bottom_only_branch = resolver.find("else if (topLevelId.Length == 0)", legacy_branch)
bounded_branch = resolver.find("else", bottom_only_branch + len("else if (topLevelId.Length == 0)"))
legacy_read = resolver.find("var legacyHeightM = CadGeometryGuard.Number", legacy_branch)
bottom_only_legacy_read = resolver.find("var legacyHeightM = CadGeometryGuard.Number", bottom_only_branch)
bounded_resolve = resolver.find("resolved = ElementVerticalPlacementService.Resolve", bounded_branch)
if min(legacy_branch, bottom_only_branch, bounded_branch, legacy_read, bottom_only_legacy_read, bounded_resolve) < 0:
    errors.append("shared CAD Level resolver must keep explicit legacy, Bottom-only, and Bottom+Top branches")
elif not legacy_branch < legacy_read < bottom_only_branch < bottom_only_legacy_read < bounded_branch < bounded_resolve:
    errors.append("shared CAD Level resolver must read legacy height only in branches that consume it")

for label, text in (
    ("LINE wall", wall),
    ("open-POLYLINE wall", path_wall),
    ("WallPier profile", wall_pier),
):
    resolve = text.find("CadElementVerticalPlacement.Resolve(")
    replace = text.find("GeneratedGeometryService.PrepareReplacement")
    if resolve < 0:
        errors.append(label + " must consume the shared CAD Level resolver")
    if replace < 0 or resolve >= replace:
        errors.append(label + " must resolve placement before replacing native ownership")
    if "CadElementVerticalPlacement.CommitSnapshot" not in text:
        errors.append(label + " must persist a generated vertical snapshot")

structural_dispatch = structural.find("solid = BuildLinePrism(")
structural_replace = structural.find("GeneratedGeometryService.PrepareReplacement")
if structural_dispatch < 0 or structural_replace < 0 or structural_dispatch >= structural_replace:
    errors.append("structural host must dispatch a placement-validating builder before replacing native ownership")
if "CadElementVerticalPlacement.Resolve(" not in structural:
    errors.append("structural host must consume the shared CAD Level resolver")
if "CadElementVerticalPlacement.CommitSnapshot" not in structural:
    errors.append("structural host must persist a generated vertical snapshot")

for token in (
    "case ElementCategory.Railing: RegenerateRailing(project, element); break;",
    "private static void RegenerateRailing(ProjectState project, ProjectElement element)",
    "ElementVerticalPlacementService.HasAnyLevelConfiguration(element)",
    'SemanticVertical.Height(project, element, "HeightM", 1.1d)',
):
    if token not in structural_regenerator:
        errors.append("Railing semantic quantities must share effective Level height: " + token)

for label, text in (
    ("LINE wall", wall),
    ("POLYLINE wall", path_wall),
    ("WallPier", wall_pier),
):
    if ('update.LegacyHeightM.HasValue' not in text or
            'update.Element.Properties["HeightM"] = update.LegacyHeightM.Value' not in text):
        errors.append(label + " must preserve legacy HeightM so clearing Level references restores legacy geometry")

for label, text in (
    ("LINE wall", wall),
    ("open-POLYLINE wall", path_wall),
    ("WallPier profile", wall_pier),
):
    if "LegacyHeightM" not in text:
        errors.append(label + " must preserve the legacy HeightM token when the legacy branch consumes it")

for token in (
    "case ElementCategory.Stair:",
    "case ElementCategory.Railing:",
    "vertical = CadElementVerticalPlacement.Resolve(",
    "CadGeometryGuard.Subtract(vertical.BottomDrawing, polyline.Elevation",
    "case ElementCategory.Earthwork:",
    'CadGeometryGuard.Number(element, family, "TopOffsetM", 0d)',
):
    if token not in structural:
        errors.append("structural host Level/legacy split is missing token: " + token)

for category in (
    "ArchitecturalWall",
    "GlassWall",
    "WallPier",
    "StructuralWall",
    "Beam",
    "Slab",
    "Column",
    "Foundation",
    "Stair",
    "Railing",
    "Door",
    "WallOpening",
):
    if "case ElementCategory." + category + ":" not in policy:
        errors.append("native Level qualification policy is missing completed category: " + category)
if "default:" not in policy or "return false;" not in policy:
    errors.append("native Level qualification policy must continue to fail closed for unsupported categories")

if (ROOT / "src/QS3D.BricsCAD.V25/Cad/CadVerticalPlacementResolver.cs").exists():
    errors.append("obsolete first-wave CadVerticalPlacementResolver.cs must be removed; native Level arithmetic has one adapter")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: wall and structural native hosts share one branch-lazy Level resolver, preserve legacy dimensions, snapshot resolved Z, and fail closed outside qualified categories.")
