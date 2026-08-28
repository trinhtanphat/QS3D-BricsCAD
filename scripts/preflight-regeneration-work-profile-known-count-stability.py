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

required_source = [
    "using (var enumerator = values.GetEnumerator())",
    "while (enumerator.MoveNext())",
    "if (knownCount.HasValue && result.Count >= knownCount.Value)",
    "if (result.Count >= maxCount)",
    "var value = enumerator.Current;",
    "if (knownCount.HasValue && result.Count != knownCount.Value)",
    "var postTraversalKnownCount = ValidateKnownCountContract",
    "known Count changed during traversal",
]
for token in required_source:
    if token not in method:
        fail("required source invariant is missing: " + token)

if "foreach (var value in values)" in method:
    fail("MaterializeBounded must not use foreach because foreach reads Current before the body guard")

known_guard = method.index("if (knownCount.HasValue && result.Count >= knownCount.Value)")
max_guard = method.index("if (result.Count >= maxCount)")
current_read = method.index("var value = enumerator.Current;")
if not (known_guard < current_read and max_guard < current_read):
    fail("known-count and streaming ceiling guards must execute before Current")

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
