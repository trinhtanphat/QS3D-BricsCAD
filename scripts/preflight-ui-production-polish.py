#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI_DIR = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
THEME = UI_DIR / "Theme.xaml"
POLISH = UI_DIR / "ProductionUiPolish.cs"
PLUGIN_ENTRY = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"
WPF_NS = "http://schemas.microsoft.com/winfx/2006/xaml/presentation"

errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except OSError as exc:
        fail(f"{path.relative_to(ROOT)}: cannot read file: {exc}")
        return ""


def load_implicit_theme_styles() -> dict[str, dict[str, str]]:
    try:
        root = ET.parse(THEME).getroot()
    except (OSError, ET.ParseError) as exc:
        fail(f"{THEME.relative_to(ROOT)}: cannot parse XAML: {exc}")
        return {}

    styles: dict[str, dict[str, str]] = {}
    for style in root.findall(f"{{{WPF_NS}}}Style"):
        target_type = style.attrib.get("TargetType")
        if not target_type:
            continue
        setters: dict[str, str] = {}
        for setter in style.findall(f"{{{WPF_NS}}}Setter"):
            property_name = setter.attrib.get("Property")
            if property_name:
                setters[property_name] = setter.attrib.get("Value", "")
        styles[target_type] = setters
    return styles


def require_setter(
    styles: dict[str, dict[str, str]],
    target_type: str,
    property_name: str,
    expected_value: str,
) -> None:
    actual = styles.get(target_type, {}).get(property_name)
    if actual != expected_value:
        fail(
            f"Theme.xaml: {target_type} must set {property_name}={expected_value!r}; "
            f"found {actual!r}."
        )


def main() -> int:
    styles = load_implicit_theme_styles()

    for target_type in ("{x:Type Window}", "{x:Type UserControl}"):
        require_setter(styles, target_type, "SnapsToDevicePixels", "True")
        require_setter(styles, target_type, "UseLayoutRounding", "True")
        require_setter(styles, target_type, "TextOptions.TextFormattingMode", "Display")

    for target_type in (
        "{x:Type ListBox}",
        "{x:Type ListView}",
        "{x:Type TreeView}",
    ):
        require_setter(styles, target_type, "ScrollViewer.CanContentScroll", "True")
        require_setter(styles, target_type, "VirtualizingPanel.IsVirtualizing", "True")
        require_setter(styles, target_type, "VirtualizingPanel.VirtualizationMode", "Recycling")

    require_setter(styles, "{x:Type DataGrid}", "EnableRowVirtualization", "True")
    require_setter(styles, "{x:Type DataGrid}", "EnableColumnVirtualization", "True")
    require_setter(styles, "{x:Type DataGrid}", "ScrollViewer.CanContentScroll", "True")

    polish = read_text(POLISH)
    required_polish_tokens = (
        "internal static void EnsureRegistered()",
        "typeof(Window)",
        "typeof(UserControl)",
        "typeof(DataGrid)",
        "typeof(ListBox)",
        "typeof(TreeView)",
        "DependencyPropertyHelper.GetValueSource",
        "BaseValueSource.Default",
        "FrameworkElement.UseLayoutRoundingProperty",
        "UIElement.SnapsToDevicePixelsProperty",
        "TextOptions.TextFormattingModeProperty",
        "ScrollViewer.CanContentScrollProperty",
        "VirtualizingPanel.IsVirtualizingProperty",
        "VirtualizingPanel.VirtualizationModeProperty",
        "VirtualizationMode.Recycling",
        "VirtualizingPanel.IsVirtualizingWhenGroupingProperty",
        "DataGrid.EnableRowVirtualizationProperty",
        "DataGrid.EnableColumnVirtualizationProperty",
        "element.GetType().Assembly == typeof(ProductionUiPolish).Assembly",
    )
    for token in required_polish_tokens:
        if token not in polish:
            fail(f"ProductionUiPolish.cs: required production contract missing: {token}")

    plugin_entry = read_text(PLUGIN_ENTRY)
    if "ProductionUiPolish.EnsureRegistered();" not in plugin_entry:
        fail("PluginEntry.cs: ProductionUiPolish must be registered before QS3D host UI starts.")

    anti_patterns = (
        (
            "item virtualization explicitly disabled",
            re.compile(
                r"(?:VirtualizingPanel|VirtualizingStackPanel)\.IsVirtualizing\s*=\s*['\"]False['\"]",
                re.IGNORECASE,
            ),
        ),
        (
            "DataGrid row virtualization explicitly disabled",
            re.compile(r"EnableRowVirtualization\s*=\s*['\"]False['\"]", re.IGNORECASE),
        ),
        (
            "DataGrid column virtualization explicitly disabled",
            re.compile(r"EnableColumnVirtualization\s*=\s*['\"]False['\"]", re.IGNORECASE),
        ),
    )

    xaml_files = sorted(UI_DIR.rglob("*.xaml"))
    for path in xaml_files:
        text = read_text(path)
        for label, pattern in anti_patterns:
            if pattern.search(text):
                fail(f"{path.relative_to(ROOT)}: {label}.")

    if errors:
        print("UI_PRODUCTION_POLISH_PREFLIGHT=FAIL", file=sys.stderr)
        for error in errors:
            print(f" - {error}", file=sys.stderr)
        return 1

    print(f"UI_PRODUCTION_POLISH_PREFLIGHT=PASS files_scanned={len(xaml_files)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
