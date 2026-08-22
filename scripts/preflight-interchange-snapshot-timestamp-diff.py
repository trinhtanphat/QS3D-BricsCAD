#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeSnapshotDiff.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeSnapshotTimestampDiffSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing interchange timestamp-diff contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        'Pair("updatedUtc", left.UpdatedUtc != right.UpdatedUtc)',
        'if (left.UpdatedUtc != right.UpdatedUtc) fields.Add("updatedUtc");',
    ):
        if token not in text:
            errors.append("ProjectInterchangeSnapshotDiff.cs missing normalized timestamp comparison token: " + token)
    if 'Pair("updatedUtc", !string.Equals(left.UpdatedUtcRaw, right.UpdatedUtcRaw' in text:
        errors.append("Project snapshot diff must compare timestamp instants, not raw timezone formatting.")
    if 'if (!string.Equals(left.UpdatedUtcRaw, right.UpdatedUtcRaw' in text:
        errors.append("Element snapshot diff must compare timestamp instants, not raw timezone formatting.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "IdenticalCanonicalTimestampsDoNotCreateFalseChange();",
        "DifferentCanonicalInstantsStillCreateChange();",
        '"2026-08-10T10:00:00.0000000Z"',
        '"2026-08-10T11:00:00.0000000Z"',
    ):
        if token not in text:
            errors.append("ProjectInterchangeSnapshotTimestampDiffSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: interchange snapshot diff compares validated canonical UTC instants without false changes for identical timestamps.")
