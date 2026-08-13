#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantityInsightPanel.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Quantity Insight XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("QuantityInsightPanel.xaml is not well-formed: " + str(exc))
        root = None


def named_grid(name):
    if root is None:
        return None
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == name:
            return grid
    errors.append("missing responsive header grid: " + name)
    return None


def require_two_column_header(grid, name):
    if grid is None:
        return
    defs = grid.find(WPF + "Grid.ColumnDefinitions")
    if defs is None:
        errors.append(name + " missing Grid.ColumnDefinitions")
        return
    widths = [child.attrib.get("Width") for child in list(defs) if child.tag == WPF + "ColumnDefinition"]
    if widths != ["*", "Auto"]:
        errors.append(name + " must use deterministic columns ['*', 'Auto']; got " + repr(widths))


header = named_grid("QuantityHeaderGrid")
summary = named_grid("QuantitySummaryHeaderGrid")
require_two_column_header(header, "QuantityHeaderGrid")
require_two_column_header(summary, "QuantitySummaryHeaderGrid")

if header is not None:
    content = next(
        (child for child in list(header)
         if child.tag == WPF + "StackPanel" and child.attrib.get("Grid.Column", "0") == "0"),
        None,
    )
    if content is None:
        errors.append("QuantityHeaderGrid missing shrinkable title stack in column 0")
    else:
        if content.attrib.get("MinWidth") != "0":
            errors.append("QuantityHeaderGrid title stack must keep MinWidth=0")
        labels = [child for child in list(content) if child.tag == WPF + "TextBlock"]
        if len(labels) < 2:
            errors.append("QuantityHeaderGrid must retain title and caption")
        for label in labels:
            if label.attrib.get("TextWrapping") != "NoWrap":
                errors.append("QuantityHeaderGrid title/caption must use TextWrapping=NoWrap")
            if label.attrib.get("TextTrimming") != "CharacterEllipsis":
                errors.append("QuantityHeaderGrid title/caption must use CharacterEllipsis")

    badge = next(
        (child for child in list(header)
         if child.tag == WPF + "Border" and child.attrib.get("Grid.Column") == "1"),
        None,
    )
    if badge is None:
        errors.append("QuantityHeaderGrid count badge must occupy auto column 1")
    elif badge.attrib.get("HorizontalAlignment") != "Right":
        errors.append("QuantityHeaderGrid count badge must remain right-aligned")

if summary is not None:
    left = next(
        (child for child in list(summary)
         if child.tag == WPF + "TextBlock" and child.attrib.get("Grid.Column", "0") == "0"),
        None,
    )
    right = next(
        (child for child in list(summary)
         if child.tag == WPF + "TextBlock" and child.attrib.get("Grid.Column") == "1"),
        None,
    )
    if left is None:
        errors.append("QuantitySummaryHeaderGrid missing summary title in column 0")
    else:
        if left.attrib.get("MinWidth") != "0":
            errors.append("Quantity summary title must keep MinWidth=0")
        if left.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Quantity summary title must use CharacterEllipsis")
        if left.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Quantity summary title must use TextWrapping=NoWrap")
    if right is None or right.attrib.get("Text") != "READ-ONLY":
        errors.append("QuantitySummaryHeaderGrid must retain READ-ONLY in auto column 1")

for token in (
    'Click="OnRefreshClick"',
    'Click="OnRegenerateClick"',
    'Click="OnOpenBqClick"',
    'Click="OnLocateClick"',
    'x:Name="QuantityTree"',
    'SelectedItemChanged="OnQuantityTreeSelectedItemChanged"',
    'MouseDoubleClick="OnQuantityTreeDoubleClick"',
    'Text="{Binding QuantityCountText}"',
    'Text="{Binding Status}"',
):
    if token not in text:
        errors.append("Quantity Insight behavior/binding contract missing: " + token)

for stale in (
    '<TextBlock DockPanel.Dock="Right" Text="READ-ONLY"',
    '<Border DockPanel.Dock="Right" Background="{StaticResource AccentSoftBrush}"',
):
    if stale in text:
        errors.append("Quantity Insight still relies on last-child DockPanel right docking: " + stale)

if errors:
    print("Quantity Insight responsive-header preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Quantity Insight uses deterministic star/auto responsive header grids, "
    "keeps compact title text shrinkable, and preserves existing quantity commands/bindings."
)
