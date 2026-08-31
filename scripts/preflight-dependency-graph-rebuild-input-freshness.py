from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/DependencyGraph.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/DependencyGraphRebuildInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/DependencyGraphRebuildInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

for token in (
    "private long _rebuildVersion;",
    "var enumerationVersion = _rebuildVersion;",
    "using (var enumerator = elements.GetEnumerator())",
    "while (true)",
    'RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency graph rebuild");',
    "if (!enumerator.MoveNext())",
    "RequireTraversalCapacity(knownCount, elementCount, \"Dependency graph rebuild\");",
    "var element = enumerator.Current;",
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
enumerator_pos = rebuild.index("using (var enumerator = elements.GetEnumerator())", capture_pos)
loop_pos = rebuild.index("while (true)", enumerator_pos)
stable = 'RequireStableKnownCount(elements, knownCount, knownCountSources, "Dependency graph rebuild");'
pre_move_count_pos = rebuild.index(stable, loop_pos)
move_next_pos = rebuild.index("if (!enumerator.MoveNext())", pre_move_count_pos)
termination_count_pos = rebuild.index(stable, move_next_pos + 1)
break_pos = rebuild.index("break;", termination_count_pos)
post_move_count_pos = rebuild.index(stable, break_pos + 1)
capacity_pos = rebuild.index("RequireTraversalCapacity(knownCount, elementCount, \"Dependency graph rebuild\");", post_move_count_pos)
current_pos = rebuild.index("var element = enumerator.Current;", capacity_pos)
freshness_pos = rebuild.index("if (_rebuildVersion != enumerationVersion)", current_pos)
next_version_pos = rebuild.index("var nextVersion = checked(_rebuildVersion + 1L);", freshness_pos)
dependents_clear_pos = rebuild.index("_dependents.Clear();", next_version_pos)
elements_clear_pos = rebuild.index("_elementsById.Clear();", dependents_clear_pos)
revision_apply_pos = rebuild.index("_rebuildVersion = nextVersion;", elements_clear_pos)
assert capture_pos < enumerator_pos < loop_pos < pre_move_count_pos < move_next_pos < termination_count_pos < break_pos < post_move_count_pos < capacity_pos < current_pos < freshness_pos < next_version_pos < dependents_clear_pos < elements_clear_pos < revision_apply_pos, (
    "DependencyGraph.Rebuild traversal/count/freshness/apply ordering changed"
)

assert "while (enumerator.MoveNext())" not in rebuild, (
    "DependencyGraph.Rebuild must rebind known Count before caller-controlled MoveNext"
)
assert "foreach (var element in elements)" not in rebuild, (
    "DependencyGraph.Rebuild must not reintroduce foreach because Current would be observed before known-Count rejection"
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

print("PASS: DependencyGraph rebuild input freshness preserves Count-safe traversal and reentrant graph-state protection")
