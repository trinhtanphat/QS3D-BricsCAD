#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/RateBook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RateBookKnownCountTraversalSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing RateBook known-count stability file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "var hasKnownCount = TryGetKnownCount(items, out var knownCount);",
        "while (true)",
        "RequireStableKnownCount(items, knownCount);",
        "if (!enumerator.MoveNext())",
        "if (hasKnownCount && index >= knownCount)",
        "if (index >= MaxItems)",
        "var item = enumerator.Current;",
        "if (item == null)",
        "if (!itemIds.Add(item.RateItemId))",
        "snapshot.Add(item);",
        "if (hasKnownCount && index != knownCount)",
        "private static void RequireStableKnownCount(IEnumerable<RateItem> items, int knownCount)",
        "var hasCurrentKnownCount = TryGetKnownCount(items, out var currentKnownCount);",
        "if (!hasCurrentKnownCount || currentKnownCount != knownCount)",
        "ThrowKnownCountChangedDuringTraversal();",
        '"Rate book item source known count changed during traversal."',
    ):
        if token not in source:
            errors.append("RateBook source missing stable Count binding/no-overread token: " + token)

    if "while (enumerator.MoveNext())" in source:
        errors.append("RateBook must not use foreach-equivalent while(MoveNext) shape that cannot pin pre-advance Count stability.")

    initial = source.find("var hasKnownCount = TryGetKnownCount(items, out var knownCount);")
    traversal = source.find("while (true)")
    pre_rebind = source.find("RequireStableKnownCount(items, knownCount);", traversal)
    move_next = source.find("if (!enumerator.MoveNext())", pre_rebind)
    post_move_rebind = source.find("RequireStableKnownCount(items, knownCount);", pre_rebind + 1)
    overrun = source.find("if (hasKnownCount && index >= knownCount)", post_move_rebind)
    ceiling = source.find("if (index >= MaxItems)", overrun)
    current = source.find("var item = enumerator.Current;", ceiling)
    post_current_rebind = source.find("RequireStableKnownCount(items, knownCount);", current)
    null_check = source.find("if (item == null)", post_current_rebind)
    duplicate_check = source.find("if (!itemIds.Add(item.RateItemId))", null_check)
    retain = source.find("snapshot.Add(item);", duplicate_check)
    observed = source.find("if (hasKnownCount && index != knownCount)", retain)
    final_rebind = source.find("RequireStableKnownCount(items, knownCount);", observed)
    sort = source.find("foreach (var pair in _byScope)", final_rebind)
    if min(initial, traversal, pre_rebind, move_next, post_move_rebind, overrun, ceiling, current, post_current_rebind, null_check, duplicate_check, retain, observed, final_rebind, sort) < 0 or not (
        initial < traversal < pre_rebind < move_next < post_move_rebind < overrun < ceiling < current < post_current_rebind < null_check < duplicate_check < retain < observed < final_rebind < sort
    ):
        errors.append(
            "RateBook must bind Count at admission, re-bind before MoveNext, after successful MoveNext, and immediately after Current; "
            "Count/ceiling overflow must fail before Current, and Current-induced Count drift must fail before item semantics or retention."
        )

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "CurrentInducedCountDriftFailsBeforeItemSemantics();",
        "CurrentCountDriftingCollection",
        "CurrentReads",
        "CountDriftAfterExactTraversalFailsClosed();",
        "PostTraversalInterfaceConflictFailsClosed();",
        "HonestMultiInterfaceCountRemainsAccepted();",
        "PureStreamingInputRemainsAccepted();",
        'Contains(\n                "known count changed during traversal"',
        'Contains(\n                "conflicting known counts"',
        "OverYieldFailsAtFirstUnexpectedItem();",
        "UnderYieldFailsAfterTraversal();",
    ):
        if token not in smoke:
            errors.append("RateBook smoke missing Count-stability assertion/control: " + token)

print("QS3D RateBook known-count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: RateBook rejects Current-induced Count drift before item semantics while preserving no-overread, under-yield, and final-rebind coverage.")
