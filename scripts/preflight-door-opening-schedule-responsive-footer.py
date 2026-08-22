#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DoorOpeningScheduleWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Door/Opening Schedule XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("DoorOpeningScheduleWindow.xaml is not well-formed: " + str(exc))
        root = None

status = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "DoorOpeningScheduleStatusGrid":
            status = grid
            break

if status is None:
    errors.append("missing responsive Door/Opening Schedule footer grid")
else:
    defs = status.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [
        item.attrib.get("Width")
        for item in list(defs)
        if item.tag == WPF + "ColumnDefinition"
    ]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("DoorOpeningScheduleStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "READ-ONLY SCHEDULE • EXPORT XLSX"), None)

    if indicator is None:
        errors.append("Door/Opening Schedule footer indicator must remain in auto column 0")
    if status_text is None:
        errors.append("Door/Opening Schedule StatusText missing")
    else:
        if status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
            errors.append("Door/Opening Schedule StatusText must be shrinkable in flexible column 1")
        if status_text.attrib.get("TextWrapping") != "Wrap":
            errors.append("Door/Opening Schedule StatusText must remain wrapping")
    if gate is None:
        errors.append("Door/Opening Schedule read-only/export footer label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Door/Opening Schedule footer label must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Door/Opening Schedule footer label must remain NoWrap")

for token in (
    'x:Name="SearchBox"',
    'TextChanged="OnSearchChanged"',
    'Content="Làm mới" Click="OnRefreshClick"',
    'Content="Xuất Excel" Style="{StaticResource AccentButton}"',
    'Click="OnExportClick"',
    'x:Name="GroupCountText"',
    'x:Name="ElementCountText"',
    'x:Name="AreaText"',
    'x:Name="HostCountText"',
    'x:Name="ScheduleGrid"',
    'AutoGenerateColumns="False"',
    'IsReadOnly="True"',
    'CanUserAddRows="False"',
):
    if token not in text:
        errors.append("Door/Opening Schedule behavior/read-only contract missing: " + token)

expected_columns = (
    ("Tầng", "{Binding Floor}"),
    ("Loại", "{Binding Category}"),
    ("Family / Loại", "{Binding FamilyName}"),
    ("Vật liệu", "{Binding Material}"),
    ("Rộng", "{Binding WidthM, StringFormat=0.###}"),
    ("Cao", "{Binding HeightM, StringFormat=0.###}"),
    ("Cao bậu", "{Binding SillHeightM, StringFormat=0.###}"),
    ("Dày", "{Binding ThicknessM, StringFormat=0.###}"),
    ("SL", "{Binding Count}"),
    ("DT mở", "{Binding OpeningAreaM2, StringFormat=0.###}"),
    ("SL host", "{Binding HostCount}"),
    ("Host IDs", "{Binding HostIdsText}"),
)
if root is not None:
    schedule_grid = next((x for x in root.iter(WPF + "DataGrid") if x.attrib.get(XAML_NS + "Name") == "ScheduleGrid"), None)
    if schedule_grid is None:
        errors.append("Door/Opening Schedule ScheduleGrid missing")
    else:
        columns_node = schedule_grid.find(WPF + "DataGrid.Columns")
        columns = [] if columns_node is None else [x for x in list(columns_node) if x.tag == WPF + "DataGridTextColumn"]
        actual_columns = tuple((x.attrib.get("Header"), x.attrib.get("Binding")) for x in columns)
        if actual_columns != expected_columns:
            errors.append("Door/Opening Schedule DataGrid schema/bindings changed: " + repr(actual_columns))

stale = '<TextBlock DockPanel.Dock="Right" Text="READ-ONLY SCHEDULE • EXPORT XLSX"'
if stale in text:
    errors.append("Door/Opening Schedule footer still uses stale final-child right docking")

if errors:
    print("Door/Opening Schedule responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Door/Opening Schedule footer uses deterministic auto/star/auto layout while preserving read-only schedule/search/export contracts."
)
