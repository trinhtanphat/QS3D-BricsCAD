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
    "bottomLevel.ElevationM",
    "topLevel.ElevationM",
    "topElevation <= bottomElevation",
    "public static double ResolveEffectiveHeight",
    "return Resolve(project, element, 0d, legacyHeightM, 0d).HeightM;",
]:
    require(placement, token, "vertical placement contract")

effective_height_call = "QualifiedVerticalQuantity.EffectiveHeight(project, "
if semantic_regenerators.count(effective_height_call) < 3:
    print("[FAIL] wall/opening quantity regenerators must route prepared Level spans through the qualification guard")
    sys.exit(1)
if structural_regenerator.count(effective_height_call) < 6:
    print("[FAIL] structural quantity regenerators must route prepared Level spans through the qualification guard")
    sys.exit(1)
for token in [
    "var effectiveHeight = ElementVerticalPlacementService.ResolveEffectiveHeight(project, element, legacyHeightM);",
    'LevelReferenceNativeIntegrationPolicy.EnsureQualified(element, "Quantity regeneration with Level references")',
    "return effectiveHeight;",
]:
    require(semantic_regenerators, token, "qualified quantity Level boundary")
quantity_resolve = semantic_regenerators.find("var effectiveHeight = ElementVerticalPlacementService.ResolveEffectiveHeight")
quantity_guard = semantic_regenerators.find("LevelReferenceNativeIntegrationPolicy.EnsureQualified(element", quantity_resolve)
quantity_return = semantic_regenerators.find("return effectiveHeight;", quantity_guard)
if min(quantity_resolve, quantity_guard, quantity_return) < 0 or not quantity_resolve < quantity_guard < quantity_return:
    print("[FAIL] quantity Level boundary must validate the semantic span before refusing unqualified production use")
    sys.exit(1)

level_lookup = placement.find("var bottomLevelId")
legacy_branch = placement.find("if (bottomLevelId.Length == 0)", level_lookup)
legacy_source_validation = placement.find("Finite(sourceBaseElevationM", legacy_branch)
bottom_resolution = placement.find("var bottomLevel = FindFloor", legacy_source_validation)
bottom_only_branch = placement.find("if (topLevelId.Length == 0)", bottom_resolution)
bottom_height_validation = placement.find("Positive(legacyHeightM", bottom_only_branch)
top_resolution = placement.find("var topLevel = FindFloor", bottom_height_validation)
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
    "return false;",
    "HasConfiguredReferences",
    "EnsureQualified",
    "until its native host and dependent placement chain is qualified",
]:
    require(qualification, token, "native integration qualification policy")

require(health_all, "new LevelReferenceHealthService().Inspect(project)", "Health All wiring")
require(release, "new LevelReferenceHealthService().Inspect(project)", "Release Check wiring")
require(smoke, "LegacyPlacementRemainsSourceRelative", "legacy compatibility smoke")
require(smoke, "BottomAndTopLevelsResolveAbsolutePlacement", "absolute placement smoke")
require(smoke, "LevelReferencesValidateOnlyConsumedLegacyInputs", "branch-local legacy input validation smoke")
require(smoke, "EffectiveHeightIsPreparedWhileProductionRegenerationStaysBlocked", "prepared-but-blocked effective quantity span smoke")
require(smoke, "ResolveEffectiveHeight(project, foundation, double.NaN)", "bounded effective height smoke")
require(smoke, "Throws<InvalidOperationException>(() => new WallRegenerator().Regenerate(project, wall))", "unqualified wall quantity refusal smoke")
require(smoke, "Throws<InvalidOperationException>(() => new StructuralRegenerator().Regenerate(project, foundation))", "unqualified structural quantity refusal smoke")
require(smoke, "transitive.IsGeneratedSolidStale()", "transitive floor invalidation smoke")
require(smoke, "bounded-ignores-all-legacy", "Bottom+Top legacy-independence smoke")
require(smoke, "TopAssignmentRequiresBottomAndValidRange", "assignment safety smoke")
require(smoke, "FloorMutationTracksAllReferenceKinds", "floor lifecycle smoke")
require(smoke, "HealthBlocksValidLevelReferencesUntilNativeQualification", "native qualification blocker smoke")
require(smoke, '"LEVEL_REFERENCE_NATIVE_INTEGRATION_PENDING"', "pending qualification smoke")
require(registration, "LevelReferenceSmoke.Run();", "smoke registration")

# Until native builders consume ElementVerticalPlacementService coherently, the Level Picker must not expose
# buttons that imply Bottom/Top references already control CAD placement.
for forbidden in ["Gán Level đáy", "Gán Level đỉnh", "OnAssignBottomLevelClick", "OnAssignTopLevelClick"]:
    if forbidden in floor_ui:
        print(f"[FAIL] Level Picker exposes native-looking level reference UI before builder integration: {forbidden}")
        sys.exit(1)

print("[PASS] opt-in level reference semantics preserve legacy placement, malformed refs fail closed, and semantically valid refs stay release-blocked until native/dependent placement is explicitly qualified")
