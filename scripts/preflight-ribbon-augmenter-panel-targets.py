#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src/QS3D.BricsCAD.V25/Ribbon"
BOOTSTRAP = RIBBON / "RibbonBootstrapper.cs"
REFERENCE = RIBBON / "ReferenceWallRibbonAugmenter.cs"
PROJECT = RIBBON / "ProjectRibbonAugmenter.cs"
QUICK = RIBBON / "QuickWorkflowRibbonAugmenter.cs"
PLUGIN = ROOT / "src/QS3D.BricsCAD.V25/PluginEntry.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing Ribbon augmenter dependency: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


bootstrap = read(BOOTSTRAP)
reference = read(REFERENCE)
project = read(PROJECT)
quick = read(QUICK)
plugin = read(PLUGIN)

for token in (
    '"QS3D_AUTHOR"',
    'Panel("ARCHITECTURE", "Kiến trúc"',
    '"QS3D_PROJECT"',
    'Panel("STATE", "Trạng thái"',
    'Panel("TEMPLATE", "Template"',
    'Panel("WORKSPACE", "Phạm vi"',
    'SetProperty(source, "Id", tabSpec.Id + "_" + panelSpec.Id + "_PANEL_SOURCE")',
):
    if token not in bootstrap:
        errors.append("grouped RibbonBootstrapper contract missing: " + token)

for token in (
    'private const string PanelSourceId = "QS3D_AUTHOR_ARCHITECTURE_PANEL_SOURCE";',
    'private const string ButtonId = "QS3D_AUTHOR_DRAWWALLREF";',
    'private const string Command = "QS3DDRAWWALLREF";',
    "var source = FindPanelSource(panelItems, PanelSourceId);",
    "if (source == null) return false;",
    "private static object? FindPanelSource(IEnumerable panels, string sourceId)",
    "CollectionContainsId(items, ButtonId) || CollectionContainsCommand(items, Command)",
    "Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + \" \", true, false, false);",
):
    if token not in reference:
        errors.append("Reference Wall grouped-panel contract missing: " + token)

for token in (
    'private const string PanelSourceId = "QS3D_PROJECT_TOOLS_PANEL_SOURCE";',
    'private const string PanelTitle = "Công cụ dự án";',
    "FindPanelSource(panelEnumerable, PanelSourceId) ?? CreateProjectToolsPanel(panels)",
    "private static object? FindPanelSource(IEnumerable panels, string sourceId)",
    "private static object CreateProjectToolsPanel(object panels)",
    'Create("Bricscad.Windows.RibbonPanelSource")',
    'SetProperty(source, "Id", PanelSourceId);',
    'SetProperty(source, "Title", PanelTitle);',
    'Create("Bricscad.Windows.RibbonPanel")',
    'SetProperty(panel, "Source", source);',
    "Add(panels, panel);",
    "if (CollectionContainsId(items, spec.Id)) continue;",
    "Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + \" \", true, false, false);",
):
    if token not in project:
        errors.append("Project Tools grouped-panel contract missing: " + token)

for token in (
    'private const string PanelSourceId = "QS3D_AUTHOR_QUICK_PANEL_SOURCE";',
    'private const string PanelTitle = "Tác vụ nhanh";',
    "FindPanelSource(panelItems, PanelSourceId) ?? CreateQuickPanel(panels)",
):
    if token not in quick:
        errors.append("Quick Workflow dedicated-panel contract missing: " + token)

for name, text in (("ReferenceWallRibbonAugmenter", reference), ("ProjectRibbonAugmenter", project), ("QuickWorkflowRibbonAugmenter", quick)):
    for forbidden in (
        'PanelSourceId = "QS3D_AUTHOR_PANEL_SOURCE"',
        'PanelSourceId = "QS3D_PROJECT_PANEL_SOURCE"',
        "if (source == null) source = candidate;",
    ):
        if forbidden in text:
            errors.append(name + " still relies on removed flat-panel/fallback routing: " + forbidden)

bootstrap_call = plugin.find("RibbonBootstrapper.TryInitialize();")
reference_call = plugin.find("ReferenceWallRibbonAugmenter.TryInitialize();")
project_call = plugin.find("ProjectRibbonAugmenter.TryInitialize();")
quick_call = plugin.find("QuickWorkflowRibbonAugmenter.TryInitialize();")
if min(bootstrap_call, reference_call, project_call, quick_call) < 0 or not (
    bootstrap_call < reference_call < project_call < quick_call
):
    errors.append("PluginEntry must initialize grouped RibbonBootstrapper before all legacy augmenters")

if errors:
    print("Ribbon augmenter grouped-panel preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Ribbon augmenter grouped-panel preflight PASS: legacy augmenters use deterministic grouped/dedicated panel sources, never first-panel fallback, remain idempotent, and dispatch against the click-time active document.")
