#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticDocumentationCatalogStore.cs"
METADATA = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectMetadataDictionary.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticDocumentationCatalogSaveBoundedEnumerationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticDocumentationCatalogSaveBoundedEnumerationSmokeRegistration.cs"


def method_slice(text, start_token, end_token):
    start = text.find(start_token)
    if start < 0:
        return ""
    end = text.find(end_token, start)
    return text[start:] if end < 0 else text[start:end]


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


def main():
    source = SOURCE.read_text(encoding="utf-8")
    metadata = METADATA.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    required_source = [
        "private const int MaxCatalogViews = 10000;",
        "private const int MaxCatalogSheets = 10000;",
        "var viewDefinitions = MaterializeViews(views);",
        "var sheetDefinitions = MaterializeSheets(sheets);",
        'throw new InvalidOperationException("Semantic view catalog supports at most " + MaxCatalogViews + " views.");',
        'throw new InvalidOperationException("Semantic sheet catalog supports at most " + MaxCatalogSheets + " sheets.");',
        "project.Metadata.Remove(MetadataKey);",
        "project.Metadata[MetadataKey] = payload;",
    ]
    for token in required_source:
        if token not in source:
            print("ERROR: missing documentation catalog save bound contract: " + token)
            return 1

    if not metadata_revision_owned(metadata):
        print("ERROR: ProjectMetadataDictionary must own exact-once project revision updates for public Remove/indexer persistence mutations.")
        return 1

    views = method_slice(source, "private static IReadOnlyList<SemanticViewDefinition> MaterializeViews", "private static IReadOnlyList<SemanticSheetDefinition> MaterializeSheets")
    sheets = method_slice(source, "private static IReadOnlyList<SemanticSheetDefinition> MaterializeSheets", "private static string Serialize")
    for label, text, count_token, error_token in [
        ("views", views, "if (result.Count >= MaxCatalogViews)", "Semantic view catalog supports at most"),
        ("sheets", sheets, "if (result.Count >= MaxCatalogSheets)", "Semantic sheet catalog supports at most"),
    ]:
        loop = text.find("foreach (var value in values)")
        cap = text.find(count_token, loop)
        null_guard = text.find("if (value == null)", loop)
        add = text.find("result.Add(value);", loop)
        if min(loop, cap, null_guard, add) < 0 or not (loop < cap < null_guard < add):
            print("ERROR: documentation catalog " + label + " guard must run during enumeration before null validation/add.")
            return 1
        if error_token not in text:
            print("ERROR: documentation catalog " + label + " capacity message is missing.")
            return 1

    save = method_slice(source, "public void Save(", "public SemanticDocumentationCatalog Load")
    view_materialize = save.find("MaterializeViews(views)")
    sheet_materialize = save.find("MaterializeSheets(sheets)")
    remove = save.find("project.Metadata.Remove(MetadataKey);")
    assign = save.find("project.Metadata[MetadataKey] = payload;")
    mutations = [position for position in (remove, assign) if position >= 0]
    first_mutation = min(mutations) if mutations else -1
    if min(view_materialize, sheet_materialize, first_mutation) < 0 or not (view_materialize < sheet_materialize < first_mutation):
        print("ERROR: documentation catalog bounded materialization must complete before persistence mutation.")
        return 1

    smoke_tokens = [
        "OversizeLazyViewsStopAtFirstItemBeyondCapacity();",
        "OversizeLazySheetsStopAtFirstItemBeyondCapacity();",
        'Equal("Semantic view catalog supports at most 10000 views.", ex.Message);',
        'Equal("Semantic sheet catalog supports at most 10000 sheets.", ex.Message);',
        "Equal(10001, source.YieldCount);",
        "if (YieldCount > 10001)",
        "Equal(beforeVersion, project.ChangeVersion);",
        "project.Metadata.ContainsKey(SemanticDocumentationCatalogStore.MetadataKey)",
    ]
    for token in smoke_tokens:
        if token not in smoke:
            print("ERROR: missing documentation catalog save bound smoke token: " + token)
            return 1

    if "[ModuleInitializer]" not in registration or "SemanticDocumentationCatalogSaveBoundedEnumerationSmoke.Run();" not in registration:
        print("ERROR: documentation catalog save bound smoke is not module-registered.")
        return 1

    print("PASS: SemanticDocumentationCatalogStore.Save bounds lazy view/sheet enumeration before metadata persistence, whose dictionary owner performs exact-once project revision mutation.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
