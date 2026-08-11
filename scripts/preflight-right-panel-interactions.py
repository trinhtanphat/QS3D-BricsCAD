#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

xaml_path = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"
code_path = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.Interactions.cs"
shortcut_path = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.SearchShortcuts.cs"
main_code_path = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"

for path in (xaml_path, code_path, shortcut_path, main_code_path):
    if not path.is_file():
        errors.append("missing RightPanel interaction dependency: " + str(path.relative_to(ROOT)))

if xaml_path.is_file():
    try:
        ET.parse(xaml_path)
    except Exception as exc:
        errors.append("RightPanel.xaml is not well-formed XML: " + str(exc))
    text = xaml_path.read_text(encoding="utf-8")
    for token in (
        'PreviewKeyDown="OnRightPanelPreviewKeyDown"',
        'PreviewMouseRightButtonDown="OnDrawingListPreviewMouseRightButtonDown"',
        'PreviewMouseRightButtonDown="OnLayerListPreviewMouseRightButtonDown"',
        'Header="Nạp lại Xref" Click="OnReloadXrefClick"',
        'Header="Di chuyển Xref" Click="OnMoveDrawingClick"',
        'Header="Gỡ Xref" Click="OnDeleteDrawingClick"',
        'Header="Hiện layer" Click="OnShowLayersClick"',
        'Header="Ẩn layer" Click="OnHideLayersClick"',
        'Header="Khóa layer" Click="OnLockLayersClick"',
        'Header="Mở khóa layer" Click="OnUnlockLayersClick"',
        'ToolTip="Tìm layer • Ctrl+F"',
    ):
        if token not in text:
            errors.append("RightPanel.xaml missing interaction contract: " + token)

if code_path.is_file():
    text = code_path.read_text(encoding="utf-8")
    for token in (
        "OnDrawingListPreviewMouseRightButtonDown",
        "OnLayerListPreviewMouseRightButtonDown",
        "if (!item.IsSelected)",
        "LayerList.UnselectAll();",
        "FindRightPanelContainer<ListViewItem>",
    ):
        if token not in text:
            errors.append("RightPanel.Interactions.cs missing guard/token: " + token)
    for forbidden in (
        "SendStringToExecute",
        "XrefService.",
        "LayerVisibilityService.",
        "StartTransaction(",
        "StartOpenCloseTransaction(",
    ):
        if forbidden in text:
            errors.append("RightPanel interaction accelerator must reuse existing handlers, not duplicate CAD mutation logic: " + forbidden)

if shortcut_path.is_file():
    text = shortcut_path.read_text(encoding="utf-8")
    for token in (
        "private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)",
        "ModifierKeys.Control && e.Key == Key.F",
        "LayerSearchBox?.Focus();",
        "ModifierKeys.None && e.Key == Key.F5",
        "Refresh();",
    ):
        if token not in text:
            errors.append("RightPanel.SearchShortcuts.cs missing keyboard contract: " + token)

if main_code_path.is_file():
    text = main_code_path.read_text(encoding="utf-8")
    for token in (
        "OnReloadXrefClick",
        "OnMoveDrawingClick",
        "OnDeleteDrawingClick",
        "OnShowLayersClick",
        "OnHideLayersClick",
        "OnLockLayersClick",
        "OnUnlockLayersClick",
        "OnRefreshClick",
    ):
        if token not in text:
            errors.append("RightPanel main handler missing: " + token)

print("QS3D RightPanel interaction preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Layer/Xref context menus and panel-scoped Ctrl+F/F5 reuse existing guarded handlers; right-click targeting preserves selected layer batches and adds no duplicate CAD mutation path.")
