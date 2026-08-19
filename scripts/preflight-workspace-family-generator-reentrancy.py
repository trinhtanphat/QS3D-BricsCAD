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
marker = "private void RefreshSelectedFamilyHighlight()"
start = family_text.find(marker)
if start < 0:
    fail("RefreshSelectedFamilyHighlight() was not found.")

method = family_text[start:start + 2200]
guard = method.find("GeneratorStatus.GeneratingContainers")
scroll = method.find("FamilyList.ScrollIntoView(selected);")
layout = method.find("FamilyList.UpdateLayout();")

if guard < 0:
    fail("RefreshSelectedFamilyHighlight() must guard ItemContainerGenerator reentrancy.")
if scroll < 0 or layout < 0:
    fail("Expected centralized FamilyList reveal/layout path is missing.")
if not (guard < scroll < layout):
    fail("Generator guard must run before ScrollIntoView and UpdateLayout.")
if family_text.count("FamilyList.ScrollIntoView(") != 1:
    fail("FamilyList scrolling must stay centralized in RefreshSelectedFamilyHighlight().")
if "FamilyList.ScrollIntoView(" in workspace_text:
    fail("WorkspacePanel.xaml.cs must not bypass the generator-safe FamilyList reveal path.")

sync_marker = "private void SyncFamilyFromSelection()"
sync_start = workspace_text.find(sync_marker)
if sync_start < 0:
    fail("SyncFamilyFromSelection() was not found.")
sync_method = workspace_text[sync_start:sync_start + 3200]
selected = sync_method.find("FamilyList.SelectedItem = family;")
refresh = sync_method.find("RefreshSelectedFamilyHighlight();")
if selected < 0 or refresh < 0 or selected > refresh:
    fail("Selection sync must route selected-family reveal through RefreshSelectedFamilyHighlight().")

print("PASS: Workspace family reveal paths are generator-safe.")
