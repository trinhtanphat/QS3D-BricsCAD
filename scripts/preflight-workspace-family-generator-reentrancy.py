#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FAMILY_SUBTYPE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.FamilySubtype.cs"
WORKSPACE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


family_text = FAMILY_SUBTYPE.read_text(encoding="utf-8")
workspace_text = WORKSPACE.read_text(encoding="utf-8")

if "using System.Windows.Threading;" not in family_text:
    fail("Workspace Family reveal must use the WPF Dispatcher deferral path.")

refresh_marker = "private void RefreshSelectedFamilyHighlight()"
refresh_start = family_text.find(refresh_marker)
if refresh_start < 0:
    fail("RefreshSelectedFamilyHighlight() was not found.")
refresh_method = family_text[refresh_start:refresh_start + 1800]

pending = refresh_method.find("_familyHighlightRefreshPending")
deferred = refresh_method.find("Dispatcher.BeginInvoke(DispatcherPriority.Loaded")
reveal_call = refresh_method.find("RevealSelectedFamilyAndRefreshHighlight();")
if pending < 0:
    fail("Selected-family highlight requests must be coalesced before dispatcher deferral.")
if deferred < 0 or reveal_call < 0 or deferred > reveal_call:
    fail("Selected-family reveal must be deferred through DispatcherPriority.Loaded.")

reveal_marker = "private void RevealSelectedFamilyAndRefreshHighlight()"
reveal_start = family_text.find(reveal_marker)
if reveal_start < 0:
    fail("RevealSelectedFamilyAndRefreshHighlight() was not found.")
reveal_method = family_text[reveal_start:reveal_start + 2200]
guard = reveal_method.find("GeneratorStatus.GeneratingContainers")
scroll = reveal_method.find("FamilyList.ScrollIntoView(selected);")
style_deferred = reveal_method.find("Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle")
if guard < 0:
    fail("Deferred FamilyList reveal must still guard active container generation.")
if scroll < 0 or not (guard < scroll):
    fail("Generator guard must run before the centralized FamilyList.ScrollIntoView call.")
if style_deferred < 0 or style_deferred < scroll:
    fail("Container styling must be deferred until after the reveal/layout turn.")

if "FamilyList.UpdateLayout(" in family_text:
    fail("FamilyList reveal must not force synchronous UpdateLayout during generator-sensitive refresh.")
if family_text.count("FamilyList.ScrollIntoView(") != 1:
    fail("FamilyList scrolling must stay centralized in the deferred reveal helper.")
if "FamilyList.ScrollIntoView(" in workspace_text:
    fail("WorkspacePanel.xaml.cs must not bypass the generator-safe FamilyList reveal path.")

style_marker = "private void ApplySelectedFamilyHighlight(ProjectFamily selected)"
style_start = family_text.find(style_marker)
if style_start < 0:
    fail("ApplySelectedFamilyHighlight(ProjectFamily) was not found.")
style_method = family_text[style_start:style_start + 1800]
if "ReferenceEquals(FamilyList.SelectedItem, selected)" not in style_method:
    fail("Deferred highlight styling must reject stale selections.")
if "GeneratorStatus.GeneratingContainers" not in style_method:
    fail("Deferred highlight styling must not touch containers while generation is active.")
if "ContainerFromItem(selected) as ListBoxItem" not in style_method:
    fail("Deferred highlight styling must resolve the realized selected Family container.")

sync_marker = "private void SyncFamilyFromSelection()"
sync_start = workspace_text.find(sync_marker)
if sync_start < 0:
    fail("SyncFamilyFromSelection() was not found.")
sync_method = workspace_text[sync_start:sync_start + 3200]
selected = sync_method.find("FamilyList.SelectedItem = family;")
refresh = sync_method.find("RefreshSelectedFamilyHighlight();")
if selected < 0 or refresh < 0 or selected > refresh:
    fail("Selection sync must route selected-family reveal through RefreshSelectedFamilyHighlight().")

print("PASS: Workspace family reveal is dispatcher-deferred and generator-safe.")
