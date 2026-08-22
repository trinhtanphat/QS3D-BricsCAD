from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/RegenerationEngine.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/RegenerationDirtySubsetInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/RegenerationDirtySubsetInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

capture = "var materializeVersion = project.ChangeVersion;"
materialize = "var unresolved = CanonicalTargetIds(elementIds, maxCount);"
freshness = "if (project.ChangeVersion != materializeVersion)"
zero_target = "if (unresolved.Count == 0)"
message = 'throw new InvalidOperationException("Project state changed while materializing regeneration targets.");'

for token in (capture, materialize, freshness, zero_target, message):
    assert token in source, f"missing regeneration input-freshness contract: {token}"

capture_pos = source.index(capture)
materialize_pos = source.index(materialize)
freshness_pos = source.index(freshness)
zero_target_pos = source.index(zero_target)
assert capture_pos < materialize_pos < freshness_pos < zero_target_pos, (
    "regeneration target freshness ordering changed: version capture must precede materialization, "
    "and freshness rejection must precede the zero-target no-op"
)

for token in (
    "StableLazyInputRegenerates",
    "MutatingLazyInputFailsClosed",
    "MutatingEmptyLazyInputFailsClosed",
    "project.Touch()",
    "InvalidOperationException",
):
    assert token in smoke, f"missing regeneration input-freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "dirty-subset input freshness smoke is not registered"
assert "RegenerationDirtySubsetInputFreshnessSmoke.Run();" in registration, "dirty-subset input freshness smoke registration drifted"

print("PASS: regeneration dirty-subset input freshness contract is locked")
