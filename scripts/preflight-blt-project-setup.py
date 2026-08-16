#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []


def read(rel):
    path = ROOT / rel
    if not path.is_file():
        errors.append("missing file: " + rel)
        return ""
    return path.read_text(encoding="utf-8")


ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs")
icons = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectSetupIconFactory.cs")
commands = read("src/QS3D.BricsCAD.V25/ProjectSetupCommands.cs")
coordinator = read("src/QS3D.BricsCAD.V25/ProjectSetupPaletteCoordinator.cs")
activation = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectTabActivationCoordinator.cs")
ribbon_init = read("src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs")
panel = read("src/QS3D.BricsCAD.V25/UI/BltProjectSetupPanel.cs")
plugin_v25 = read("src/QS3D.BricsCAD.V25/PluginEntry.cs")
plugin_v26 = read("src/QS3D.BricsCAD.V26/PluginEntry.cs")
v26 = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")
properties_command = read("src/QS3D.BricsCAD.V25/ProjectPropertiesCommands.cs")
properties_window = read("src/QS3D.BricsCAD.V25/UI/ProjectPropertiesWindow.cs")

for needle in (
    '"QS3D_PROJECT_INFO",\n                "Thông tin\\ndự án",\n                "QS3DPROJECTINFO",\n                ProjectSetupIconKind.ProjectInformation',
    '"QS3D_PROJECT_FLOORS",\n                "Cài đặt\\ntầng",\n                "QS3DLEVELS",\n                ProjectSetupIconKind.FloorSettings',
    '"QS3D_PROJECT_PROPERTIES",\n                "Thuộc tính\\ndự án",\n                "QS3DPROJECTPROPERTIES",\n                ProjectSetupIconKind.ProjectProperties',
):
    if needle not in ribbon:
        errors.append("Project BLT ribbon mapping/icon missing: " + needle.replace("\n", " "))

if re.search(r'QS3D_PROJECT_INFO"[^\n]*"QS3DPROJECTTOOLS"', ribbon):
    errors.append("visible BLT Project Information must not route to legacy QS3DPROJECTTOOLS")

for needle in (
    'SetProperty(button, "ShowImage", true);',
    'SetProperty(button, "Image", ProjectSetupIconFactory.Create(spec.Icon.Value, 16));',
    'SetProperty(button, "LargeImage", ProjectSetupIconFactory.Create(spec.Icon.Value, 32));',
    'SetEnumProperty(button, "Size", "Large");',
):
    if needle not in ribbon:
        errors.append("Project Setup buttons must use visible dedicated large icons: " + needle)

for needle in (
    "ProjectInformation,",
    "FloorSettings,",
    "ProjectProperties",
    "RenderTargetBitmap",
    "AddGearBadge",
    "AddSlider",
):
    if needle not in icons:
        errors.append("Project Setup vector icon contract missing: " + needle)

if '[CommandMethod("QS3DPROJECTINFO", CommandFlags.Modal)]' not in commands:
    errors.append("missing project information command: QS3DPROJECTINFO")
if "ProjectSetupPaletteCoordinator.ShowProjectInformation();" not in commands:
    errors.append("project information command is not wired to embedded palette")
if "QS3DPROJECTPROPERTIES" in commands:
    errors.append("ProjectSetupCommands must not shadow the independently landed Project Properties command")

# Preserve the current-main Project Properties implementation instead of replacing it during
# reconciliation of the overlapping owner-reference work.
if '[CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]' not in properties_command:
    errors.append("dedicated Project Properties command from current main must remain intact")
if "new ProjectPropertiesWindow()" not in properties_command:
    errors.append("Project Properties must continue using its dedicated current-main surface")
if "(Chưa xây dựng — Thuộc tính dự án)" not in properties_window:
    errors.append("dedicated Project Properties placeholder drifted")

placeholder = "(Chưa xây dựng — Thông tin dự án / Thuộc tính dự án)"
if placeholder not in panel:
    errors.append("owner-reference Project Information placeholder text drifted")

for forbidden in ("ProjectContextCoordinator.GetOrCreate", "QS3DREGEN", "QS3DSAVE"):
    if forbidden in panel or forbidden in coordinator or forbidden in activation:
        errors.append("project information surface must remain presentation-only: " + forbidden)

for needle in (
    'private const string ProjectTabId = "QS3D_PROJECT";',
    "StartCenterPaletteCoordinator.Hide();",
    "PaletteCoordinator.Hide();",
    "if (string.Equals(selectedId, ProjectTabId, StringComparison.OrdinalIgnoreCase)) return;",
    "Hide();",
    "Dock = DockSides.Left",
    'new WpfSize(1040, 680)',
):
    if needle not in coordinator:
        errors.append("embedded project information palette lifecycle contract missing: " + needle)

for needle in (
    'private const string ProjectTabId = "QS3D_PROJECT";',
    "ProjectSetupPaletteCoordinator.ShowProjectInformation();",
    "ProjectSetupPaletteCoordinator.Hide();",
    "StartCenterPaletteCoordinator.Hide();",
    "PaletteCoordinator.Hide();",
):
    if needle not in activation:
        errors.append("project-tab activation contract missing: " + needle)

for needle in (
    "ProjectTabActivationCoordinator.Start();",
    "ProjectTabActivationCoordinator.Stop();",
):
    if needle not in ribbon_init:
        errors.append("RibbonInitializationCoordinator missing project-tab lifecycle: " + needle)

for host, plugin in (("V25", plugin_v25), ("V26", plugin_v26)):
    if "ProjectSetupPaletteCoordinator.Dispose" not in plugin:
        errors.append(host + " PluginEntry must dispose embedded project information palette on unload")

if "..\\QS3D.BricsCAD.V25\\**\\*.cs" not in v26:
    errors.append("V26 must continue linking the V25 adapter source for project information parity")

legacy = read("src/QS3D.BricsCAD.V25/ProjectToolsCommands.cs")
if '[CommandMethod("QS3DPROJECTTOOLS", CommandFlags.Modal)]' not in legacy:
    errors.append("legacy QS3DPROJECTTOOLS command must remain available")
if "new ProjectToolsWindow(document)" not in legacy:
    errors.append("legacy Project Tools command unexpectedly lost its advanced window")

if errors:
    print("BLT3D Project Information preflight FAILED")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: BLT3D Project Setup has dedicated large vector icons and preserves current-main Project Properties behavior.")
