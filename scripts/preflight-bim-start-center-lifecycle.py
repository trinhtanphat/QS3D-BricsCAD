#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing required source: {relative}")
    return path.read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def require_order(text, needles, label):
    cursor = -1
    for needle in needles:
        index = text.find(needle, cursor + 1)
        if index < 0:
            fail(f"{label}: missing ordered token: {needle}")
        if index <= cursor:
            fail(f"{label}: token out of order: {needle}")
        cursor = index


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def main():
    home = read("src/QS3D.BricsCAD.V25/Ribbon/HomeTabActivationCoordinator.cs")
    bim = read("src/QS3D.BricsCAD.V25/Ribbon/BltBimWorkspaceActivationCoordinator.cs")

    # HOME is a large embedded PaletteSet. It must be released as soon as another tab wins,
    # otherwise the MÔ HÌNH BIM tab can show its Ribbon while the stale Start Center still covers
    # the native BricsCAD viewport underneath.
    require_order(
        home,
        (
            "_lastSelectedTabId = selectedId;",
            "if (!string.Equals(selectedId, HomeTabId, StringComparison.OrdinalIgnoreCase))",
            "StartCenterPaletteCoordinator.Hide();",
            "return;",
        ),
        "HOME exit lifecycle",
    )

    # Re-entering HOME releases QS3D side palettes before reopening the Start Center so the two
    # workspace modes do not compete for the same docked host area.
    require_order(
        home,
        (
            "PaletteCoordinator.Hide();",
            "new StartCenterCommands().ShowStartCenter();",
        ),
        "HOME entry lifecycle",
    )

    # BricsCAD minor versions expose selected Ribbon state through several shapes. BIM selection
    # must support the same index/fallback paths as HOME instead of depending only on CurrentTab.
    for token in (
        '"SelectedTabIndex"',
        '"SelectedIndex"',
        '"CurrentTabIndex"',
        'ReadBool(tab, "Selected")',
        'GetProperty(tab, "Name") as string',
        "ItemAt(tabs, index)",
    ):
        require(bim, token, "BIM tab selection compatibility")

    # The BIM activation path itself also owns a fail-safe Start Center release, then opens the
    # production left/right palettes around the native BricsCAD viewport.
    require_order(
        bim,
        (
            "StartCenterPaletteCoordinator.Hide();",
            "PaletteCoordinator.ShowBimWorkspace();",
        ),
        "BIM entry lifecycle",
    )

    print("PASS: HOME/BIM palette lifecycle prevents stale Start Center overlap and recognizes BricsCAD tab-selection variants.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
