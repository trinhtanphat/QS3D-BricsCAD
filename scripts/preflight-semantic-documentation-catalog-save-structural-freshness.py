#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Documentation/SemanticDocumentationCatalogStore.cs"
METADATA = ROOT / "src/QS3D.Core/Domain/ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogSaveStructuralFreshnessSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SemanticDocumentationCatalogSaveStructuralFreshnessSmokeRegistration.cs"
errors = []


def metadata_revision_owned(metadata):
    setter = metadata.find('public string this[string key] { get => _items[key]; set => SetPublic(key, value, false); }')
    set_public = metadata.find('Set(canonicalKey, xmlValue, addOnly, true);')
    remove_public = metadata.find('public bool Remove(string key) => Remove(key, true);')
    remove_private = metadata.find('private bool Remove(string key, bool touchMutation)')
    remove_touch = metadata.find('if (touchMutation) TouchProject();', remove_private)
    remove_storage = metadata.find('return _items.Remove(key);', remove_private)
    set_private = metadata.find('private void Set(string key, string value, bool addOnly, bool touchMutation)')
    set_touch = metadata.find('if (touchMutation) TouchProject();', set_private)
    set_storage = metadata.find('if (addOnly) _items.Add(key, normalizedValue); else _items[key] = normalizedValue;', set_private)
    touch_owner = metadata.find('private void TouchProject()')
    project_touch = metadata.find('project.Touch();', touch_owner)
    return (
        min(setter, set_public, remove_public, remove_private, remove_touch, remove_storage, set_private, set_touch, set_storage, touch_owner, project_touch) >= 0
        and remove_private < remove_touch < remove_storage
        and set_private < set_touch < set_storage
        and touch_owner < project_touch
    )


for path in (SOURCE, METADATA, SMOKE, REGISTRATION):
    if not path.is_file():
        errors.append("missing documentation catalog save freshness file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file() and METADATA.is_file():
    source = SOURCE.read_text(encoding="utf-8")
    metadata = METADATA.read_text(encoding="utf-8")
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
    empty_remove = method.find("project.Metadata.Remove(MetadataKey);", after_planners)
    empty_pre_mutation = method.rfind("EnsureProjectStructureUnchanged(project, projectSnapshot);", after_planners, empty_remove)
    payload = method.find("var payload = Serialize(viewDefinitions, sheetDefinitions);", empty_remove)
    normal_assign = method.find("project.Metadata[MetadataKey] = payload;", payload)
    normal_pre_mutation = method.rfind("EnsureProjectStructureUnchanged(project, projectSnapshot);", payload, normal_assign)

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
        empty_remove,
        payload,
        normal_pre_mutation,
        normal_assign,
    )
    if min(positions) < 0 or not (
        capture < views < after_views < sheets < after_sheets < view_plans < sheet_plans < after_planners
        < empty_pre_mutation < empty_remove < payload < normal_pre_mutation < normal_assign
    ):
        errors.append("Save must snapshot before caller enumerations, recheck after enumeration/planning, and recheck immediately before either metadata persistence mutation.")

    if method.count("EnsureProjectStructureUnchanged(project, projectSnapshot);") != 5:
        errors.append("Save must perform exactly five project freshness checks across enumeration, planning and mutation boundaries.")

    if not metadata_revision_owned(metadata):
        errors.append("ProjectMetadataDictionary must own exact-once project revision updates for public Remove/indexer persistence mutations.")

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

print("PASS: SemanticDocumentationCatalogStore.Save rejects structural drift immediately before metadata persistence while ProjectMetadataDictionary owns exact-once revision mutation.")
