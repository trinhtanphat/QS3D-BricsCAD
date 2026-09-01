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
        "var enumerator = sourceElementIds.GetEnumerator();",
        "using (enumerator)",
        "while (enumerator.MoveNext())",
        "RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount, nameof(sourceElementIds));",
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
    pre_acquisition = source.find("RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount")
    acquisition = source.find("var enumerator = sourceElementIds.GetEnumerator();")
    post_acquisition = source.find("RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount", pre_acquisition + 1)
    traversal = source.find("while (enumerator.MoveNext())")
    rebound_during = source.find("RequireKnownCountStableDuringTraversal(sourceElementIds, knownCount", post_acquisition + 1)
    advertised = source.find("if (knownCount.HasValue && index >= knownCount.Value)")
    current = source.find("var value = enumerator.Current;")
    observed = source.find("if (knownCount.HasValue && index != knownCount.Value)")
    rebound = source.find("RequireKnownCountStableAfterTraversal(sourceElementIds, knownCount")
    publish = source.find("result.Sort(StringComparer.OrdinalIgnoreCase)")
    if min(initial, pre_acquisition, acquisition, post_acquisition, traversal, rebound_during, advertised, current, observed, rebound, publish) < 0 or not (
        initial < pre_acquisition < acquisition < post_acquisition < traversal < rebound_during < advertised < current < observed < rebound < publish
    ):
        errors.append("DependencyImpactPlanner must enforce Count rebound -> GetEnumerator -> Count rebound -> MoveNext -> traversal Count rebound -> advertised-count guard -> Current, then final Count rebound before publication")
    if "foreach (var value in sourceElementIds)" in source:
        errors.append("DependencyImpactPlanner caller-controlled root traversal must not use foreach before Count revalidation")

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

print("PASS: dependency-impact roots enforce Count rebound -> GetEnumerator -> Count rebound -> MoveNext -> traversal Count rebound -> advertised-count guard -> Current and final Count rebound before publication.")
