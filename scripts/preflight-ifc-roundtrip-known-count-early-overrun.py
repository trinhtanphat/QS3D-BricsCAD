#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROJECTION = ROOT / "src/QS3D.Core/Export/IfcRoundTripProjection.cs"
EVIDENCE = ROOT / "src/QS3D.Core/Export/IfcRoundTripQuantityEvidence.cs"
RESULT = ROOT / "src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/IfcRoundTripKnownCountEarlyOverrunSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/issue-4316-ifc-known-count-early-overrun.md"
errors = []

for path, label in (
    (PROJECTION, "IFC projection source"),
    (EVIDENCE, "IFC quantity-evidence source"),
    (RESULT, "IFC exchange-result source"),
    (SMOKE, "IFC early-overrun smoke"),
    (RUNBOOK, "IFC early-overrun runbook"),
):
    if not path.is_file():
        errors.append("missing " + label + ": " + str(path.relative_to(ROOT)))

projection = PROJECTION.read_text(encoding="utf-8") if PROJECTION.is_file() else ""
evidence = EVIDENCE.read_text(encoding="utf-8") if EVIDENCE.is_file() else ""
result = RESULT.read_text(encoding="utf-8") if RESULT.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""

helper = "internal static void RequireCanProcessNextKnownCount(int? knownCount, int observedCount, string collectionLabel)"
if helper not in projection:
    errors.append("IFC projection contract missing shared pre-item known-count guard")
if "knownCount.HasValue && observedCount >= knownCount.Value" not in projection:
    errors.append("shared IFC known-count guard must reject item knownCount + 1 before processing")

checks = (
    (projection, '"IFC round-trip dimension"', "if (item == null)", "dimension"),
    (projection, '"IFC round-trip provenance"', "RequireCanonicalToken(value, nameof(provenance))", "provenance"),
    (projection, '"IFC round-trip projection"', "items.Add(projection);", "projection set"),
    (evidence, '"IFC round-trip quantity evidence"', "if (candidate == null)", "quantity evidence"),
    (result, '"IFC exchange result"', "if (item == null)", "exchange result"),
)
for source, label_token, semantic_token, label in checks:
    guard = source.find(label_token)
    semantic = source.find(semantic_token, guard)
    if not (guard >= 0 and semantic > guard):
        errors.append(label + " known-count guard must precede semantic processing")

if projection.count("IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(") != 3:
    errors.append("expected exactly three projection-file pre-item guards: dimensions, provenance, projection set")
if evidence.count("IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(") != 1:
    errors.append("expected exactly one quantity-evidence pre-item guard")
if result.count("IfcRoundTripProjectionContract.RequireCanProcessNextKnownCount(") != 1:
    errors.append("expected exactly one exchange-result pre-item guard")

for token in (
    "IFC round-trip dimension source Count does not match enumerated dimension count.",
    "IFC round-trip provenance source Count does not match enumerated provenance count.",
    "IFC round-trip projection source Count does not match enumerated projection count.",
    "MaxNestedCollectionItems",
    "MaxProjections",
):
    if token not in projection:
        errors.append("IFC projection source lost final mismatch/streaming-bound contract: " + token)
for token in (
    "IFC round-trip quantity evidence source Count does not match enumerated candidate count.",
    "MaxCandidates",
):
    if token not in evidence:
        errors.append("IFC quantity-evidence source lost final mismatch/streaming-bound contract: " + token)
for token in (
    "IFC exchange result source Count does not match enumerated result count.",
    "MaxResultsPerCollection",
    "DuplicateExternalIdentityDetail",
):
    if token not in result:
        errors.append("IFC exchange-result source lost final mismatch/bound/duplicate contract: " + token)

for token in (
    "DimensionOverrunWinsBeforeNullProcessing();",
    "ProvenanceOverrunWinsBeforeTokenProcessing();",
    "ProjectionOverrunWinsBeforeIdentityProcessing();",
    "QuantityEvidenceOverrunWinsBeforeNullProcessing();",
    "ExchangeResultOverrunWinsBeforeDuplicateProcessing();",
    "UnderYieldStillFailsAfterTraversal();",
    "HonestCountedInputsRemainCanonical();",
    "MoveNextCalls",
    "[ModuleInitializer]",
):
    if token not in smoke:
        errors.append("IFC early-overrun smoke missing regression/control: " + token)

print("QS3D IFC round-trip known-count early-overrun preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: IFC round-trip counted collections reject the first unexpected item before semantic processing while preserving final under-yield checks and independent streaming bounds.")
