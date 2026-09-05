#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.exists():
        print(f"[FAIL] missing {path}")
        sys.exit(1)
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        print(f"[FAIL] {label}: missing {token}")
        sys.exit(1)


def find_any(text: str, tokens, start: int = 0) -> int:
    matches = [text.find(token, start) for token in tokens]
    matches = [index for index in matches if index >= 0]
    return min(matches) if matches else -1


floors = read("src/QS3D.Core/Domain/ProjectFloorService.cs")
placement = read("src/QS3D.Core/Domain/ElementVerticalPlacementService.cs")
geometry_policy = read("src/QS3D.Core/Domain/ElementGeometryPolicy.cs")
semantic_regenerators = read("src/QS3D.Core/Services/SemanticRegenerators.cs")
structural_regenerator = read("src/QS3D.Core/Services/StructuralRegenerator.cs")
health = read("src/QS3D.Core/Diagnostics/LevelReferenceHealthService.cs")
qualification = read("src/QS3D.Core/Diagnostics/LevelReferenceNativeIntegrationPolicy.cs")
health_all = read("src/QS3D.BricsCAD.V25/HealthAllCommands.cs")
release = read("src/QS3D.BricsCAD.V25/ReleaseReadinessCommands.cs")
smoke = read("tests/QS3D.Core.SmokeTests/LevelReferenceSmoke.cs")
registration = read("tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs")
floor_ui = read("src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml")
floor_ui_code = read("src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs")
cad_placement = read("src/QS3D.BricsCAD.V25/Cad/CadElementVerticalPlacement.cs")
auto_host = read("src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs")
runtime_probe = read("src/QS3D.BricsCAD.V25/LevelZRuntimeProbeCommands.cs")
runtime_runner = read("scripts/test-bricscad-v25-level-z.ps1")
wall_builder = read("src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs")
structural_builder = read("src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs")
opening_boolean = read("src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs")
curtain_frame = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs")
curtain_panel = read("src/QS3D.BricsCAD.V25/Cad/CurtainWallPanelSolidBuilder.cs")
rebar_builders = "\n".join(
    read(path)
    for path in [
        "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs",
        "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs",
        "src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs",
        "src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs",
        "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs",
    ]
)
beam_rebar = read("src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs")
slab_mesh = read("src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs")
wall_mesh = read("src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs")
semantic_regenerators = read("src/QS3D.Core/Services/SemanticRegenerators.cs")
structural_regenerator = read("src/QS3D.Core/Services/StructuralRegenerator.cs")

for token in [
    'BottomLevelIdKey = "BottomLevelId"',
    'BottomLevelOffsetKey = "BottomLevelOffsetM"',
    'TopLevelIdKey = "TopLevelId"',
    'TopLevelOffsetKey = "TopLevelOffsetM"',
    "AssignBottomLevel",
    "AssignTopLevel",
    "ClearVerticalLevels",
    "ReferencesFloor",
    "new DependencyGraph()",
    "GetDependentsTransitive",
    "dependentIds.ExceptWith(referencedIds)",
]:
    require(floors, token, "ProjectFloorService")

for token in [
    "ProjectFloorService.BottomLevelIdKey",
    "ProjectFloorService.BottomLevelOffsetKey",
    "ProjectFloorService.TopLevelIdKey",
    "ProjectFloorService.TopLevelOffsetKey",
]:
    require(geometry_policy, token, "level geometry invalidation policy")

for token in [
    "TopLevelId requires BottomLevelId",
    "sourceBaseElevationM",
    "legacyBottomOffsetM",
    "topElevation <= bottomElevation",
    "public static bool HasAnyLevelConfiguration",
]:
    require(placement, token, "vertical placement contract")

legacy_floor_resolution = all(
    token in placement
    for token in (
        "var bottomLevel = FindFloor",
        "bottomLevel.ElevationM",
        "var topLevel = FindFloor",
        "topLevel.ElevationM",
    )
)
captured_floor_resolution = all(
    token in placement
    for token in (
        "CaptureFloorGeneration(project)",
        "FindCapturedFloor(",
        "bottomLevelElevation",
        "topLevelElevation",
        "StringComparer.OrdinalIgnoreCase",
    )
)
if not legacy_floor_resolution and not captured_floor_resolution:
    print("[FAIL] vertical placement must resolve Bottom/Top Level elevations through either unique live lookup or one fenced captured floor generation")
    sys.exit(1)

