from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/Services/SelectionState.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/SelectionStateReplaceInputFreshnessSmoke.cs").read_text(encoding="utf-8")
registration = (root / "tests/QS3D.Core.SmokeTests/SelectionStateReplaceInputFreshnessSmokeRegistration.cs").read_text(encoding="utf-8")

for token in (
    "private const int MaxInputCount = 10000;",
    "private long _changeVersion;",
    "ids is ICollection<string> collection",
    "ids is IReadOnlyCollection<string> readOnlyCollection",
    "var enumerationVersion = _changeVersion;",
    "using (var enumerator = ids.GetEnumerator())",
    "while (true)",
    "RequireStableKnownCount(ids, knownCount);",
    "if (!enumerator.MoveNext()) break;",
    "if (knownCount.HasValue && inputCount >= knownCount.Value)",
    "if (inputCount >= MaxInputCount)",
    "var raw = enumerator.Current;",
    "if (_changeVersion != enumerationVersion)",
    "if (_ids.SetEquals(next)) return;",
    "var nextVersion = checked(_changeVersion + 1L);",
    "_changeVersion = nextVersion;",
    "Selection changed while replacement element ids were being enumerated",
):
    assert token in source, f"missing SelectionState freshness contract: {token}"

replace_start = source.index("public void Replace(IEnumerable<string> ids)")
clear_start = source.index("public void Clear()", replace_start)
replace = source[replace_start:clear_start]
clear = source[clear_start:]

capture_pos = replace.index("var enumerationVersion = _changeVersion;")
enumerator_pos = replace.index("using (var enumerator = ids.GetEnumerator())", capture_pos)
loop_pos = replace.index("while (true)", enumerator_pos)
pre_move_count_pos = replace.index("RequireStableKnownCount(ids, knownCount);", loop_pos)
move_next_pos = replace.index("if (!enumerator.MoveNext()) break;", pre_move_count_pos)
post_move_count_pos = replace.index("RequireStableKnownCount(ids, knownCount);", pre_move_count_pos + 1)
known_guard_pos = replace.index("if (knownCount.HasValue && inputCount >= knownCount.Value)", post_move_count_pos)
cap_guard_pos = replace.index("if (inputCount >= MaxInputCount)", known_guard_pos)
current_pos = replace.index("var raw = enumerator.Current;", cap_guard_pos)
freshness_pos = replace.index("if (_changeVersion != enumerationVersion)", current_pos)
noop_pos = replace.index("if (_ids.SetEquals(next)) return;", freshness_pos)
next_version_pos = replace.index("var nextVersion = checked(_changeVersion + 1L);", noop_pos)
clear_ids_pos = replace.index("_ids.Clear();", next_version_pos)
revision_apply_pos = replace.index("_changeVersion = nextVersion;", clear_ids_pos)
event_pos = replace.index("Changed?.Invoke(this, EventArgs.Empty);", revision_apply_pos)
assert (
    capture_pos
    < enumerator_pos
    < loop_pos
    < pre_move_count_pos
    < move_next_pos
    < post_move_count_pos
    < known_guard_pos
    < cap_guard_pos
    < current_pos
    < freshness_pos
    < noop_pos
    < next_version_pos
    < clear_ids_pos
    < revision_apply_pos
    < event_pos
), "SelectionState.Replace freshness/mutation ordering changed"

assert "while (enumerator.MoveNext())" not in replace, (
    "SelectionState.Replace must preserve Count validation before and after caller-controlled MoveNext"
)
assert "foreach (var raw in ids)" not in replace, (
    "SelectionState.Replace caller-controlled traversal must not regress to foreach before Count admission"
)

clear_next_version_pos = clear.index("var nextVersion = checked(_changeVersion + 1L);")
clear_ids_pos = clear.index("_ids.Clear();", clear_next_version_pos)
clear_revision_pos = clear.index("_changeVersion = nextVersion;", clear_ids_pos)
clear_event_pos = clear.index("Changed?.Invoke(this, EventArgs.Empty);", clear_revision_pos)
assert clear_next_version_pos < clear_ids_pos < clear_revision_pos < clear_event_pos, (
    "SelectionState.Clear revision must be prepared before mutation and applied before notification"
)

for token in (
    "StableLazyReplacementPreservesSelectionSemantics",
    "ReentrantReplacementFailsWithoutOverwritingNewerSelection",
    "ReentrantClearWithEmptyOuterInputFailsBeforeNoOp",
    "ReentrantNoOpDoesNotInvalidateOuterReplacement",
    "ReplaceThenYield",
    "ClearThenStop",
    "NoOpThenYield",
):
    assert token in smoke, f"missing SelectionState freshness smoke coverage: {token}"

assert "[ModuleInitializer]" in registration, "SelectionState freshness smoke is not registered"
assert "SelectionStateReplaceInputFreshnessSmoke.Run();" in registration, "SelectionState freshness smoke registration drifted"

print("PASS: SelectionState replacement input freshness contract is locked with pre/post-MoveNext Count stability")
