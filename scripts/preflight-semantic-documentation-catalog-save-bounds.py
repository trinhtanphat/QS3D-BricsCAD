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
    remove_generation_admission = metadata.find('var nextMutationVersion = checked(_mutationVersion + 1L);', remove_private)
    remove_touch = metadata.find('if (touchMutation) TouchProject();', remove_private)
    remove_storage = metadata.find('var removed = _items.Remove(key);', remove_private)
    remove_generation_commit = metadata.find('if (removed) _mutationVersion = nextMutationVersion;', remove_storage)
    set_private = metadata.find('private void Set(string key, string value, bool addOnly, bool touchMutation)')
    set_generation_admission = metadata.find('var nextMutationVersion = checked(_mutationVersion + 1L);', set_private)
    set_touch = metadata.find('if (touchMutation) TouchProject();', set_private)
    set_storage = metadata.find('if (addOnly) _items.Add(key, normalizedValue); else _items[key] = normalizedValue;', set_private)
    set_generation_commit = metadata.find('_mutationVersion = nextMutationVersion;', set_storage)
    touch_owner = metadata.find('private void TouchProject()')
    project_touch = metadata.find('project.Touch();', touch_owner)
    return (
        min(
            setter,
            set_public,
            remove_public,
            remove_private,
            remove_generation_admission,
            remove_touch,
            remove_storage,
            remove_generation_commit,
            set_private,
            set_generation_admission,
            set_touch,
            set_storage,
            set_generation_commit,
            touch_owner,
            project_touch,
        ) >= 0
        and remove_private < remove_generation_admission < remove_touch < remove_storage < remove_generation_commit
        and set_private < set_generation_admission < set_touch < set_storage < set_generation_commit
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
        '"Semantic view catalog supports at most " + MaxCatalogViews + " views."',
        '"Semantic sheet catalog supports at most " + MaxCatalogSheets + " sheets."',
        "private static IReadOnlyList<T> MaterializeBounded<T>(",
        "if (result.Count >= maxCount)",
        "throw new InvalidOperationException(capacityError);",
        "if (value is null) throw new ArgumentException(nullEntryError, nameof(values));",
        "result.Add(value);",
        "project.Metadata.Remove(MetadataKey);",
        "project.Metadata[MetadataKey] = payload;",
    ]
    for token in required_source:
        if token not in source:
            print("ERROR: missing documentation catalog save bound contract: " + token)
            return 1

    if not metadata_revision_owned(metadata):
        print("ERROR: ProjectMetadataDictionary must own exact-once project revision updates for public Remove/indexer persistence mutations and commit metadata mutation generation only after storage mutation succeeds.")
        return 1

    views = method_slice(
        source,
        "private static IReadOnlyList<SemanticViewDefinition> MaterializeViews",
        "private static IReadOnlyList<SemanticSheetDefinition> MaterializeSheets")
    sheets = method_slice(
        source,
        "private static IReadOnlyList<SemanticSheetDefinition> MaterializeSheets",
        "private static IReadOnlyList<T> MaterializeBounded<T>")
    bounded = method_slice(
        source,
        "private static IReadOnlyList<T> MaterializeBounded<T>",
        "private static void ValidateKnownCount<T>(ICollection<T>? values")

    for label, text, max_token, error_token, null_token in [
        (
            "views",
            views,
            "MaxCatalogViews",
            '"Semantic view catalog supports at most " + MaxCatalogViews + " views."',
            '"Semantic documentation view cannot be null."',
        ),
        (
            "sheets",
            sheets,
            "MaxCatalogSheets",
            '"Semantic sheet catalog supports at most " + MaxCatalogSheets + " sheets."',
            '"Semantic documentation sheet cannot be null."',
        ),
    ]:
        if "return MaterializeBounded(" not in text or max_token not in text or error_token not in text or null_token not in text:
            print("ERROR: documentation catalog " + label + " must delegate to the shared bounded materializer with its capacity/null contract.")
            return 1

    loop = bounded.find("while (enumerator.MoveNext())")
    cap = bounded.find("if (result.Count >= maxCount)", loop)
    capacity_throw = bounded.find("throw new InvalidOperationException(capacityError);", cap)
    current = bounded.find("var value = enumerator.Current;", capacity_throw)
    null_guard = bounded.find("if (value is null) throw new ArgumentException(nullEntryError, nameof(values));", current)
    add = bounded.find("result.Add(value);", null_guard)
    if min(loop, cap, capacity_throw, current, null_guard, add) < 0 or not (loop < cap < capacity_throw < current < null_guard < add):
        print("ERROR: documentation catalog shared bounded guard must run during enumeration before null validation/add.")
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

    print("PASS: SemanticDocumentationCatalogStore.Save bounds lazy view/sheet enumeration through the shared materializer before metadata persistence, whose dictionary owner performs exact-once project revision mutation and post-storage mutation-generation commit.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