level_lookup = placement.find("var bottomLevelId")
legacy_branch = placement.find("if (bottomLevelId.Length == 0)", level_lookup)
legacy_source_validation = placement.find("Finite(sourceBaseElevationM", legacy_branch)
bottom_resolution = find_any(
    placement,
    ("var bottomLevel = FindFloor", "var bottomLevelElevation = FindCapturedFloor"),
    legacy_source_validation,
)
bottom_only_branch = placement.find("if (topLevelId.Length == 0)", bottom_resolution)
bottom_height_validation = placement.find("Positive(legacyHeightM", bottom_only_branch)
top_resolution = find_any(
    placement,
    ("var topLevel = FindFloor", "var topLevelElevation = FindCapturedFloor"),
    bottom_height_validation,
)
if min(level_lookup, legacy_branch, legacy_source_validation, bottom_resolution, bottom_only_branch, bottom_height_validation, top_resolution) < 0 or not (
    level_lookup < legacy_branch < legacy_source_validation < bottom_resolution < bottom_only_branch < bottom_height_validation < top_resolution
):
    print("[FAIL] vertical placement must validate legacy source/offset/height only inside branches that consume those inputs")
    sys.exit(1)

for token in [
    '"BOTTOM_LEVEL_REFERENCE_INVALID"',
    '"TOP_LEVEL_REFERENCE_INVALID"',
    '"TOP_LEVEL_REQUIRES_BOTTOM_LEVEL"',
    '"BOTTOM_LEVEL_OFFSET_INVALID"',
    '"TOP_LEVEL_OFFSET_INVALID"',
    '"LEVEL_RANGE_INVALID"',
    '"LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING"',
    "LevelReferenceNativeIntegrationPolicy.IsQualified(element.Category)",
    "AddNativeIntegrationPendingIfSemanticallyValid",
]:
    require(health, token, "level health")

for token in [
    "public static class LevelReferenceNativeIntegrationPolicy",
    "public static bool IsQualified(ElementCategory category)",
    "HasConfiguredReferences",
    "EnsureQualified",
    "until its native host and dependent placement chain is qualified",
    "case ElementCategory.ArchitecturalWall:",
    "case ElementCategory.GlassWall:",
    "case ElementCategory.Beam:",
    "case ElementCategory.Slab:",
    "case ElementCategory.Column:",
    "case ElementCategory.Foundation:",
    "case ElementCategory.Door:",
    "case ElementCategory.WallOpening:",
    "default:",
]:
    require(qualification, token, "native integration qualification policy")

require(health_all, "new LevelReferenceHealthService().Inspect(project)", "Health All wiring")
require(release, "new LevelReferenceHealthService().Inspect(project)", "Release Check wiring")
require(smoke, "LegacyPlacementRemainsSourceRelative", "legacy compatibility smoke")
require(smoke, "BottomAndTopLevelsResolveAbsolutePlacement", "absolute placement smoke")
require(smoke, "LevelReferencesValidateOnlyConsumedLegacyInputs", "branch-local legacy input validation smoke")
require(smoke, "transitive.IsGeneratedSolidStale()", "transitive floor invalidation smoke")
require(smoke, "bounded-ignores-all-legacy", "Bottom+Top legacy-independence smoke")
require(smoke, "TopAssignmentRequiresBottomAndValidRange", "assignment safety smoke")
require(smoke, "FloorMutationTracksAllReferenceKinds", "floor lifecycle smoke")
require(smoke, "HealthAcceptsQualifiedNativeCategories", "native qualification smoke")
require(smoke, "VerticalSnapshotsDetectStaleNativeOutputs", "native vertical snapshot health smoke")
require(smoke, "OpeningLevelChangesInvalidateHostOutputs", "opening-host invalidation smoke")
require(smoke, "UnsupportedCategoryFailsBeforeQuantityMutation", "unsupported-category mutation smoke")
require(registration, "LevelReferenceSmoke.Run();", "smoke registration")

for token in [
    "ElementVerticalPlacementService.Resolve",
    'LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Native 3D Level placement")',
    "FingerprintBottomM",
    "CommitSnapshot",
    "CadHostedOpeningVerticalPlacement",
]:
    require(cad_placement, token, "shared CAD Level placement adapter")

for source, label in [
    (wall_builder, "wall native builder"),
    (structural_builder, "structural native builder"),
    (opening_boolean, "opening boolean chain"),
    (curtain_frame, "curtain frame chain"),
    (curtain_panel, "curtain panel chain"),
]:
    require(source, "CadElementVerticalPlacement", label)
