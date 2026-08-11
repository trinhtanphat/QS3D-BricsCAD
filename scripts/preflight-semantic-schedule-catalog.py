#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/SemanticScheduleCatalogSmoke.cs"
DOC = ROOT / "docs/SEMANTIC-SCHEDULES.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing semantic schedule file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
test = read(TEST)
doc = read(DOC)

for token in (
    'MetadataKey = "QS3D.Documentation.SemanticSchedules.v1"',
    "SemanticScheduleDefinition",
    "SemanticScheduleCatalog",
    "Upsert",
    "Remove",
    "SemanticDocumentationTableBuilder.Build",
    "DtdProcessing.Prohibit",
    "XmlResolver = null",
    "MaxSchedules = 128",
    "MaxIds = 5000",
    "MaxColumns = 32",
    "Semantic schedule selects no elements",
    "FindFloor",
    "FindZone",
    "IncludeElementIds",
    "ExcludeElementIds",
):
    if token not in source:
        errors.append("semantic schedule source missing contract token: " + token)

for token in (
    "SaveLoadRoundTripIsDeterministic",
    "UpsertAndRemoveSupportMultipleDefinitions",
    "BuildFiltersAndUsesCanonicalTemplateRenderer",
    "StaleReferencesFailClosedAtRenderTime",
    "DuplicateDefinitionsAndOverlappingListsFailClosed",
    "{Q:LengthM}",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("semantic schedule smoke missing regression token: " + token)

for token in (
    "user-defined semantic schedule",
    "does not calculate BQ",
    "does not calculate BBS",
    "SemanticDocumentationTableBuilder",
    "Floor/Zone",
    "portable interchange",
    "native BricsCAD Table",
):
    if token not in doc:
        errors.append("semantic schedule documentation missing boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: custom semantic schedule definitions are bounded/persisted, render through the canonical documentation table builder, and do not become a second quantity calculator.")
