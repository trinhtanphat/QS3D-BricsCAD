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
    required_order = (
        "var knownCount = TryGetKnownCount(elementIds",
        "using (var enumerator = elementIds.GetEnumerator())",
        "while (enumerator.MoveNext())",
        "inputCount >= knownCount.Value",
        "var rawId = enumerator.Current;",
        "inputCount != knownCount.Value",
        "RevalidateKnownCountAfterTraversal(elementIds, knownCount);",
        "return roots.AsReadOnly();",
    )
    positions = [materialize.find(token) for token in required_order]
    if not materialize or any(pos < 0 for pos in positions) or positions != sorted(positions):
        errors.append("Locate root selection must enforce MoveNext -> known-Count guard -> Current and rebind Count before return.")
    if "foreach (var rawId in elementIds)" in materialize:
        errors.append("Locate root selection must not use outer foreach because it can observe Current before Count-overrun rejection.")
    for token in (
        "elementIds is ICollection<string>",
        "elementIds is IReadOnlyCollection<string>",
        "elementIds is System.Collections.ICollection",
        "negativeKnownCount",
        "conflictingKnownCounts",
        "!reboundCount.HasValue || reboundCount.Value != admittedCount.Value",
        "MaxRootElementIdInputCount",
        "non-canonical semantic element id",
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

print("PASS: Locate root materialization rejects Count overrun before Current and revalidates deterministic Count evidence.")
