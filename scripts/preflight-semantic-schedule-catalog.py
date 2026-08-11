#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]

FILES = {
    "planner": ROOT / "src/QS3D.Core/Documentation/SemanticSchedulePlanner.cs",
    "table": ROOT / "src/QS3D.Core/Documentation/SemanticDocumentationTableBuilder.cs",
    "store": ROOT / "src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs",
    "editor": ROOT / "src/QS3D.Core/Documentation/SemanticDocumentationCatalogEditor.cs",
    "service": ROOT / "src/QS3D.Core/Documentation/SemanticScheduleService.cs",
    "renderer": ROOT / "src/QS3D.Core/Documentation/SemanticTagRenderer.cs",
    "store_smoke": ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogStoreSmoke.cs",
    "editor_smoke": ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogEditorSmoke.cs",
    "table_smoke": ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationTableSmoke.cs",
    "docs": ROOT / "docs/SEMANTIC-SCHEDULE-CATALOG.md",
}

errors = []
texts = {}
for name, path in FILES.items():
    if not path.exists():
        errors.append(f"missing required file: {path.relative_to(ROOT)}")
        texts[name] = ""
    else:
        texts[name] = path.read_text(encoding="utf-8")


def require(name, *tokens):
    text = texts[name]
    for token in tokens:
        if token not in text:
            errors.append(f"{name} missing contract token: {token}")


require(
    "planner",
    "public sealed class SemanticScheduleDefinition",
    "public sealed class SemanticSchedulePlan",
    "SemanticDocumentationColumnPolicy.Normalize",
    "SemanticViewKind.Schedule",
    "Semantic schedule references missing semantic view id",
    "Semantic schedule catalog contains duplicate schedule id",
    "SemanticDocumentationTableBuilder.Build(project, plan.Name, plan.ElementIds, plan.Columns, allowEmpty: true)",
)
require(
    "table",
    "bool allowEmpty",
    "SemanticDocumentationColumnPolicy.Normalize(columns, nameof(columns))",
    "SemanticTagRenderer.Render(context, element, column.Template, allowEmpty: true)",
)
require(
    "store",
    "private const int LegacyFormatVersion = 1;",
    "private const int FormatVersion = 2;",
    "public IReadOnlyList<SemanticScheduleDefinition> Schedules",
    "SemanticSchedulePlanner.BuildCatalog(project, scheduleDefinitions, viewPlans)",
    "new XElement(\"schedules\"",
    "ReadSchedules(root.Element(\"schedules\"))",
    "version != LegacyFormatVersion && version != FormatVersion",
)
require(
    "editor",
    "UpsertSchedule",
    "RemoveSchedule",
    "rewriteScheduleReferences",
    "RewrittenScheduleReferenceCount",
    "CountScheduleViewReferences",
    "RemoveSchedulesForView",
    "_store.Save(project, views, sheets, schedules)",
)
require(
    "service",
    "catalog.Schedules",
    "SemanticSchedulePlanner.BuildTable(project, matches[0], catalog.Views)",
)
require(
    "renderer",
    "GeneratedHandleOwnershipPolicy.IsOwnerSlot",
    "Semantic tag cannot expose generated/native runtime property",
)
require(
    "store_smoke",
    "LegacyV1LoadsAndMigratesOnSave",
    "catalog.Schedules.Count",
    "version=\\\"2\\\"",
)
require(
    "editor_smoke",
    "ScheduleCrudAndViewReferenceGuards",
    "RewrittenScheduleReferenceCount",
    "UpsertSchedule",
)
require(
    "table_smoke",
    "PersistedScheduleBuildsDeterministically",
    "ScheduleRequiresScheduleView",
    "new SemanticScheduleService().BuildTable",
)
require(
    "docs",
    "schema v2",
    "SemanticViewKind.Schedule",
    "không phải",
    "BricsCAD V25",
)

for name in ("planner", "table", "store", "editor", "service"):
    lowered = texts[name].lower()
    for forbidden in ("bricscad.", "teigha.", "brxmgd", "objectid", "database.services"):
        if forbidden in lowered:
            errors.append(f"{name} leaks CAD/runtime dependency into Core: {forbidden}")

if "new Quantity" in texts["planner"] or "RebarNotationParser" in texts["planner"]:
    errors.append("semantic schedule planner must not become a second quantity/BBS calculation engine")

print("QS3D semantic schedule catalog preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic schedules remain persisted, bounded, view-backed, renderer-backed, migration-safe Core definitions without native CAD ownership.")
