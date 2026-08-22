from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/GridNamingService.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/GridNamingInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/GridNamingInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

capture = "var targetEnumerationVersion = project.ChangeVersion;"
enumeration = "foreach (var value in orderedGridElementIds)"
freshness = "if (project.ChangeVersion != targetEnumerationVersion)"
empty_check = "if (ids.Count == 0)"
message = 'throw new InvalidOperationException("Project changed while Grid renumber targets were being enumerated. Retry renumbering against the current project state.");'

for token in (capture, enumeration, freshness, empty_check, message):
    assert token in source, f"missing Grid naming input-freshness contract: {token}"

capture_pos = source.index(capture)
enumeration_pos = source.index(enumeration)
freshness_pos = source.index(freshness)
empty_pos = source.index(empty_check)
assert capture_pos < enumeration_pos < freshness_pos < empty_pos, (
    "Grid naming input freshness ordering changed: version capture must precede target enumeration, "
    "and freshness rejection must precede empty-input validation"
)

for token in (
    "StableLazyInputRenumbers",
    "MutatingLazyInputFailsBeforeNamingMutation",
    "MutatingEmptyInputFailsBeforeEmptyValidation",
    "TouchThenYield",
    "TouchThenStop",
    "Project changed while Grid renumber targets were being enumerated",
):
    assert token in smoke, f"missing Grid naming input-freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "Grid naming input freshness smoke is not registered"
assert "GridNamingInputFreshnessSmoke.Run();" in registration, "Grid naming input freshness smoke registration drifted"

print("PASS: Grid naming input freshness contract is locked")
