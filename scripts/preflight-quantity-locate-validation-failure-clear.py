#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SUMMARY_GUARD = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.LocateSelectionFailureGuard.cs"
INSIGHT_GUARD = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.LocateSelectionFailureGuard.cs"
SUMMARY_CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs"
INSIGHT_CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs"
SUMMARY_XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml"
INSIGHT_XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
PROJECT = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
errors = []

paths = (SUMMARY_GUARD, INSIGHT_GUARD, SUMMARY_CODE, INSIGHT_CODE, SUMMARY_XAML, INSIGHT_XAML, PROJECT)
for path in paths:
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if errors:
    for error in errors:
        print("FAIL:", error)
    sys.exit(1)

summary_guard = SUMMARY_GUARD.read_text(encoding="utf-8")
insight_guard = INSIGHT_GUARD.read_text(encoding="utf-8")
summary_code = SUMMARY_CODE.read_text(encoding="utf-8")
insight_code = INSIGHT_CODE.read_text(encoding="utf-8")
summary_xaml = SUMMARY_XAML.read_text(encoding="utf-8")
insight_xaml = INSIGHT_XAML.read_text(encoding="utf-8")
project = PROJECT.read_text(encoding="utf-8")

if 'Project Sdk="Microsoft.NET.Sdk.WindowsDesktop"' not in project or "<UseWPF>true</UseWPF>" not in project:
    errors.append("V25 project must remain SDK-style WPF so new partial .cs files are default-included")
if "<EnableDefaultCompileItems>false</EnableDefaultCompileItems>" in project:
    errors.append("V25 project must not disable default Compile item discovery")

for name, guard in (("Summary", summary_guard), ("Insight", insight_guard)):
    ctor = "static QuantitySummaryWindow()" if name == "Summary" else "static QuantityInsightPanel()"
    if ctor not in guard:
        errors.append(name + " guard must use an explicit static constructor so class-handler registration precedes instance initialization")
    if "static readonly bool LocateSelectionFailureGuardRegistered" in guard:
        errors.append(name + " guard must not depend on beforefieldinit static-field timing")
    register_pos = guard.find("RegisterLocateSelectionFailureGuard();")
    handler_pos = guard.find("EventManager.RegisterClassHandler")
    if not (0 <= register_pos < handler_pos):
        errors.append(name + " explicit type initialization must register handlers before runtime events")
    if "EventManager.RegisterClassHandler" not in guard:
        errors.append(name + " guard must use WPF class handlers so pre-clear runs before instance locate handlers")
    if "BcadApplication.DocumentManager.MdiActiveDocument" not in guard:
        errors.append(name + " guard must recheck active-document affinity before clearing")
    if "Cad.CadHandleService.Select" not in guard or "Array.Empty<string>()" not in guard:
        errors.append(name + " guard must clear through explicit Select(empty)")
    active_pos = guard.find("BcadApplication.DocumentManager.MdiActiveDocument")
    select_pos = guard.find("Cad.CadHandleService.Select")
    if not (0 <= active_pos < select_pos):
        errors.append(name + " guard must recheck active document before explicit empty selection")
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate",
        "ExistingProjectMutationContext",
        "ProjectContextCoordinator.Save",
        ".Touch()",
        "QS3DZOOMSELECTED",
    ):
        if forbidden in guard:
            errors.append(name + " guard must remain selection-only/read-only: " + forbidden)

for needle in (
    "typeof(Button)",
    "Button.ClickEvent",
    "typeof(DataGrid)",
    "Selector.SelectionChangedEvent",
    "Control.MouseDoubleClickEvent",
    'string.Equals(button.Content as string, "Định vị", StringComparison.Ordinal)',
    "Window.GetWindow(button) as QuantitySummaryWindow",
    "Window.GetWindow(grid) as QuantitySummaryWindow",
    "!owner._detailMode || owner.AutoRevealCheck?.IsChecked != true",
    "owner._detailMode && owner.AutoRevealCheck?.IsChecked == true",
):
    if needle not in summary_guard:
        errors.append("Summary guard missing trigger/ownership contract: " + needle)

if summary_xaml.count('Content="Định vị" Click="OnLocateClick"') != 1:
    errors.append("Summary XAML must expose exactly one explicit Định vị button")
for needle in (
    'SelectionChanged="OnQuantityGridSelectionChanged"',
    'MouseDoubleClick="OnQuantityGridDoubleClick"',
):
    if needle not in summary_xaml:
        errors.append("Summary XAML locate wiring changed: " + needle)

for needle in (
    "typeof(Button)",
    "Button.ClickEvent",
    "typeof(TreeView)",
    "TreeView.SelectedItemChangedEvent",
    "Control.MouseDoubleClickEvent",
    'string.Equals(button.Content as string, "Định vị", StringComparison.Ordinal)',
    "FindInsightOwner(button)",
    "FindInsightOwner(tree)",
    "owner.AutoRevealCheck?.IsChecked != true",
    "owner.AutoRevealCheck?.IsChecked == true",
    "VisualTreeHelper.GetParent(current)",
):
    if needle not in insight_guard:
        errors.append("Insight guard missing trigger/ownership contract: " + needle)

if insight_xaml.count('Content="Định vị" Click="OnLocateClick"') != 1:
    errors.append("Insight XAML must expose exactly one explicit Định vị button")
for needle in (
    'SelectedItemChanged="OnQuantityTreeSelectedItemChanged"',
    'MouseDoubleClick="OnQuantityTreeDoubleClick"',
):
    if needle not in insight_xaml:
        errors.append("Insight XAML locate wiring changed: " + needle)

for name, source, method, zoom in (
    ("Summary", summary_code, "private void LocateCurrent()", '_document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);'),
    ("Insight", insight_code, "private void LocateSelected()", 'document.SendStringToExecute("QS3DZOOMSELECTED ", true, false, false);'),
):
    start = source.find(method)
    if start < 0:
        errors.append(name + " canonical locate method missing")
        continue
    end_token = "private QuantityReportRow ResolveCurrentRow"
    end = source.find(end_token, start)
    block = source[start:end if end >= 0 else len(source)]
    if "Cad.CadHandleService.Select" not in block:
        errors.append(name + " canonical locate must still select validated targets")
    if zoom not in block:
        errors.append(name + " canonical locate zoom dispatch missing")
    if name == "Summary":
        select_pos = block.find("Cad.CadHandleService.Select")
        zero_pos = block.find("if (selectedCount <= 0)")
        zoom_pos = block.find(zoom)
        if not (0 <= select_pos < zero_pos < zoom_pos):
            errors.append("Summary normal locate must keep zero guard before zoom")
    else:
        select_pos = block.rfind("Cad.CadHandleService.Select")
        positive_pos = block.find("if (count > 0)", select_pos)
        zoom_pos = block.find(zoom, select_pos)
        if not (0 <= select_pos < positive_pos <= zoom_pos):
            errors.append("Insight normal locate must keep positive-count-only zoom")

if errors:
    for error in errors:
        print("FAIL:", error)
    sys.exit(1)

print(
    "PASS: quantity locate triggers pre-clear only the same active DWG before validation, "
    "wrong-DWG behavior remains non-clearing, and canonical locate selection/zoom contracts remain intact."
)
