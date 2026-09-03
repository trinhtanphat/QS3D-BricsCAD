from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Domain/GridNamingService.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/GridNamingInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/GridNamingInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

capture = "var targetEnumerationVersion = project.ChangeVersion;"
enumerator = "using (var enumerator = orderedGridElementIds.GetEnumerator())"
pre_move_freshness = "RequireStableKnownCountDuringTraversal(project, orderedGridElementIds, knownCount, targetEnumerationVersion);"
move = "if (!enumerator.MoveNext()) break;"
current = "var value = enumerator.Current;"
freshness = "if (project.ChangeVersion != targetEnumerationVersion)"
empty_check = "if (ids.Count == 0)"
message = 'throw new InvalidOperationException("Project changed while Grid renumber targets were being enumerated. Retry renumbering against the current project state.");'

for token in (capture, enumerator, pre_move_freshness, move, current, freshness, empty_check, message):
    assert token in source, f"missing Grid naming input-freshness contract: {token}"

capture_pos = source.index(capture)
enumerator_pos = source.index(enumerator)
pre_move_pos = source.index(pre_move_freshness, enumerator_pos)
move_pos = source.index(move, pre_move_pos)
post_move_pos = source.index(pre_move_freshness, move_pos)
current_pos = source.index(current, post_move_pos)
freshness_pos = source.index(freshness, current_pos)
empty_pos = source.index(empty_check)
assert capture_pos < enumerator_pos < pre_move_pos < move_pos < post_move_pos < current_pos < freshness_pos < empty_pos, (
    "Grid naming input freshness ordering changed: version capture must precede explicit target traversal, "
    "freshness/Count rebound must bracket MoveNext before semantic Current, and final freshness rejection must precede empty-input validation"
)
assert "foreach (var value in orderedGridElementIds)" not in source, (
    "Grid naming input freshness must not regress to caller-controlled foreach traversal"
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
