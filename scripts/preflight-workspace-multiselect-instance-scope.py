#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.MultiSelectionProperties.cs"


def fail(message: str) -> None:
    print("ERROR: workspace multi-selection instance-scope preflight failed: " + message)
    raise SystemExit(1)


def main() -> None:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private void PresentMultiSelection(")
    end = text.find("private void LoadMultiSelectionRows(", start)
    if start < 0 or end < 0:
        fail("cannot isolate PresentMultiSelection implementation")

    method = text[start:end]
    required = (
        "_viewModel.SetSelectedElement(null);",
        "_viewModel.PropertyScopes.Clear();",
        "_viewModel.PropertyScopes.Add(WorkspaceViewModel.FamilyScope);",
        "_viewModel.SelectedPropertyScope = WorkspaceViewModel.FamilyScope;",
        "FamilyList.SelectedItem = commonFamily;",
        "LoadMultiSelectionRows(project, inspection);",
    )
    for marker in required:
        if marker not in method:
            fail("missing required fail-closed/presentation marker: " + marker)

    forbidden = (
        "scopeAnchor",
        "_viewModel.SetSelectedElement(scopeAnchor)",
        "_viewModel.PropertyScopes.Add(WorkspaceViewModel.InstanceScope)",
        "_viewModel.SelectedPropertyScope = WorkspaceViewModel.InstanceScope",
    )
    for marker in forbidden:
        if marker in method:
            fail("multi-selection reintroduced mutable Instance scope: " + marker)

    clear_pos = method.find("_viewModel.SetSelectedElement(null);")
    render_pos = method.find("FamilyList.SelectedItem = commonFamily;")
    rows_pos = method.find("LoadMultiSelectionRows(project, inspection);")
    if not (0 <= clear_pos < render_pos < rows_pos):
        fail("selected Instance context must be cleared before Family presentation and aggregate rows")

    print("PASS workspace multi-selection clears Instance scope while preserving Family presentation")


if __name__ == "__main__":
    main()
