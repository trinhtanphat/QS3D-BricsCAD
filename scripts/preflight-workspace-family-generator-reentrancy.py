#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.FamilySubtype.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


text = TARGET.read_text(encoding="utf-8")
marker = "private void RefreshSelectedFamilyHighlight()"
start = text.find(marker)
if start < 0:
    fail("RefreshSelectedFamilyHighlight() was not found.")

method = text[start:start + 2200]
guard = method.find("GeneratorStatus.GeneratingContainers")
scroll = method.find("FamilyList.ScrollIntoView(selected);")
layout = method.find("FamilyList.UpdateLayout();")

if guard < 0:
    fail("RefreshSelectedFamilyHighlight() must guard ItemContainerGenerator reentrancy.")
if scroll < 0 or layout < 0:
    fail("Expected centralized FamilyList reveal/layout path is missing.")
if not (guard < scroll < layout):
    fail("Generator guard must run before ScrollIntoView and UpdateLayout.")
if text.count("FamilyList.ScrollIntoView(") != 1:
    fail("FamilyList scrolling must stay centralized in RefreshSelectedFamilyHighlight().")

print("PASS: Workspace family highlight refresh is generator-safe.")
