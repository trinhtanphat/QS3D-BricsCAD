#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "src/QS3D.Core/Export/IfcRoundTripKnownCountContract.cs"
PROJECTION = ROOT / "src/QS3D.Core/Export/IfcRoundTripProjection.cs"
EVIDENCE = ROOT / "src/QS3D.Core/Export/IfcRoundTripQuantityEvidence.cs"
RESULT = ROOT / "src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/IfcRoundTripKnownCountStabilitySmoke.cs"
TRANSIENT_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/IfcRoundTripTransientKnownCountStabilitySmoke.cs"
CURRENT_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/IfcRoundTripProjectionCurrentCountIntegritySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/ifc-roundtrip-known-count-stability.md"
errors = []

for path, label in (
    (HELPER, "IFC Count contract"),
    (PROJECTION, "IFC projection source"),
    (EVIDENCE, "IFC quantity-evidence source"),
    (RESULT, "IFC exchange-result source"),
    (SMOKE, "IFC Count-stability smoke"),
    (TRANSIENT_SMOKE, "IFC transient Count-stability smoke"),
    (CURRENT_SMOKE, "IFC Current-induced Count-integrity smoke"),
    (RUNBOOK, "IFC Count-stability runbook"),
):
    if not path.is_file():
        errors.append("missing " + label + ": " + str(path.relative_to(ROOT)))

helper = HELPER.read_text(encoding="utf-8") if HELPER.is_file() else ""
projection = PROJECTION.read_text(encoding="utf-8") if PROJECTION.is_file() else ""
evidence = EVIDENCE.read_text(encoding="utf-8") if EVIDENCE.is_file() else ""
result = RESULT.read_text(encoding="utf-8") if RESULT.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""
transient_smoke = TRANSIENT_SMOKE.read_text(encoding="utf-8") if TRANSIENT_SMOKE.is_file() else ""
current_smoke = CURRENT_SMOKE.read_text(encoding="utf-8") if CURRENT_SMOKE.is_file() else ""
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.is_file() else ""

for token in (
    "RequireStableDuringTraversal<T>",
    "RequireStableAfterTraversal<T>",
    "values is ICollection<T>",
    "values is IReadOnlyCollection<T>",
    "values is ICollection nonGenericCollection",
    '"during traversal"',
    '"after traversal"',
    "source Count changed during traversal.",
):
    if token not in helper:
        errors.append("IFC Count contract missing token: " + token)

traversal_token = "IfcRoundTripKnownCountContract.RequireStableDuringTraversal("
post_traversal_token = "IfcRoundTripKnownCountContract.RequireStableAfterTraversal("
count_guard_token = "IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount("

if projection.count(traversal_token) != 9:
    errors.append("expected exactly nine traversal Count rebound checks in projection source")
if projection.count(post_traversal_token) != 3:
    errors.append("expected exactly three post-traversal Count checks in projection source")
if evidence.count(post_traversal_token) != 1:
    errors.append("expected exactly one post-traversal Count check in quantity-evidence source")
if result.count(post_traversal_token) != 1:
    errors.append("expected exactly one post-traversal Count check in exchange-result source")


def method_region(source, start_token, end_token, label):
    start = source.find(start_token)
    if start < 0:
        errors.append("could not find " + label + " traversal start")
        return ""
    end = source.find(end_token, start + len(start_token))
    if end < 0:
        errors.append("could not find " + label + " traversal end")
        return ""
    return source[start:end]


dimension_region = method_region(
    projection,
    "private static IReadOnlyList<IfcRoundTripNumericProperty> CanonicalizeDimensions",
    "private static IReadOnlyList<string> CanonicalizeProvenance",
    "dimension")
provenance_region = method_region(
    projection,
    "private static IReadOnlyList<string> CanonicalizeProvenance",
    "private static int? TryGetKnownCount<T>",
    "provenance")
projection_set_source = projection[projection.find("public sealed class IfcRoundTripProjectionSet"):]
projection_set_region = method_region(
    projection_set_source,
    "public static IfcRoundTripProjectionSet Create",
    "private static int? TryGetKnownCount(",
    "projection set")

for region, label in (
    (dimension_region, "dimension"),
    (provenance_region, "provenance"),
    (projection_set_region, "projection set"),
):
    if not region:
        continue
    if region.count(traversal_token) != 3:
        errors.append(label + " traversal must contain exactly three Count rebound checks")
        continue
    first = region.find(traversal_token)
    move = region.find("enumerator.MoveNext()", first)
    second = region.find(traversal_token, first + len(traversal_token))
    count_guard = region.find(count_guard_token, second)
    current = region.find("enumerator.Current", count_guard)
    third = region.find(traversal_token, current)
    if not (0 <= first < move < second < count_guard < current < third):
        errors.append(label + " must rebind Count before MoveNext, after successful MoveNext before Current, and immediately after Current before staging")

