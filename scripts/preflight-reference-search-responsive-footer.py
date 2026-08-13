#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ReferenceSearchWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Reference Search XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("ReferenceSearchWindow.xaml is not well-formed: " + str(exc))
        root = None

status = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "ReferenceSearchStatusGrid":
            status = grid
            break

if status is None:
    errors.append("missing responsive footer grid: ReferenceSearchStatusGrid")
else:
    defs = status.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [x.attrib.get("Width") for x in list(defs) if x.tag == WPF + "ColumnDefinition"]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("ReferenceSearchStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "DOCUMENT-BOUND • HTTPS • SAFESEARCH"), None)

    if indicator is None:
        errors.append("Reference Search footer indicator must remain in auto column 0")
    if status_text is None:
        errors.append("Reference Search StatusText missing")
    else:
        if status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
            errors.append("Reference Search StatusText must be shrinkable in flexible column 1")
        if status_text.attrib.get("TextWrapping") != "Wrap":
            errors.append("Reference Search StatusText must remain wrapping")
    if gate is None:
        errors.append("Reference Search safe-launch gate label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Reference Search gate label must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Reference Search gate label must remain NoWrap")

for token in (
    'x:Name="QueryBox"',
    'KeyDown="OnQueryKeyDown"',
    'x:Name="TechnicalContextCheck"',
    'Tag="images"',
    'Tag="web"',
    'Tag="video"',
    'Tag="shopping"',
    'Tag="shorts"',
    'Tag="news"',
    'Click="OnSearchClick"',
    'Click="OnQuickQueryClick"',
    'Text="QS3D chỉ tạo URL HTTPS cố định, mã hóa từ khóa và mở trình duyệt hệ thống. Không tải/scrape HTML kết quả vào plugin."',
):
    if token not in text:
        errors.append("Reference Search behavior/safety contract missing: " + token)

quick_tags = (
    "Ván khuôn móng",
    "Cốt thép móng",
    "Chi tiết dầm",
    "Chi tiết sàn",
    "Cấu tạo tường",
    "Mặt cắt móng",
)
for tag in quick_tags:
    if ('Tag="' + tag + '"') not in text:
        errors.append("Reference Search quick query missing: " + tag)

if text.count('Click="OnQuickQueryClick"') != len(quick_tags):
    errors.append("Reference Search quick-query handler count changed")
if text.count('Click="OnSearchClick"') != 7:
    errors.append("Reference Search category/primary search handler count changed; expected 7")

stale = '<TextBlock DockPanel.Dock="Right" Text="DOCUMENT-BOUND • HTTPS • SAFESEARCH"'
if stale in text:
    errors.append("Reference Search footer still uses stale final-child right docking")

if errors:
    print("Reference Search responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Reference Search footer uses deterministic auto/star/auto layout while preserving guarded query/search contracts."
)
