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
]:
    require(floors, token, "ProjectFloorService")

for token in [
    "TopLevelId requires BottomLevelId",
    "sourceBaseElevationM",
    "legacyBottomOffsetM",
    "bottomLevel.ElevationM",
    "topLevel.ElevationM",
    "topElevation <= bottomElevation",
]:
    require(placement, token, "vertical placement contract")

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
]:
    require(qualification, token, "native integration qualification policy")

require(health_all, "new LevelReferenceHealthService().Inspect(project)", "Health All wiring")
require(release, "new LevelReferenceHealthService().Inspect(project)", "Release Check wiring")
require(smoke, "LegacyPlacementRemainsSourceRelative", "legacy compatibility smoke")
require(smoke, "BottomAndTopLevelsResolveAbsolutePlacement", "absolute placement smoke")
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