ordering_checks = (
    (projection, "IFC round-trip dimension source Count does not match enumerated dimension count.", '"IFC round-trip dimension");', "items.Sort(IfcRoundTripNumericPropertyComparer.Instance)", "dimension"),
    (projection, "IFC round-trip provenance source Count does not match enumerated provenance count.", '"IFC round-trip provenance");', "if (items.Count == 0)", "provenance"),
    (projection, "IFC round-trip projection source Count does not match enumerated projection count.", '"IFC round-trip projection");', "items.Sort(IfcRoundTripProjectionComparer.CanonicalOrder)", "projection set"),
    (evidence, "IFC round-trip quantity evidence source Count does not match enumerated candidate count.", '"IFC round-trip quantity evidence");', "candidates.Sort(IfcRoundTripQuantityEvidenceComparer.Instance)", "quantity evidence"),
    (result, "IFC exchange result source Count does not match enumerated result count.", '"IFC exchange result");', "var items = byExternalIdentity.Values.ToList()", "exchange result"),
)
for source, mismatch_token, stable_label, publication_token, label in ordering_checks:
    mismatch = source.find(mismatch_token)
    stable = source.find(post_traversal_token, mismatch)
    stable_label_index = source.find(stable_label, stable)
    publication = source.find(publication_token, stable)
    if not (mismatch >= 0 and stable > mismatch and stable_label_index > stable and publication > stable_label_index):
        errors.append(label + " post-traversal Count check must occur after under-yield detection and before canonical publication")

if projection.count(count_guard_token) != 3:
    errors.append("projection source lost one of the three pre-item Count overrun guards")
if evidence.count(count_guard_token) != 1:
    errors.append("quantity-evidence source lost its pre-item Count overrun guard")
if result.count(count_guard_token) != 1:
    errors.append("exchange-result source lost its pre-item Count overrun guard")
for source, tokens in (
    (projection, ("MaxNestedCollectionItems", "MaxProjections")),
    (evidence, ("MaxCandidates",)),
    (result, ("MaxResultsPerCollection", "DuplicateExternalIdentityDetail")),
):
    for token in tokens:
        if token not in source:
            errors.append("IFC source lost existing bound/semantic token: " + token)

for token in (
    "DimensionCountDriftFailsBeforePublication();",
    "ProvenanceCountDriftFailsBeforePublication();",
    "ProjectionSetCountDriftFailsBeforePublication();",
    "QuantityEvidenceCountDriftFailsBeforePublication();",
    "ExchangeResultCountDriftFailsBeforePublication();",
    "RejectsNegativePostTraversalCount();",
    "RejectsConflictingPostTraversalCounts();",
    "StableCountedInputsRemainAccepted();",
    "PureStreamingInputsRemainAccepted();",
    "[ModuleInitializer]",
):
    if token not in smoke:
        errors.append("IFC Count-stability smoke missing historical regression/control: " + token)

for token in (
    "DimensionGrowthFailsBeforeCurrent();",
    "ProvenanceShrinkFailsBeforeCurrent();",
    "ProjectionNegativeCountFailsBeforeCurrent();",
    "DimensionConflictingCountFailsBeforeCurrent();",
    "StableCountedInputsRemainAccepted();",
    "CurrentReads",
    "DriftAfterMoveCollection<T>",
    "ConflictAfterMoveCollection<T>",
    "[ModuleInitializer]",
):
    if token not in transient_smoke:
        errors.append("IFC transient Count smoke missing regression/control: " + token)

for token in (
    "DimensionCurrentDriftWinsOverNullSemanticFailure();",
    "ProvenanceCurrentDriftWinsOverTokenSemanticFailure();",
    "ProjectionCurrentDriftWinsOverNullSemanticFailure();",
    "StableCountedInputsRemainAccepted();",
    "Equal(4, dimensions.CountReads",
    "Equal(4, provenance.CountReads",
    "Equal(4, projections.CountReads",
    "Equal(6, dimensions.CountReads",
    "CurrentCountCollection<T>",
    "[ModuleInitializer]",
):
    if token not in current_smoke:
        errors.append("IFC Current-induced Count smoke missing regression/control: " + token)

for token in (
    "transient",
    "before each `MoveNext`",
    "before any `Current` read",
    "post-`Current` rebound",
    "caller-controlled `Current` getter",
    "five established IFC boundaries",
    "pure streaming",
    "10,000",
    "licensed BricsCAD",
):
    if token not in runbook:
        errors.append("IFC Count-stability runbook missing contract text: " + token)

print("QS3D IFC round-trip known-count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: IFC projection Count evidence is rebound before MoveNext, before Current, immediately after Current before semantic staging, and after traversal before publication while historical early-overrun, under-yield and streaming caps remain enforced.")