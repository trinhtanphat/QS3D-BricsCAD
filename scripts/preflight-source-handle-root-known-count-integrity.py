#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Services/SourceHandleResolver.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/SourceHandleRootKnownCountIntegritySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing SourceHandleResolver root Count-integrity file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    start = text.find("private static IReadOnlyList<string> MaterializeRootElementIds(IEnumerable<string> elementIds)")
    end = text.find("private static IReadOnlyDictionary<string, ProjectElement> BuildElementIndex", start)
    materialize = text[start:end] if start >= 0 and end > start else ""

    rebound = "RequireStableKnownCountDuringTraversal(elementIds, knownCount);"
    move = "if (!enumerator.MoveNext())"
    known_guard = "inputCount >= knownCount.Value"
    current = "var rawId = enumerator.Current;"
    final_count = "inputCount != knownCount.Value"
    final_rebound = "RevalidateKnownCountAfterTraversal(elementIds, knownCount);"
    return_roots = "return roots.AsReadOnly();"

    positions = {
        "known": materialize.find("var knownCount = TryGetKnownCount(elementIds"),
        "enumerator": materialize.find("using (var enumerator = elementIds.GetEnumerator())"),
        "loop": materialize.find("while (true)"),
    }
    first_rebound = materialize.find(rebound, positions["loop"] + len("while (true)"))
    move_pos = materialize.find(move, first_rebound + len(rebound))
    second_rebound = materialize.find(rebound, move_pos + len(move))
    known_guard_pos = materialize.find(known_guard, second_rebound + len(rebound))
    current_pos = materialize.find(current, known_guard_pos + len(known_guard))
    third_rebound = materialize.find(rebound, current_pos + len(current))
    final_count_pos = materialize.find(final_count, third_rebound + len(rebound))
    final_rebound_pos = materialize.find(final_rebound, final_count_pos + len(final_count))
    return_pos = materialize.find(return_roots, final_rebound_pos + len(final_rebound))

    ordered = [
        positions["known"],
        positions["enumerator"],
        positions["loop"],
        first_rebound,
        move_pos,
        second_rebound,
        known_guard_pos,
        current_pos,
        third_rebound,
        final_count_pos,
        final_rebound_pos,
        return_pos,
    ]
    if not materialize or any(pos < 0 for pos in ordered) or ordered != sorted(ordered):
        errors.append(
            "Locate root selection must enforce Count rebound -> MoveNext -> Count rebound -> known-Count guard -> Current -> Count rebound and final Count revalidation before return.")
    if "foreach (var rawId in elementIds)" in materialize:
        errors.append("Locate root selection must not use outer foreach because it can observe Current before Count-overrun rejection.")
    if "while (enumerator.MoveNext())" in materialize:
        errors.append("Locate root selection must use explicit loop control so Count can be rebound before MoveNext.")

    for token in (
        "elementIds is ICollection<string>",
        "elementIds is IReadOnlyCollection<string>",
        "elementIds is System.Collections.ICollection",
        "negativeKnownCount",
        "conflictingKnownCounts",
        "!reboundCount.HasValue || reboundCount.Value != admittedCount.Value",
        "MaxRootElementIdInputCount",
        "non-canonical semantic element id",
        "invalid negative known Count value during traversal",
        "conflicting known Count values during traversal",
    ):
        if token not in materialize:
            errors.append("Locate root Count-integrity implementation missing contract token: " + token)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "OverrunRejectsBeforeUnexpectedCurrent",
        "CurrentReads == 1",
        "UnderYieldRejects",
        "GenericCountDriftRejects",
        "ReadOnlyCountDriftRejects",
        "NonGenericCountDriftRejects",
        "NegativeAdmissionRejectsBeforeEnumeration",
        "ConflictingAdmissionRejectsBeforeEnumeration",
        "NegativePostTraversalCountRejects",
        "ConflictingPostTraversalCountsReject",
        "StableMultiInterfaceCountResolves",
        "CanonicalValidationStillWinsInsideAdmittedCount",
        "PureStreamingInputResolves",
    ):
        if token not in text:
            errors.append("Locate root Count-integrity smoke missing regression token: " + token)

print("QS3D SourceHandleResolver root known-Count integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Locate root selection must enforce Count rebound -> MoveNext -> Count rebound -> known-Count guard -> Current -> Count rebound and final deterministic Count evidence before return.")