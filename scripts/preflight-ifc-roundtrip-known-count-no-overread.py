#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    ROOT / "src/QS3D.Core/Export/IfcRoundTripProjection.cs",
    ROOT / "src/QS3D.Core/Export/IfcRoundTripQuantityEvidence.cs",
    ROOT / "src/QS3D.Core/Export/IfcRoundTripExchangeResult.cs",
]
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/IfcRoundTripKnownCountNoOverreadSmoke.cs"
errors = []

for path in FILES + [SMOKE]:
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

projection = FILES[0].read_text(encoding="utf-8") if FILES[0].is_file() else ""
evidence = FILES[1].read_text(encoding="utf-8") if FILES[1].is_file() else ""
result = FILES[2].read_text(encoding="utf-8") if FILES[2].is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""

for source, foreach_token, label in (
    (projection, "foreach (var item in dimensions)", "dimensions"),
    (projection, "foreach (var value in provenance)", "provenance"),
    (projection, "foreach (var projection in projections)", "projection set"),
    (evidence, "foreach (var candidate in evidence)", "quantity evidence"),
    (result, "foreach (var item in results)", "exchange results"),
):
    if foreach_token in source:
        errors.append(label + " still uses foreach and can read unexpected Current before the Count guard")

for source, current_token, guard_label, limit_token, label in (
    (projection, "var item = enumerator.Current;", '"IFC round-trip dimension"', "MaxNestedCollectionItems", "dimensions"),
    (projection, "var value = enumerator.Current;", '"IFC round-trip provenance"', "MaxNestedCollectionItems", "provenance"),
    (projection, "var projection = enumerator.Current;", '"IFC round-trip projection"', "MaxProjections", "projection set"),
    (evidence, "var candidate = enumerator.Current;", '"IFC round-trip quantity evidence"', "MaxCandidates", "quantity evidence"),
    (result, "var item = enumerator.Current;", '"IFC exchange result"', "MaxResultsPerCollection", "exchange results"),
):
    guard = source.find(guard_label)
    limit = source.find(limit_token, guard)
    current = source.find(current_token, guard)
    if not (guard >= 0 and limit > guard and current > limit):
        errors.append(label + " must enforce known Count and streaming ceiling before Current")

for token in (
    "CurrentReads",
    "DimensionKnownCountOverrunDoesNotReadUnexpectedCurrent();",
    "ProvenanceKnownCountOverrunDoesNotReadUnexpectedCurrent();",
    "ProjectionKnownCountOverrunDoesNotReadUnexpectedCurrent();",
    "QuantityEvidenceKnownCountOverrunDoesNotReadUnexpectedCurrent();",
    "ExchangeResultKnownCountOverrunDoesNotReadUnexpectedCurrent();",
    "Equal(1, source.CurrentReads",
    "[ModuleInitializer]",
):
    if token not in smoke:
        errors.append("no-overread smoke missing token: " + token)

print("QS3D IFC round-trip known-Count no-overread preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: IFC bounded collections reject Count/streaming overruns before caller-controlled Current is observed.")
