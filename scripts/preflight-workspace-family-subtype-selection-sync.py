#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SYNC = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySubtypeSelectionSync.cs"
PANEL = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []


def read(path: Path, label: str) -> str:
    if not path.is_file():
        errors.append("missing " + label)
        return ""
    return path.read_text(encoding="utf-8")


sync = read(SYNC, "WorkspacePanel.FamilySubtypeSelectionSync.cs")
panel = read(PANEL, "WorkspacePanel.xaml.cs")

for token in (
    "Selector.SelectionChangedEvent",
    "ReferenceEquals(e.OriginalSource, panel.FamilyList)",
    "!_loadingContext",
    "_applyingFamilySubtypeFilter",
    "_inspection.Count == 0",
    "family.Category == ElementCategory.Foundation",
    "? InferFoundationSubtype(family.Name)",
    ": string.Empty;",
    "_familySubtypeFilter = inferred;",
    "_categoryFilter = family.Category;",
    "if (inferred.Length == 0)",
    "ApplyFamilyFilter();",
    "ApplyFamilySubtypeFilter();",
):
    if token not in sync:
        errors.append("CAD-selection subtype resync contract missing: " + token)

for forbidden in (
    "ProjectContextCoordinator",
    "ExistingProjectMutationContext",
    "ProjectFamilyService",
    "AuditTrail",
    "SendStringToExecute",
    "CadHandleService",
    ".Touch(",
):
    if forbidden in sync:
        errors.append("Subtype resync must remain UI-only/read-only: " + forbidden)

method_start = panel.find("private void SyncFamilyFromSelection()")
method_end = panel.find("private void OnZoneChanged", method_start)
method = panel[method_start:method_end] if method_start >= 0 and method_end > method_start else ""
if not method:
    errors.append("SyncFamilyFromSelection method was not found")
else:
    loading = method.find("_loadingContext = true;")
    selected = method.find("FamilyList.SelectedItem = family;")
    if loading < 0 or selected < 0 or loading >= selected:
        errors.append("CAD selection must set FamilyList.SelectedItem while _loadingContext is active")

print("QS3D Workspace CAD-selection subtype sync preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: programmatic CAD-family selection resynchronizes/clears Foundation subtype state before later search filtering, while remaining UI-only and non-mutating.")
