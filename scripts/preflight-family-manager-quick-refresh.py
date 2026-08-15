#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fail(message):
    print("ERROR:", message)
    return 1


def require(path, tokens):
    if not path.is_file():
        raise RuntimeError(f"missing Family Manager surface: {path.relative_to(ROOT)}")
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            raise RuntimeError(f"{path.relative_to(ROOT)} missing Quick Form refresh contract token: {token}")
    return text


def main():
    try:
        quick = require(
            ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.QuickWorkflow.cs",
            (
                "private ElementCategory? ResolveQuickCategory()",
                "if (_creatingNew)",
                "return (NewCategoryCombo.SelectedItem as CategoryChoice)?.Category;",
                "return (FamilyList.SelectedItem as ProjectFamily)?.Category;",
            ),
        )
        catalog = require(
            ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.TemplateCatalog.cs",
            (
                "OnFamilyCatalogItemsSourceChanged",
                "ApplyFamilyCatalogGrouping();",
                "Dispatcher.BeginInvoke(new Action(() =>",
                "if (!_loading) RefreshQuickWorkflow();",
            ),
        )
    except RuntimeError as exc:
        return fail(str(exc))

    resolver = quick.find("private ElementCategory? ResolveQuickCategory()")
    new_mode = quick.find("if (_creatingNew)", resolver)
    new_category = quick.find("return (NewCategoryCombo.SelectedItem as CategoryChoice)?.Category;", resolver)
    selected_family = quick.find("return (FamilyList.SelectedItem as ProjectFamily)?.Category;", resolver)
    if resolver < 0 or new_mode < 0 or new_category < 0 or selected_family < 0 or not (
        resolver < new_mode < new_category < selected_family
    ):
        return fail("Quick category resolution must use NewCategoryCombo only in explicit new-Family mode")

    legacy = "if (!_creatingNew && FamilyList.SelectedItem is ProjectFamily selected)"
    if legacy in quick[resolver : resolver + 600]:
        return fail("normal no-selection state must not fall back to NewCategoryCombo defaults")

    rebind = catalog.find("private void OnFamilyCatalogItemsSourceChanged")
    grouping = catalog.find("ApplyFamilyCatalogGrouping();", rebind)
    defer = catalog.find("Dispatcher.BeginInvoke(new Action(() =>", rebind)
    refresh = catalog.find("if (!_loading) RefreshQuickWorkflow();", defer)
    if rebind < 0 or grouping < 0 or defer < 0 or refresh < 0 or not (
        rebind < grouping < defer < refresh
    ):
        return fail("catalog rebind must group first and defer a Quick Form refresh until loading completes")

    print(
        "PASS: Family Manager Quick Form binds NewCategory only in explicit draft mode, collapses normal "
        "no-selection state, and refreshes after catalog ItemsSource rebinding suppressed SelectionChanged."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
