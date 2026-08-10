#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
DIFF = ROOT / "src/QS3D.Core/Export/ProjectInterchangeSnapshotDiff.cs"
READER = ROOT / "src/QS3D.Core/Export/ProjectInterchangeValidatedSnapshotReader.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/ProjectInterchangeSnapshotDiffSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/INTERCHANGE-SEMANTIC-DIFF.md"
errors = []

for path in (DIFF, READER, SMOKE, REG, DOC):
    if not path.is_file(): errors.append("missing interchange semantic-diff contract file: " + str(path.relative_to(ROOT)))

if DIFF.is_file():
    text = DIFF.read_text(encoding="utf-8")
    for token in (
        "private const int MaxChanges = 120000",
        "ProjectInterchangeValidatedSnapshotReader.Read(leftJson)",
        "ProjectInterchangeValidatedSnapshotReader.Read(rightJson)",
        "InterchangeSnapshotChangeKind.Added",
        "InterchangeSnapshotChangeKind.Removed",
        "InterchangeSnapshotChangeKind.Changed",
        "InterchangeSnapshotObjectKind.Manifest",
        "InterchangeSnapshotObjectKind.Element",
        "SetEquals(left.SourceHandles, right.SourceHandles, StringComparer.OrdinalIgnoreCase)",
        "SetEquals(left.Dependencies, right.Dependencies, StringComparer.OrdinalIgnoreCase)",
        "StringMapEquals(left.Properties, right.Properties)",
        "NumberMapEquals(left.Quantities, right.Quantities)",
        "changes.Sort(ChangeComparer.Instance)",
        ".ToList().AsReadOnly()",
    ):
        if token not in text: errors.append("ProjectInterchangeSnapshotDiff.cs missing deterministic/read-only token: " + token)
    for token in (
        "new ProjectState(",
        ".Elements.Add(",
        ".Families.Add(",
        ".Floors.Add(",
        ".Zones.Add(",
        "GeneratedSolidHandle",
        "ProjectStateSnapshot.Restore(",
    ):
        if token in text: errors.append("semantic diff must remain detached/read-only; found forbidden mutation/ownership token: " + token)

if READER.is_file() and "var validation = ProjectInterchangeJsonValidator.Validate(json);" not in READER.read_text(encoding="utf-8"):
    errors.append("validated reader lost validation-first prerequisite")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "IdenticalSnapshotsHaveNoChanges",
        "AddedRemovedAndChangedObjectsAreClassified",
        "ElementPortableFieldsAreCompared",
        "ProvenanceCollectionsAreOrderInsensitive",
        "CompareJsonIsValidationFirst",
        "ResultCollectionsAreImmutable",
        "Throws<NotSupportedException>",
    ):
        if token not in text: errors.append("ProjectInterchangeSnapshotDiffSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "ProjectInterchangeSnapshotDiffSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("interchange semantic-diff smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "read-only semantic diff",
        "portable provenance difference only",
        "not a merge instruction",
        "not import permission",
        "REMOTE_DONE for deterministic read-only Semantic Snapshot v1 diff only",
    ):
        if token not in text: errors.append("INTERCHANGE-SEMANTIC-DIFF.md missing diff/import boundary: " + token)

print("QS3D interchange semantic-diff preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: validated Semantic Snapshot v1 data can be compared deterministically and read-only without turning provenance differences into import/native ownership authority.")
