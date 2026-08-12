from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/ProjectFloorService.cs").read_text(encoding="utf-8")
compat = (root / "src/QS3D.Core/Domain/DictionaryCompatibilityExtensions.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/FloorReferencedLevelStructuralFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/FloorReferencedLevelStructuralFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

bottom_start = source.index("public static int AssignBottomLevel")
bottom_end = source.index("public static int AssignTopLevel", bottom_start)
bottom = source[bottom_start:bottom_end]
top_start = bottom_end
top_end = source.index("public static int ClearVerticalLevels", top_start)
top = source[top_start:top_end]

for block, level_key, level_label in (
    (bottom, "TopLevelIdKey", '"Top Level"'),
    (top, "BottomLevelIdKey", '"Bottom Level"'),
):
    for token in (
        "var floorOwnership = SnapshotFloorOwnership(project);",
        "var targets = ResolveOwnedElements(project, elements);",
        "RequireCurrentFloorOwnership(project,",
        "RequireReferencedLevelOwnershipUnchanged(project, floorOwnership, targets, " + level_key + ", " + level_label + ");",
        "foreach (var element in targets)",
    ):
        assert token in block, f"missing referenced-level freshness contract: {token}"
    snapshot = block.index("var floorOwnership = SnapshotFloorOwnership(project);")
    enumeration = block.index("var targets = ResolveOwnedElements(project, elements);")
    target_check = block.index("RequireCurrentFloorOwnership(project,")
    referenced_check = block.index("RequireReferencedLevelOwnershipUnchanged(project, floorOwnership, targets")
    validation = block.index("foreach (var element in targets)")
    assert snapshot < enumeration < target_check < referenced_check < validation, (
        "Floor referenced-level freshness ordering drifted"
    )

for token in (
    "private static IReadOnlyDictionary<string, FloorDefinition> SnapshotFloorOwnership",
    "private static void RequireReferencedLevelOwnershipUnchanged",
    "expected.TryGetValue(levelId, out var original)",
    "var current = project.FindFloor(levelId);",
    "!ReferenceEquals(current, original)",
):
    assert token in source, f"missing referenced Floor ownership guard: {token}"

for token in (
    "internal static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary",
    "dictionary.ContainsKey(key)",
    "dictionary.Add(key, value)",
):
    assert token in compat, f"missing netstandard2 Dictionary compatibility contract: {token}"

for token in (
    "StableLazyBottomAssignmentStillWorks",
    "ReplacedTopLevelFailsBottomAssignmentClosed",
    "ReplacedBottomLevelFailsTopAssignmentClosed",
    "RemovedTopLevelFailsBottomAssignmentClosed",
    "ReplaceFloorThenYield",
    "RemoveFloorThenYield",
    "project.ChangeVersion != version",
    "new FloorDefinition(original.Id, original.Name, replacementElevation)",
):
    assert token in smoke, f"missing Floor referenced-level smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "Floor referenced-level freshness smoke is not registered"
assert "FloorReferencedLevelStructuralFreshnessSmoke.Run();" in registration, (
    "Floor referenced-level freshness registration drifted"
)

print("PASS: vertical Floor assignment rejects referenced opposite-level structural ownership drift")
