#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ZoneManagerWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Zone Manager XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("ZoneManagerWindow.xaml is not well-formed: " + str(exc))
        root = None

status_grid = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "ZoneManagerStatusGrid":
            status_grid = grid
            break

if status_grid is None:
    errors.append("missing responsive footer grid: ZoneManagerStatusGrid")
else:
    defs = status_grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("ZoneManagerStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status_grid)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    boundary = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "SEMANTIC SCOPE ONLY • NO CAD MOVE"), None)

    if indicator is None:
        errors.append("Zone Manager success indicator must remain in auto column 0")
    elif indicator.attrib.get("Background") != "{StaticResource SuccessBrush}":
        errors.append("Zone Manager footer must preserve SuccessBrush")

    if status is None:
        errors.append("Zone Manager StatusText missing")
    else:
        if status.attrib.get("Grid.Column") != "1" or status.attrib.get("MinWidth") != "0":
            errors.append("Zone Manager StatusText must be shrinkable in flexible column 1")
        if status.attrib.get("TextWrapping") != "Wrap":
            errors.append("Zone Manager StatusText must preserve wrapping")

    if boundary is None:
        errors.append("Zone Manager semantic-scope boundary label missing")
    else:
        if boundary.attrib.get("Grid.Column") != "2" or boundary.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Zone Manager boundary label must occupy right-aligned auto column 2")
        if boundary.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Zone Manager boundary label must remain NoWrap")

for token in (
    'x:Name="ZoneList"',
    'SelectionChanged="OnZoneSelectionChanged"',
    'Click="OnRefreshClick"',
    'Click="OnNewClick"',
    'Click="OnSaveClick"',
    'Click="OnDeleteClick"',
    'Click="OnActivateClick"',
    'Click="OnAssignClick"',
    'Click="OnInspectClick"',
    'Đổi/gán Zone chỉ thay semantic scope, không Move CAD source.',
):
    if token not in text:
        errors.append("Zone Manager workflow/boundary contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right" Text="SEMANTIC SCOPE ONLY • NO CAD MOVE"'
if stale in text:
    errors.append("Zone Manager footer still uses stale final-child right docking")

if errors:
    print("Zone Manager responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Zone Manager footer uses deterministic auto/star/auto layout while preserving zone and semantic-scope contracts."
)
