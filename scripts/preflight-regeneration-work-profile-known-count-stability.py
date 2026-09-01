#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/RegenerationWorkProfiler.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RegenerationWorkProfileKnownCountStabilitySmoke.cs"


def fail(message: str) -> None:
    print("FAIL regeneration work profile known-count stability: " + message)
    sys.exit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

method_start = source.find("private static IReadOnlyList<T> MaterializeBounded<T>")
method_end = source.find("private static int? ValidateKnownCountContract<T>", method_start)
if method_start < 0 or method_end < 0:
    fail("MaterializeBounded source boundary is missing")
method = source[method_start:method_end]

stable_token = "RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);"
required_source = [
    "using (var enumerator = values.GetEnumerator())",
    "while (true)",
    stable_token,
    "if (!enumerator.MoveNext()) break;",
    "if (knownCount.HasValue && result.Count >= knownCount.Value)",
    "if (result.Count >= maxCount)",
    "var value = enumerator.Current;",
    "if (knownCount.HasValue && result.Count != knownCount.Value)",
    "var observedKnownCount = ValidateKnownCountContract",
    "known Count changed during traversal",
]
for token in required_source:
    if token not in method:
        fail("required source invariant is missing: " + token)

for forbidden in (
    "foreach (var value in values)",
    "while (enumerator.MoveNext())",
    "postTraversalKnownCount",
):
    if forbidden in method:
        fail("MaterializeBounded contains stale traversal shape: " + forbidden)

stable_positions = []
cursor = 0
while True:
    position = method.find(stable_token, cursor)
    if position < 0:
        break
    stable_positions.append(position)
    cursor = position + len(stable_token)
if len(stable_positions) != 4:
    fail("MaterializeBounded must expose pre-traversal, post-MoveNext, post-Current and final stable Count checks")

pre_traversal, post_move, post_current, final_stable = stable_positions
move_next = method.index("if (!enumerator.MoveNext()) break;")
known_guard = method.index("if (knownCount.HasValue && result.Count >= knownCount.Value)")
max_guard = method.index("if (result.Count >= maxCount)")
current_read = method.index("var value = enumerator.Current;")
null_guard = method.index("if (ReferenceEquals(value, null))")
retention = method.index("result.Add(value);")
count_mismatch = method.index("if (knownCount.HasValue && result.Count != knownCount.Value)")
if not (
    pre_traversal < move_next < post_move < known_guard < current_read
    and post_move < max_guard < current_read
    and current_read < post_current < null_guard < retention
    and retention < count_mismatch < final_stable
):
    fail("stable Count checks and capacity guards must bracket MoveNext, Current, retention and final publication in fail-closed order")

required_smoke = [
    "TargetOverrunRejectsBeforeSecondCurrent",
    "ItemOverrunRejectsBeforeSecondCurrent",
    "CategoryOverrunRejectsBeforeSecondCurrent",
    "UnderYieldStillFailsClosed",
    "PostTraversalCountDriftStillFailsClosed",
    "StreamingCeilingRejectsBeforeOverflowCurrent",
    "HonestCountedInputsRemainAccepted",
    "Equal(1, source.CurrentReads",
    "[ModuleInitializer]",
]
for token in required_smoke:
    if token not in smoke:
        fail("required deterministic smoke evidence is missing: " + token)

print("PASS regeneration work profile known-count stability source guard")
