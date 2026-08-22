#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RecognitionWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Recognition XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("RecognitionWindow.xaml is not well-formed: " + str(exc))
        root = None

status_grid = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "RecognitionStatusGrid":
            status_grid = grid
            break

if status_grid is None:
    errors.append("missing responsive footer grid: RecognitionStatusGrid")
else:
    defs = status_grid.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("RecognitionStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status_grid)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "Status"), None)
    review = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "LOW CONFIDENCE = REVIEW"), None)

    if indicator is None:
        errors.append("Recognition warning indicator must remain in auto column 0")
    elif indicator.attrib.get("Background") != "{StaticResource WarningBrush}":
        errors.append("Recognition footer must preserve WarningBrush")

    if status is None:
        errors.append("Recognition Status missing")
    else:
        if status.attrib.get("Grid.Column") != "1" or status.attrib.get("MinWidth") != "0":
            errors.append("Recognition Status must be shrinkable in flexible column 1")
        if status.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Recognition Status must preserve CharacterEllipsis trimming")

    if review is None:
        errors.append("Recognition low-confidence review label missing")
    else:
        if review.attrib.get("Grid.Column") != "2" or review.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Recognition review label must occupy right-aligned auto column 2")
        if review.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Recognition review label must remain NoWrap")

for token in (
    'x:Name="Grid"',
    'SelectionMode="Extended"',
    'MouseDoubleClick="OnGridDoubleClick"',
    'Click="OnLocateClick"',
    'Click="OnApplyClick"',
    'Click="OnApplyConfidentClick"',
    'Header="Độ tin cậy"',
    'Binding="{Binding RequiresReview}"',
    'Text="REVIEW GATED"',
):
    if token not in text:
        errors.append("Recognition workflow/review contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right" Text="LOW CONFIDENCE = REVIEW"'
if stale in text:
    errors.append("Recognition footer still uses stale final-child right docking")

if errors:
    print("Recognition responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Recognition footer uses deterministic auto/star/auto layout while preserving review-gated recognition contracts."
)
