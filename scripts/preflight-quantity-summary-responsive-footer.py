#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "QuantitySummaryWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Quantity Summary XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("QuantitySummaryWindow.xaml is not well-formed: " + str(exc))
        root = None

status_grid = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "QuantitySummaryStatusGrid":
            status_grid = grid
            break

if status_grid is None:
    errors.append("missing responsive footer grid: QuantitySummaryStatusGrid")
else:
    defs = status_grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("QuantitySummaryStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status_grid)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    totals = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "TotalsText"), None)
    hint = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "BÁM 3D: CLICK → LOCATE • TẮT BÁM 3D: DOUBLE-CLICK / ĐỊNH VỊ • EXPORT XLSX"), None)

    if indicator is None:
        errors.append("Quantity Summary success indicator must remain in auto column 0")
    elif indicator.attrib.get("Background") != "{StaticResource SuccessBrush}":
        errors.append("Quantity Summary footer must preserve SuccessBrush")

    if totals is None:
        errors.append("Quantity Summary TotalsText missing")
    else:
        if totals.attrib.get("Grid.Column") != "1" or totals.attrib.get("MinWidth") != "0":
            errors.append("Quantity Summary TotalsText must be shrinkable in flexible column 1")
        if totals.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Quantity Summary TotalsText must use CharacterEllipsis when space is constrained")

    if hint is None:
        errors.append("Quantity Summary interaction/export hint missing")
    else:
        if hint.attrib.get("Grid.Column") != "2" or hint.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Quantity Summary hint must occupy right-aligned auto column 2")
        if hint.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Quantity Summary hint must remain NoWrap")

for token in (
    'x:Name="QuantityGrid"',
    'SelectionChanged="OnQuantityGridSelectionChanged"',
    'MouseDoubleClick="OnQuantityGridDoubleClick"',
    'x:Name="AutoRevealCheck"',
    'Click="OnLocateClick"',
    'Click="OnRecalculateClick"',
    'Click="OnEd2ExportClick"',
    'Click="OnExcelLocateClick"',
    'Click="OnExportClick"',
    'Checked="OnColumnVisibilityChanged"',
    'Unchecked="OnColumnVisibilityChanged"',
):
    if token not in text:
        errors.append("Quantity Summary takeoff/interaction contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right" Text="BÁM 3D: CLICK → LOCATE • TẮT BÁM 3D: DOUBLE-CLICK / ĐỊNH VỊ • EXPORT XLSX"'
if stale in text:
    errors.append("Quantity Summary footer still uses stale final-child right docking")

if errors:
    print("Quantity Summary responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Quantity Summary footer uses deterministic auto/star/auto layout while preserving takeoff, locate and export contracts."
)
