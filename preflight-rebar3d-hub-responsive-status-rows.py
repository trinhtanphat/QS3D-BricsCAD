#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "Rebar3DHubWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Rebar 3D Hub XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("Rebar3DHubWindow.xaml is not well-formed: " + str(exc))
        root = None


def named_grid(name):
    if root is None:
        return None
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == name:
            return grid
    errors.append("missing responsive Rebar 3D Hub grid: " + name)
    return None


health = named_grid("RebarHubHealthHeaderGrid")
status = named_grid("RebarHubStatusGrid")

if health is not None:
    defs = health.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["*", "Auto"]:
        errors.append("RebarHubHealthHeaderGrid must use ['*', 'Auto']; got " + repr(widths))
    labels = [x for x in list(health) if x.tag == WPF + "TextBlock"]
    title = next((x for x in labels if x.attrib.get("Text") == "HEALTH"), None)
    gate = next((x for x in labels if x.attrib.get("Text") == "FAIL-CLOSED"), None)
    if title is None:
        errors.append("Rebar Hub HEALTH title missing")
    else:
        if title.attrib.get("Grid.Column", "0") != "0" or title.attrib.get("MinWidth") != "0":
            errors.append("Rebar Hub HEALTH title must be shrinkable in flexible column 0")
        if title.attrib.get("TextWrapping") != "NoWrap" or title.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Rebar Hub HEALTH title must use NoWrap + CharacterEllipsis")
    if gate is None:
        errors.append("Rebar Hub FAIL-CLOSED status missing")
    else:
        if gate.attrib.get("Grid.Column") != "1" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Rebar Hub FAIL-CLOSED must occupy right-aligned auto column 1")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Rebar Hub FAIL-CLOSED must remain NoWrap")

if status is not None:
    defs = status.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("RebarHubStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))
    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "EXPLICIT REBAR INPUTS • NATIVE 3D"), None)
    if indicator is None:
        errors.append("Rebar Hub footer warning indicator must remain in auto column 0")
    if status_text is None:
        errors.append("Rebar Hub StatusText missing")
    else:
        if status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
            errors.append("Rebar Hub StatusText must be shrinkable in flexible column 1")
        if status_text.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Rebar Hub StatusText must retain CharacterEllipsis")
    if gate is None:
        errors.append("Rebar Hub explicit-input footer label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Rebar Hub footer label must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Rebar Hub footer label must remain NoWrap")

command_tags = (
    "QS3DREBAR3D",
    "QS3DREBARTIES3D",
    "QS3DBEAMREBAR3D",
    "QS3DREBARSTIRRUP3D",
    "QS3DREBARMESHSETUP",
    "QS3DSLABREBAR3D",
    "QS3DWALLREBAR3D",
    "QS3DFOUNDATIONREBAR3D",
    "QS3DREBAR3DSHAPE",
    "QS3DBBSVIEW",
    "QS3DBBS",
    "QS3DBBSCSV",
    "QS3DHEALTHALL",
    "QS3DREBARHEALTHALL",
    "QS3DFOUNDATIONREBARHEALTH",
    "QS3DREBARTIEHEALTH",
    "QS3DREBARSTIRRUPHEALTH",
    "QS3DREBARSHAPEHEALTH",
)
for tag in command_tags:
    if ('Tag="' + tag + '"') not in text:
        errors.append("Rebar 3D Hub command Tag missing: " + tag)
if text.count('Click="OnCommandClick"') != len(command_tags):
    errors.append("Rebar 3D Hub command handler count changed; expected " + str(len(command_tags)))

for stale in (
    '<TextBlock DockPanel.Dock="Right" Text="FAIL-CLOSED"',
    '<TextBlock DockPanel.Dock="Right" Text="EXPLICIT REBAR INPUTS • NATIVE 3D"',
):
    if stale in text:
        errors.append("Rebar 3D Hub still uses stale final-child right docking: " + stale)

if errors:
    print("Rebar 3D Hub responsive status-row preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Rebar 3D Hub uses deterministic responsive health/footer grids while preserving all rebar command contracts."
)
