#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/SelectionState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SelectionStateFinalCountReentrancySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "if (_changeVersion != enumerationVersion)\n                throw new InvalidOperationException(\"Selection changed while replacement element ids were being enumerated.",
    "var finalKnownCount = ResolveKnownCount(ids);",
    "RequireStableKnownCount(ids, knownCount);\n            using (var enumerator = ids.GetEnumerator())",
    "using (var enumerator = ids.GetEnumerator())\n            {\n                RequireStableKnownCount(ids, knownCount);",
]
required_smoke = [
    "FinalCountReentrancyCannotPublishStaleSelection();",
    "StableCountedReplacementKeepsObservationBudget();",
    "CountReads == 9",
    "Equal(9, source.CountReads, \"reentrant final Count reads\")",
    "SequenceEqual(new[] { \"INNER\" }, state.ElementIds",
    "Equal(1, changed, \"only the nested replacement may publish\")",
    "Equal(9, source.CountReads, \"stable final Count reads\")",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("SelectionState final Count reentrancy preflight failed; missing contract token(s): " + repr(missing))

acquisition = source.find("using (var enumerator = ids.GetEnumerator())")
pre_acquisition = source.rfind("RequireStableKnownCount(ids, knownCount);", 0, acquisition)
post_acquisition = source.find("RequireStableKnownCount(ids, knownCount);", acquisition)
loop = source.find("while (true)", post_acquisition)
if min(pre_acquisition, acquisition, post_acquisition, loop) < 0 or not (
    pre_acquisition < acquisition < post_acquisition < loop
):
    raise SystemExit("SelectionState final Count reentrancy preflight failed: known Count must rebound around enumerator acquisition")

final_count = source.find("var finalKnownCount = ResolveKnownCount(ids);")
post_check = source.find("if (_changeVersion != enumerationVersion)", final_count)
publication = source.find("if (_ids.SetEquals(next)) return;", final_count)
if final_count < 0 or post_check < 0 or publication < 0 or not (final_count < post_check < publication):
    raise SystemExit("SelectionState final Count reentrancy preflight failed: final Count callback must be followed by version validation before publication")

if source.count("if (_changeVersion != enumerationVersion)") < 2:
    raise SystemExit("SelectionState final Count reentrancy preflight failed: traversal and final-Count publication boundaries both require version validation")

print("PASS SelectionState final Count reentrancy publication guard with enumerator-acquisition Count rebound")