require(opening_boolean, "CadHostedOpeningVerticalPlacement", "opening boolean chain")
for token in [
    "UsesLevelPlacement = usesLevelPlacement",
    "openingLocation.UsesLevelPlacement || hostUsesLevelPlacement",
    "opening.TopElevationM.Value <= host.TopElevationM + toleranceM",
    "CadElementVerticalPlacement.Resolve(",
]:
    require(auto_host, token, "Auto Host mixed legacy/Level elevation matching")
for token in ["GeneratedRebar", "GeneratedSlabMesh", "GeneratedWallMesh", "GeneratedFoundationMesh"]:
    require(rebar_builders, token, "rebar Level snapshot chain")
require(rebar_builders, "CadElementVerticalPlacement", "rebar Level placement chain")
require(beam_rebar, 'line.StartPoint.Z, "HeightM", .5d', "Beam host/rebar fallback alignment")
require(slab_mesh, '"ThicknessM",\n                            .12d', "Slab host/mesh fallback alignment")
require(wall_mesh, '"HeightM",\n                            3.6d', "StructuralWall host/mesh fallback alignment")
require(semantic_regenerators, "SemanticVertical.Height", "wall/opening effective quantity chain")
require(structural_regenerator, "SemanticVertical.Height", "structural effective quantity chain")
require(
    semantic_regenerators,
    'LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Quantity regeneration with Level references")',
    "semantic quantity unsupported-category gate",
)
require(
    structural_regenerator,
    'LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Structural quantity regeneration with Level references")',
    "structural quantity unsupported-category gate",
)
require(health, '"LEVEL_NATIVE_VERTICAL_SNAPSHOT_STALE"', "Level native snapshot health")

for token in ["Gán Level đáy", "Gán Level đỉnh", "Xóa Level đứng"]:
    require(floor_ui, token, "Level Picker vertical assignment UI")
for token in [
    "OnAssignBottomLevelClick",
    "OnAssignTopLevelClick",
    "OnClearVerticalLevelsClick",
    "AssignBottomLevel",
    "AssignTopLevel",
    "ClearVerticalLevels",
    "LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, operation)",
]:
    require(floor_ui_code, token, "Level Picker guarded mutation wiring")

for token in [
    'CommandMethod("QS3DLEVELZPROBE"',
    "QS3D_LEVEL_Z_SOURCE_SHA",
    "RequireAssemblyRevision",
    "VerifyTopOnlyFailsBeforeMutation",
    "OpeningBooleanService.CutLinkedOpenings",
    "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls",
    "CurtainWallPanelSolidBuilder.BuildSelectedLineWalls",
    "BeamRebarSolidBuilder.BuildSelected",
    "BeamStirrupSolidBuilder.BuildSelected",
    '"LEVEL_NATIVE_VERTICAL_SNAPSHOT_STALE"',
    "ProjectFloorService.Update",
    'ProjectFloorService.Update(project, "L1"',
]:
    require(runtime_probe, token, "licensed Level Z runtime probe")
for token in [
    "ConfirmDisposableCopy",
    "*.level-z-probe-copy.dwg",
    "bricscad-runner-window-interop.ps1",
    ". $windowInteropPath",
    "QS3D_LEVEL_Z_RESULT",
    "QS3D_LEVEL_Z_NONCE",
    "QS3D_LEVEL_Z_SOURCE_SHA",
    "ExpectedSourceSha",
    "status --porcelain=v1 --untracked-files=all",
    "Assert-Qs3dExactSourceIdentity -RepoRoot $repoRoot -PluginDll $PluginDll -ExpectedSourceSha $ExpectedSourceSha",
    "source_sha = $ExpectedSourceSha",
    "drawingHashBefore",
    "drawingHashAfter",
    "QS3D_LEVEL_Z_RUNTIME_V1",
]:
    require(runtime_runner, token, "guarded Level Z runtime runner")

for stale_token in ["Assembly was not built from ExpectedSourceSha", "$expectedAssemblyRevision"]:
    if stale_token in runtime_runner:
        print(f"[FAIL] guarded Level Z runtime runner retains stale ProductVersion source identity: {stale_token}")
        sys.exit(1)

print("[PASS] Level references preserve legacy placement, resolve qualified native/dependent Z through one adapter, expose guarded Bottom/Top/Clear UI, and health-check generated vertical snapshots")
