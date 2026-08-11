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
    'SetProperty(source, "Id", PanelSourceId(tabSpec, panelSpec));',
    'tabSpec.Id + "_" + panelSpec.Id + "_PANEL_SOURCE"',
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
    "var button = FindById(items, ButtonId) ?? FindByCommand(items, Command);",
    "if (button == null)",
    'button = Create("Bricscad.Windows.RibbonButton");',
    "Add(items, button);",
    'SetProperty(button, "Id", ButtonId);',
    'SetProperty(button, "Name", ButtonText);',
    'SetProperty(button, "Text", ButtonText);',
    'SetProperty(button, "CommandParameter", Command);',
    'SetProperty(button, "CommandHandler", new CommandHandler());',
    "private static object? FindById(object collection, string id)",
    "private static object? FindByCommand(object collection, string command)",
    "Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + \" \", true, false, false);",
):
    if token not in reference:
        errors.append("Reference Wall grouped-panel/reconciliation contract missing: " + token)

for forbidden in (
    "CollectionContainsId(items, ButtonId) || CollectionContainsCommand(items, Command)",
    "private static bool CollectionContainsId(object collection, string id)",
    "private static bool CollectionContainsCommand(object collection, string command)",
):
    if forbidden in reference:
        errors.append("Reference Wall must reconcile an existing stable button instead of accepting stale state: " + forbidden)

reference_find = reference.find("var button = FindById(items, ButtonId) ?? FindByCommand(items, Command);")
reference_create = reference.find("if (button == null)", reference_find)
reference_name = reference.find('SetProperty(button, "Name", ButtonText);', reference_create)
reference_command = reference.find('SetProperty(button, "CommandParameter", Command);', reference_name)
if min(reference_find, reference_create, reference_name, reference_command) < 0 or not (
    reference_find < reference_create < reference_name < reference_command
):
    errors.append("Reference Wall must find-or-create by stable identity before reconciling current presentation/command state")

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
    "var button = FindById(items, spec.Id);",
    "if (button == null)",
    'button = Create("Bricscad.Windows.RibbonButton");',
    'SetProperty(button, "Id", spec.Id);',
    "Add(items, button);",
    'SetProperty(button, "Name", spec.Text);',
    'SetProperty(button, "Text", spec.Text);',
    'SetProperty(button, "CommandParameter", spec.Command);',
    'SetProperty(button, "CommandHandler", new CommandHandler());',
    "private static object? FindById(object collection, string id)",
    "Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + \" \", true, false, false);",
):
    if token not in project:
        errors.append("Project Tools grouped-panel/reconciliation contract missing: " + token)

for forbidden in (
    "if (CollectionContainsId(items, spec.Id)) continue;",
    "private static bool CollectionContainsId(object collection, string id)",
):
    if forbidden in project:
        errors.append("Project Tools must reconcile existing stable buttons instead of skipping stale state: " + forbidden)

project_loop = project.find("foreach (var spec in Buttons)")
project_find = project.find("var button = FindById(items, spec.Id);", project_loop)
project_create = project.find("if (button == null)", project_find)
project_name = project.find('SetProperty(button, "Name", spec.Text);', project_create)
project_command = project.find('SetProperty(button, "CommandParameter", spec.Command);', project_name)
if min(project_loop, project_find, project_create, project_name, project_command) < 0 or not (
    project_loop < project_find < project_create < project_name < project_command
):
    errors.append("Project Tools must find-or-create each stable ID before reconciling current presentation/command state")

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

print(
    "Ribbon augmenter grouped-panel preflight PASS: Reference Wall and Project Tools reconcile stable existing button state, "
    "all legacy augmenters use deterministic grouped/dedicated panel sources without first-panel fallback, and command dispatch remains click-time active-document routed."
)
