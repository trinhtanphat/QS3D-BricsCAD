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
                "if (FamilyList.SelectedItem != null) _creatingNew = false;",
            ),
        )
        manager = require(
            ROOT / "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs",
            (
                "private void OnFamilySelectionChanged",
                "if (_creatingNew && FamilyList.SelectedItem == null)",
                "private void OnNewClick",
                "FamilyList.SelectedItem = null;",
                "FamilyNameBox.Focus();",
                "RefreshQuickWorkflow();",
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

    if "_creatingNew = FamilyList.SelectedItem == null;" in quick:
        return fail("ordinary Family deselection must not implicitly enter New mode")

    selection = manager.find("private void OnFamilySelectionChanged")
    preserve_new = manager.find("if (_creatingNew && FamilyList.SelectedItem == null)", selection)
    preserve_refresh = manager.find("RefreshQuickWorkflow();", preserve_new)
    reset_mode = manager.find("_creatingNew = false;", selection)
    load_family = manager.find("LoadFamily();", reset_mode)
    if min(selection, preserve_new, preserve_refresh, reset_mode, load_family) < 0 or not (
        selection < preserve_new < preserve_refresh < reset_mode < load_family
    ):
        return fail("selection handler must preserve explicit New-mode clearing before normal selection reset/load")

    new_click = manager.find("private void OnNewClick")
    set_new = manager.find("_creatingNew = true;", new_click)
    clear_selection = manager.find("FamilyList.SelectedItem = null;", set_new)
    focus = manager.find("FamilyNameBox.Focus();", clear_selection)
    refresh_new = manager.find("RefreshQuickWorkflow();", focus)
    if min(new_click, set_new, clear_selection, focus, refresh_new) < 0 or not (
        new_click < set_new < clear_selection < focus < refresh_new
    ):
        return fail("OnNewClick must refresh the Quick Form even when selection was already empty")

    rebind = catalog.find("private void OnFamilyCatalogItemsSourceChanged")
    grouping = catalog.find("ApplyFamilyCatalogGrouping();", rebind)
    defer = catalog.find("Dispatcher.BeginInvoke(new Action(() =>", rebind)
    refresh = catalog.find("if (!_loading) RefreshQuickWorkflow();", defer)
    if rebind < 0 or grouping < 0 or defer < 0 or refresh < 0 or not (
        rebind < grouping < defer < refresh
    ):
        return fail("catalog rebind must group first and defer a Quick Form refresh until loading completes")

    print(
        "PASS: Family Manager Quick Form keeps New mode explicit, handles New from an already-empty selection, "
        "collapses ordinary no-selection state, and refreshes after catalog rebinding suppresses SelectionChanged."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
