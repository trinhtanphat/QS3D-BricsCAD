#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySettingsWindow.xaml.cs"


def method_body(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:] if end < 0 else text[start:end]


def main():
    code = CODE.read_text(encoding="utf-8")
    import_method = method_body(
        code,
        "private void ImportTemplate_Click(object sender, RoutedEventArgs e)",
        "private void ExportTemplate_Click(object sender, RoutedEventArgs e)",
    )
    if not import_method:
        print("ERROR: QuantitySettingsWindow.ImportTemplate_Click was not found.")
        return 1

    required = [
        "var imported = _store.Import(dialog.FileName);",
        "LoadIntoView(imported);",
        "imported.CategoryRules.Select(x => x.Category)",
        ".Concat(imported.IntersectionRules.Select(x => x.Source))",
        ".Concat(imported.IntersectionRules.Select(x => x.Target))",
        ".Where(code => !QuantityCategoryDisplayName.IsKnown(code))",
        ".Distinct()",
        ".OrderBy(code => code)",
        ".Count();",
    ]
    missing = [token for token in required if token not in import_method]
    if missing:
        print("ERROR: quantity-settings import diagnostics no longer cover the complete imported category-code union:")
        for token in missing:
            print(" -", token)
        return 1

    category_pos = import_method.find("imported.CategoryRules.Select(x => x.Category)")
    source_pos = import_method.find(".Concat(imported.IntersectionRules.Select(x => x.Source))", category_pos)
    target_pos = import_method.find(".Concat(imported.IntersectionRules.Select(x => x.Target))", source_pos)
    known_pos = import_method.find(".Where(code => !QuantityCategoryDisplayName.IsKnown(code))", target_pos)
    distinct_pos = import_method.find(".Distinct()", known_pos)
    order_pos = import_method.find(".OrderBy(code => code)", distinct_pos)
    count_pos = import_method.find(".Count();", order_pos)
    if min(category_pos, source_pos, target_pos, known_pos, distinct_pos, order_pos, count_pos) < 0 or not (
        category_pos < source_pos < target_pos < known_pos < distinct_pos < order_pos < count_pos
    ):
        print("ERROR: imported category/source/target codes must be unified before known-code filtering, deduplication, ordering and counting.")
        return 1

    if "IntersectionRules =" in import_method or "CategoryRules =" in import_method:
        print("ERROR: diagnostics must not rewrite or filter the imported rule payload.")
        return 1

    print("PASS: quantity-settings import diagnostics include category, intersection-source and intersection-target codes without mutating the imported payload.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
