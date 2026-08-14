#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "theme": ROOT / "src/QS3D.BricsCAD.V25/UI/Theme.xaml",
    "workspace": ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "right": ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml",
    "right_code": ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs",
}

for label, path in files.items():
    if not path.is_file():
        errors.append("missing UI/HiDPI file: " + str(path.relative_to(ROOT)))
        continue
    if path.suffix.lower() == ".xaml":
        try:
            ET.parse(path)
        except ET.ParseError as exc:
            errors.append(str(path.relative_to(ROOT)) + " is not well-formed XAML/XML: " + str(exc))

theme = files["theme"]
if theme.is_file():
    text = theme.read_text(encoding="utf-8")
    for needle in (
        'Property="UseLayoutRounding" Value="True"',
        'Property="TextOptions.TextFormattingMode" Value="Display"',
        'Property="IsKeyboardFocused" Value="True"',
        'Property="IsKeyboardFocusWithin" Value="True"',
        'VirtualizingPanel.VirtualizationMode" Value="Recycling"',
        'Property="EnableRowVirtualization" Value="True"',
        'Property="EnableColumnVirtualization" Value="True"',
        'Property="ScrollViewer.CanContentScroll" Value="True"',
    ):
        if needle not in text:
            errors.append("Theme.xaml missing HiDPI/focus/virtualization contract: " + needle)

workspace = files["workspace"]
if workspace.is_file():
    text = workspace.read_text(encoding="utf-8")
    for needle in (
        'MinWidth="0"', 'MinHeight="0"', 'x:Name="WorkspaceContentRoot"', 'MinWidth="560"',
        'HorizontalScrollBarVisibility="Auto"', 'VerticalScrollBarVisibility="Disabled"',
        'Width="{Binding ViewportWidth, ElementName=WorkspaceOverflow}"',
        'ResourceDictionary Source="Theme.xaml"',
        'TextTrimming="CharacterEllipsis"', 'ToolTip="{Binding Status}"',
        'ToolTip="Reset override về giá trị Family"', 'VIEWPORT BRICSCAD',
    ):
        if needle not in text:
            errors.append("WorkspacePanel.xaml missing compact/overflow/native-viewport UI contract: " + needle)

right = files["right"]
if right.is_file():
    text = right.read_text(encoding="utf-8")
    for needle in (
        'Background="{Binding ColorBrush}"', 'IsChecked="{Binding IsLocked}"',
        'Click="OnLockLayersClick"', 'Click="OnUnlockLayersClick"',
        'ToolTip="Màu layer native"',
    ):
        if needle not in text:
            errors.append("RightPanel.xaml missing live layer state contract: " + needle)
    if 'Grid.Column="2" Width="9" Height="9" CornerRadius="1" Background="{StaticResource AccentBrush}"' in text:
        errors.append("RightPanel layer swatch must not use a fixed accent color for live DWG layer color")

right_code = files["right_code"]
if right_code.is_file():
    text = right_code.read_text(encoding="utf-8")
    for needle in (
        "Color.FromRgb(item.Red, item.Green, item.Blue)", "brush.Freeze();",
        "IsLocked = item.IsLocked", "LayerVisibilityService.SetLocked", "_refreshingLayers",
    ):
        if needle not in text:
            errors.append("RightPanel.xaml.cs missing live color/lock/refresh guard: " + needle)

print("QS3D UI HiDPI/focus/live-layer preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: shared dark theme has explicit HiDPI/focus/virtualization guards and RightPanel renders live DWG layer color/lock state without a fake swatch.")
