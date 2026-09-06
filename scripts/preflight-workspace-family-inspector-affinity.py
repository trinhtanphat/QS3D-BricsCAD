#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PANEL = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
VIEW_MODEL = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def method_body(text, signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]


panel = read(PANEL)
view_model = read(VIEW_MODEL)
selection = method_body(panel, "private void OnFamilySelectionChanged", "private void OnFamilySearchChanged")
activation = method_body(view_model, "public bool SetActiveFamily(ProjectFamily? family)", "public void ShowFamilyProperties")

if "public bool SetActiveFamily(ProjectFamily? family)" not in view_model:
    errors.append("SetActiveFamily must expose activation success/failure to UI callers")

for required, message in [
    ("return false;", "SetActiveFamily must fail closed with false when Family/current-project affinity is not proven"),
    ("ReferenceEquals(ownedFamily, family)", "SetActiveFamily must retain exact Family object-identity validation"),
    ("ProjectFamilyActivationService.SetActive(project, family.Id)", "SetActiveFamily must retain canonical project activation"),
    ("return true;", "SetActiveFamily must report true only after successful canonical activation and property-state update"),
]:
    if required not in activation:
        errors.append(message)

if "var selectedFamily = FamilyList.SelectedItem as ProjectFamily;" not in selection:
    errors.append("Family selection handler must capture the selected Family explicitly")
if "if (selectedFamily != null && !_viewModel.SetActiveFamily(selectedFamily))" not in selection:
    errors.append("Family selection handler must stop property rendering when selected-Family activation is rejected")
if "RefreshProject();" not in selection:
    errors.append("Rejected stale Family selection must reconcile Workspace from the current document/project")

activation_call = selection.find("_viewModel.SetActiveFamily(selectedFamily)")
show = selection.find("_viewModel.ShowFamilyProperties()")
if activation_call >= 0 and show >= 0 and activation_call > show:
    errors.append("Family properties are rendered before selected-Family activation succeeds")

rejected = selection.find("!_viewModel.SetActiveFamily(selectedFamily)")
refresh = selection.find("RefreshProject();", rejected if rejected >= 0 else 0)
if rejected >= 0 and refresh >= 0:
    between = selection[rejected:refresh]
    if "ShowFamilyProperties" in between:
        errors.append("Rejected stale Family path must not repopulate old Family property rows before reconciliation")

print("QS3D Workspace Family inspector affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Family property inspector renders only after selected-Family activation succeeds; stale generation rejection reconciles Workspace before old rows can persist.")
