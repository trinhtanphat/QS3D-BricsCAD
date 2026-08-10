#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
READER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeValidatedSnapshotReader.cs"
VALIDATOR = ROOT / "src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeValidatedSnapshotReaderSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/INTERCHANGE-VALIDATED-READER.md"
errors = []

for path in (READER, VALIDATOR, SMOKE, REG, DOC):
    if not path.is_file(): errors.append("missing validated-reader contract file: " + str(path.relative_to(ROOT)))

if READER.is_file():
    text = READER.read_text(encoding="utf-8")
    for token in (
        "var validation = ProjectInterchangeJsonValidator.Validate(json);",
        "if (!validation.IsValid)",
        "throw new InvalidDataException(\"Semantic snapshot validation failed before reading\"",
        "new ReadOnlyDictionary<string, string>(copy)",
        "new ReadOnlyDictionary<string, double>(copy)",
        ".ToList().AsReadOnly()",
        "public IReadOnlyList<string> SourceHandles",
        "public IReadOnlyList<string> Dependencies",
        "public IReadOnlyDictionary<string, string> Properties",
        "public IReadOnlyDictionary<string, double> Quantities",
        "Enum.IsDefined(typeof(ElementCategory), category)",
        "public ProjectInterchangeValidationResult Validation",
        "public string UpdatedUtcRaw",
        "public DateTime? UpdatedUtc",
    ):
        if token not in text: errors.append("ProjectInterchangeValidatedSnapshotReader.cs missing validation/immutability token: " + token)
    for token in (
        "new ProjectState(",
        ".Elements.Add(",
        ".Families.Add(",
        ".Floors.Add(",
        ".Zones.Add(",
        "GeneratedSolidHandle",
        "ProjectStateSnapshot.Restore(",
    ):
        if token in text: errors.append("validated reader must remain detached/read-only; found forbidden mutation/ownership token: " + token)

if VALIDATOR.is_file() and "source handles are provenance only and are not import authority" not in VALIDATOR.read_text(encoding="utf-8"):
    errors.append("validator lost drawing-local source provenance boundary")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ExportedSnapshotReadsAllPortableFields",
        "ReaderCollectionsAreImmutableSnapshots",
        "InvalidSnapshotFailsBeforeTypedRead",
        "MissingTimestampWarningRemainsReadable",
        "Throws<NotSupportedException>",
    ):
        if token not in text: errors.append("ProjectInterchangeValidatedSnapshotReaderSmoke.cs missing scenario: " + token)

if REG.is_file() and "ProjectInterchangeValidatedSnapshotReaderSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("validated snapshot reader smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "validation-first immutable typed reading only",
        "not import permission",
        "does not construct or replace `ProjectState`",
        "sourceRefScope = drawing-local",
        "JSON import/round-trip remains intentionally incomplete",
    ):
        if token not in text: errors.append("INTERCHANGE-VALIDATED-READER.md missing reader/import boundary: " + token)

print("QS3D interchange validated snapshot reader preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: validated Semantic Snapshot v1 data is exposed as immutable typed portable data without constructing target ProjectState, rebinding drawing-local Handles or granting native ownership/import authority.")
