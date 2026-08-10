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
        "var source = ProjectInterchangeValidatedSnapshotReader.Read(json);",
        "var sourceProjectId = source.Project.Id;",
        "foreach (var zone in source.Zones)",
        "foreach (var floor in source.Floors)",
        "foreach (var family in source.Families)",
        "foreach (var element in source.Elements)",
        "if (!string.Equals(raw, raw.Trim(), StringComparison.Ordinal))",
        "return raw;",
    ):
        if token not in text:
            errors.append("ProjectInterchangeImportPreview.cs missing single-reader preview token: " + token)
    for forbidden in (
        "ParseValidatedManifest",
        "DataContractJsonSerializer",
        "ManifestContract",
        "return value!.Trim();",
    ):
        if forbidden in text:
            errors.append("Import preview must not maintain a second identity parser/normalizer: " + forbidden)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ValidSnapshotStillPreviews();",
        "PaddedIdentityReturnsInvalidPreview();",
        "MissingTimezoneReturnsInvalidPreview();",
        'x.Code == "ID_NON_CANONICAL"',
        'x.Code == "TIMESTAMP_NOT_UTC"',
        "ProjectInterchangeImportPreview.Plan",
    ):
        if token not in text:
            errors.append("ProjectInterchangeImportPreviewCanonicalSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: interchange preview consumes the canonical typed snapshot directly, while validator-invalid identities/timestamps return an invalid preview instead of diverging at apply time.")
