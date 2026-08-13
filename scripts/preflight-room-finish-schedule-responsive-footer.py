#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RoomFinishScheduleWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Room Finish Schedule XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("RoomFinishScheduleWindow.xaml is not well-formed: " + str(exc))
        root = None

status = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "RoomFinishScheduleStatusGrid":
            status = grid
            break

if status is None:
    errors.append("missing responsive Room Finish Schedule footer grid")
else:
    defs = status.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [
        item.attrib.get("Width")
        for item in list(defs)
        if item.tag == WPF + "ColumnDefinition"
    ]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("RoomFinishScheduleStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "ROOM FINISH SCHEDULE • EXPORT XLSX"), None)

    if indicator is None:
        errors.append("Room Finish footer indicator must remain in auto column 0")
    if status_text is None:
        errors.append("Room Finish StatusText missing")
    else:
        if status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
            errors.append("Room Finish StatusText must be shrinkable in flexible column 1")
        if status_text.attrib.get("TextWrapping") != "Wrap":
            errors.append("Room Finish StatusText must remain wrapping")
    if gate is None:
        errors.append("Room Finish schedule/export footer label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Room Finish footer label must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Room Finish footer label must remain NoWrap")

for token in (
    'x:Name="SearchBox"',
    'TextChanged="OnSearchChanged"',
    'Content="Làm mới" Click="OnRefreshClick"',
    'Content="Xuất Excel" Style="{StaticResource AccentButton}" Click="OnExportClick"',
    'x:Name="GroupCountText"',
    'x:Name="ElementCountText"',
    'x:Name="LengthText"',
    'x:Name="AreaText"',
    'x:Name="ScheduleGrid"',
    'AutoGenerateColumns="False"',
    'IsReadOnly="True"',
    'CanUserAddRows="False"',
):
    if token not in text:
        errors.append("Room Finish behavior/read-only contract missing: " + token)

expected_columns = (
    ("Tầng", "{Binding Floor}"),
    ("Phòng", "{Binding Room}"),
    ("Loại hoàn thiện", "{Binding Category}"),
    ("Family / Loại", "{Binding FamilyName}"),
    ("Vật liệu", "{Binding Material}"),
    ("Đơn vị", "{Binding UnitHint}"),
    ("SL", "{Binding Count}"),
    ("KL chính", "{Binding PrimaryQuantity, StringFormat=0.###}"),
    ("Dài", "{Binding LengthM, StringFormat=0.###}"),
    ("Diện tích", "{Binding AreaM2, StringFormat=0.###}"),
    ("Room IDs", "{Binding RoomIdsText}"),
)
if root is not None:
    schedule_grid = next((x for x in root.iter(WPF + "DataGrid") if x.attrib.get(XAML_NS + "Name") == "ScheduleGrid"), None)
    if schedule_grid is None:
        errors.append("Room Finish ScheduleGrid missing")
    else:
        columns_node = schedule_grid.find(WPF + "DataGrid.Columns")
        columns = [] if columns_node is None else [x for x in list(columns_node) if x.tag == WPF + "DataGridTextColumn"]
        actual_columns = tuple((x.attrib.get("Header"), x.attrib.get("Binding")) for x in columns)
        if actual_columns != expected_columns:
            errors.append("Room Finish DataGrid schema/bindings changed: " + repr(actual_columns))

stale = '<TextBlock DockPanel.Dock="Right" Text="ROOM FINISH SCHEDULE • EXPORT XLSX"'
if stale in text:
    errors.append("Room Finish footer still uses stale final-child right docking")

if errors:
    print("Room Finish Schedule responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Room Finish Schedule footer uses deterministic auto/star/auto layout while preserving read-only search/export/schema contracts."
)
