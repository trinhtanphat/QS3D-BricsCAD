#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogSaveStructuralFreshnessSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogSaveStructuralFreshnessSmokeRegistration.cs"
errors = []

for path in (SOURCE, SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing documentation catalog save freshness file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    start = source.find("public void Save(")
    end = source.find("public SemanticDocumentationCatalog Load", start)
    method = source[start:end] if start >= 0 and end > start else ""

    capture = method.find("var projectSnapshot = CaptureProjectStructure(project);")
    views = method.find("var viewDefinitions = MaterializeViews(views);", capture)
    after_views = method.find("EnsureProjectStructureUnchanged(project, projectSnapshot);", views)
    sheets = method.find("var sheetDefinitions = MaterializeSheets(sheets);", after_views)
    after_sheets = method.find("EnsureProjectStructureUnchanged(project, projectSnapshot);", sheets)
    view_plans = method.find("var viewPlans = SemanticViewPlanner.BuildCatalog(project, viewDefinitions);", after_sheets)
    sheet_plans = method.find("SemanticSheetPlanner.BuildCatalog(sheetDefinitions, viewPlans);", view_plans)
    after_planners = method.find("EnsureProjectStructureUnchanged(project, projectSnapshot);", sheet_plans)
    empty_mutation = method.find("project.Metadata.Remove(MetadataKey);", after_planners)
    empty_pre_mutation = method.rfind("EnsureProjectStructureUnchanged(project, projectSnapshot);", after_planners, empty_mutation)
    normal_mutation = method.find("project.Metadata[MetadataKey] = payload;", empty_mutation + 1)
    normal_pre_mutation = method.rfind("EnsureProjectStructureUnchanged(project, projectSnapshot);", empty_mutation + 1, normal_mutation)

    positions = (
        capture,
        views,
        after_views,
        sheets,
        after_sheets,
        view_plans,
        sheet_plans,
        after_planners,
        empty_pre_mutation,
        empty_mutation,
        normal_pre_mutation,
        normal_mutation,
    )
    if min(positions) < 0 or not (
        capture < views < after_views < sheets < after_sheets < view_plans < sheet_plans < after_planners
        < empty_pre_mutation < empty_mutation < normal_pre_mutation < normal_mutation
    ):
        errors.append("Save must snapshot before both caller enumerations, recheck after each/planning, and recheck immediately before either metadata persistence mutation.")

    if method.count("EnsureProjectStructureUnchanged(project, projectSnapshot);") != 5:
        errors.append("Save must perform exactly five project freshness checks across enumeration, planning and mutation boundaries.")

    if "project.Touch();" in method:
        errors.append("Save must not directly Touch before public metadata Set/Remove; ProjectMetadataDictionary owns the exact-once revision boundary.")

    for token in (
        "project.ChangeVersion,",
        "project.Elements.ToArray(),",
        "project.Floors.ToArray(),",
        "project.Zones.ToArray());",
        "if (project.ChangeVersion != snapshot.ChangeVersion)",
        "EnsureSameReferences(project.Elements, snapshot.Elements);",
        "EnsureSameElementPlanningValues(project.Elements, snapshot.ElementPlanningValues);",
        "EnsureSameReferences(project.Floors, snapshot.Floors);",
        "EnsureSameReferences(project.Zones, snapshot.Zones);",
        "if (!ReferenceEquals(current[i], expected[i]))",
        "ElementPlanningValues = elements.Select(x => new ProjectElementPlanningValues(x)).ToArray();",
        "!string.Equals(element.Id, values.Id, StringComparison.Ordinal)",
        "element.Category != values.Category",
        "!string.Equals(element.FloorId, values.FloorId, StringComparison.Ordinal)",
        "!string.Equals(element.ZoneId, values.ZoneId, StringComparison.Ordinal)",
    ):
        if token not in source:
            errors.append("SemanticDocumentationCatalogStore missing structural freshness token: " + token)

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "ViewEnumerationElementReplacementFailsClosed();",
        "SheetEnumerationElementReplacementFailsClosed();",
        "ViewEnumerationSameInstancePlannerValueDriftFailsClosed();",
        "SheetEnumerationSameInstancePlannerValueDriftFailsClosed();",
        "ViewEnumerationRevisionDriftFailsClosed();",
        "SheetEnumerationRevisionDriftFailsClosed();",
        "StableSaveRemainsDeterministic();",
        "project.Elements[0] = ReplacementElement();",
        "project.Elements[0].Category = ElementCategory.Column;",
        'project.Elements[0].FloorId = "F-02";',
        'project.Elements[0].ZoneId = "Z-02";',
        "project.Touch();",
        "MetadataAbsent(project);",
        "store.Save(project, views, sheets);",
        "Equal(firstPayload, project.Metadata[SemanticDocumentationCatalogStore.MetadataKey]);",
    ):
        if token not in smoke:
            errors.append("Documentation catalog save freshness smoke missing token: " + token)

if REGISTRATION.is_file():
    registration = REGISTRATION.read_text(encoding="utf-8")
    if "[ModuleInitializer]" not in registration or "SemanticDocumentationCatalogSaveStructuralFreshnessSmoke.Run();" not in registration:
        errors.append("Documentation catalog save structural freshness smoke is not module-registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: SemanticDocumentationCatalogStore.Save rejects project revision, ordered reference, or planner-value drift across caller-controlled view/sheet enumeration immediately before public metadata persistence.")
