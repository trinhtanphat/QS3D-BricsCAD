#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/SelectionState.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SelectionStateFinalCountReentrancySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "RequireUnchangedSelectionVersion(enumerationVersion);\n\n            var finalKnownCount = ResolveKnownCount(ids);\n            RequireUnchangedSelectionVersion(enumerationVersion);",
    "private void RequireUnchangedSelectionVersion(long expectedVersion)",
    "Selection changed while replacement element ids were being enumerated",
]
required_smoke = [
    "FinalCountReentrancyCannotPublishStaleSelection();",
    "StableCountedReplacementKeepsObservationBudget();",
    "CountReads == 7",
    "SequenceEqual(new[] { \"INNER\" }, state.ElementIds",
    "Equal(1, changed, \"only the nested replacement may publish\")",
    "Equal(7, source.CountReads, \"stable final Count reads\")",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit("SelectionState final Count reentrancy preflight failed; missing contract token(s): " + repr(missing))

final_count = source.find("var finalKnownCount = ResolveKnownCount(ids);")
post_check = source.find("RequireUnchangedSelectionVersion(enumerationVersion);", final_count)
publication = source.find("if (_ids.SetEquals(next)) return;", final_count)
if final_count < 0 or post_check < 0 or publication < 0 or not (final_count < post_check < publication):
    raise SystemExit("SelectionState final Count reentrancy preflight failed: final Count callback must be followed by version validation before publication")

print("PASS SelectionState final Count reentrancy publication guard")
