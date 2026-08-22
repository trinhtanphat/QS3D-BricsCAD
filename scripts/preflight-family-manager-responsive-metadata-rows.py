#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FamilyManagerWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Family Manager XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("FamilyManagerWindow.xaml is not well-formed: " + str(exc))
        root = None


def named_grid(name):
    if root is None:
        return None
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == name:
            return grid
    errors.append("missing responsive Family Manager grid: " + name)
    return None


def require_star_auto(grid, name):
    if grid is None:
        return
    definitions = grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if definitions is None else [
        item.attrib.get("Width")
        for item in list(definitions)
        if item.tag == WPF + "ColumnDefinition"
    ]
    if widths != ["*", "Auto"]:
        errors.append(name + " must use deterministic columns ['*', 'Auto']; got " + repr(widths))


reference = named_grid("FamilyReferenceSummaryGrid")
property_header = named_grid("FamilyPropertyHeaderGrid")
require_star_auto(reference, "FamilyReferenceSummaryGrid")
require_star_auto(property_header, "FamilyPropertyHeaderGrid")

if reference is not None:
    children = [item for item in list(reference) if item.tag == WPF + "TextBlock"]
    label = next((item for item in children if item.attrib.get("Text") == "Instance tham chiếu"), None)
    count = next((item for item in children if item.attrib.get(XAML_NS + "Name") == "ReferenceCountText"), None)
    if label is None:
        errors.append("FamilyReferenceSummaryGrid must retain Instance tham chiếu label")
    else:
        if label.attrib.get("Grid.Column", "0") != "0":
            errors.append("reference label must stay in flexible column 0")
        if label.attrib.get("MinWidth") != "0":
            errors.append("reference label must use MinWidth=0")
        if label.attrib.get("TextWrapping") != "NoWrap":
            errors.append("reference label must use NoWrap")
        if label.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("reference label must use CharacterEllipsis")
    if count is None:
        errors.append("FamilyReferenceSummaryGrid must retain ReferenceCountText")
    else:
        if count.attrib.get("Grid.Column") != "1":
            errors.append("ReferenceCountText must occupy auto column 1")
        if count.attrib.get("HorizontalAlignment") != "Right":
            errors.append("ReferenceCountText must remain right aligned")
        if count.attrib.get("TextWrapping") != "NoWrap":
            errors.append("ReferenceCountText must use NoWrap")

if property_header is not None:
    children = [item for item in list(property_header) if item.tag == WPF + "TextBlock"]
    title = next((item for item in children if item.attrib.get("Text") == "PROPERTY CỦA FAMILY"), None)
    status = next((item for item in children if item.attrib.get("Text") == "KEY / VALUE"), None)
    if title is None:
        errors.append("FamilyPropertyHeaderGrid must retain PROPERTY CỦA FAMILY title")
    else:
        if title.attrib.get("Grid.Column", "0") != "0":
            errors.append("property title must stay in flexible column 0")
        if title.attrib.get("MinWidth") != "0":
            errors.append("property title must use MinWidth=0")
        if title.attrib.get("TextWrapping") != "NoWrap":
            errors.append("property title must use NoWrap")
        if title.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("property title must use CharacterEllipsis")
    if status is None:
        errors.append("FamilyPropertyHeaderGrid must retain KEY / VALUE status")
    else:
        if status.attrib.get("Grid.Column") != "1":
            errors.append("KEY / VALUE must occupy auto column 1")
        if status.attrib.get("HorizontalAlignment") != "Right":
            errors.append("KEY / VALUE must remain right aligned")
        if status.attrib.get("TextWrapping") != "NoWrap":
            errors.append("KEY / VALUE must use NoWrap")

for token in (
    'x:Name="FamilyList"',
    'SelectionChanged="OnFamilySelectionChanged"',
    'x:Name="PropertyList"',
    'SelectionChanged="OnPropertySelectionChanged"',
    'Click="OnRefreshClick"',
    'Click="OnActivateClick"',
    'Click="OnNewClick"',
    'Click="OnDuplicateClick"',
    'Click="OnRenameClick"',
    'Click="OnDeleteClick"',
    'Click="OnSavePropertyClick"',
    'Click="OnRemovePropertyClick"',
    'Click="OnAssignClick"',
    'x:Name="StatusText"',
):
    if token not in text:
        errors.append("Family Manager behavior contract missing: " + token)

for stale in (
    'x:Name="ReferenceCountText" DockPanel.Dock="Right"',
    '<TextBlock DockPanel.Dock="Right" Text="KEY / VALUE"',
):
    if stale in text:
        errors.append("Family Manager still uses stale last-child right docking: " + stale)

if errors:
    print("Family Manager responsive metadata-row preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Family Manager uses deterministic star/auto metadata grids for reference counts and property headers, "
    "while preserving all Family/property handlers."
)
