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
WORKSPACE = UI_DIR / "WorkspacePanel.xaml"
PLUGIN_ENTRIES = (
    ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs",
    ROOT / "src" / "QS3D.BricsCAD.V26" / "PluginEntry.cs",
)
V26_PLUGIN_ENTRY = PLUGIN_ENTRIES[1]
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


def mask_room_finish_tree_exception(path: Path, text: str) -> str:
    """Exclude the one host-crash containment exception from the global anti-pattern scan.

    RoomFinishTree is a small static tree whose virtualization state is deliberately fixed in
    XAML before first Measure. The dedicated V25 Room finish preflight owns that exact contract.
    Every other XAML surface remains subject to the production-polish virtualization policy.
    """
    if path != WORKSPACE:
        return text

    match = re.search(
        r'<TreeView\s+x:Name="RoomFinishTree"(?P<attrs>[^>]*)>',
        text,
        flags=re.DOTALL,
    )
    if not match:
        return text

    attrs = match.group("attrs")
    required = (
        'VirtualizingPanel.VirtualizationMode="Standard"',
        'VirtualizingPanel.IsVirtualizing="False"',
        'ScrollViewer.CanContentScroll="False"',
    )
    if any(token not in attrs for token in required):
        return text

    start, end = match.span()
    opening_tag = text[start:end].replace(
        'VirtualizingPanel.IsVirtualizing="False"',
        'VirtualizingPanel.IsVirtualizing="__ROOM_FINISH_STATIC_EXCEPTION__"',
        1,
    )
    return text[:start] + opening_tag + text[end:]


def main() -> int:
    styles = load_implicit_theme_styles()

    for target_type in ("{x:Type Window}", "{x:Type UserControl}"):
        require_setter(styles, target_type, "SnapsToDevicePixels", "True")
        require_setter(styles, target_type, "UseLayoutRounding", "True")
        require_setter(styles, target_type, "TextOptions.TextFormattingMode", "Display")

    # Virtualization belongs to declarative/pre-layout styles. ProductionUiPolish runs
    # from Loaded and must never switch VirtualizationMode after an ItemsHost Measure.
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
        "HasQs3dRootAncestor(root)",
        "DependencyPropertyHelper.GetValueSource",
        "BaseValueSource.Default",
        "FrameworkElement.UseLayoutRoundingProperty",
        "UIElement.SnapsToDevicePixelsProperty",
        "TextOptions.TextFormattingModeProperty",
        "element.GetType().Assembly == typeof(ProductionUiPolish).Assembly",
    )
    for token in required_polish_tokens:
        if token not in polish:
            fail(f"ProductionUiPolish.cs: required production contract missing: {token}")

    forbidden_polish_tokens = (
        "ApplyVirtualizationDefaults(root)",
        "ApplyItemVirtualizationDefaults(",
        "VirtualizingPanel.VirtualizationModeProperty",
        "VirtualizingPanel.IsVirtualizingProperty",
        "VirtualizingPanel.IsVirtualizingWhenGroupingProperty",
        "ScrollViewer.CanContentScrollProperty",
        "DataGrid.EnableRowVirtualizationProperty",
        "DataGrid.EnableColumnVirtualizationProperty",
        "VirtualizationMode.Recycling",
    )
    for token in forbidden_polish_tokens:
        if token in polish:
            fail(
                "ProductionUiPolish.cs: Loaded-time polish must not mutate item virtualization "
                f"state after ItemsHost Measure: {token}"
            )

    for forbidden_token in ("typeof(DataGrid),", "typeof(ListBox),", "typeof(TreeView),"):
        if forbidden_token in polish:
            fail(
                "ProductionUiPolish.cs: item-control class handlers must not be registered "
                f"globally against the BricsCAD AppDomain: {forbidden_token}"
            )

    for plugin_entry_path in PLUGIN_ENTRIES:
        plugin_entry = read_text(plugin_entry_path)
        if "ProductionUiPolish.EnsureRegistered();" not in plugin_entry:
            fail(
                f"{plugin_entry_path.relative_to(ROOT)}: ProductionUiPolish must be "
                "registered before QS3D host UI starts."
            )

    v26_plugin_entry = read_text(V26_PLUGIN_ENTRY)
    register_index = v26_plugin_entry.find("ProductionUiPolish.EnsureRegistered();")
    palette_index = v26_plugin_entry.find("PaletteCoordinator.EnsureCreated();")
    if register_index >= 0 and palette_index >= 0 and register_index > palette_index:
        fail(
            "src/QS3D.BricsCAD.V26/PluginEntry.cs: ProductionUiPolish registration must "
            "run before V26 palette creation."
        )

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
        text = mask_room_finish_tree_exception(path, read_text(path))
        for label, pattern in anti_patterns:
            if pattern.search(text):
                fail(f"{path.relative_to(ROOT)}: {label}.")

    if errors:
        print("UI_PRODUCTION_POLISH_PREFLIGHT=FAIL", file=sys.stderr)
        for error in errors:
            print(f" - {error}", file=sys.stderr)
        return 1

    print(f"UI_PRODUCTION_POLISH_PREFLIGHT=PASS files_scanned={len(xaml_files)} host_entries={len(PLUGIN_ENTRIES)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
