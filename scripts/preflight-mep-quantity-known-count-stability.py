#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepQuantity.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepQuantityInputBoundSmoke.cs"
MID_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepQuantityMidTraversalCountDriftSmoke.cs"
errors = []

for path in (SOURCE, SMOKE, MID_SMOKE):
    if not path.is_file():
        errors.append("missing MEP quantity Count-stability file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "var hasKnownCount = TryGetKnownCount(elements, out var knownCount);",
        "while (true)",
        "EnsureKnownCountStable(elements, knownCount);",
        "if (!enumerator.MoveNext())",
        "if (index >= knownCount)",
        "var element = enumerator.Current;",
        "if (hasKnownCount && index != knownCount)",
        "private static void EnsureKnownCountStable",
        "var hasCurrentKnownCount = TryGetKnownCount(elements, out var currentKnownCount);",
        "if (!hasCurrentKnownCount || currentKnownCount != admittedCount)",
        "ThrowKnownCountChangedDuringTraversal();",
        '"MEP takeoff source known count changed during traversal."',
    ):
        if token not in source:
            errors.append("MEP quantity source missing traversal Count-binding token: " + token)

    initial = source.find("var hasKnownCount = TryGetKnownCount(elements, out var knownCount);")
    loop = source.find("while (true)")
    first_rebind = source.find("EnsureKnownCountStable(elements, knownCount);", loop)
    move_next = source.find("if (!enumerator.MoveNext())", loop)
    second_rebind = source.find("EnsureKnownCountStable(elements, knownCount);", first_rebind + 1)
    current = source.find("var element = enumerator.Current;", loop)
    final_mismatch = source.find("if (hasKnownCount && index != knownCount)")
    final_rebind = source.find("EnsureKnownCountStable(elements, knownCount);", final_mismatch)
    publication = source.find("var result = new List<MepQuantityGroup>(builders.Count);")
    if min(initial, loop, first_rebind, move_next, second_rebind, current, final_mismatch, final_rebind, publication) < 0 or not (
        initial < loop < first_rebind < move_next < second_rebind < current < final_mismatch < final_rebind < publication
    ):
        errors.append("MEP quantity aggregation must bind Count before and after each MoveNext before Current, then rebind before publication")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "CountDriftAfterExactTraversalFailsClosed();",
        "PostTraversalInterfaceConflictFailsClosed();",
        "PostTraversalNegativeKnownCountFailsClosed();",
        "HonestTwoPhaseCountRemainsAccepted();",
        "PureStreamingInputRemainsAccepted();",
        '"known count changed during traversal"',
        '"conflicting known counts"',
        '"negative known count"',
        "StreamingOversizeStopsAtFirstDisallowedElement();",
        "Equal(7, source.CountReads",
        "Equal(9, source.CountReads",
    ):
        if token not in smoke:
            errors.append("MEP quantity smoke missing Count-stability assertion/control: " + token)

if MID_SMOKE.is_file():
    smoke = MID_SMOKE.read_text(encoding="utf-8")
    for token in (
        "CountDriftBeforeMoveNextFailsBeforeAdvancement();",
        "CountDriftAfterMoveNextFailsBeforeCurrent();",
        "TransientCountDriftCannotRestoreBeforePublication();",
        "CurrentReads",
        "MoveNextCalls",
        '"known count changed during traversal"',
    ):
        if token not in smoke:
            errors.append("MEP quantity mid-traversal smoke missing assertion/control: " + token)

print("QS3D MEP quantity known-count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: MEP quantity aggregation binds deterministic Count evidence around every caller-controlled advancement and before publication.")
