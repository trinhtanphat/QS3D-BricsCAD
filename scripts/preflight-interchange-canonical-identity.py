#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeValidatedSnapshotReader.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeCanonicalIdentitySmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing interchange canonical identity contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "private static string CanonicalOptional",
        "private static string CanonicalRequired",
        "if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))",
        'CanonicalOptional(x.FamilyId, "element familyId")',
        'CanonicalOptional(x.FloorId, "element floorId")',
        'CanonicalOptional(x.ZoneId, "element zoneId")',
        'CanonicalRequired(x.SourceRefScope, "sourceRefScope")',
        "CanonicalRequired(x, label + \"[\"",
        "CanonicalRequired(pair.Key, label + \" key\")",
        "DateTimeOffset.TryParse",
        "return parsed.UtcDateTime;",
    ):
        if token not in text:
            errors.append("ProjectInterchangeValidatedSnapshotReader.cs missing canonical identity token: " + token)
    if "source.Select(x => (x ?? string.Empty).Trim())" in text:
        errors.append("Interchange identity lists must reject padding rather than silently trim it.")
    if "var key = (pair.Key ?? string.Empty).Trim();" in text:
        errors.append("Interchange map/quantity keys must reject padding rather than silently trim it.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RejectsPaddedProjectId();",
        "RejectsPaddedRelationId();",
        "RejectsPaddedDependency();",
        "RejectsPaddedSourceHandle();",
        "RejectsPaddedPropertyKey();",
        "RejectsTimestampWithoutOffset();",
        "RejectsTimestampWithExplicitOffset();",
        "AcceptsCanonicalUtcDeterministically();",
    ):
        if token not in text:
            errors.append("ProjectInterchangeCanonicalIdentitySmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: typed interchange snapshots reject padded semantic identities and non-canonical timestamps, accept exact UTC round-trip timestamps, and preserve free-text property values.")
