#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
source = ROOT / "src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingKnownCountStabilitySmoke.cs"
errors = []

for path in (source, smoke):
    if not path.is_file():
        errors.append("missing mapping Count-stability file: " + str(path.relative_to(ROOT)))

if source.is_file():
    text = source.read_text(encoding="utf-8")
    ctor_start = text.find("public MeasurementWorkItemMappingCatalog(IEnumerable<MeasurementWorkItemMapping> mappings)")
    resolve_start = text.find("public MeasurementWorkItemMappingResolution Resolve", ctor_start)
    ctor = text[ctor_start:resolve_start] if ctor_start >= 0 and resolve_start > ctor_start else ""
    required = (
        "var knownCount = TryGetKnownCount(mappings",
        "using (var enumerator = mappings.GetEnumerator())",
        "var hasNext = enumerator.MoveNext();",
        'RevalidateKnownCount(mappings, knownCount, "MoveNext")',
        "if (!hasNext)",
        "index >= knownCount.Value",
        "var mapping = enumerator.Current;",
        'RevalidateKnownCount(mappings, knownCount, "Current")',
        'RevalidateKnownCount(mappings, knownCount, "completed traversal")',
        "index != knownCount.Value",
        "items.Sort(CompareMappings);",
        "Mappings = new ReadOnlyCollection<MeasurementWorkItemMapping>",
    )
    positions = [ctor.find(token) for token in required]
    if not ctor or any(pos < 0 for pos in positions) or positions != sorted(positions):
        errors.append("Mapping catalog must bind known Count across MoveNext/Current/completion before sort/publication.")

    stable_start = text.find("private static void RevalidateKnownCount(")
    stable_end = text.find("private static int? TryGetKnownCount(", stable_start)
    stable = text[stable_start:stable_end] if stable_start >= 0 and stable_end > stable_start else ""
    for token in (
        "TryGetKnownCount(mappings",
        "negativeKnownCount",
        "conflictingKnownCounts",
        "!reboundCount.HasValue || reboundCount.Value != admittedCount.Value",
    ):
        if token not in stable:
            errors.append("Mapping Count rebound validator missing contract token: " + token)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "GenericCountDriftRejects",
        "ReadOnlyCountDriftRejects",
        "NonGenericCountDriftRejects",
        "NegativePostTraversalCountRejects",
        "ConflictingPostTraversalCountsReject",
        "StableCountedSourceSucceeds",
        "PureStreamingSourceSucceeds",
    ):
        if token not in text:
            errors.append("Mapping Count-stability smoke missing regression token: " + token)

print("QS3D measurement/work-item mapping known-Count stability preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: mapping catalog binds deterministic Count evidence across traversal before publication.")
