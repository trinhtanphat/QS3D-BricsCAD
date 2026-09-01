#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "RegenerationWorkProfiler.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RegenerationWorkProfileKnownCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
failures = []

start = source.find("private static IReadOnlyList<T> MaterializeBounded<T>")
end = source.find("private static int? ValidateKnownCountContract<T>", start)
if start < 0 or end < 0:
    failures.append("missing RegenerationWorkProfile.MaterializeBounded source window")
else:
    window = source[start:end]
    stable = "RequireStableKnownCountContract(values, knownCount, maxCount, parameterName, label);"
    first_stable = window.find(stable)
    move_next = window.find("if (!enumerator.MoveNext()) break;")
    second_stable = window.find(stable, move_next + 1) if move_next >= 0 else -1
    current = window.find("var value = enumerator.Current;")
    final_stable = window.rfind(stable)
    positions = (first_stable, move_next, second_stable, current, final_stable)
    if any(position < 0 for position in positions):
        failures.append("missing traversal-boundary Count stability tokens: " + str(positions))
    elif not (first_stable < move_next < second_stable < current < final_stable):
        failures.append("Count stability must be checked before MoveNext, after successful MoveNext before Current, and after traversal: " + str(positions))

helper_start = source.find("private static void RequireStableKnownCountContract<T>")
helper_end = source.find("private static int? ValidateKnownCountContract<T>", helper_start)
if helper_start < 0 or helper_end < 0:
    failures.append("missing stable Count contract helper")
else:
    helper = source[helper_start:helper_end]
    for token in (
        "ValidateKnownCountContract(values, maxCount, parameterName, label)",
        "observedKnownCount != knownCount",
        "collection known Count changed during traversal.",
    ):
        if token not in helper:
            failures.append("stable Count helper missing token: " + token)

for token in (
    "TransientTargetCountGrowthRejectsBeforeCurrent",
    "TransientItemCountShrinkRejectsBeforeCurrent",
    "TransientCategoryNegativeCountRejectsBeforeCurrent",
    "TransientCountProbeCollection<T>",
    'Equal(0, source.CurrentReads, "Target transient drift must reject before Current under changed Count evidence.")',
    'Equal(0, source.CurrentReads, "Work-item transient drift must reject before Current under changed Count evidence.")',
    'Equal(0, source.CurrentReads, "Category transient negative Count must reject before Current.")',
    "[ModuleInitializer]",
):
    if token not in smoke:
        failures.append("smoke missing token: " + token)

if failures:
    for failure in failures:
        print("FAIL regeneration work profile transient Count stability: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("PASS regeneration work profile transient Count stability")
