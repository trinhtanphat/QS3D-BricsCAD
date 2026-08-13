#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RightPanel.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing RightPanel XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("RightPanel.xaml is not well-formed: " + str(exc))
        root = None


def named_grid(name):
    if root is None:
        return None
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == name:
            return grid
    errors.append("missing RightPanel responsive header grid: " + name)
    return None


def require_header(name):
    grid = named_grid(name)
    if grid is None:
        return

    defs = grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [
        child.attrib.get("Width")
        for child in list(defs)
        if child.tag == WPF + "ColumnDefinition"
    ]
    if widths != ["*", "Auto"]:
        errors.append(name + " must use deterministic top-level columns ['*', 'Auto']; got " + repr(widths))

    title_grid = next(
        (child for child in list(grid)
         if child.tag == WPF + "Grid" and child.attrib.get("Grid.Column", "0") == "0"),
        None,
    )
    if title_grid is None:
        errors.append(name + " missing title/accent grid in column 0")
    else:
        if title_grid.attrib.get("MinWidth") != "0":
            errors.append(name + " title/accent grid must keep MinWidth=0")
        title_stack = next(
            (node for node in title_grid.iter(WPF + "StackPanel")
             if node.attrib.get("Grid.Column") == "1"),
            None,
        )
        if title_stack is None:
            errors.append(name + " missing title stack")
        else:
            if title_stack.attrib.get("MinWidth") != "0":
                errors.append(name + " title stack must keep MinWidth=0")
            labels = [child for child in list(title_stack) if child.tag == WPF + "TextBlock"]
            if len(labels) < 2:
                errors.append(name + " must retain title and caption")
            for label in labels:
                if label.attrib.get("TextWrapping") != "NoWrap":
                    errors.append(name + " title/caption must use TextWrapping=NoWrap")
                if label.attrib.get("TextTrimming") != "CharacterEllipsis":
                    errors.append(name + " title/caption must use CharacterEllipsis")

    actions = next(
        (child for child in list(grid)
         if child.tag == WPF + "StackPanel" and child.attrib.get("Grid.Column") == "1"),
        None,
    )
    if actions is None:
        errors.append(name + " missing right action cluster in auto column 1")
    else:
        if actions.attrib.get("Orientation") != "Horizontal":
            errors.append(name + " right action cluster must remain horizontal")
        if actions.attrib.get("HorizontalAlignment") != "Right":
            errors.append(name + " right action cluster must remain right-aligned")


require_header("DrawingHeaderGrid")
require_header("LayerHeaderGrid")

for token in (
    'Text="QUẢN LÝ BẢN VẼ"',
    'Text="QUẢN LÝ LỚP"',
    'Text="{Binding Drawings.Count, StringFormat={}{0} bản vẽ}"',
    'Text="{Binding LayerCountText}"',
    'Click="OnClearDrawingSelectionClick"',
    'Click="OnRefreshClick"',
    'Click="OnAttachXrefClick"',
    'Click="OnReloadXrefClick"',
    'Click="OnMoveDrawingClick"',
    'Click="OnDeleteDrawingClick"',
    'Click="OnShowLayersClick"',
    'Click="OnHideLayersClick"',
    'Click="OnLockLayersClick"',
    'Click="OnUnlockLayersClick"',
    'x:Name="DrawingList"',
    'x:Name="LayerList"',
):
    if token not in text:
        errors.append("RightPanel behavior/binding contract missing: " + token)

if text.count("<ContextMenu ") != 2:
    errors.append("RightPanel must retain exactly two existing context-menu surfaces")

if '<StackPanel DockPanel.Dock="Right" Orientation="Horizontal">' in text:
    errors.append("RightPanel section headers must not regress to last-child right-docked StackPanels")

if errors:
    print("RightPanel responsive-header preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: RightPanel drawing/layer headers use deterministic star/auto responsive grids, "
    "keep title text shrinkable, and preserve existing Xref/layer commands and context menus."
)
