from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/RegenerationEngine.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/RegenerationDirtySubsetInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/RegenerationDirtySubsetInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

capture_version = "var inputVersion = project.ChangeVersion;"
capture_structure = "var sourceElements = project.Elements.ToArray();"
materialize = "var unresolved = CanonicalTargetIds(elementIds, sourceElements.Length);"
version_freshness = "if (project.ChangeVersion != inputVersion)"
structure_freshness = "RequireElementStructureFresh(project, sourceElements);"
zero_target = "if (unresolved.Count == 0) return 0;"
version_message = 'throw new InvalidOperationException("Project state changed while materializing regeneration target ids.");'
structure_message = '"Project element structure changed while materializing regeneration target ids. Retry targeted regeneration against the current project state."'

for token in (
    capture_version,
    capture_structure,
    materialize,
    version_freshness,
    structure_freshness,
    zero_target,
    version_message,
    "private static void RequireElementStructureFresh",
    "ReferenceEquals(project.Elements[index], sourceElements[index])",
    structure_message,
):
    assert token in source, f"missing regeneration input-freshness contract: {token}"

capture_version_pos = source.index(capture_version)
capture_structure_pos = source.index(capture_structure, capture_version_pos)
materialize_pos = source.index(materialize, capture_structure_pos)
version_freshness_pos = source.index(version_freshness, materialize_pos)
structure_freshness_pos = source.index(structure_freshness, version_freshness_pos)
zero_target_pos = source.index(zero_target, structure_freshness_pos)
assert capture_version_pos < capture_structure_pos < materialize_pos < version_freshness_pos < structure_freshness_pos < zero_target_pos, (
    "regeneration target freshness ordering changed: version/structure capture must precede bounded caller enumeration, "
    "and both revision plus same-instance structural freshness checks must precede the zero-target no-op"
)

for token in (
    "StableLazyInputStillRegenerates",
    "MutatingLazyInputFailsBeforeRegeneration",
    "MutatingEmptyInputFailsBeforeNoOp",
    "project.Touch()",
    "InvalidOperationException",
):
    assert token in smoke, f"missing regeneration input-freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "dirty-subset input freshness smoke is not registered"
assert "RegenerationDirtySubsetInputFreshnessSmoke.Run();" in registration, "dirty-subset input freshness smoke registration drifted"

print("PASS: regeneration dirty-subset input freshness binds bounded caller enumeration to ChangeVersion plus same-instance project structure before any zero-target no-op or regeneration")
