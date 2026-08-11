#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
KEYBOARD = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.Keyboard.cs"
COMPACT = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.CompactShell.cs"
DOC = ROOT / "docs/UI-RIGHT-PANEL-COMPACT-INTERACTIONS-2026-08-11.md"
errors = []

for path in (XAML, CODE, KEYBOARD, COMPACT, DOC):
    if not path.is_file():
        errors.append("missing RightPanel compact-interaction dependency: " + str(path.relative_to(ROOT)))

xaml = XAML.read_text(encoding="utf-8") if XAML.is_file() else ""
code = CODE.read_text(encoding="utf-8") if CODE.is_file() else ""
keyboard = KEYBOARD.read_text(encoding="utf-8") if KEYBOARD.is_file() else ""
compact = COMPACT.read_text(encoding="utf-8") if COMPACT.is_file() else ""
doc = DOC.read_text(encoding="utf-8") if DOC.is_file() else ""

if xaml:
    try:
        ET.fromstring(xaml)
    except ET.ParseError as exc:
        errors.append("RightPanel.xaml is not well-formed XML: " + str(exc))

    required_xaml = (
        'PreviewKeyDown="OnRightPanelPreviewKeyDown"',
        'x:Name="DrawingList"',
        'SelectionChanged="OnDrawingSelectionChanged"',
        'Click="OnAttachXrefClick"',
        'Click="OnReloadXrefClick"',
        'Click="OnMoveDrawingClick"',
        'Click="OnZoomWindowClick"',
        'Click="OnDeleteDrawingClick"',
        'Click="OnClearDrawingSelectionClick"',
        'x:Name="LayerSearchBox"',
        'TextChanged="OnLayerSearchChanged"',
        'x:Name="LayerList"',
        'Click="OnShowLayersClick"',
        'Click="OnHideLayersClick"',
        'Click="OnLockLayersClick"',
        'Click="OnUnlockLayersClick"',
        'Click="OnInvertSelectionClick"',
        'Click="OnClearLayerSelectionClick"',
        'Click="OnRefreshClick"',
        'QUẢN LÝ BẢN VẼ',
        'QUẢN LÝ LỚP',
        'Ctrl+F',
    )
    for token in required_xaml:
        if token not in xaml:
            errors.append("RightPanel XAML contract missing: " + token)

all_cs = "\n".join((code, keyboard, compact))
callback_count = len(re.findall(r"\bvoid\s+OnRightPanelPreviewKeyDown\s*\(", all_cs))
if callback_count != 1:
    errors.append("OnRightPanelPreviewKeyDown must have exactly one implementation; found " + str(callback_count))

if keyboard:
    required_keyboard = (
        "public partial class RightPanel",
        "private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)",
        "var modifiers = Keyboard.Modifiers;",
        "modifiers == ModifierKeys.Control && e.Key == Key.F",
        "LayerSearchBox.Focus();",
        "LayerSearchBox.SelectAll();",
        "modifiers == ModifierKeys.None && e.Key == Key.F5",
        "OnRefreshClick(this, new RoutedEventArgs());",
        "modifiers == ModifierKeys.None && e.Key == Key.Escape",
        "string.IsNullOrWhiteSpace(LayerSearchBox.Text)",
        "LayerSearchBox.Clear();",
        "OnClearLayerSelectionClick(this, new RoutedEventArgs());",
        "OnClearDrawingSelectionClick(this, new RoutedEventArgs());",
        "e.Handled = true;",
    )
    for token in required_keyboard:
        if token not in keyboard:
            errors.append("RightPanel keyboard contract missing: " + token)

    for forbidden in (
        "XrefService",
        "LayerVisibilityService",
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ProjectState",
        '"_XATTACH"',
        '"_MOVE"',
        '"_ZOOM',
    ):
        if forbidden in keyboard:
            errors.append("RightPanel keyboard routing must not duplicate behavior: " + forbidden)

if compact:
    required_compact = (
        "public partial class RightPanel",
        "static RightPanel()",
        "EventManager.RegisterClassHandler(",
        "ApplyRightCompactShellPresentation()",
        "_rightCompactShellApplied",
        "UseLayoutRounding = true",
        "SnapsToDevicePixels = true",
        "root.RowDefinitions[0].Height = new GridLength(238)",
        "root.RowDefinitions[0].MinHeight = 145",
        "root.RowDefinitions[3].Height = new GridLength(28)",
        "splitter.ShowsPreview = true",
        "DrawingList.MinHeight = 105",
        "LayerList.MinHeight = 165",
        "LayerSearchBox.MinHeight = 24",
        'AppendRightShortcutHint(LayerSearchBox, "Ctrl+F")',
        'AppendRightShortcutHint(FindRightButton("Làm mới"), "F5")',
        'AppendRightShortcutHint(LayerSearchBox, "Esc xóa bộ lọc")',
        '"QUẢN LÝ BẢN VẼ"',
        '"QUẢN LÝ LỚP"',
    )
    for token in required_compact:
        if token not in compact:
            errors.append("RightPanel compact presentation missing: " + token)

    for forbidden in (
        "XrefService",
        "LayerVisibilityService",
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ProjectState",
        "Quantity",
        "OnAttachXrefClick(",
        "OnReloadXrefClick(",
        "OnMoveDrawingClick(",
        "OnDeleteDrawingClick(",
        "OnShowLayersClick(",
        "OnHideLayersClick(",
        "OnLockLayersClick(",
        "OnUnlockLayersClick(",
    ):
        if forbidden in compact:
            errors.append("RightPanel compact presentation must remain presentation-only: " + forbidden)

if doc:
    for token in (
        "Ctrl+F",
        "F5",
        "Esc",
        "Quản lý bản vẽ",
        "Quản lý lớp",
        "238-DIP",
        "145-DIP",
        "presentation-only",
        "XrefService",
        "LayerVisibilityService",
        "does **not** claim",
    ):
        if token not in doc:
            errors.append("RightPanel compact-interaction documentation missing: " + token)

if errors:
    print("RightPanel compact-interaction preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("RightPanel compact-interaction preflight PASS: the declared keyboard callback is implemented once, shortcuts route through existing handlers, and compact styling remains presentation-only.")
