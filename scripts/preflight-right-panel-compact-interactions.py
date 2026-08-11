#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src/QS3D.BricsCAD.V25/UI"
XAML = UI / "RightPanel.xaml"
CODE = UI / "RightPanel.xaml.cs"
SHORTCUTS = UI / "RightPanel.SearchShortcuts.cs"
COMPACT = UI / "RightPanel.CompactShell.cs"
DOC = ROOT / "docs/UI-RIGHT-PANEL-COMPACT-INTERACTIONS-2026-08-11.md"
errors = []

for path in (XAML, CODE, SHORTCUTS, COMPACT, DOC):
    if not path.is_file():
        errors.append("missing RightPanel compact-interaction dependency: " + str(path.relative_to(ROOT)))

xaml = XAML.read_text(encoding="utf-8") if XAML.is_file() else ""
code = CODE.read_text(encoding="utf-8") if CODE.is_file() else ""
shortcuts = SHORTCUTS.read_text(encoding="utf-8") if SHORTCUTS.is_file() else ""
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

right_panel_code = "\n".join(
    path.read_text(encoding="utf-8")
    for path in sorted(UI.glob("RightPanel*.cs"))
)
callback_count = len(re.findall(r"\bvoid\s+OnRightPanelPreviewKeyDown\s*\(", right_panel_code))
if callback_count != 1:
    errors.append("OnRightPanelPreviewKeyDown must have exactly one implementation across RightPanel partials; found " + str(callback_count))
if (UI / "RightPanel.Keyboard.cs").exists():
    errors.append("RightPanel.Keyboard.cs must not return; RightPanel.SearchShortcuts.cs is the canonical key-handler owner")

if shortcuts:
    required_shortcuts = (
        "public partial class RightPanel",
        "private void OnRightPanelPreviewKeyDown(object sender, KeyEventArgs e)",
        "var modifiers = Keyboard.Modifiers;",
        "modifiers == ModifierKeys.Control && e.Key == Key.F",
        "LayerSearchBox?.Focus();",
        "LayerSearchBox?.SelectAll();",
        "modifiers == ModifierKeys.None && e.Key == Key.F5",
        "Refresh();",
        "modifiers == ModifierKeys.None && e.Key == Key.Escape",
        "LayerSearchBox.IsKeyboardFocusWithin",
        "LayerSearchBox.Clear();",
        "e.Handled = true;",
    )
    for token in required_shortcuts:
        if token not in shortcuts:
            errors.append("RightPanel canonical keyboard contract missing: " + token)

    for forbidden in (
        "XrefService",
        "LayerVisibilityService",
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ProjectState",
        "PreviewKeyDown += OnRightPanelPreviewKeyDown",
        "OnInitialized(",
        '"_XATTACH"',
        '"_MOVE"',
        '"_ZOOM',
    ):
        if forbidden in shortcuts:
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
        "RightPanel.SearchShortcuts.cs",
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
    if "existing code-behind did not provide that callback" in doc:
        errors.append("RightPanel compact documentation must not repeat the superseded missing-callback claim")

if errors:
    print("RightPanel compact-interaction preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("RightPanel compact-interaction preflight PASS: the canonical SearchShortcuts partial owns the single XAML keyboard callback, and compact styling remains presentation-only.")
