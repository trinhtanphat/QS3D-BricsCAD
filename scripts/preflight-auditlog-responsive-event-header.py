#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "AuditLogWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing AuditLog XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("AuditLogWindow.xaml is not well-formed: " + str(exc))
        root = None

header = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "AuditEventHeaderGrid":
            header = grid
            break
if header is None:
    errors.append("missing responsive event header grid: AuditEventHeaderGrid")
else:
    definitions = header.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if definitions is None else [
        item.attrib.get("Width")
        for item in list(definitions)
        if item.tag == WPF + "ColumnDefinition"
    ]
    if widths != ["*", "Auto"]:
        errors.append("AuditEventHeaderGrid must use deterministic columns ['*', 'Auto']; got " + repr(widths))

    children = [item for item in list(header) if item.tag == WPF + "TextBlock"]
    title = next((item for item in children if item.attrib.get("Text") == "DÒNG SỰ KIỆN"), None)
    status = next((item for item in children if item.attrib.get("Text") == "UTC • PROJECT AUDIT"), None)

    if title is None:
        errors.append("AuditEventHeaderGrid must retain DÒNG SỰ KIỆN title")
    else:
        if title.attrib.get("Grid.Column", "0") != "0":
            errors.append("event title must remain in flexible column 0")
        if title.attrib.get("MinWidth") != "0":
            errors.append("event title must use MinWidth=0")
        if title.attrib.get("TextWrapping") != "NoWrap":
            errors.append("event title must use TextWrapping=NoWrap")
        if title.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("event title must use CharacterEllipsis")

    if status is None:
        errors.append("AuditEventHeaderGrid must retain UTC • PROJECT AUDIT status")
    else:
        if status.attrib.get("Grid.Column") != "1":
            errors.append("audit status must occupy auto column 1")
        if status.attrib.get("HorizontalAlignment") != "Right":
            errors.append("audit status must remain right aligned")
        if status.attrib.get("TextWrapping") != "NoWrap":
            errors.append("audit status must use TextWrapping=NoWrap")

for token in (
    'x:Name="SearchBox"',
    'TextChanged="OnSearchChanged"',
    'x:Name="Grid"',
    'AutoGenerateColumns="False"',
    'IsReadOnly="True"',
    'Header="Thời gian UTC"',
    'Header="Hành động"',
    'Header="Element"',
    'Header="Nội dung"',
    'Header="Người thực hiện"',
    'Header="Correlation"',
    'x:Name="Summary"',
    'Text="MỚI NHẤT HIỂN THỊ TRƯỚC"',
):
    if token not in text:
        errors.append("AuditLog behavior/binding continuity contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right" Text="UTC • PROJECT AUDIT"'
if stale in text:
    errors.append("AuditLog event header still depends on last-child DockPanel right docking")

if errors:
    print("AuditLog responsive event-header preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: AuditLog event header uses a deterministic star/auto responsive grid, "
    "keeps the title shrinkable and preserves search/read-only audit-grid/footer behavior."
)
