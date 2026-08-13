#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ScheduleHubWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Schedule Hub XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("ScheduleHubWindow.xaml is not well-formed: " + str(exc))
        root = None

status = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "ScheduleHubStatusGrid":
            status = grid
            break

if status is None:
    errors.append("missing responsive footer grid: ScheduleHubStatusGrid")
else:
    defs = status.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("ScheduleHubStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    lock = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "SCHEDULE-SAFE • DWG CONTEXT LOCK"), None)

    if indicator is None:
        errors.append("Schedule Hub footer indicator must remain in auto column 0")
    elif indicator.attrib.get("Background") != "{StaticResource SuccessBrush}":
        errors.append("Schedule Hub footer indicator must preserve SuccessBrush")

    if status_text is None:
        errors.append("Schedule Hub StatusText missing")
    else:
        if status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
            errors.append("Schedule Hub StatusText must be shrinkable in flexible column 1")
        if status_text.attrib.get("TextWrapping") != "Wrap":
            errors.append("Schedule Hub StatusText must preserve wrapping")

    if lock is None:
        errors.append("Schedule Hub context-lock label missing")
    else:
        if lock.attrib.get("Grid.Column") != "2" or lock.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Schedule Hub context-lock label must occupy right-aligned auto column 2")
        if lock.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Schedule Hub context-lock label must remain NoWrap")

for token in (
    'Click="OnRefreshClick"',
    'Tag="QS3DBQ"',
    'Tag="QS3DFINISHSCHEDULE"',
    'Tag="QS3DMATERIALS"',
    'Tag="QS3DCURTAIN"',
    'Tag="QS3DDOORSCHEDULE"',
    'Tag="QS3DREBARHUB"',
    'Tag="QS3DBBSCSV"',
    'Click="OnCommandClick"',
    'Text="Native DWG Table có vòng đời Create/Refresh/Health/Remove riêng, dùng project-level QS3DDOC ownership.',
):
    if token not in text:
        errors.append("Schedule Hub behavior/context contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right" Text="SCHEDULE-SAFE • DWG CONTEXT LOCK"'
if stale in text:
    errors.append("Schedule Hub footer still uses stale final-child right docking")

if errors:
    print("Schedule Hub responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Schedule Hub footer uses deterministic auto/star/auto layout while preserving schedule/context-lock contracts."
)
