#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
THEME = UI / "Theme.xaml"
XAML_NS = "http://schemas.microsoft.com/winfx/2006/xaml"
XAML_KEY = "{" + XAML_NS + "}Key"
XAML_NAME = "{" + XAML_NS + "}Name"
errors = []


def local_name(element):
    return element.tag.rsplit("}", 1)[-1]


def style_target(style):
    return style.attrib.get("TargetType", "").replace(" ", "")


def find_implicit_style(root, type_name):
    wanted = "{x:Type" + type_name + "}"
    for element in root:
        if local_name(element) != "Style":
            continue
        if element.attrib.get(XAML_KEY):
            continue
        if style_target(element) == wanted:
            return element
    return None


def style_template(style):
    if style is None:
        return None
    for setter in style:
        if local_name(setter) != "Setter" or setter.attrib.get("Property") != "Template":
            continue
        for child in setter.iter():
            if local_name(child) == "ControlTemplate":
                return child
    return None


def descendant_names(element):
    if element is None:
        return set()
    names = set()
    for child in element.iter():
        name = child.attrib.get(XAML_NAME) or child.attrib.get("Name")
        if name:
            names.add(name)
    return names


if not THEME.is_file():
    errors.append("missing WPF theme: " + str(THEME.relative_to(ROOT)))
    theme_root = None
else:
    try:
        theme_root = ET.parse(THEME).getroot()
    except ET.ParseError as exc:
        errors.append("Theme.xaml is not well formed: " + str(exc))
        theme_root = None

color_keys = set()
brush_keys = set()
keyed_styles = {}
keyed_templates = {}

if theme_root is not None:
    for element in theme_root.iter():
        local = local_name(element)
        key = element.attrib.get(XAML_KEY, "")
        if local == "Color" and key:
            color_keys.add(key)
        elif local.endswith("Brush") and key:
            brush_keys.add(key)
        elif local == "Style" and key:
            keyed_styles[key] = element
        elif local == "ControlTemplate" and key:
            keyed_templates[key] = element

    # Color resources are not brushes. Guard every XAML consumer against assigning a
    # Color directly to Background/Foreground/BorderBrush/etc.
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
                if property_name == "Value" and local_name(element) == "Setter":
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
        # Backward-compatible foundation.
        "Bg0Brush",
        "Bg1Brush",
        "Bg2Brush",
        "BgHoverBrush",
        "BgSelectedBrush",
        "TextBrush",
        "MutedBrush",
        "AccentBrush",
        "LuxuryBrush",
        # Premium v2 contract.
        "BgRaisedBrush",
        "BgInputBrush",
        "SubtleTextBrush",
        "BorderFocusBrush",
        "BorderLuxuryBrush",
        "AccentHoverBrush",
        "AccentPressedBrush",
        "LuxuryMutedBrush",
        "LuxurySoftBrush",
        "DangerHoverBrush",
        "DangerPressedBrush",
    }
    missing = sorted(required_brushes - brush_keys)
    if missing:
        errors.append("Theme.xaml is missing required premium brush resource(s): " + ", ".join(missing))

    # A keyed TextBlock style does not inherit the implicit TextBlock style. BricsCAD
    # can otherwise leak a black foreground into dark section headings.
    panel_title_style = keyed_styles.get("PanelTitle")
    if panel_title_style is None:
        errors.append("Theme.xaml is missing the PanelTitle style")
    else:
        foreground = None
        for child in panel_title_style:
            if local_name(child) == "Setter" and child.attrib.get("Property") == "Foreground":
                foreground = child.attrib.get("Value")
                break
        if foreground != "{StaticResource TextBrush}":
            errors.append(
                "PanelTitle must explicitly set Foreground to {StaticResource TextBrush}; "
                "do not rely on implicit TextBlock or BricsCAD host foreground"
            )

    # Core controls shown in the owner runtime screenshot must own their dark chrome.
    combo_style = find_implicit_style(theme_root, "ComboBox")
    combo_template = style_template(combo_style)
    combo_names = descendant_names(combo_template)
    if combo_template is None:
        errors.append("ComboBox must have a host-independent dark ControlTemplate")
    else:
        for required_part in ("PART_Popup", "PART_EditableTextBox"):
            if required_part not in combo_names:
                errors.append("ComboBox template is missing " + required_part)

    text_style = find_implicit_style(theme_root, "TextBox")
    text_template = style_template(text_style)
    if text_template is None:
        errors.append("TextBox must have a host-independent dark ControlTemplate")
    elif "PART_ContentHost" not in descendant_names(text_template):
        errors.append("TextBox template is missing PART_ContentHost")

    check_style = find_implicit_style(theme_root, "CheckBox")
    check_template = style_template(check_style)
    check_names = descendant_names(check_template)
    if check_template is None:
        errors.append("CheckBox must have a host-independent dark ControlTemplate")
    else:
        for required_part in ("Box", "CheckMark"):
            if required_part not in check_names:
                errors.append("CheckBox template is missing " + required_part)

    scrollbar_style = find_implicit_style(theme_root, "ScrollBar")
    if scrollbar_style is None:
        errors.append("Theme.xaml is missing the implicit ScrollBar style")
    for key in ("Qs3DVerticalScrollBarTemplate", "Qs3DHorizontalScrollBarTemplate"):
        template = keyed_templates.get(key)
        if template is None:
            errors.append("Theme.xaml is missing " + key)
        elif "PART_Track" not in descendant_names(template):
            errors.append(key + " is missing PART_Track")

    tooltip_style = find_implicit_style(theme_root, "ToolTip")
    if style_template(tooltip_style) is None:
        errors.append("ToolTip must have a dark host-independent ControlTemplate")

    # Premium inside a CAD host must stay light-weight.
    forbidden_effects = {"DropShadowEffect", "BlurEffect"}
    found_effects = sorted(
        {
            local_name(element)
            for element in theme_root.iter()
            if local_name(element) in forbidden_effects
        }
    )
    if found_effects:
        errors.append(
            "Theme.xaml must not use heavy WPF effects inside BricsCAD palettes: "
            + ", ".join(found_effects)
        )

print("QS3D WPF premium theme preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: premium v2 brushes, explicit PanelTitle contrast, dark ComboBox/TextBox/"
    "CheckBox/ScrollBar/ToolTip chrome, and no heavy effects are present."
)
