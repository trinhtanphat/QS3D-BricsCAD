#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "GeometryExtensionsWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Geometry Extensions XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("GeometryExtensionsWindow.xaml is not well-formed: " + str(exc))
        root = None


def named_grid(name):
    if root is None:
        return None
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == name:
            return grid
    errors.append("missing responsive Geometry Extensions grid: " + name)
    return None


health = named_grid("RebarHealthHeaderGrid")
status = named_grid("GeometryStatusGrid")

if health is not None:
    defs = health.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["*", "Auto"]:
        errors.append("RebarHealthHeaderGrid must use ['*', 'Auto']; got " + repr(widths))
    labels = [x for x in list(health) if x.tag == WPF + "TextBlock"]
    title = next((x for x in labels if x.attrib.get("Text") == "REBAR HEALTH"), None)
    gate = next((x for x in labels if x.attrib.get("Text") == "FAIL-CLOSED"), None)
    if title is None:
        errors.append("Rebar Health title missing")
    else:
        if title.attrib.get("Grid.Column", "0") != "0" or title.attrib.get("MinWidth") != "0":
            errors.append("Rebar Health title must be shrinkable in flexible column 0")
        if title.attrib.get("TextWrapping") != "NoWrap" or title.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Rebar Health title must use NoWrap + CharacterEllipsis")
    if gate is None:
        errors.append("FAIL-CLOSED status missing")
    else:
        if gate.attrib.get("Grid.Column") != "1" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("FAIL-CLOSED must occupy the right-aligned auto column")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("FAIL-CLOSED must remain NoWrap")

if status is not None:
    defs = status.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("GeometryStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))
    children = list(status)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "PREVIEW / FINGERPRINT / HEALTH GATES"), None)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    if indicator is None:
        errors.append("Geometry footer warning indicator must remain in auto column 0")
    if status_text is None:
        errors.append("Geometry footer StatusText missing")
    else:
        if status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
            errors.append("Geometry footer StatusText must remain shrinkable in flexible column 1")
        if status_text.attrib.get("TextWrapping") != "Wrap":
            errors.append("Geometry footer StatusText must remain wrapping")
    if gate is None:
        errors.append("Geometry footer gate label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Geometry footer gate label must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Geometry footer gate label must remain NoWrap")

command_tags = (
    "QS3DWALLJUNCTIONS",
    "QS3DWALLSNAPPREVIEW",
    "QS3DWALLSNAPAPPLY",
    "QS3DAUTOLINKHOSTS",
    "QS3DCUTOPENINGS",
    "QS3DCUTOPENINGSCURVED",
    "QS3DREBAR3D",
    "QS3DREBARTIES3D",
    "QS3DBEAMREBAR3D",
    "QS3DREBARSTIRRUP3D",
    "QS3DSLABREBAR3D",
    "QS3DWALLREBAR3D",
    "QS3DREBAR3DSHAPE",
    "QS3DREBARHEALTH",
    "QS3DREBARSHAPEHEALTH",
    "QS3DREBARTIEHEALTH",
    "QS3DREBARSTIRRUPHEALTH",
    "QS3DSLABREBARHEALTH",
    "QS3DWALLREBARHEALTH",
    "QS3DREBARHEALTHALL",
)
for tag in command_tags:
    token = 'Tag="' + tag + '"'
    if token not in text:
        errors.append("Geometry Extensions command Tag missing: " + tag)

if text.count('Click="OnCommandClick"') != len(command_tags):
    errors.append("Geometry Extensions command handler count changed; expected " + str(len(command_tags)))

for stale in (
    '<TextBlock DockPanel.Dock="Right" Text="FAIL-CLOSED"',
    '<TextBlock DockPanel.Dock="Right" Text="PREVIEW / FINGERPRINT / HEALTH GATES"',
):
    if stale in text:
        errors.append("Geometry Extensions still uses stale final-child right docking: " + stale)

if errors:
    print("Geometry Extensions responsive status-row preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Geometry Extensions uses deterministic responsive health/footer grids and preserves all command/gate contracts."
)
