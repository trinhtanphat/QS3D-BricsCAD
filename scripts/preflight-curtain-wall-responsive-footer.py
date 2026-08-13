#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CurtainWallWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Curtain Wall XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("CurtainWallWindow.xaml is not well-formed: " + str(exc))
        root = None

status = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "CurtainWallStatusGrid":
            status = grid
            break

if status is None:
    errors.append("missing responsive footer grid: CurtainWallStatusGrid")
else:
    defs = status.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("CurtainWallStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "CURVE FRAME = V25 GATE"), None)

    if indicator is None:
        errors.append("Curtain Wall footer indicator must remain in auto column 0")
    elif indicator.attrib.get("Background") != "{StaticResource WarningBrush}":
        errors.append("Curtain Wall footer indicator must preserve WarningBrush")

    if status_text is None:
        errors.append("Curtain Wall StatusText missing")
    else:
        if status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
            errors.append("Curtain Wall StatusText must be shrinkable in flexible column 1")
        if status_text.attrib.get("TextWrapping") != "Wrap":
            errors.append("Curtain Wall StatusText must preserve wrapping")

    if gate is None:
        errors.append("Curtain Wall V25 gate label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Curtain Wall gate label must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Curtain Wall gate label must remain NoWrap")

for token in (
    'Click="OnRefreshClick"',
    'Click="OnSaveClick"',
    'Click="OnRecalculateClick"',
    'Tag="QS3DGLASSWALL"',
    'Tag="QS3DBUILD3D"',
    'Tag="QS3DCURTAINFRAMES3D"',
    'Tag="QS3DCURTAINFRAMEHEALTH"',
    'Tag="QS3DCUTOPENINGSCURVED"',
    'Tag="QS3DCURTAINXLSX"',
    'Click="OnCommandClick"',
    'Text="Khung 3D hiện hỗ trợ GlassWall semantic có source LINE nằm ngang.',
):
    if token not in text:
        errors.append("Curtain Wall workflow/gate contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right" Text="CURVE FRAME = V25 GATE"'
if stale in text:
    errors.append("Curtain Wall footer still uses stale final-child right docking")

if errors:
    print("Curtain Wall responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Curtain Wall footer uses deterministic auto/star/auto layout while preserving workflow and V25 gate contracts."
)
