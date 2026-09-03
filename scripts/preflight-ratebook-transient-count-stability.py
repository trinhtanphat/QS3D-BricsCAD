#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/RateBook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RateBookTransientCountStabilitySmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing RateBook transient Count stability file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    loop = source.find("while (true)")
    pre_rebind = source.find("RequireStableKnownCount(items, knownCount);", loop)
    move_next = source.find("if (!enumerator.MoveNext())", pre_rebind)
    post_rebind = source.find("RequireStableKnownCount(items, knownCount);", pre_rebind + 1)
    overrun = source.find("if (hasKnownCount && index >= knownCount)", post_rebind)
    ceiling = source.find("if (index >= MaxItems)", overrun)
    current = source.find("var item = enumerator.Current;", ceiling)
    under_yield = source.find("if (hasKnownCount && index != knownCount)", current)
    final_rebind = source.find("RequireStableKnownCount(items, knownCount);", under_yield)
    publish = source.find("Items = new ReadOnlyCollection<RateItem>(snapshot.ToArray());", final_rebind)

    if min(loop, pre_rebind, move_next, post_rebind, overrun, ceiling, current, under_yield, final_rebind, publish) < 0 or not (
        loop < pre_rebind < move_next < post_rebind < overrun < ceiling < current < under_yield < final_rebind < publish
    ):
        errors.append(
            "RateBook transient Count contract must be pre-rebind -> MoveNext -> post-rebind -> admitted-count/ceiling -> Current, "
            "with under-yield and final rebound before publication"
        )

    if "while (enumerator.MoveNext())" in source:
        errors.append("RateBook must not regress to while(enumerator.MoveNext()) traversal.")

    for token in (
        "private static void RequireStableKnownCount(IEnumerable<RateItem> items, int knownCount)",
        "var hasCurrentKnownCount = TryGetKnownCount(items, out var currentKnownCount);",
        "if (!hasCurrentKnownCount || currentKnownCount != knownCount)",
        "if (items is ICollection<RateItem> collection)",
        "if (items is IReadOnlyCollection<RateItem> readOnlyCollection)",
        "if (items is ICollection nonGenericCollection)",
    ):
        if token not in source:
            errors.append("RateBook transient Count source guard missing token: " + token)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "PreAdvanceGrowthFailsBeforeMoveNext();",
        "PostAdvanceGrowthFailsBeforeCurrent();",
        "PreAdvanceShrinkFailsBeforeMoveNext();",
        "PostAdvanceNegativeFailsBeforeCurrent();",
        "StableCountedInputRemainsAccepted();",
        'Equal(0, source.MoveNextCalls, "Pre-advance Count drift must fail before caller traversal advances.");',
        'Equal(0, source.CurrentReads, "Post-advance Count drift must fail before Current is observed.");',
        'Equal(6, source.MoveNextCalls, "Stable counted input must include terminal false MoveNext once for admission and once for semantic replay.");',
        'Equal(4, source.CurrentReads, "Stable counted input must observe each item once for admission and once for semantic replay.");',
        "internal static class RateBookTransientCountStabilityRegistration",
        "[ModuleInitializer]",
    ):
        if token not in smoke:
            errors.append("RateBook transient Count smoke missing assertion/control: " + token)

print("QS3D RateBook transient known-count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: RateBook revalidates known Count before traversal advance and before Current observation, with deterministic hostile regression coverage.")
