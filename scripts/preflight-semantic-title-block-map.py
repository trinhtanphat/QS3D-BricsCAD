#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
BUILDER = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticTitleBlockParameterMapBuilder.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticViewSheetPlannerSmoke.cs"
COUNT_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticTitleBlockKnownCountIntegritySmoke.cs"


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(f"{label}: missing {needle!r}")


def require_order(text, first, second, label, failures):
    first_index = text.find(first)
    second_index = text.find(second)
    if first_index < 0 or second_index < 0 or first_index >= second_index:
        failures.append(f"{label}: expected {first!r} before {second!r}")


def main():
    failures = []
    for path in (BUILDER, SMOKE, COUNT_SMOKE):
        if not path.is_file():
            failures.append(f"missing required source file: {path.relative_to(ROOT)}")
    if failures:
        for failure in failures:
            print("ERROR:", failure)
        return 1

    builder = BUILDER.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    count_smoke = COUNT_SMOKE.read_text(encoding="utf-8")

    for field in ("SheetId", "SheetNumber", "SheetName", "TitleBlockName", "PlacedViewCount"):
        require(builder, field, "explicit semantic Sheet field", failures)
    require(builder, "private const int MaxParameters = 128;", "mapping bound", failures)
    require(builder, "private const int MaxDestinationTagLength = 128;", "destination-tag bound", failures)
    require(builder, "new HashSet<string>(StringComparer.OrdinalIgnoreCase)", "case-insensitive destination uniqueness", failures)
    require(builder, "duplicate destination tag", "duplicate destination failure", failures)
    require(builder, ".OrderBy(x => x.DestinationTag, StringComparer.OrdinalIgnoreCase)", "deterministic destination ordering", failures)
    require(builder, "CultureInfo.InvariantCulture", "invariant numeric rendering", failures)
    require(builder, "sheet.TitleBlockName ?? string.Empty", "optional title-block rendering", failures)
    require(builder, "Unsupported semantic title-block source field", "unknown-field fail closed", failures)
    require(builder, "new List<SemanticTitleBlockParameterValue>(values).AsReadOnly()", "defensive map snapshot", failures)

    require(builder, "observedCount >= knownCount.Value", "known Count overrun guard", failures)
    require(builder, "known Count was exceeded during traversal", "known Count overrun diagnostic", failures)
    require(builder, "RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);", "Count rebound", failures)
    require(builder, "known Count changed during traversal", "Count drift diagnostic", failures)
    require(builder, "conflicting known Count values after traversal", "Count conflict diagnostic", failures)
    require(builder, "negative known Count value after traversal", "negative Count diagnostic", failures)

    method_start = builder.find("private static List<SemanticTitleBlockParameterDefinition> MaterializeDefinitionsBounded(")
    method_end = builder.find("private static void RevalidateKnownCountAfterTraversal(", method_start)
    materialize = builder[method_start:method_end] if method_start >= 0 and method_end > method_start else ""
    if not materialize:
        failures.append("bounded title-block materialization method could not be isolated")
    else:
        anchors = [
            "RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);",
            "var moved = enumerator.MoveNext();",
            "RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);",
            "if (!moved)",
            "if (knownCount.HasValue && observedCount >= knownCount.Value)",
            "if (observedCount >= MaxParameters)",
            "var definition = enumerator.Current;",
            "RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);",
            "result.Add(definition);",
            "observedCount++;",
        ]
        cursor = 0
        for anchor in anchors:
            found = materialize.find(anchor, cursor)
            if found < 0:
                failures.append(f"known Count traversal ordering: missing ordered anchor {anchor!r}")
                break
            cursor = found + len(anchor)

        if materialize.count("RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);") != 4:
            failures.append(
                "known Count traversal must rebind before MoveNext, after MoveNext, after Current, and after traversal"
            )

        current = materialize.find("var definition = enumerator.Current;")
        post_current = materialize.find("RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);", current)
        retention = materialize.find("result.Add(definition);", post_current)
        if min(current, post_current, retention) < 0 or not (current < post_current < retention):
            failures.append("Count evidence must be rebound after Current before retaining a definition")

        if "while (enumerator.MoveNext())" in materialize:
            failures.append("bounded materialization must expose explicit pre/post MoveNext Count rebound boundaries")

    require_order(
        builder,
        "RevalidateKnownCountAfterTraversal(definitions, knownCount.Value);",
        "return result;",
        "Count evidence must be rebound before publishing materialized definitions",
        failures,
    )

    for forbidden in ("Bricscad.", "Teigha.", "ObjectId", "BlockReference", "AttributeReference"):
        if forbidden in builder:
            failures.append(f"pure-Core title-block mapping must not depend on native CAD APIs: found {forbidden!r}")

    require(smoke, "TitleBlockParameterMapIsDeterministicAndImmutable", "deterministic title-block smoke", failures)
    require(smoke, "TitleBlockParameterMapFailsClosed", "fail-closed title-block smoke", failures)
    require(smoke, "definitions.Clear();", "source-list defensive-copy smoke", failures)
    require(smoke, "mutable.Add(map.Values[0])", "read-only map smoke", failures)
    require(smoke, "(SemanticTitleBlockSheetField)999", "unknown enum smoke", failures)
    require(smoke, "129", "mapping bound smoke", failures)

    require(count_smoke, "[ModuleInitializer]", "registered Count integrity smoke", failures)
    require(count_smoke, "KnownCountOverrunFailsBeforeRetentionAndLaterTail", "early Count overrun regression", failures)
    require(count_smoke, "CurrentReads", "overrun no-retention evidence", failures)
    require(count_smoke, "PostTraversalUniformCountDriftFailsClosed", "uniform Count drift regression", failures)
    require(count_smoke, "PostTraversalSingleSurfaceDriftFailsClosed", "per-interface Count drift regression", failures)
    require(count_smoke, "PostTraversalNegativeCountFailsClosed", "post-traversal negative Count regression", failures)
    require(count_smoke, "UnderYieldMismatchFailsClosed", "under-yield regression", failures)
    require(count_smoke, "EnumerableOnlyStreamingBoundRemainsSupported", "streaming bound regression", failures)
    require(count_smoke, "ConsistentBoundaryCountRemainsSupported", "honest counted boundary regression", failures)

    if failures:
        print("QS3D Semantic Title Block parameter-map preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: title-block parameter mapping is pure-Core and native-API independent.")
    print("PASS: only explicit semantic Sheet fields can supply P0 values.")
    print("PASS: destination tags are bounded, case-insensitively unique and deterministically ordered.")
    print("PASS: returned parameter values form a defensive read-only snapshot.")
    print("PASS: deterministic Count evidence is rebound around MoveNext and after Current before retention.")
    return 0


if __name__ == "__main__":
    sys.exit(main())