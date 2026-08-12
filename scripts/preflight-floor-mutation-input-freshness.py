from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/ProjectFloorService.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/FloorMutationInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/FloorMutationInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

helper = "private static IReadOnlyList<ProjectElement> ResolveOwnedElements(ProjectState project, IEnumerable<ProjectElement> elements)"
capture = "var targetEnumerationVersion = project.ChangeVersion;"
enumeration = "foreach (var element in elements)"
freshness = "if (project.ChangeVersion != targetEnumerationVersion)"
ordered_return = "return unique.Values.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();"
message = 'throw new InvalidOperationException("Project changed while Floor mutation targets were being enumerated. Retry the operation against the current project state.");'

for token in (helper, capture, enumeration, freshness, ordered_return, message):
    assert token in source, f"missing Floor mutation input-freshness contract: {token}"

helper_pos = source.index(helper)
capture_pos = source.index(capture, helper_pos)
enumeration_pos = source.index(enumeration, capture_pos)
freshness_pos = source.index(freshness, enumeration_pos)
return_pos = source.index(ordered_return, freshness_pos)
assert helper_pos < capture_pos < enumeration_pos < freshness_pos < return_pos, (
    "Floor mutation freshness ordering changed: version capture must precede caller enumeration, "
    "and freshness rejection must precede resolved-target return"
)

assert source.count("ResolveOwnedElements(project, elements);") >= 4, (
    "Floor mutation freshness helper must remain shared by Assign, AssignBottomLevel, AssignTopLevel, and ClearVerticalLevels"
)
for method in ("public static int Assign(", "public static int AssignBottomLevel(", "public static int AssignTopLevel(", "public static int ClearVerticalLevels("):
    assert method in source, f"missing protected Floor mutation caller: {method}"

for token in (
    "StableLazyInputAssignsFloor",
    "MutatingLazyInputFailsBeforeFloorAssignment",
    "MutatingEmptyInputFailsBeforeNoOp",
    "MutatingBottomLevelInputUsesSharedGuard",
    "TouchThenYield",
    "TouchThenStop",
    "Project changed while Floor mutation targets were being enumerated",
):
    assert token in smoke, f"missing Floor mutation input-freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "Floor mutation input freshness smoke is not registered"
assert "FloorMutationInputFreshnessSmoke.Run();" in registration, "Floor mutation input freshness smoke registration drifted"

print("PASS: Floor mutation input freshness contract is locked")
