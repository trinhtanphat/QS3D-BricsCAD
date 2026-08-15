#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticDocumentationCatalogStore.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticDocumentationCatalogSaveBoundedEnumerationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticDocumentationCatalogSaveBoundedEnumerationSmokeRegistration.cs"


def method_slice(text, start_token, end_token):
    start = text.find(start_token)
    if start < 0:
        return ""
    end = text.find(end_token, start)
    return text[start:] if end < 0 else text[start:end]


def main():
    source = SOURCE.read_text(encoding="utf-8")
    smoke = SMOKE.read_text(encoding="utf-8")
    registration = REGISTRATION.read_text(encoding="utf-8")

    required_source = [
        "private const int MaxCatalogViews = 10000;",
        "private const int MaxCatalogSheets = 10000;",
        "var viewDefinitions = MaterializeViews(views);",
        "var sheetDefinitions = MaterializeSheets(sheets);",
        'throw new InvalidOperationException("Semantic view catalog supports at most " + MaxCatalogViews + " views.");',
        'throw new InvalidOperationException("Semantic sheet catalog supports at most " + MaxCatalogSheets + " sheets.");',
    ]
    for token in required_source:
        if token not in source:
            print("ERROR: missing documentation catalog save bound contract: " + token)
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
    empty_mutation = save.find("project.Metadata.Remove(MetadataKey);", sheet_materialize)
    normal_mutation = save.find("project.Metadata[MetadataKey] = payload;", empty_mutation + 1)
    if min(view_materialize, sheet_materialize, empty_mutation, normal_mutation) < 0 or not (
        view_materialize < sheet_materialize < empty_mutation < normal_mutation
    ):
        print("ERROR: documentation catalog bounded materialization must complete before either metadata persistence mutation.")
        return 1
    if "project.Touch();" in save:
        print("ERROR: documentation catalog Save must not directly Touch before public metadata Set/Remove; the metadata dictionary owns revision advancement.")
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

    print("PASS: SemanticDocumentationCatalogStore.Save bounds lazy view/sheet enumeration before planner or public metadata persistence mutation without redundant direct project touches.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
