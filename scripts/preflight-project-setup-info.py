#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


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


def main():
    ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs")
    commands = read("src/QS3D.BricsCAD.V25/ProjectSetupCommands.cs")
    palette = read("src/QS3D.BricsCAD.V25/ProjectSetupPaletteCoordinator.cs")
    panel = read("src/QS3D.BricsCAD.V25/UI/BltProjectSetupPanel.cs")
    icons = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectSetupIconFactory.cs")
    activation = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectTabActivationCoordinator.cs")
    lifecycle = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
    plugin = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")

    blt_start = ribbon.find("private static readonly ButtonSpec[] BltButtons =")
    blt_end = ribbon.find("public static bool TryInitialize()", blt_start)
    if blt_start < 0 or blt_end < 0:
        fail("Project Setup BLT button inventory is missing")
    blt = ribbon[blt_start:blt_end]

    for token in (
        '"QS3D_PROJECT_INFO"',
        '"QS3DPROJECTINFO"',
        'ProjectSetupIconKind.ProjectInformation',
        '"QS3D_PROJECT_FLOORS"',
        '"QS3DLEVELS"',
        'ProjectSetupIconKind.FloorSettings',
        '"QS3D_PROJECT_PROPERTIES"',
        '"QS3DPROJECTPROPERTIES"',
        'ProjectSetupIconKind.ProjectProperties',
    ):
        require(blt, token, "Project Setup button routes")
    if '"QS3DPROJECTTOOLS"' in blt:
        fail("Thông tin dự án must not route to generic QS3DPROJECTTOOLS")

    for token in (
        'SetProperty(button, "ShowImage", true);',
        'SetProperty(button, "Image", ProjectSetupIconFactory.Create(spec.Icon.Value, 16));',
        'SetProperty(button, "LargeImage", ProjectSetupIconFactory.Create(spec.Icon.Value, 32));',
        'SetEnumProperty(button, "Size", "Large");',
    ):
        require(ribbon, token, "Project Setup image contract")

    for token in (
        '[CommandMethod("QS3DPROJECTINFO", CommandFlags.Modal)]',
        '[CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]',
        'public void ShowProjectInformation()',
        'public void ShowProjectProperties()',
    ):
        require(commands, token, "Project Setup public commands")
    if commands.count("ProjectSetupPaletteCoordinator.ShowProjectInformation();") != 2:
        fail("Project Info and Project Properties must intentionally share the same embedded placeholder surface")

    require(
        panel,
        '"(Chưa xây dựng — Thông tin dự án / Thuộc tính dự án)"',
        "Project Setup placeholder text",
    )
    require(panel, "public void ShowProjectInformation()", "Project Setup placeholder selection")

    for token in (
        'new PaletteSet("QS3D — Thiết lập dự án", ProjectSetupGuid)',
        'DockEnabled = DockSides.Left | DockSides.Right',
        '_palette.AddVisual("Dự án", _panel, true);',
        'StartCenterPaletteCoordinator.Hide();',
        'PaletteCoordinator.Hide();',
    ):
        require(palette, token, "BricsCAD-hosted Project Setup surface")
    if "MessageBox" in palette or "ShowDialog" in palette:
        fail("Project Setup must remain embedded/non-modal, not a detached dialog")

    for token in (
        'private const string ProjectTabId = "QS3D_PROJECT";',
        'ProjectSetupPaletteCoordinator.ShowProjectInformation();',
        'ProjectSetupPaletteCoordinator.Hide();',
    ):
        require(activation, token, "Project tab activation contract")

    require_order(
        lifecycle,
        (
            "BltBimWorkspaceActivationCoordinator.Start();",
            "ProjectTabActivationCoordinator.Start();",
            "StopTimedRetry();",
        ),
        "Project Setup activation lifecycle",
    )
    require(lifecycle, "ProjectTabActivationCoordinator.Stop();", "Project Setup teardown lifecycle")
    require(plugin, "TryCleanup(ProjectSetupPaletteCoordinator.Dispose);", "Project Setup palette disposal")

    for token in (
        "ProjectInformation",
        "FloorSettings",
        "ProjectProperties",
        "RenderTargetBitmap",
        "bitmap.Freeze();",
    ):
        require(icons, token, "Project Setup deterministic icons")

    print("PASS: Project Setup uses distinct Info/Properties commands, embedded placeholder surface, production Levels route, and deterministic icons.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
