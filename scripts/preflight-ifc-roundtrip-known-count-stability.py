#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "src/QS3D.Core/Export/IfcRoundTripKnownCountContract.cs"
PROJECTION = ROOT / "src/QS3D.Core/Export/IfcRoundTripProjection.cs"
EVIDENCE = ROOT / "src/QS3D.Core/Export/IfcRoundTripQuantityEvidence.cs"
RESULT = ROOT / "src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/IfcRoundTripKnownCountStabilitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/ifc-roundtrip-known-count-stability.md"
errors = []

for path, label in (
    (HELPER, "IFC post-traversal Count contract"),
    (PROJECTION, "IFC projection source"),
    (EVIDENCE, "IFC quantity-evidence source"),
    (RESULT, "IFC exchange-result source"),
    (SMOKE, "IFC Count-stability smoke"),
    (RUNBOOK, "IFC Count-stability runbook"),
):
    if not path.is_file():
        errors.append("missing " + label + ": " + str(path.relative_to(ROOT)))

helper = HELPER.read_text(encoding="utf-8") if HELPER.is_file() else ""
projection = PROJECTION.read_text(encoding="utf-8") if PROJECTION.is_file() else ""
evidence = EVIDENCE.read_text(encoding="utf-8") if EVIDENCE.is_file() else ""
result = RESULT.read_text(encoding="utf-8") if RESULT.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.is_file() else ""

for token in (
    "RequireStableAfterTraversal<T>",
    "values is ICollection<T>",
    "values is IReadOnlyCollection<T>",
    "values is ICollection nonGenericCollection",
    "invalid negative known Count value after traversal.",
    "conflicting known Count values after traversal.",
    "source Count changed during traversal.",
):
    if token not in helper:
        errors.append("IFC post-traversal Count contract missing token: " + token)

if projection.count("IfcRoundTripKnownCountContract.RequireStableAfterTraversal(") != 3:
    errors.append("expected exactly three post-traversal Count checks in projection source")
if evidence.count("IfcRoundTripKnownCountContract.RequireStableAfterTraversal(") != 1:
    errors.append("expected exactly one post-traversal Count check in quantity-evidence source")
if result.count("IfcRoundTripKnownCountContract.RequireStableAfterTraversal(") != 1:
    errors.append("expected exactly one post-traversal Count check in exchange-result source")

ordering_checks = (
    (
        projection,
        "IFC round-trip dimension source Count does not match enumerated dimension count.",
        '"IFC round-trip dimension");',
        "items.Sort(IfcRoundTripNumericPropertyComparer.Instance)",
        "dimension",
    ),
    (
        projection,
        "IFC round-trip provenance source Count does not match enumerated provenance count.",
        '"IFC round-trip provenance");',
        "if (items.Count == 0)",
        "provenance",
    ),
    (
        projection,
        "IFC round-trip projection source Count does not match enumerated projection count.",
        '"IFC round-trip projection");',
        "items.Sort(IfcRoundTripProjectionComparer.CanonicalOrder)",
        "projection set",
    ),
    (
        evidence,
        "IFC round-trip quantity evidence source Count does not match enumerated candidate count.",
        '"IFC round-trip quantity evidence");',
        "candidates.Sort(IfcRoundTripQuantityEvidenceComparer.Instance)",
        "quantity evidence",
    ),
    (
        result,
        "IFC exchange result source Count does not match enumerated result count.",
        '"IFC exchange result");',
        "var items = byExternalIdentity.Values.ToList()",
        "exchange result",
    ),
)
for source, mismatch_token, stable_label, publication_token, label in ordering_checks:
    mismatch = source.find(mismatch_token)
    stable = source.find("IfcRoundTripKnownCountContract.RequireStableAfterTraversal(", mismatch)
    stable_label_index = source.find(stable_label, stable)
    publication = source.find(publication_token, stable)
    if not (mismatch >= 0 and stable > mismatch and stable_label_index > stable and publication > stable_label_index):
        errors.append(label + " post-traversal Count check must occur after under-yield detection and before canonical publication")

# Preserve the existing issue-4316 early-overrun contract and independent streaming bounds.
if projection.count("IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(") != 3:
    errors.append("projection source lost one of the three pre-item Count overrun guards")
if evidence.count("IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(") != 1:
    errors.append("quantity-evidence source lost its pre-item Count overrun guard")
if result.count("IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(") != 1:
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
    "PostTraversalCountCollection<T>",
    "PostTraversalConflictingCountCollection<T>",
    "CountReads",
    "Stream<T>",
    "[ModuleInitializer]",
):
    if token not in smoke:
        errors.append("IFC Count-stability smoke missing regression/control: " + token)

for token in (
    "SOURCE_READY",
    "post-traversal",
    "five IFC collection boundaries",
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

print("PASS: IFC round-trip deterministic Count evidence is rebound after traversal before canonical sorting/grouping/publication, while early-overrun, under-yield and streaming caps remain enforced.")
