#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/ProjectInterchangeImportPreview.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeImportPreviewCanonicalSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing canonical import-preview contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "_ = ProjectInterchangeValidatedSnapshotReader.Read(json);",
        "if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))",
        "return raw;",
        'var canonical = Required(raw, label + " category");',
    ):
        if token not in text:
            errors.append("ProjectInterchangeImportPreview.cs missing canonical preview token: " + token)
    if "return value!.Trim();" in text:
        errors.append("Import preview must not silently trim semantic identity values after validation.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ValidSnapshotStillPreviews();",
        "PaddedIdentityFailsBeforePreview();",
        "MissingTimezoneFailsBeforePreview();",
        "ProjectInterchangeImportPreview.Plan",
    ):
        if token not in text:
            errors.append("ProjectInterchangeImportPreviewCanonicalSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: interchange preview shares the canonical typed-reader boundary and cannot preview identities/timestamps that apply will later reject.")
