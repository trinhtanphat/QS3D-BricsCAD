#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "MaterialCatalogWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Material Catalog XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("MaterialCatalogWindow.xaml is not well-formed: " + str(exc))
        root = None

status_grid = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "MaterialCatalogStatusGrid":
            status_grid = grid
            break

if status_grid is None:
    errors.append("missing responsive footer grid: MaterialCatalogStatusGrid")
else:
    defs = status_grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("MaterialCatalogStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status_grid)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "AMBIGUOUS HANDLE = FAIL CLOSED"), None)

    if indicator is None:
        errors.append("Material Catalog success indicator must remain in auto column 0")
    elif indicator.attrib.get("Background") != "{StaticResource SuccessBrush}":
        errors.append("Material Catalog footer must preserve SuccessBrush")

    if status is None:
        errors.append("Material Catalog StatusText missing")
    else:
        if status.attrib.get("Grid.Column") != "1" or status.attrib.get("MinWidth") != "0":
            errors.append("Material Catalog StatusText must be shrinkable in flexible column 1")
        if status.attrib.get("TextWrapping") != "Wrap":
            errors.append("Material Catalog StatusText must preserve wrapping")

    if gate is None:
        errors.append("Material Catalog fail-closed ambiguity label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Material Catalog fail-closed label must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Material Catalog fail-closed label must remain NoWrap")

for token in (
    'x:Name="MaterialList"',
    'SelectionChanged="OnMaterialSelectionChanged"',
    'Click="OnExportClick"',
    'Click="OnRefreshClick"',
    'Click="OnNewClick"',
    'Click="OnSaveClick"',
    'Click="OnDeleteClick"',
    'Click="OnApplyClick"',
    'Tag="CurtainFrameMaterial"',
    'Nếu một selected handle bị nhiều semantic element claim, QS3D sẽ từ chối thay vì sửa mơ hồ.',
):
    if token not in text:
        errors.append("Material Catalog workflow/fail-closed contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right" Text="AMBIGUOUS HANDLE = FAIL CLOSED"'
if stale in text:
    errors.append("Material Catalog footer still uses stale final-child right docking")

if errors:
    print("Material Catalog responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Material Catalog footer uses deterministic auto/star/auto layout while preserving material and fail-closed contracts."
)
