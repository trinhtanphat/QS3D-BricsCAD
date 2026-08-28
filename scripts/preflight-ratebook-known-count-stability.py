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
        "if (hasKnownCount && index >= knownCount)",
        "if (hasKnownCount && index != knownCount)",
        "var hasFinalKnownCount = TryGetKnownCount(items, out var finalKnownCount);",
        "if (!hasFinalKnownCount || finalKnownCount != knownCount)",
        "ThrowKnownCountChangedDuringTraversal();",
        '"Rate book item source known count changed during traversal."',
    ):
        if token not in source:
            errors.append("RateBook source missing two-phase Count binding token: " + token)

    initial = source.find("var hasKnownCount = TryGetKnownCount(items, out var knownCount);")
    traversal = source.find("foreach (var item in items)")
    observed = source.find("if (hasKnownCount && index != knownCount)")
    rebound = source.find("var hasFinalKnownCount = TryGetKnownCount(items, out var finalKnownCount);")
    sort = source.find("foreach (var pair in _byScope)")
    if min(initial, traversal, observed, rebound, sort) < 0 or not (initial < traversal < observed < rebound < sort):
        errors.append("RateBook must bind Count before traversal and re-bind it before committed sorting/publication")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
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

print("PASS: RateBook re-binds deterministic Count evidence after traversal while preserving early overrun, under-yield and streaming behavior.")
