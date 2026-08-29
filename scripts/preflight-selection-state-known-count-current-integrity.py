#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "SelectionState.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SelectionStateKnownCountStabilitySmoke.cs"
FOCUSED_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SelectionStateMidCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "selection-state-known-count-current-integrity.md"

for path in (SOURCE, SMOKE, FOCUSED_SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("SelectionState Count/Current integrity preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
focused_smoke = FOCUSED_SMOKE.read_text(encoding="utf-8")

required_source = (
    "using (var enumerator = ids.GetEnumerator())",
    "while (true)",
    "RequireStableKnownCount(ids, knownCount);",
    "if (!enumerator.MoveNext()) break;",
    "if (knownCount.HasValue && inputCount >= knownCount.Value)",
    "if (inputCount >= MaxInputCount)",
    "var raw = enumerator.Current;",
    "if (_changeVersion != enumerationVersion)",
    "var finalKnownCount = ResolveKnownCount(ids);",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("SelectionState Count/Current source contract missing: " + repr(missing))

replace_start = source.index("public void Replace(IEnumerable<string> ids)")
clear_start = source.index("public void Clear()", replace_start)
replace = source[replace_start:clear_start]

loop = replace.index("while (true)")
pre_move_count = replace.index("RequireStableKnownCount(ids, knownCount);", loop)
move_next = replace.index("if (!enumerator.MoveNext()) break;", pre_move_count)
post_move_count = replace.index("RequireStableKnownCount(ids, knownCount);", pre_move_count + 1)
known_guard = replace.index("if (knownCount.HasValue && inputCount >= knownCount.Value)", post_move_count)
cap_guard = replace.index("if (inputCount >= MaxInputCount)", known_guard)
current = replace.index("var raw = enumerator.Current;", cap_guard)
count_increment = replace.index("inputCount++;", current)
version_guard = replace.index("if (_changeVersion != enumerationVersion)", count_increment)
final_count = replace.index("var finalKnownCount = ResolveKnownCount(ids);", version_guard)
publication = replace.index("_ids.Clear();", final_count)

if not (
    loop < pre_move_count < move_next < post_move_count < known_guard < cap_guard < current <
    count_increment < version_guard < final_count < publication
):
    raise SystemExit("SelectionState Count/Current traversal or publication ordering changed.")
if "while (enumerator.MoveNext())" in replace:
    raise SystemExit("SelectionState traversal regressed to terminal-only Count validation around MoveNext.")
if "foreach (var raw in ids)" in replace:
    raise SystemExit("SelectionState caller-controlled traversal must not regress to foreach before Count admission.")

required_smoke = (
    "KnownCountOverrunFailsBeforeCurrentAndThrowingTail",
    "MoveNextCalls",
    "CurrentReads",
    "Equal(2, source.MoveNextCalls);",
    "Equal(1, source.CurrentReads);",
    "GenericCountDriftFailsWithoutPublication",
    "ReadOnlyCountDriftFailsWithoutPublication",
    "NonGenericCountDriftFailsWithoutPublication",
    "KnownCountUnderYieldStillFailsWithoutPublication",
    "StableMultiInterfaceCountAndStreamingInputsRemainSupported",
    "[ModuleInitializer]",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("SelectionState Count/Current smoke contract missing: " + repr(missing_smoke))

required_focused = (
    "DriftAfterCurrentFailsBeforeNextMoveNext",
    "MoveNextInducedDriftFailsBeforeCurrent",
    "CrossInterfaceConflictFailsBeforeNextMoveNext",
    "Equal(1, source.MoveNextCalls, \"pre-MoveNext drift MoveNext calls\")",
    "Equal(1, source.CurrentReads, \"MoveNext-induced drift Current reads\")",
    "[ModuleInitializer]",
)
missing_focused = [token for token in required_focused if token not in focused_smoke]
if missing_focused:
    raise SystemExit("SelectionState mid-traversal Count/Current smoke contract missing: " + repr(missing_focused))

print("PASS SelectionState known-Count Current observation integrity with pre/post MoveNext stability")
