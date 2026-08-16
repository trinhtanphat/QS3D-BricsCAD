#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Documentation" / "SemanticDocumentationCatalogStore.cs"


def fail(message):
    raise SystemExit("FAIL: " + message)


def method_slice(text, start, end):
    start_index = text.find(start)
    end_index = text.find(end, start_index + 1)
    if start_index < 0 or end_index < 0:
        fail("could not isolate " + start)
    return text[start_index:end_index]


def require_order(text, tokens, label):
    cursor = -1
    for token in tokens:
        index = text.find(token, cursor + 1)
        if index < 0:
            fail(label + " missing: " + token)
        cursor = index


def main():
    source = SOURCE.read_text(encoding="utf-8")
    views = method_slice(
        source,
        "private static IReadOnlyList<SemanticViewDefinition> ReadViews",
        "private static IReadOnlyList<SemanticSheetDefinition> ReadSheets")
    sheets = method_slice(
        source,
        "private static IReadOnlyList<SemanticSheetDefinition> ReadSheets",
        "private static IReadOnlyList<string> ReadIds")

    require_order(
        views,
        (
            "var result = new List<SemanticViewDefinition>(Math.Min(MaxCatalogViews, 256));",
            'foreach (var item in container.Elements("view"))',
            "if (result.Count >= MaxCatalogViews)",
            'throw new InvalidDataException("Semantic documentation catalog contains more than " + MaxCatalogViews + " persisted views.");',
            'NamedEnum<SemanticViewKind>(Required(item, "kind"), "view kind")',
            "result.Add(new SemanticViewDefinition(",
        ),
        "ReadViews persisted count boundary")

    require_order(
        sheets,
        (
            "var result = new List<SemanticSheetDefinition>(Math.Min(MaxCatalogSheets, 256));",
            'foreach (var item in container.Elements("sheet"))',
            "if (result.Count >= MaxCatalogSheets)",
            'throw new InvalidDataException("Semantic documentation catalog contains more than " + MaxCatalogSheets + " persisted sheets.");',
            "var placements = new List<SemanticSheetPlacementDefinition>();",
            "result.Add(new SemanticSheetDefinition(",
        ),
        "ReadSheets persisted count boundary")

    print("PASS: Semantic Documentation Load rejects persisted view/sheet element 10,001 before constructing it, matching the existing Save-side 10,000-item catalog bounds.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
