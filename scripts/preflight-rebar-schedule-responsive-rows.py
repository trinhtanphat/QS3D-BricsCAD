#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RebarScheduleWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Rebar Schedule XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("RebarScheduleWindow.xaml is not well-formed: " + str(exc))
        root = None


def named_grid(name):
    if root is None:
        return None
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == name:
            return grid
    errors.append("missing responsive Rebar Schedule grid: " + name)
    return None


def widths(grid):
    defs = None if grid is None else grid.find(WPF + "Grid.ColumnDefinitions")
    return [] if defs is None else [
        item.attrib.get("Width")
        for item in list(defs)
        if item.tag == WPF + "ColumnDefinition"
    ]


header = named_grid("RebarScheduleHeaderGrid")
status = named_grid("RebarScheduleStatusGrid")

if header is not None:
    if widths(header) != ["*", "Auto"]:
        errors.append("RebarScheduleHeaderGrid must use ['*', 'Auto']; got " + repr(widths(header)))
    children = list(header)
    body = next((x for x in children if x.tag == WPF + "StackPanel" and x.attrib.get("Grid.Column", "0") == "0"), None)
    actions = next((x for x in children if x.tag == WPF + "StackPanel" and x.attrib.get("Grid.Column") == "1"), None)
    if body is None or body.attrib.get("MinWidth") != "0":
        errors.append("Rebar Schedule header content must be shrinkable in flexible column 0")
    if actions is None or actions.attrib.get("HorizontalAlignment") != "Right":
        errors.append("Rebar Schedule actions must occupy right-aligned auto column 1")
    else:
        buttons = [x for x in actions if x.tag == WPF + "Button"]
        signature = tuple((x.attrib.get("Content"), x.attrib.get("Click")) for x in buttons)
        if signature != (("Locate", "OnLocateClick"), ("Xuất XLSX", "OnExportClick")):
            errors.append("Rebar Schedule header command wiring changed: " + repr(signature))

if status is not None:
    if widths(status) != ["Auto", "*", "Auto"]:
        errors.append("RebarScheduleStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths(status)))
    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    totals = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "Totals"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "PROVENANCE ≠ CODE COMPLIANCE • DOUBLE-CLICK TO LOCATE • EXPORT XLSX"), None)
    if indicator is None:
        errors.append("Rebar Schedule footer indicator must remain in auto column 0")
    if totals is None:
        errors.append("Rebar Schedule Totals binding target missing")
    else:
        if totals.attrib.get("Grid.Column") != "1" or totals.attrib.get("MinWidth") != "0":
            errors.append("Rebar Schedule Totals must be shrinkable in flexible column 1")
        if totals.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Rebar Schedule Totals must use CharacterEllipsis")
    if gate is None:
        errors.append("Rebar Schedule provenance/export label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Rebar Schedule footer label must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Rebar Schedule footer label must remain NoWrap")

for token in (
    'x:Name="Grid"',
    'AutoGenerateColumns="False"',
    'IsReadOnly="True"',
    'CanUserAddRows="False"',
    'MouseDoubleClick="OnGridDoubleClick"',
    'SelectionMode="Single"',
    'Text="BBS REVIEW"',
    'Review bar mark • notation • cutting length • weight • fabrication provenance trước khi xuất',
):
    if token not in text:
        errors.append("Rebar Schedule review/interaction contract missing: " + token)

expected_columns = (
    ("Element", "{Binding ElementId}"),
    ("Mark", "{Binding BarMark}"),
    ("Shape", "{Binding ShapeCode}"),
    ("Notation", "{Binding Notation}"),
    ("Ø (mm)", "{Binding DiameterMm, StringFormat=0.#}"),
    ("SL", "{Binding Quantity}"),
    ("L cắt (m)", "{Binding CuttingLengthM, StringFormat=0.###}"),
    ("ΣL (m)", "{Binding TotalLengthM, StringFormat=0.###}"),
    ("kg/m", "{Binding UnitWeightKgM, StringFormat=0.###}"),
    ("Net kg", "{Binding NetWeightKg, StringFormat=0.###}"),
    ("Waste %", "{Binding WastePercent, StringFormat=0.##}"),
    ("Σ kg", "{Binding TotalWeightKg, StringFormat=0.###}"),
    ("Fabrication", "{Binding FabricationStatus}"),
    ("Standard", "{Binding FabricationStandardCode}"),
    ("Detailing Rev.", "{Binding FabricationDetailingRevision}"),
)
if root is not None:
    data_grid = next((x for x in root.iter(WPF + "DataGrid") if x.attrib.get(XAML_NS + "Name") == "Grid"), None)
    if data_grid is None:
        errors.append("Rebar Schedule DataGrid missing")
    else:
        columns_node = data_grid.find(WPF + "DataGrid.Columns")
        columns = [] if columns_node is None else [x for x in list(columns_node) if x.tag == WPF + "DataGridTextColumn"]
        actual_columns = tuple((x.attrib.get("Header"), x.attrib.get("Binding")) for x in columns)
        if actual_columns != expected_columns:
            errors.append("Rebar Schedule DataGrid schema/bindings changed: " + repr(actual_columns))

for stale in (
    '<StackPanel DockPanel.Dock="Right" Orientation="Horizontal" VerticalAlignment="Center">',
    '<TextBlock DockPanel.Dock="Right" Text="PROVENANCE ≠ CODE COMPLIANCE • DOUBLE-CLICK TO LOCATE • EXPORT XLSX"',
):
    if stale in text:
        errors.append("Rebar Schedule still uses stale final-child right docking: " + stale)

if errors:
    print("Rebar Schedule responsive-row preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Rebar Schedule uses deterministic responsive header/footer grids while preserving BBS review, locate and export contracts."
)
