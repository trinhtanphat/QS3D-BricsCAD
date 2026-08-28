#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepQuantity.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepQuantityInputBoundSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing MEP quantity Count-stability file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "var hasKnownCount = TryGetKnownCount(elements, out var knownCount);",
        "if (hasKnownCount && index >= knownCount)",
        "if (hasKnownCount && index != knownCount)",
        "var hasFinalKnownCount = TryGetKnownCount(elements, out var finalKnownCount);",
        "if (!hasFinalKnownCount || finalKnownCount != knownCount)",
        "ThrowKnownCountChangedDuringTraversal();",
        '"MEP takeoff source known count changed during traversal."',
    ):
        if token not in source:
            errors.append("MEP quantity source missing two-phase Count binding token: " + token)

    initial = source.find("var hasKnownCount = TryGetKnownCount(elements, out var knownCount);")
    traversal = source.find("foreach (var element in elements)")
    observed = source.find("if (hasKnownCount && index != knownCount)")
    rebound = source.find("var hasFinalKnownCount = TryGetKnownCount(elements, out var finalKnownCount);")
    publication = source.find("var result = new List<MepQuantityGroup>(builders.Count);")
    if min(initial, traversal, observed, rebound, publication) < 0 or not (initial < traversal < observed < rebound < publication):
        errors.append("MEP quantity aggregation must bind Count before traversal and re-bind it before result publication")

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
    ):
        if token not in smoke:
            errors.append("MEP quantity smoke missing Count-stability assertion/control: " + token)

print("QS3D MEP quantity known-count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: MEP quantity aggregation re-binds deterministic Count evidence after traversal while preserving early overrun and streaming bounds.")
