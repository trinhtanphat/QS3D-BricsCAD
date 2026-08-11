#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
THEME = UI / "Theme.xaml"
WORKSPACE = UI / "WorkspacePanel.xaml"
RIGHT = UI / "RightPanel.xaml"

PRESENTATION_NS = "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
XAML_NS = "http://schemas.microsoft.com/winfx/2006/xaml"
X_KEY = "{" + XAML_NS + "}Key"

errors = []

surfaces = [WORKSPACE, RIGHT] + sorted(UI.glob("*Window.xaml"), key=lambda p: p.name.lower())

for path in surfaces:
    if not path.is_file():
        errors.append("missing dark-host UI surface: " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    if 'ResourceDictionary Source="Theme.xaml"' not in text:
        errors.append(str(path.relative_to(ROOT)) + " must merge Theme.xaml")

    forbidden = (
        (r'Foreground\s*=\s*"Black"', 'Foreground="Black"'),
        (r'Foreground\s*=\s*"#000000"', 'Foreground="#000000"'),
        (r'Foreground\s*=\s*"#FF000000"', 'Foreground="#FF000000"'),
        (r'Background\s*=\s*"#17191C"', 'Background="#17191C"'),
    )
    for pattern, label in forbidden:
        if re.search(pattern, text, flags=re.IGNORECASE):
            errors.append(str(path.relative_to(ROOT)) + " contains dark-host-risk styling: " + label)

    try:
        ET.parse(path)
    except ET.ParseError as exc:
        errors.append(str(path.relative_to(ROOT)) + " is not well-formed XAML/XML: " + str(exc))


def guard_keyed_textblock_styles(path):
    if not path.is_file():
        return
    try:
        tree = ET.parse(path)
    except ET.ParseError:
        return
    for style in tree.getroot().iter("{" + PRESENTATION_NS + "}Style"):
        key = style.attrib.get(X_KEY)
        target = style.attrib.get("TargetType", "")
        based_on = style.attrib.get("BasedOn")
        if not key or "TextBlock" not in target or based_on:
            continue
        has_foreground = any(
            setter.attrib.get("Property") == "Foreground"
            for setter in style.findall("{" + PRESENTATION_NS + "}Setter")
        )
        if not has_foreground:
            errors.append(
                str(path.relative_to(ROOT)) + ": keyed TextBlock style '" + key +
                "' must set Foreground explicitly or use BasedOn; keyed styles do not inherit the implicit TextBlock style"
            )


for path in (THEME, WORKSPACE, RIGHT):
    guard_keyed_textblock_styles(path)

if THEME.is_file():
    theme = THEME.read_text(encoding="utf-8")
    panel_title_start = theme.find('<Style x:Key="PanelTitle" TargetType="TextBlock">')
    panel_title_end = theme.find("</Style>", panel_title_start)
    if panel_title_start < 0 or panel_title_end < 0:
        errors.append("Theme.xaml missing keyed PanelTitle style")
    else:
        panel_title = theme[panel_title_start:panel_title_end]
        if '<Setter Property="Foreground" Value="{StaticResource TextBrush}"/>' not in panel_title:
            errors.append("Theme.xaml PanelTitle must explicitly use TextBrush to prevent BricsCAD host foreground leakage")
else:
    errors.append("missing shared Theme.xaml")

if WORKSPACE.is_file():
    workspace = WORKSPACE.read_text(encoding="utf-8")
    selected_heading = 'Text="ĐỐI TƯỢNG ĐANG CHỌN"'
    heading_pos = workspace.find(selected_heading)
    if heading_pos < 0:
        errors.append("WorkspacePanel.xaml missing selected-object heading")
    else:
        heading_slice = workspace[heading_pos:heading_pos + 220]
        if 'Style="{StaticResource PanelTitle}"' not in heading_slice:
            errors.append("Workspace selected-object heading must use PanelTitle so its foreground stays explicit on the dark BricsCAD host")

print("QS3D dark-host contrast preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: Workspace, Right Panel and modeless windows merge the shared theme, reject explicit black/legacy dark-host styling, "
    "and shared/palette keyed TextBlock styles keep an explicit foreground contract."
)
