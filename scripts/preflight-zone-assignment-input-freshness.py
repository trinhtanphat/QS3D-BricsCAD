from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/ProjectZoneService.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/ZoneAssignmentInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/ZoneAssignmentInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

capture = "var targetEnumerationVersion = project.ChangeVersion;"
enumerator = "using (var enumerator = elements.GetEnumerator())"
move_next = "while (enumerator.MoveNext())"
freshness = "if (project.ChangeVersion != targetEnumerationVersion)"
changed = "var changed = unique.Values"
message = 'throw new InvalidOperationException("Project changed while Zone assignment targets were being enumerated. Retry assignment against the current project state.");'

for token in (capture, enumerator, move_next, freshness, changed, message):
    assert token in source, f"missing Zone assignment input-freshness contract: {token}"

capture_pos = source.index(capture)
enumerator_pos = source.index(enumerator, capture_pos)
move_next_pos = source.index(move_next, enumerator_pos)
freshness_pos = source.index(freshness, move_next_pos)
changed_pos = source.index(changed, freshness_pos)
assert capture_pos < enumerator_pos < move_next_pos < freshness_pos < changed_pos, (
    "Zone assignment freshness ordering changed: version capture must precede explicit target traversal, "
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

print("PASS: Zone assignment input freshness contract is locked across explicit target traversal")