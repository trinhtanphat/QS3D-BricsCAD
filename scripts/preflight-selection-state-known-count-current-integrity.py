#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "SelectionState.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SelectionStateKnownCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "selection-state-known-count-current-integrity.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("SelectionState Count/Current integrity preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "using (var enumerator = ids.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (knownCount.HasValue && inputCount >= knownCount.Value)",
    "if (inputCount >= MaxInputCount)",
    "var raw = enumerator.Current;",
    "var finalKnownCount = ResolveKnownCount(ids);",
    "if (_changeVersion != enumerationVersion)",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("SelectionState Count/Current source contract missing: " + repr(missing))

replace_start = source.index("public void Replace(IEnumerable<string> ids)")
clear_start = source.index("public void Clear()", replace_start)
replace = source[replace_start:clear_start]

move_next = replace.index("while (enumerator.MoveNext())")
known_guard = replace.index("if (knownCount.HasValue && inputCount >= knownCount.Value)", move_next)
cap_guard = replace.index("if (inputCount >= MaxInputCount)", known_guard)
current = replace.index("var raw = enumerator.Current;", cap_guard)
count_increment = replace.index("inputCount++;", current)
version_guard = replace.index("if (_changeVersion != enumerationVersion)", count_increment)
final_count = replace.index("var finalKnownCount = ResolveKnownCount(ids);", version_guard)
publication = replace.index("_ids.Clear();", final_count)

if not (move_next < known_guard < cap_guard < current < count_increment < version_guard < final_count < publication):
    raise SystemExit("SelectionState Count/Current traversal or publication ordering changed.")
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

print("PASS SelectionState known-Count Current observation integrity")
