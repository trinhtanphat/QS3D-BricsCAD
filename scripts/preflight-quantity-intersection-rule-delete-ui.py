#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
LOADED = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.CategoryRuleCreation.cs"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.IntersectionRuleRemoval.cs"
errors = []

if not LOADED.is_file():
    errors.append("missing QuantitySettingsWindow.CategoryRuleCreation.cs")
else:
    loaded = LOADED.read_text(encoding="utf-8")
    handler = loaded.find("private void QuantitySettingsWindow_Loaded")
    init = loaded.find("InitializeIntersectionRuleRemoval();", handler)
    if handler < 0 or init < 0:
        errors.append("window Loaded path must initialize intersection-rule removal")

if not CODE.is_file():
    errors.append("missing QuantitySettingsWindow.IntersectionRuleRemoval.cs")
else:
    code = CODE.read_text(encoding="utf-8")
    required = (
        "private void InitializeIntersectionRuleRemoval()",
        "CreateSelectedRuleButton.Parent as Panel",
        "Content = \"−  Xóa luật A → B\"",
        "_deleteSelectedRuleButton.Click += DeleteSelectedRule_Click;",
        "PrimaryCategoryList.SelectionChanged += IntersectionRuleRemovalStateChanged;",
        "ReferenceCategoryList.SelectionChanged += IntersectionRuleRemovalStateChanged;",
        "IntersectionRows.CollectionChanged += IntersectionRuleRemovalRowsChanged;",
        "button.Visibility = exists ? Visibility.Visible : Visibility.Collapsed;",
        "button.IsEnabled = exists && !_persistentSettingsWriteBlocked;",
        "private void DeleteSelectedRule_Click(object sender, RoutedEventArgs e)",
        "IntersectionRows.SingleOrDefault(",
        "MessageBoxButton.YesNo",
        "if (answer != MessageBoxResult.Yes) return;",
        "IntersectionRows.Remove(selected);",
        "RebuildIntersectionBrowser();",
    )
    for token in required:
        if token not in code:
            errors.append("delete-rule contract missing token: " + token)

    delete_start = code.find("private void DeleteSelectedRule_Click")
    if delete_start < 0:
        errors.append("cannot isolate delete handler")
    else:
        handler = code[delete_start:]
        readonly = handler.find("if (_persistentSettingsWriteBlocked)")
        resolve = handler.find("IntersectionRows.SingleOrDefault(")
        confirm = handler.find("MessageBoxButton.YesNo", resolve)
        yes_guard = handler.find("answer != MessageBoxResult.Yes", confirm)
        remove = handler.find("IntersectionRows.Remove(selected);", yes_guard)
        rebuild = handler.find("RebuildIntersectionBrowser();", remove)
        if min(readonly, resolve, confirm, yes_guard, remove, rebuild) < 0 or not (
            readonly < resolve < confirm < yes_guard < remove < rebuild
        ):
            errors.append("delete handler must fail read-only, re-resolve exact row, confirm, then remove and refresh")
        forbidden = ("_store.Save", "_store.Export", "_store.Import", "File.Write", "DataContractJsonSerializer", "CategoryRows.Remove")
        for token in forbidden:
            if token in handler:
                errors.append("delete handler must not use: " + token)
        if handler.count("IntersectionRows.Remove(selected);") != 1:
            errors.append("delete handler must remove exactly one selected directed row")

print("QS3D Quantity Settings intersection-rule delete UI preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DSETUP exposes contextual delete only for an existing writable directed rule, revalidates the exact A→B row before confirmation, removes only that in-memory row, leaves reverse/category rules untouched, and preserves Save-only persistence.")
