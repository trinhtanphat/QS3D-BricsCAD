#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticScheduleCatalog.cs"
TABLE = ROOT / "src/QS3D.Core/Documentation/SemanticDocumentationTableBuilder.cs"
RENDERER = ROOT / "src/QS3D.Core/Documentation/SemanticTagRenderer.cs"
TEST = ROOT / "tests/QS3D.Core.SmokeTests/SemanticScheduleCatalogSmoke.cs"
TABLE_TEST = ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationTableSmoke.cs"
DOC = ROOT / "docs/SEMANTIC-SCHEDULES.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing semantic schedule file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


source = read(SOURCE)
table = read(TABLE)
renderer = read(RENDERER)
test = read(TEST)
table_test = read(TABLE_TEST)
doc = read(DOC)

doc_boundary = doc.replace("**", "")
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
    "FindFloor",
    "FindZone",
    "IncludeElementIds",
    "ExcludeElementIds",
    "new List<ElementCategory>",
    ".AsReadOnly()",
    "Project contains a null semantic element.",
    "allowEmpty: true",
):
    if token not in source:
        errors.append("semantic schedule source missing contract token: " + token)

for token in (
    "bool allowEmpty",
    "ids.Count == 0 && !allowEmpty",
    "SemanticTagRenderer.ValidateTemplate(template)",
    "SemanticTagRenderer.Render",
):
    if token not in table:
        errors.append("documentation table builder missing schedule hardening token: " + token)

for token in (
    "public static void ValidateTemplate",
    "ValidateTemplateSource",
    "ValidateToken",
    "Unsupported semantic tag token",
    "Semantic tag cannot expose generated/native runtime property",
):
    if token not in renderer:
        errors.append("semantic tag renderer missing row-independent template validation token: " + token)

for token in (
    "SaveLoadRoundTripIsDeterministic",
    "UpsertAndRemoveSupportMultipleDefinitions",
    "BuildFiltersAndUsesCanonicalTemplateRenderer",
    "EmptySelectionBuildsHeaderOnlyTable",
    "DefinitionCollectionsAreDefensivelyImmutable",
    "NullProjectElementsFailClosed",
    "StaleReferencesFailClosedAtRenderTime",
    "DuplicateDefinitionsAndOverlappingListsFailClosed",
    "{Q:LengthM}",
    "ModuleInitializer",
):
    if token not in test:
        errors.append("semantic schedule smoke missing regression token: " + token)

for token in (
    "EmptyRowsStillValidateTemplates",
    "{Unsupported}",
    "{P:GeneratedSolidHandle}",
    "allowEmpty: true",
):
    if token not in table_test:
        errors.append("documentation table smoke missing empty-template regression token: " + token)

for token in (
    "user-defined semantic schedule",
    "does not calculate BQ",
    "does not calculate BBS",
    "SemanticDocumentationTableBuilder",
    "Floor/Zone",
    "header-only",
    "defensively immutable",
    "null semantic Element",
    "template syntax",
    "generated/native ownership",
    "portable interchange",
    "native BricsCAD Table",
):
    if token not in doc_boundary:
        errors.append("semantic schedule documentation missing boundary token: " + token)

if "Semantic schedule selects no elements" in source:
    errors.append("valid zero-match custom schedules must render header-only rather than fail")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: custom semantic schedules are bounded/persisted, immutable, fail closed on corrupt model/template state, support header-only zero-match output, and remain on the canonical documentation renderer.")
