#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
THEME = UI / "Theme.xaml"
XAML_NS = "http://schemas.microsoft.com/winfx/2006/xaml"
errors = []

if not THEME.is_file():
    errors.append("missing WPF theme: " + str(THEME.relative_to(ROOT)))
else:
    try:
        theme_root = ET.parse(THEME).getroot()
    except ET.ParseError as exc:
        errors.append("Theme.xaml is not well formed: " + str(exc))
        theme_root = None

    color_keys = set()
    brush_keys = set()
    panel_title_style = None
    if theme_root is not None:
        for element in theme_root.iter():
            local = element.tag.rsplit("}", 1)[-1]
            key = element.attrib.get("{" + XAML_NS + "}Key", "")
            if local == "Color" and key:
                color_keys.add(key)
            elif local.endswith("Brush") and key:
                brush_keys.add(key)
            elif local == "Style" and key == "PanelTitle":
                panel_title_style = element

    resource_pattern = re.compile(r"^\{StaticResource\s+([^}\s]+)\}$")
    for path in sorted(UI.glob("*.xaml")):
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError as exc:
            errors.append(str(path.relative_to(ROOT)) + " is not well formed: " + str(exc))
            continue
        for element in root.iter():
            for attribute, value in element.attrib.items():
                match = resource_pattern.match(value.strip())
                if not match or match.group(1) not in color_keys:
                    continue
                property_name = attribute.rsplit("}", 1)[-1]
                if property_name == "Value" and element.tag.rsplit("}", 1)[-1] == "Setter":
                    property_name = element.attrib.get("Property", "Value").rsplit(".", 1)[-1]
                if property_name != "Color":
                    errors.append(
                        str(path.relative_to(ROOT))
                        + ": "
                        + property_name
                        + " references Color resource "
                        + match.group(1)
                        + "; use a SolidColorBrush resource"
                    )

    required_brushes = {
        "Bg0Brush",
        "Bg1Brush",
        "Bg2Brush",
        "BgHoverBrush",
        "BgSelectedBrush",
        "TextBrush",
        "MutedBrush",
    }
    missing = sorted(required_brushes - brush_keys)
    if missing:
        errors.append("Theme.xaml is missing required brush resource(s): " + ", ".join(missing))

    # A keyed TextBlock style does not inherit the implicit TextBlock style. When QS3D
    # is hosted by a BricsCAD palette, omitting Foreground here can leak the host/system
    # foreground (black) into dark headings such as "ĐỐI TƯỢNG ĐANG CHỌN".
    if panel_title_style is None:
        errors.append("Theme.xaml is missing the PanelTitle style")
    else:
        foreground = None
        for child in panel_title_style:
            if child.tag.rsplit("}", 1)[-1] != "Setter":
                continue
            if child.attrib.get("Property") == "Foreground":
                foreground = child.attrib.get("Value")
                break
        if foreground != "{StaticResource TextBrush}":
            errors.append(
                "PanelTitle must explicitly set Foreground to {StaticResource TextBrush}; "
                "do not rely on the implicit TextBlock style or BricsCAD host foreground"
            )

print("QS3D WPF theme resource preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: WPF colors resolve through Brush resources and PanelTitle explicitly keeps "
    "high-contrast text inside BricsCAD dark palettes."
)
