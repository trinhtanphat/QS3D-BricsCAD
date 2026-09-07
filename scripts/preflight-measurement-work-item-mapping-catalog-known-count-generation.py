#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Mapping" / "MeasurementWorkItemMapping.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "MeasurementWorkItemMappingCatalogKnownCountGenerationSmoke.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

constructor_start = source.index("public MeasurementWorkItemMappingCatalog(IEnumerable<MeasurementWorkItemMapping> mappings)")
resolve_start = source.index("public MeasurementWorkItemMappingResolution Resolve", constructor_start)
constructor = source[constructor_start:resolve_start]

for token in (
    "using (var enumerator = mappings.GetEnumerator())",
    "var hasNext = enumerator.MoveNext();",
    'RevalidateKnownCount(mappings, knownCount, "MoveNext")',
    "if (!hasNext)",
    "if (knownCount.HasValue && index >= knownCount.Value)",
    "var mapping = enumerator.Current;",
    'RevalidateKnownCount(mappings, knownCount, "Current")',
    'RevalidateKnownCount(mappings, knownCount, "completed traversal")',
):
    if token not in constructor:
        fail(f"mapping catalog generation binding must retain constructor token: {token}")

move_next = constructor.index("var hasNext = enumerator.MoveNext();")
move_rebind = constructor.index('RevalidateKnownCount(mappings, knownCount, "MoveNext")')
current = constructor.index("var mapping = enumerator.Current;")
current_rebind = constructor.index('RevalidateKnownCount(mappings, knownCount, "Current")')
accept = constructor.index("mappingIds.Add(mapping.MappingId)")
terminal_rebind = constructor.index('RevalidateKnownCount(mappings, knownCount, "completed traversal")')
final_cardinality = constructor.index("knownCount.HasValue && index != knownCount.Value")
if not (move_next < move_rebind < current < current_rebind < accept < terminal_rebind < final_cardinality):
    fail("mapping catalog must rebind Count after MoveNext and Current before mapping acceptance, then again at terminal traversal")

helper_start = source.index("private static void RevalidateKnownCount(")
helper_end = source.index("private static int? TryGetKnownCount(", helper_start)
helper = source[helper_start:helper_end]
for token in (
    "negativeKnownCount",
    "conflictingKnownCounts",
    "reboundCount.Value != admittedCount.Value",
    '" known Count changed during " + boundary',
):
    if token not in helper:
        fail(f"mapping catalog Count rebound helper must retain: {token}")

for token in (
    "MoveNextInducedCountDriftFailsBeforeCurrent",
    "CurrentInducedCountDriftFailsBeforeMappingAcceptance",
    "StableCountedSourceRemainsAccepted",
    "PureStreamingSourceRemainsAccepted",
    "_owner._count = 1",
    "[ModuleInitializer]",
):
    if token not in smoke:
        fail(f"mapping catalog generation smoke must retain: {token}")

print("PASS: mapping catalog binds supported known Count at MoveNext, Current and terminal traversal boundaries")
