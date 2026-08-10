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
    if theme_root is not None:
        for element in theme_root.iter():
            local = element.tag.rsplit("}", 1)[-1]
            key = element.attrib.get("{" + XAML_NS + "}Key", "")
            if local == "Color" and key:
                color_keys.add(key)
            elif local.endswith("Brush") and key:
                brush_keys.add(key)

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

    required_brushes = {"Bg0Brush", "Bg1Brush", "Bg2Brush", "BgHoverBrush", "BgSelectedBrush", "TextBrush", "MutedBrush"}
    missing = sorted(required_brushes - brush_keys)
    if missing:
        errors.append("Theme.xaml is missing required brush resource(s): " + ", ".join(missing))

print("QS3D WPF theme resource preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Color resources are used only as Color values; WPF Background/Foreground/border styles resolve SolidColorBrush resources.")
