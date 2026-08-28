#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Services/DependencyImpactPlanner.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/DependencyImpactPlannerKnownCountSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing dependency-impact known-count stability file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "var knownCount = TryGetKnownCount(sourceElementIds",
        "if (knownCount.HasValue && index >= knownCount.Value)",
        "if (knownCount.HasValue && index != knownCount.Value)",
        "RequireKnownCountStableAfterTraversal(sourceElementIds, knownCount",
        "var observedKnownCount = TryGetKnownCount(source",
        "if (observedKnownCount != expectedKnownCount)",
        '"Dependency impact source known Count changed while its roots were being traversed."',
    ):
        if token not in source:
            errors.append("DependencyImpactPlanner source missing two-phase Count binding token: " + token)

    initial = source.find("var knownCount = TryGetKnownCount(sourceElementIds")
    traversal = source.find("foreach (var value in sourceElementIds)")
    observed = source.find("if (knownCount.HasValue && index != knownCount.Value)")
    rebound = source.find("RequireKnownCountStableAfterTraversal(sourceElementIds, knownCount")
    publish = source.find("result.Sort(StringComparer.OrdinalIgnoreCase)")
    if min(initial, traversal, observed, rebound, publish) < 0 or not (initial < traversal < observed < rebound < publish):
        errors.append("DependencyImpactPlanner must bind Count before traversal and re-bind it before root sorting/publication")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "PostTraversalKnownCountDriftFailsClosed();",
        "new DriftingCountContractRoots",
        "EnumerationCompleted",
        "TraversalMustMatchKnownCount();",
        "InvalidKnownCountsFailBeforeEnumeration();",
        "HonestCountedAndStreamingSourcesRemainSupported();",
    ):
        if token not in smoke:
            errors.append("DependencyImpactPlanner smoke missing Count-stability assertion/control: " + token)

print("QS3D dependency-impact known-count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: dependency-impact root traversal re-binds deterministic Count evidence before plan publication while preserving early overrun, under-yield and streaming behavior.")
