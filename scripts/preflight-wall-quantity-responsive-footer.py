#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WallQuantityWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Wall Quantity XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("WallQuantityWindow.xaml is not well-formed: " + str(exc))
        root = None

status_grid = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "WallQuantityStatusGrid":
            status_grid = grid
            break

if status_grid is None:
    errors.append("missing responsive status grid: WallQuantityStatusGrid")
else:
    defs = status_grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*"]:
        errors.append("WallQuantityStatusGrid must use ['Auto', '*']; got " + repr(widths))
    if status_grid.attrib.get("MinWidth") != "0":
        errors.append("WallQuantityStatusGrid must be shrinkable with MinWidth=0")

    children = list(status_grid)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)

    if indicator is None:
        errors.append("Wall Quantity status indicator must remain in auto column 0")
    elif indicator.attrib.get("Background") != "{StaticResource SuccessBrush}":
        errors.append("Wall Quantity status indicator must preserve SuccessBrush")

    if status is None:
        errors.append("Wall Quantity StatusText missing")
    else:
        if status.attrib.get("Grid.Column") != "1" or status.attrib.get("MinWidth") != "0":
            errors.append("Wall Quantity StatusText must be shrinkable in flexible column 1")
        if status.attrib.get("TextWrapping") != "Wrap":
            errors.append("Wall Quantity StatusText must preserve wrapping")

if '<StackPanel Orientation="Horizontal" VerticalAlignment="Center">\n                    <Border Background="{StaticResource SuccessBrush}"' in text:
    errors.append("Wall Quantity footer still measures StatusText in a horizontal StackPanel")

for token in (
    'x:Name="TotalCountText" Text="0"',
    'x:Name="TotalLengthText" Text="0 m"',
    'x:Name="TotalGrossText" Text="0 m³"',
    'x:Name="TotalDeductionText" Text="0 m³"',
    'x:Name="TotalNetText" Text="0 m³"',
    'x:Name="TotalFormworkText" Text="0 m²"',
    'x:Name="TakeoffGrid"',
    'x:Name="WallList"',
    'Click="OnLocateClick"',
    'Click="OnRefreshClick"',
    'Click="OnExportClick"',
):
    if token not in text:
        errors.append("Wall Quantity footer/workflow contract missing: " + token)

if errors:
    print("Wall Quantity responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Wall Quantity status uses a constrained auto/star grid while preserving totals and takeoff workflow contracts."
)
