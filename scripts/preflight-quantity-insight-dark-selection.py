#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.DarkHostTheme.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.xaml"
THEME = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Theme.xaml"
errors = []

for path in (PARTIAL, XAML, THEME):
    if not path.is_file():
        errors.append("missing Quantity Insight dark-selection dependency: " + str(path.relative_to(ROOT)))

if PARTIAL.is_file():
    partial = PARTIAL.read_text(encoding="utf-8")
    for token in (
        "public partial class QuantityInsightPanel",
        "RegisterQuantityDarkHostThemeGuard()",
        "EventManager.RegisterClassHandler(",
        "FrameworkElement.LoadedEvent",
        "ApplyQuantityDarkHostTheme()",
        'TryFindResource("BgSelectedBrush") is Brush selectionBrush',
        'TryFindResource("TextBrush") is Brush selectionTextBrush',
        "PinQuantitySelectionResource(SystemColors.HighlightBrushKey, selectionBrush);",
        "PinQuantitySelectionResource(SystemColors.InactiveSelectionHighlightBrushKey, selectionBrush);",
        "PinQuantitySelectionResource(SystemColors.HighlightTextBrushKey, selectionTextBrush);",
        "PinQuantitySelectionResource(SystemColors.InactiveSelectionHighlightTextBrushKey, selectionTextBrush);",
        "Resources[key] = brush;",
        "QuantityTree.Resources[key] = brush;",
    ):
        if token not in partial:
            errors.append("Quantity Insight dark-selection guard missing: " + token)

    for forbidden in (
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ProjectState",
        "QuantityReport",
        "OnQuantityTreeSelectedItemChanged(",
        "OnQuantityTreeDoubleClick(",
    ):
        if forbidden in partial:
            errors.append("Quantity Insight dark-selection guard must remain presentation-only: " + forbidden)

if XAML.is_file():
    xaml = XAML.read_text(encoding="utf-8")
    for token in (
        '<ResourceDictionary Source="Theme.xaml"/>',
        'x:Name="QuantityTree"',
        'SelectedItemChanged="OnQuantityTreeSelectedItemChanged"',
        'MouseDoubleClick="OnQuantityTreeDoubleClick"',
        'x:Name="QuantityHeaderGrid"',
        'x:Name="QuantitySummaryHeaderGrid"',
    ):
        if token not in xaml:
            errors.append("Quantity Insight XAML continuity contract missing: " + token)

if THEME.is_file():
    theme = THEME.read_text(encoding="utf-8")
    for token in (
        '<SolidColorBrush x:Key="BgSelectedBrush"',
        '<SolidColorBrush x:Key="TextBrush"',
        '<Style TargetType="{x:Type TreeViewItem}">',
        '<Trigger Property="IsSelected" Value="True">',
    ):
        if token not in theme:
            errors.append("canonical Quantity Insight theme contract missing: " + token)

if errors:
    print("Quantity Insight dark-selection preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Quantity Insight pins active/inactive WPF selection resources to QS3D dark brushes "
    "at both panel and QuantityTree boundaries without changing quantity selection semantics."
)
