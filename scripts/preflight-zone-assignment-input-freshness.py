from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/ProjectZoneService.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ZoneAssignmentInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/ZoneAssignmentInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

capture = "var targetEnumerationVersion = project.ChangeVersion;"
enumeration = "foreach (var element in elements)"
freshness = "if (project.ChangeVersion != targetEnumerationVersion)"
changed = "var changed = unique.Values"
message = 'throw new InvalidOperationException("Project changed while Zone assignment targets were being enumerated. Retry assignment against the current project state.");'

for token in (capture, enumeration, freshness, changed, message):
    assert token in source, f"missing Zone assignment input-freshness contract: {token}"

capture_pos = source.index(capture)
enumeration_pos = source.index(enumeration, capture_pos)
freshness_pos = source.index(freshness, enumeration_pos)
changed_pos = source.index(changed, freshness_pos)
assert capture_pos < enumeration_pos < freshness_pos < changed_pos, (
    "Zone assignment freshness ordering changed: version capture must precede target enumeration, "
    "and freshness rejection must precede changed-target calculation"
)

for token in (
    "StableLazyInputAssignsZone",
    "MutatingLazyInputFailsBeforeAssignment",
    "MutatingEmptyInputFailsBeforeNoOp",
    "TouchThenYield",
    "TouchThenStop",
    "Project changed while Zone assignment targets were being enumerated",
):
    assert token in smoke, f"missing Zone assignment input-freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "Zone assignment input freshness smoke is not registered"
assert "ZoneAssignmentInputFreshnessSmoke.Run();" in registration, "Zone assignment input freshness smoke registration drifted"

print("PASS: Zone assignment input freshness contract is locked")
