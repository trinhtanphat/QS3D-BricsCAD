#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingCatalogTraversalCountSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing measurement mapping known-count file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    for token in (
        "if (knownCount.HasValue && index >= knownCount.Value)",
        '"Measurement/work-item mapping source traversal produced more entries than its known Count reported " + knownCount.Value + "."',
        "if (index == MaximumEntries)",
        "if (knownCount.HasValue && index != knownCount.Value)",
        "TryGetKnownCount(mappings, out var conflictingKnownCounts, out var negativeKnownCount)",
    ):
        if token not in source:
            errors.append("mapping catalog missing Count-integrity contract: " + token)

    early = source.find("if (knownCount.HasValue && index >= knownCount.Value)")
    null_check = source.find("if (mapping == null)")
    duplicate_check = source.find("if (!mappingIds.Add(mapping.MappingId))")
    if early < 0 or null_check < 0 or duplicate_check < 0 or not (early < null_check < duplicate_check):
        errors.append("known-count overrun guard must run before unexpected mapping semantic validation")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "OverEnumerationRejectsEarly();",
        "KnownCountOverrunPrecedesUnexpectedMappingValidation();",
        "new UnexpectedOverrunCollection()",
        "yield return null!;",
        'Contains("traversal produced more entries than its known Count reported 1", error.Message);',
        "UnderEnumerationRejects();",
        "HonestKnownCountRemainsAccepted();",
        "PureStreamingRemainsAccepted();",
    ):
        if token not in smoke:
            errors.append("mapping traversal smoke missing Count-drift assertion/control: " + token)

print("QS3D measurement/work-item mapping known-count early-drift preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: mapping catalogs reject the first known-count overrun before semantic processing while retaining under-yield, exact-count, and streaming behavior.")
