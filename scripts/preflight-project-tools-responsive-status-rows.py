#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ProjectToolsWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Project Tools XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("ProjectToolsWindow.xaml is not well-formed: " + str(exc))
        root = None


def named_grid(name):
    if root is None:
        return None
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == name:
            return grid
    errors.append("missing responsive Project Tools grid: " + name)
    return None


def grid_widths(grid):
    defs = None if grid is None else grid.find(WPF + "Grid.ColumnDefinitions")
    return [] if defs is None else [
        item.attrib.get("Width")
        for item in list(defs)
        if item.tag == WPF + "ColumnDefinition"
    ]


snapshot = named_grid("ProjectSnapshotHeaderGrid")
readiness = named_grid("ProjectReadinessHeaderGrid")
status = named_grid("ProjectToolsStatusGrid")

if snapshot is not None:
    widths = grid_widths(snapshot)
    if widths != ["*", "Auto"]:
        errors.append("ProjectSnapshotHeaderGrid must use ['*', 'Auto']; got " + repr(widths))
    labels = [x for x in list(snapshot) if x.tag == WPF + "TextBlock"]
    title = next((x for x in labels if x.attrib.get("Text") == "PROJECT SNAPSHOT"), None)
    gate = next((x for x in labels if x.attrib.get("Text") == "LIVE • READ-ONLY"), None)
    if title is None or title.attrib.get("Grid.Column", "0") != "0" or title.attrib.get("MinWidth") != "0":
        errors.append("Project snapshot title must be shrinkable in flexible column 0")
    elif title.attrib.get("TextTrimming") != "CharacterEllipsis" or title.attrib.get("TextWrapping") != "NoWrap":
        errors.append("Project snapshot title must use NoWrap + CharacterEllipsis")
    if gate is None or gate.attrib.get("Grid.Column") != "1" or gate.attrib.get("HorizontalAlignment") != "Right":
        errors.append("LIVE • READ-ONLY must occupy right-aligned auto column 1")
    elif gate.attrib.get("TextWrapping") != "NoWrap":
        errors.append("LIVE • READ-ONLY must remain NoWrap")

if readiness is not None:
    widths = grid_widths(readiness)
    if widths != ["*", "Auto"]:
        errors.append("ProjectReadinessHeaderGrid must use ['*', 'Auto']; got " + repr(widths))
    children = list(readiness)
    body = next((x for x in children if x.tag == WPF + "StackPanel" and x.attrib.get("Grid.Column", "0") == "0"), None)
    badge = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "1"), None)
    if body is None or body.attrib.get("MinWidth") != "0":
        errors.append("Project readiness body must be shrinkable in flexible column 0")
    else:
        readiness_text = next((x for x in body.iter(WPF + "TextBlock") if x.attrib.get(XAML_NS + "Name") == "ReadinessText"), None)
        if readiness_text is None or readiness_text.attrib.get("TextWrapping") != "Wrap":
            errors.append("ReadinessText must remain present and wrapping")
    if badge is None or badge.attrib.get("HorizontalAlignment") != "Right":
        errors.append("Project readiness badge must occupy right-aligned auto column 1")
    else:
        badge_text = next((x for x in badge.iter(WPF + "TextBlock") if x.attrib.get(XAML_NS + "Name") == "ReadinessBadgeText"), None)
        if badge_text is None or badge_text.attrib.get("TextWrapping") != "NoWrap":
            errors.append("ReadinessBadgeText must remain present and NoWrap")

if status is not None:
    widths = grid_widths(status)
    if widths != ["Auto", "*", "Auto"]:
        errors.append("ProjectToolsStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))
    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "PROJECT-SAFE • READ-ONLY SNAPSHOT • DWG CONTEXT LOCK"), None)
    if indicator is None:
        errors.append("Project Tools footer indicator must remain in auto column 0")
    if status_text is None or status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
        errors.append("Project Tools StatusText must be shrinkable in flexible column 1")
    elif status_text.attrib.get("TextWrapping") != "Wrap":
        errors.append("Project Tools StatusText must remain wrapping")
    if gate is None or gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
        errors.append("Project Tools footer gate must occupy right-aligned auto column 2")
    elif gate.attrib.get("TextWrapping") != "NoWrap":
        errors.append("Project Tools footer gate must remain NoWrap")

