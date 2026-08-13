#!/usr/bin/env python3
"""Reject the proven V25 status/footer DockPanel ordering regression family."""

from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
WPF = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
XAML_NS = "{http://schemas.microsoft.com/winfx/2006/xaml}"
STATUS_NAME_PARTS = ("status", "total", "summary")


def is_status_like_textblock(node: ET.Element) -> bool:
    if node.tag != WPF + "TextBlock":
        return False

    name = (node.attrib.get(XAML_NS + "Name") or "").strip().lower()
    if any(part in name for part in STATUS_NAME_PARTS):
        return True

    text = (node.attrib.get("Text") or "").replace(" ", "").lower()
    return "{bindingstatus" in text


def node_label(node: ET.Element) -> str:
    name = node.attrib.get(XAML_NS + "Name")
    if name:
        return name
    text = node.attrib.get("Text")
    if text:
        return text[:72]
    return node.tag.rsplit("}", 1)[-1]


def main() -> None:
    if not UI.is_dir():
        raise SystemExit(f"FAIL: missing V25 UI directory: {UI.relative_to(ROOT)}")

    violations: list[str] = []
    scanned_files = 0
    scanned_dockpanels = 0

    for xaml in sorted(UI.glob("*.xaml")):
        if xaml.name == "Theme.xaml":
            continue

        scanned_files += 1
        try:
            root = ET.fromstring(xaml.read_text(encoding="utf-8"))
        except ET.ParseError as exc:
            violations.append(f"{xaml.name}: malformed XAML: {exc}")
            continue

        for dock in root.iter(WPF + "DockPanel"):
            scanned_dockpanels += 1
            children = list(dock)
            if len(children) < 2:
                continue

            last_child_fill = (dock.attrib.get("LastChildFill") or "True").strip().lower()
            if last_child_fill == "false":
                continue

            final = children[-1]
            if (final.attrib.get("DockPanel.Dock") or "").strip().lower() != "right":
                continue

            status_nodes = [child for child in children[:-1] if is_status_like_textblock(child)]
            for status in status_nodes:
                violations.append(
                    f"{xaml.name}: status-like '{node_label(status)}' precedes final right-docked "
                    f"'{node_label(final)}' while LastChildFill is enabled; move the right-docked "
                    "child before the final fill text, set LastChildFill=False when that is truly the "
                    "intended docking contract, or use an explicit responsive Grid."
                )

    if violations:
        print("V25 status DockPanel ordering preflight FAILED:")
        for violation in violations:
            print("ERROR:", violation)
        sys.exit(1)

    print(
        "PASS: V25 status DockPanel ordering gate — "
        f"scanned {scanned_files} XAML surface file(s) / {scanned_dockpanels} DockPanel(s); "
        "no status/totals/summary fill text precedes a final right-docked child with LastChildFill enabled."
    )


if __name__ == "__main__":
    main()
