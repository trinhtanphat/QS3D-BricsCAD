from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/DependencyGraph.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/DependencyGraphRebuildInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/DependencyGraphRebuildInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

for token in (
    "private long _rebuildVersion;",
    "var enumerationVersion = _rebuildVersion;",
    "foreach (var element in elements)",
    "if (_rebuildVersion != enumerationVersion)",
    "var nextVersion = checked(_rebuildVersion + 1L);",
    "_dependents.Clear();",
    "_elementsById.Clear();",
    "_rebuildVersion = nextVersion;",
    "Dependency graph changed while rebuild elements were being enumerated",
):
    assert token in source, f"missing DependencyGraph rebuild freshness contract: {token}"

rebuild_start = source.index("public void Rebuild(IEnumerable<ProjectElement> elements)")
rebuild_end = source.index("public bool TryGetElement", rebuild_start)
rebuild = source[rebuild_start:rebuild_end]

capture_pos = rebuild.index("var enumerationVersion = _rebuildVersion;")
enumeration_pos = rebuild.index("foreach (var element in elements)", capture_pos)
freshness_pos = rebuild.index("if (_rebuildVersion != enumerationVersion)", enumeration_pos)
next_version_pos = rebuild.index("var nextVersion = checked(_rebuildVersion + 1L);", freshness_pos)
dependents_clear_pos = rebuild.index("_dependents.Clear();", next_version_pos)
elements_clear_pos = rebuild.index("_elementsById.Clear();", dependents_clear_pos)
revision_apply_pos = rebuild.index("_rebuildVersion = nextVersion;", elements_clear_pos)
assert capture_pos < enumeration_pos < freshness_pos < next_version_pos < dependents_clear_pos < elements_clear_pos < revision_apply_pos, (
    "DependencyGraph.Rebuild freshness/apply ordering changed"
)

assert "depends on missing semantic element" in rebuild, "existing missing-dependency validation must remain in Rebuild"
assert rebuild.index("depends on missing semantic element") < freshness_pos, (
    "DependencyGraph missing-dependency validation precedence changed"
)

for token in (
    "StableLazyRebuildPreservesGraphSemantics",
    "ReentrantRebuildFailsWithoutOverwritingNewerGraph",
    "ReentrantRebuildWithEmptyOuterInputFailsBeforeClear",
    "RebuildThenYield",
    "RebuildThenStop",
    "Dependency graph changed while rebuild elements were being enumerated",
):
    assert token in smoke, f"missing DependencyGraph rebuild freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "DependencyGraph rebuild freshness smoke is not registered"
assert "DependencyGraphRebuildInputFreshnessSmoke.Run();" in registration, "DependencyGraph rebuild freshness smoke registration drifted"

print("PASS: DependencyGraph rebuild input freshness contract is locked")