expected_tags = {
    "QS3DREFRESH",
    "QS3DLEVELS",
    "QS3DZONES",
    "QS3DFAMILIES",
    "QS3DMATERIALS",
    "QS3DSYNCSOURCE",
    "QS3DUNITS",
    "QS3DINTERCHANGEJSON",
    "QS3DINTERCHANGEVALIDATE",
    "QS3DINTERCHANGEIMPORT",
    "QS3DINTERCHANGEFIELDMERGE",
    "QS3DINTERCHANGEAPPEND",
    "QS3DINTERCHANGEUSESOURCEALL",
    "QS3DINTERCHANGEUSESOURCE",
    "QS3DINTERCHANGEUSESOURCECATALOG",
    "QS3DINTERCHANGEPROVENANCE",
    "QS3DINTERCHANGEREMAPPLAN",
    "QS3DINTERCHANGEREMAPAPPEND",
    "QS3DMATERIALXLSX",
    "QS3DTEMPLATEEXPORT",
    "QS3DTEMPLATEIMPORT",
    "QS3DSAVE",
    "QS3DRELOAD",
    "QS3DSCHEDULES",
    "QS3DCURTAIN",
    "QS3DGEOMETRYEXT",
    "QS3DREBARHUB",
    "QS3DBQ",
    "QS3DGRID",
    "QS3DGRIDNUMBER",
    "QS3DGRIDANNOTATE",
    "QS3DGRIDANNOTATEALL",
    "QS3DGRIDINTERSECTIONS",
    "QS3DGRIDNUMBERAUTO",
    "QS3DGRIDSYSTEMPREVIEW",
    "QS3DREGEN",
    "QS3DHEALTHALL",
    "QS3DRUNTIMECHECK",
    "QS3DAUDIT",
    "QS3D",
}
if root is not None:
    buttons = [button for button in root.iter(WPF + "Button") if button.attrib.get("Tag")]
    actual_tags = {button.attrib.get("Tag") for button in buttons}
    if actual_tags != expected_tags:
        errors.append(
            "Project Tools command Tag set changed; missing="
            + repr(sorted(expected_tags - actual_tags))
            + ", unexpected="
            + repr(sorted(actual_tags - expected_tags))
        )
    bad_handlers = [button.attrib.get("Tag") for button in buttons if button.attrib.get("Click") != "OnCommandClick"]
    if bad_handlers:
        errors.append("Project Tools commands lost OnCommandClick wiring: " + repr(sorted(bad_handlers)))

for token in (
    'x:Name="ProjectNameText"',
    'x:Name="ZoneText"',
    'x:Name="FloorText"',
    'x:Name="UnitText"',
    'x:Name="ReadinessText"',
    'x:Name="ReadinessBadgeText"',
    'x:Name="DirtyCountText"',
    'x:Name="GeometryDirtyCountText"',
    'x:Name="QuantityDirtyCountText"',
    'x:Name="ChangeVersionText"',
    'x:Name="UpdatedText"',
    'mở/refresh Project Tools không tạo project, không regenerate và không thay đổi dirty state.',
):
    if token not in text:
        errors.append("Project Tools read-only snapshot/readiness contract missing: " + token)

for stale in (
    '<TextBlock DockPanel.Dock="Right" Text="LIVE • READ-ONLY"',
    '<Border DockPanel.Dock="Right" Background="{StaticResource AccentSoftBrush}"',
    '<TextBlock DockPanel.Dock="Right" Text="PROJECT-SAFE • READ-ONLY SNAPSHOT • DWG CONTEXT LOCK"',
):
    if stale in text:
        errors.append("Project Tools still uses stale final-child right docking: " + stale)

if errors:
    print("Project Tools responsive status-row preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Project Tools uses deterministic responsive snapshot/readiness/footer grids while preserving read-only command/state contracts."
)
