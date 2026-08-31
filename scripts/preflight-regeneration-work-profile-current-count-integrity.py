#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationWorkProfiler.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationWorkProfileCurrentCountIntegritySmoke.cs"


def stop(message: str) -> None:
    print("FAIL regeneration work profile Current-count integrity: " + message)
    sys.exit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
start = source.find("private static IReadOnlyList<T> MaterializeBounded<T>")
end = source.find("private static void RequireStableKnownCountContract<T>", start)
if start < 0 or end < 0:
    stop("MaterializeBounded source boundary is missing")
method = source[start:end]
stable = "RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);"
required = [
    "var knownCount = ValidateKnownCountContract(values, maxCount, parameterName, label);",
    "if (!enumerator.MoveNext()) break;",
    "if (knownCount.HasValue && result.Count >= knownCount.Value)",
    "if (result.Count >= maxCount)",
    "var value = enumerator.Current;",
    "if (ReferenceEquals(value, null))",
    "result.Add(value);",
    "if (knownCount.HasValue && result.Count != knownCount.Value)",
    "return result.AsReadOnly();",
]
for token in required:
    if token not in method:
        stop("required source invariant is missing: " + token)

positions = []
cursor = 0
while True:
    found = method.find(stable, cursor)
    if found < 0:
        break
    positions.append(found)
    cursor = found + len(stable)
if len(positions) != 4:
    stop("MaterializeBounded must contain exactly four stable Count checkpoints")

admission = method.index("var knownCount = ValidateKnownCountContract(values, maxCount, parameterName, label);")
move_next = method.index("if (!enumerator.MoveNext()) break;")
known_guard = method.index("if (knownCount.HasValue && result.Count >= knownCount.Value)")
max_guard = method.index("if (result.Count >= maxCount)")
current = method.index("var value = enumerator.Current;")
null_guard = method.index("if (ReferenceEquals(value, null))")
retention = method.index("result.Add(value);")
count_mismatch = method.index("if (knownCount.HasValue && result.Count != knownCount.Value)")
publication = method.index("return result.AsReadOnly();")
pre, post_move, post_current, final = positions
if not (
    admission < pre < move_next < post_move < known_guard < current
    and post_move < max_guard < current
    and current < post_current < null_guard < retention
    and retention < count_mismatch < final < publication
):
    stop("Count checkpoints do not dominate traversal, retention and publication in required order")

for token in (
    "TargetCurrentGrowthRejectsImmediately",
    "ItemCurrentShrinkRejectsImmediately",
    "CategoryCurrentNegativeCountRejectsImmediately",
    "CurrentCanExposeCrossInterfaceConflict",
    "StableCountedInputKeepsObservationBudget",
    "PureStreamingInputRemainsAccepted",
    "[ModuleInitializer]",
):
    if token not in smoke:
        stop("required deterministic smoke evidence is missing: " + token)

print("PASS regeneration work profile Current-count integrity source guard")
