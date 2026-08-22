#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/SemanticScheduleHealthService.cs"
COMPREHENSIVE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticScheduleHealthSmoke.cs"
DOC = ROOT / "docs/SEMANTIC-SCHEDULES.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing semantic schedule health file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


health = read(HEALTH)
comprehensive = read(COMPREHENSIVE)
smoke = read(SMOKE)
doc = read(DOC)

for token in (
    "public sealed class SemanticScheduleHealthService",
    "SemanticScheduleCatalog.MetadataKey",
    "SemanticScheduleCatalog.Load(project)",
    "SemanticTagRenderer.ValidateTemplate",
    "BuildIdentityCounts",
    "MaxIssues = 768",
    "SEMANTIC_SCHEDULE_CATALOG_INVALID",
    "SEMANTIC_SCHEDULE_MISSING_FLOOR",
    "SEMANTIC_SCHEDULE_AMBIGUOUS_FLOOR",
    "SEMANTIC_SCHEDULE_MISSING_ZONE",
    "SEMANTIC_SCHEDULE_AMBIGUOUS_ZONE",
    "SEMANTIC_SCHEDULE_MISSING_ELEMENT",
    "SEMANTIC_SCHEDULE_AMBIGUOUS_ELEMENT",
    "SEMANTIC_SCHEDULE_TEMPLATE_INVALID",
    "SEMANTIC_SCHEDULE_HEALTH_TRUNCATED",
):
    if token not in health:
        errors.append("semantic schedule health missing contract token: " + token)

for forbidden in (
    "project.Touch(",
    "SemanticScheduleCatalog.Save",
    "SemanticScheduleCatalog.Upsert",
    "SemanticScheduleCatalog.Remove",
    "project.Metadata[SemanticScheduleCatalog.MetadataKey] =",
):
    if forbidden in health:
        errors.append("semantic schedule health must remain read-only: " + forbidden)

for token in (
    'new DiagnosticProvider("SemanticScheduleHealthService", () => new SemanticScheduleHealthService().Inspect(project))',
    '"HEALTH_PROVIDER_FAILED"',
    "ExecuteProvider",
):
    if token not in comprehensive:
        errors.append("ComprehensiveModelHealthService does not include fail-isolated SemanticScheduleHealthService token: " + token)

for token in (
    "ValidAndZeroMatchSchedulesAreHealthy",
    "StaleAndAmbiguousReferencesAreReported",
    "InvalidTemplateAndCatalogAreReportedReadOnly",
    "ComprehensiveHealthIncludesSemanticScheduleProvider",
    "SEMANTIC_SCHEDULE_TEMPLATE_INVALID",
    "SEMANTIC_SCHEDULE_CATALOG_INVALID",
    "ModuleInitializer",
):
    if token not in smoke:
        errors.append("semantic schedule health smoke missing regression token: " + token)

for token in (
    "Model Health",
    "SEMANTIC_SCHEDULE_",
    "zero-match",
    "read-only",
    "Native BricsCAD Table",
):
    if token not in doc:
        errors.append("semantic schedule docs missing health/boundary token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: SemanticSchedule health is bounded/read-only, reports stale/ambiguous/template/catalog problems, treats zero-match as valid, and is wired into comprehensive Model Health.")
