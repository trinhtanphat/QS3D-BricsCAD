#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DomainHubWindow.xaml"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
errors = []

if not XAML.is_file():
    errors.append("missing Domain Hub XAML: " + str(XAML.relative_to(ROOT)))
    root = None
    text = ""
else:
    text = XAML.read_text(encoding="utf-8")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        errors.append("DomainHubWindow.xaml is not well-formed: " + str(exc))
        root = None

status = None
if root is not None:
    for grid in root.iter(WPF + "Grid"):
        if grid.attrib.get(XAML_NS + "Name") == "DomainHubStatusGrid":
            status = grid
            break

if status is None:
    errors.append("missing responsive Domain Hub footer grid: DomainHubStatusGrid")
else:
    defs = status.find(WPF + "Grid.ColumnDefinitions")
    widths = [] if defs is None else [
        item.attrib.get("Width")
        for item in list(defs)
        if item.tag == WPF + "ColumnDefinition"
    ]
    if widths != ["Auto", "*", "Auto"]:
        errors.append("DomainHubStatusGrid must use ['Auto', '*', 'Auto']; got " + repr(widths))

    children = list(status)
    indicator = next((x for x in children if x.tag == WPF + "Border" and x.attrib.get("Grid.Column") == "0"), None)
    status_text = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get(XAML_NS + "Name") == "StatusText"), None)
    gate = next((x for x in children if x.tag == WPF + "TextBlock" and x.attrib.get("Text") == "3D native cần runtime gate V25 thật trước release."), None)

    if indicator is None:
        errors.append("Domain Hub footer success indicator must remain in auto column 0")
    if status_text is None:
        errors.append("Domain Hub StatusText missing")
    else:
        if status_text.attrib.get("Grid.Column") != "1" or status_text.attrib.get("MinWidth") != "0":
            errors.append("Domain Hub StatusText must be shrinkable in flexible column 1")
        if status_text.attrib.get("TextTrimming") != "CharacterEllipsis":
            errors.append("Domain Hub StatusText must retain CharacterEllipsis")
    if gate is None:
        errors.append("Domain Hub native runtime gate label missing")
    else:
        if gate.attrib.get("Grid.Column") != "2" or gate.attrib.get("HorizontalAlignment") != "Right":
            errors.append("Domain Hub runtime gate must occupy right-aligned auto column 2")
        if gate.attrib.get("TextWrapping") != "NoWrap":
            errors.append("Domain Hub runtime gate must remain NoWrap")

expected_tags = {
    "QS3DFAMILIES", "QS3DDRAWWALL", "QS3DDRAWGLASSWALL", "QS3DDRAWWALLPIER",
    "QS3DDRAWBEAM", "QS3DDRAWSTRUCTWALL", "QS3DDRAWCOLUMN", "QS3DDRAWSLAB",
    "QS3DDRAWFOUNDATION", "QS3DDRAWDOOR", "QS3DDRAWOPENING", "QS3DCUTSELECTEDOPENINGS",
    "QS3DWALL", "QS3DGLASSWALL", "QS3DCURTAIN", "QS3DCURTAIN3D",
    "QS3DCURTAINFRAMEHEALTH", "QS3DWALLPIER", "QS3DWALLJUNCTIONS", "QS3DWALLSNAPPREVIEW",
    "QS3DWALLSNAPAPPLY", "QS3DOPENING", "QS3DDOOR", "QS3DDOORSCHEDULE", "QS3DDOORXLSX",
    "QS3DAUTOLINKHOSTS", "QS3DLINKHOST", "QS3DCUTOPENINGS", "QS3DCUTOPENINGSCURVED", "QS3DFINISH",
    "QS3DROOMAUTO", "QS3DBEAM", "QS3DSLAB", "QS3DCOLUMN", "QS3DSTRUCTWALL", "QS3DFOUNDATION",
    "QS3DSTAIR", "QS3DRAILING", "QS3DEARTHWORK", "QS3DBUILD3D", "QS3DSCHEDULES", "QS3DSAVE",
    "QS3DTEMPLATEIMPORT", "QS3DTEMPLATEEXPORT", "QS3DAUDIT", "QS3DRECOGNIZE", "QS3DRECOGNIZEAUTO",
    "QS3DREBARMESHSETUP", "QS3DREBAR3D", "QS3DREBARTIES3D", "QS3DREBARTIEHEALTH",
    "QS3DBEAMREBAR3D", "QS3DREBARSTIRRUP3D", "QS3DREBARSTIRRUPHEALTH", "QS3DSLABREBAR3D",
    "QS3DSLABREBARHEALTH", "QS3DWALLREBAR3D", "QS3DWALLREBARHEALTH", "QS3DFOUNDATIONREBAR3D",
    "QS3DFOUNDATIONREBARHEALTH", "QS3DREBAR3DSHAPE", "QS3DREBARHEALTHALL", "QS3DREBARHEALTH",
    "QS3DREBARSHAPEHEALTH", "QS3DBBSVIEW", "QS3DBBS", "QS3DBBSCSV", "QS3DBQ", "QS3DSECTIONBOX",
    "QS3DSECTIONPLANE", "QS3DCLIPDISPLAY", "QS3DREVBASE", "QS3DREVDIFF", "QS3DRELEASECHECK",
    "QS3DHEALTHALL", "QS3DHEALTH", "QS3DRUNTIMECHECK", "QS3DSUPPORTBUNDLE",
}
if root is not None:
    buttons = [button for button in root.iter(WPF + "Button") if button.attrib.get("Tag")]
    actual_tags = {button.attrib.get("Tag") for button in buttons}
    if len(buttons) != 81:
        errors.append("Domain Hub command surface count changed; expected 81 tagged buttons, got " + str(len(buttons)))
    if actual_tags != expected_tags:
        errors.append(
            "Domain Hub command Tag set changed; missing="
            + repr(sorted(expected_tags - actual_tags))
            + ", unexpected="
            + repr(sorted(actual_tags - expected_tags))
        )
    bad_handlers = [button.attrib.get("Tag") for button in buttons if button.attrib.get("Click") != "OnCommandClick"]
    if bad_handlers:
        errors.append("Domain Hub commands lost OnCommandClick wiring: " + repr(sorted(bad_handlers)))

for token in (
    'Text="WORKFLOW HUB"',
    'Text="PROFESSIONAL CAD WORKSPACE"',
    'Text="Lệnh được gửi sang BricsCAD V25"',
    'Text="KIỂM TRA / RELEASE"',
    'Text="Chọn một thao tác để gửi lệnh sang BricsCAD."',
    'Text="3D native cần runtime gate V25 thật trước release."',
):
    if token not in text:
        errors.append("Domain Hub workflow/runtime contract missing: " + token)

stale = '<TextBlock DockPanel.Dock="Right"\n                           Text="3D native cần runtime gate V25 thật trước release."'
if stale in text or 'DockPanel.Dock="Right"' in text:
    errors.append("Domain Hub footer still uses stale final-child right docking")

if errors:
    print("Domain Hub responsive footer preflight FAILED:")
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print(
    "PASS: Domain Hub footer uses deterministic auto/star/auto layout while preserving all command and runtime-gate contracts."
)
