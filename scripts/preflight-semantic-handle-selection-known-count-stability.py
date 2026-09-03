#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/SemanticHandleSelectionKnownCountStabilitySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing semantic handle Count-stability file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    start = text.find("private static HashSet<string> MaterializeSelectedHandles(IEnumerable<string> selectedHandles)")
    end = text.find("private static IReadOnlyDictionary<string, ProjectElement> SnapshotElementOwnership", start)
    materialize = text[start:end] if start >= 0 and end > start else ""
    required = (
        "var knownCount = TryGetKnownCount(selectedHandles",
        "using (var enumerator = selectedHandles.GetEnumerator())",
        "RequireStableKnownCountDuringTraversal(selectedHandles, knownCount);",
        "var moved = enumerator.MoveNext();",
        "if (!moved) break;",
        "inputCount >= knownCount.Value",
        "var rawHandle = enumerator.Current;",
        "inputCount != knownCount.Value",
        "RevalidateKnownCountAfterTraversal(selectedHandles, knownCount);",
        "return selected;",
    )
    positions = [materialize.find(token) for token in required]
    if not materialize or any(pos < 0 for pos in positions) or positions != sorted(positions):
        errors.append(
            "Semantic handle selection must rebind admitted Count around MoveNext, reject overrun before Current, and rebind after exact traversal.")
    if materialize.count("RequireStableKnownCountDuringTraversal(selectedHandles, knownCount);") < 3:
        errors.append("Semantic handle selection must rebind known Count before MoveNext, after successful MoveNext, and after Current.")
    if "foreach (var rawHandle in selectedHandles)" in materialize:
        errors.append("Semantic handle selection must not use foreach because Current would be read before the overrun guard.")
    for token in (
        "selectedHandles is ICollection<string>",
        "selectedHandles is IReadOnlyCollection<string>",
        "selectedHandles is System.Collections.ICollection",
        "negativeKnownCount",
        "conflictingKnownCounts",
        "!reboundCount.HasValue || reboundCount.Value != admittedCount.Value",
        "negative known Count value during traversal",
        "conflicting known Count values during traversal",
    ):
        if token not in materialize:
            errors.append("Semantic handle Count-stability implementation missing contract token: " + token)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "GenericCountDriftRejects",
        "ReadOnlyCountDriftRejects",
        "NonGenericCountDriftRejects",
        "NegativePostTraversalCountRejects",
        "ConflictingPostTraversalCountsReject",
        "CountOverrunRejectsBeforeSecondCurrent",
        "MoveNextTransientGrowthRejectsBeforeCurrent",
        "MoveNextTransientShrinkRejectsBeforeCurrent",
        "MoveNextTransientNegativeRejectsBeforeCurrent",
        "MoveNextTransientConflictRejectsBeforeCurrent",
        "MoveNextCalls == 2",
        "CurrentReads == 1",
        "MoveNextCalls == 1",
        "CurrentReads == 0",
        "CountUnderYieldRejects",
        "StableCountedSelectionResolves",
        "StableMultiInterfaceSelectionResolves",
        "PureStreamingSelectionResolves",
    ):
        if token not in text:
            errors.append("Semantic handle Count-stability smoke missing regression token: " + token)

print("QS3D semantic handle selection known-Count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic handle selection rejects transient Count drift before Current and preserves exact traversal cardinality.")
