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
    "ParseCategory",
    "Enum.TryParse(raw, false",
    "category.ToString()",
    "EnsureRequiredAttributes",
    "RequireExactlyOneChild",
    "MaterializeScheduleNodesBounded",
    "var scheduleNodes = MaterializeScheduleNodesBounded(root);",
    "ValidateSchema(root, scheduleNodes);",
    "var definitions = scheduleNodes.Select(ReadDefinition).ToList();",
):
    if token not in source:
        errors.append("semantic schedule source missing contract token: " + token)

if "Enum.Parse(typeof(ElementCategory)" in source:
    errors.append("persisted semantic schedule categories must not use permissive Enum.Parse")

load_start = source.find("public static IReadOnlyList<SemanticScheduleDefinition> Load(ProjectState project)")
save_start = source.find("public static void Save(ProjectState project", load_start)
load = source[load_start:save_start] if load_start >= 0 and save_start > load_start else ""
materialize = load.find("var scheduleNodes = MaterializeScheduleNodesBounded(root);")
schema = load.find("ValidateSchema(root, scheduleNodes);")
definitions = load.find("var definitions = scheduleNodes.Select(ReadDefinition).ToList();")
if min(materialize, schema, definitions) < 0 or not materialize < schema < definitions:
    errors.append("semantic schedule load must apply capacity before detailed schema and definition parsing")
if 'root.Elements("schedule").Select(ReadDefinition).ToList()' in source:
    errors.append("legacy unbounded semantic schedule load materialization remains")

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
    "PersistedCategoriesRequireCanonicalNames",
    "PersistedSchemaRequiresCanonicalShape",
    "UpsertAndRemoveSupportMultipleDefinitions",
    "BuildFiltersAndUsesCanonicalTemplateRenderer",
    "EmptySelectionBuildsHeaderOnlyTable",
    "DefinitionCollectionsAreDefensivelyImmutable",
    "NullProjectElementsFailClosed",
    "StaleReferencesFailClosedAtRenderTime",
    "DuplicateDefinitionsAndOverlappingListsFailClosed",
    "LoadAcceptsCapacityAndRejectsMalformedExcessByCapacity",
    "MalformedScheduleWithinCapacityKeepsSchemaFailure",
    "Equal(128, SemanticScheduleCatalog.Load(project).Count);",
    "unsupported-excess-detail",
    "The malformed 129th schedule reached detailed schema validation before the catalog capacity guard.",
    "unsupported-within-capacity",
    "Semantic schedule catalog exceeds the supported 128 definitions.",
    "InvalidDataException",
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
    "canonical ElementCategory names",
    "exactly one canonical",
    "portable interchange",
    "Native BricsCAD Table",
):
    if token not in doc:
        errors.append("semantic schedule documentation missing boundary token: " + token)

if "Semantic schedule selects no elements" in source:
    errors.append("valid zero-match custom schedules must render header-only rather than fail")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: custom semantic schedules are bounded/persisted, immutable, strict-canonical on v1 XML shape/category names, fail closed on corrupt model/template state, support header-only zero-match output, and remain on the canonical documentation renderer.")
