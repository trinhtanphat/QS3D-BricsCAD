#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
errors = []

files = {
    "workspace": UI / "WorkspacePanel.xaml",
    "right": UI / "RightPanel.xaml",
    "theme": UI / "Theme.xaml",
}

for label, path in files.items():
    if not path.is_file():
        errors.append("missing premium UI file: " + str(path.relative_to(ROOT)))
        continue
    try:
        ET.parse(path)
    except ET.ParseError as exc:
        errors.append(str(path.relative_to(ROOT)) + " is not well-formed XAML/XML: " + str(exc))

workspace = files["workspace"]
if workspace.is_file():
    text = workspace.read_text(encoding="utf-8")
    required = (
        'x:Key="WorkspaceCard"',
        'x:Key="WorkspaceBadge"',
        'x:Key="WorkspaceToolbarBand"',
        'Text="BIM WORKSPACE"',
        'Text="PHẠM VI LÀM VIỆC"',
        'Text="Tìm Family / Type"',
        'Text="ĐỐI TƯỢNG ĐANG CHỌN"',
        'Text="CAD + SEMANTIC"',
        'Foreground="{StaticResource LuxuryBrush}"',
        'Click="OnWallJunctionsClick"',
        'Click="OnWallSnapPreviewClick"',
        'Click="OnWallSnapApplyClick"',
        'Click="OnAutoHostClick"',
        'Click="OnFocusSelectedClick"',
        'Click="OnIsolateSelectedClick"',
        'Click="OnUnisolateClick"',
        'ItemsSource="{Binding PropertyScopes}"',
        'SelectedItem="{Binding SelectedPropertyScope, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
        'ToolTip="Reset override về giá trị Family"',
        'Text="VIEWPORT BRICSCAD • PAN • ZOOM • ORBIT • PICK"',
    )
    for needle in required:
        if needle not in text:
            errors.append("WorkspacePanel.xaml missing premium/workflow contract: " + needle)

    forbidden = ('Foreground="Black"', 'Foreground="#000000"', 'Foreground="#FF000000"')
    for needle in forbidden:
        if needle in text:
            errors.append("WorkspacePanel.xaml contains dark host-risk foreground: " + needle)

right = files["right"]
if right.is_file():
    text = right.read_text(encoding="utf-8")
    required = (
        'x:Key="RightBadge"',
        'x:Key="RightToolbarBand"',
        'Drawings.Count, StringFormat={}{0} bản vẽ',
        'Layers.Count, StringFormat={}{0} lớp',
        'Text="Xref / Drawing"',
        'Text="Hiện / Ẩn / Khóa / Màu native"',
        'Text="Tìm lớp"',
        'Background="{Binding ColorBrush}"',
        'IsChecked="{Binding IsLocked}"',
        'Click="OnLockLayersClick"',
        'Click="OnUnlockLayersClick"',
        'ToolTip="Màu layer native"',
        'ToolTip="{Binding Status}"',
    )
    for needle in required:
        if needle not in text:
            errors.append("RightPanel.xaml missing premium/live-state contract: " + needle)

    forbidden = ('Foreground="Black"', 'Foreground="#000000"', 'Foreground="#FF000000"')
    for needle in forbidden:
        if needle in text:
            errors.append("RightPanel.xaml contains dark host-risk foreground: " + needle)

theme = files["theme"]
if theme.is_file():
    text = theme.read_text(encoding="utf-8")
    for needle in (
        'x:Key="PanelTitle"',
        '<Setter Property="Foreground" Value="{StaticResource TextBrush}"/>',
        'x:Key="AccentSoftBrush"',
        'x:Key="LuxuryBrush"',
        'x:Key="BorderFocusBrush"',
    ):
        if needle not in text:
            errors.append("Theme.xaml missing premium design-system contract: " + needle)

print("QS3D premium workspace/right-panel preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: Workspace and RightPanel use the premium CAD-first hierarchy, preserve BLT workflow handlers, "
    "retain live layer state, and avoid black-text regressions on the dark BricsCAD host."
)
