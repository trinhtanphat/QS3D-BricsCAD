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
    theme_root = None
    theme_text = ""
else:
    theme_text = THEME.read_text(encoding="utf-8")
    try:
        theme_root = ET.parse(THEME).getroot()
    except ET.ParseError as exc:
        errors.append("Theme.xaml is not well formed: " + str(exc))
        theme_root = None

color_keys = set()
brush_keys = set()
keyed_templates = set()
styles = {}
panel_title_style = None

if theme_root is not None:
    for element in theme_root.iter():
        local = element.tag.rsplit("}", 1)[-1]
        key = element.attrib.get("{" + XAML_NS + "}Key", "")
        if local == "Color" and key:
            color_keys.add(key)
        elif local.endswith("Brush") and key:
            brush_keys.add(key)
        elif local == "ControlTemplate" and key:
            keyed_templates.add(key)
        elif local == "Style":
            target = element.attrib.get("TargetType", "")
            if key == "PanelTitle":
                panel_title_style = element
            if target:
                styles.setdefault(target, []).append(element)

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
    "BgControlBrush",
    "BgHoverBrush",
    "BgSelectedBrush",
    "BgPressedBrush",
    "BorderBrush",
    "BorderStrongBrush",
    "BorderFocusBrush",
    "TextBrush",
    "MutedBrush",
    "DisabledTextBrush",
    "AccentBrush",
    "AccentHoverBrush",
    "AccentSoftBrush",
    "LuxuryBrush",
    "LuxurySoftBrush",
}
missing = sorted(required_brushes - brush_keys)
if missing:
    errors.append("Theme.xaml is missing required premium brush resource(s): " + ", ".join(missing))

required_templates = {
    "ComboBoxToggleButtonTemplate",
    "VerticalScrollBarTemplate",
    "HorizontalScrollBarTemplate",
}
missing_templates = sorted(required_templates - keyed_templates)
if missing_templates:
    errors.append("Theme.xaml is missing required dark control template(s): " + ", ".join(missing_templates))

required_target_styles = {
    "{x:Type ComboBox}",
    "{x:Type ComboBoxItem}",
    "{x:Type TextBox}",
    "{x:Type CheckBox}",
    "{x:Type ScrollBar}",
}
missing_target_styles = sorted(target for target in required_target_styles if target not in styles)
if missing_target_styles:
    errors.append("Theme.xaml is missing required host-independent control style(s): " + ", ".join(missing_target_styles))

required_tokens = {
    'x:Name="PART_Popup"': "ComboBox popup must be explicitly templated",
    'x:Name="PART_EditableTextBox"': "editable ComboBox text host must be retained",
    'x:Name="PART_ContentHost"': "TextBox content host must be retained",
    'x:Name="PART_Track"': "ScrollBar track must be explicitly templated",
    'Property="IsHighlighted" Value="True"': "ComboBoxItem highlighted state must be explicit",
    'Property="IsSelected" Value="True"': "selected state must be explicit",
    'Value="{StaticResource BgHoverBrush}"': "dark hover surface must be used",
    'Value="{StaticResource BgSelectedBrush}"': "dark selected surface must be used",
    'Value="{StaticResource BorderFocusBrush}"': "focus border must be explicit",
}
for token, message in required_tokens.items():
    if token not in theme_text:
        errors.append(message + " (missing token: " + token + ")")

for forbidden in ("SystemColors.", "DropShadowEffect", "BlurEffect"):
    if forbidden in theme_text:
        errors.append("Theme.xaml must remain CAD-first and host-independent; forbidden token: " + forbidden)

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

print("QS3D WPF premium theme v2 preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: premium graphite/navy resources, explicit PanelTitle contrast and "
    "host-independent dark ComboBox/TextBox/CheckBox/ScrollBar interaction chrome are present."
)
