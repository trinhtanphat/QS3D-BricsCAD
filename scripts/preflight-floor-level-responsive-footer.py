#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Floor Level XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("FloorLevelWindow.xaml is not well-formed: " + str(exc))
        root = None

status_grid = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "FloorLevelStatusGrid":
            status_grid = grid
            break

if status_grid is None:
    errors.append("missing responsive footer grid: FloorLevelStatusGrid")
else:
    defs = status_grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("FloorLevelStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status_grid)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    lifecycle = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "NO CAD MOVE • STALE ON LEVEL CHANGE"), None)

    if indicator is None:
        errors.append("Floor Level success indicator must remain in auto column 0")
    elif indicator.attrib.get("Background") != "{StaticResource SuccessBrush}":
        errors.append("Floor Level footer must preserve SuccessBrush")

    if status is None:
        errors.append("Floor Level StatusText missing")
    else:
        if status.attrib.get("Grid.Column") != "1" or status.attrib.get("MinWidth") != "0":
            errors.append("Floor Level StatusText must be shrinkable in flexible column 1")
        if status.attrib.get("TextWrapping") != "Wrap":
            errors.append("Floor Level StatusText must preserve wrapping")

    if lifecycle is None:
        errors.append("Floor Level lifecycle boundary label missing")
    else:
        if lifecycle.attrib.get("Grid.Column") != "2" or lifecycle.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Floor Level lifecycle label must occupy right-aligned auto column 2")
        if lifecycle.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Floor Level lifecycle label must remain NoWrap")

for token in (
    'x:Name="FloorList"',
    'SelectionChanged="OnFloorSelectionChanged"',
    'Click="OnRefreshClick"',
    'Click="OnNewFloorClick"',
    'Click="OnSaveFloorFirstBootstrapClick"',
    'Click="OnDeleteFloorClick"',
    'Click="OnActivateClick"',
    'Click="OnAssignClick"',
    'Click="OnAssignBottomLevelClick"',
    'Click="OnAssignTopLevelClick"',
    'Click="OnClearVerticalLevelsClick"',
    'Click="OnInspectSelectionClick"',
    'QS3D KHÔNG tự Move/Translate source CAD.',
):
    if token not in text:
        errors.append("Floor Level workflow/lifecycle contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right" Text="NO CAD MOVE • STALE ON LEVEL CHANGE"'
if stale in text:
    errors.append("Floor Level footer still uses stale final-child right docking")

if errors:
    print("Floor Level responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Floor Level footer uses deterministic auto/star/auto layout while preserving level and no-CAD-move contracts."
)
