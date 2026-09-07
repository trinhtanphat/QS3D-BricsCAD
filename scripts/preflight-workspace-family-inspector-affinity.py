#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AFFINITY = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FamilySelectionAffinity.cs"
PANEL = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def method_body(text, signature, next_signature=None):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    if next_signature:
        end = text.find(next_signature, start + len(signature))
        return text[start:end if end >= 0 else len(text)]
    return text[start:]


affinity = read(AFFINITY)
panel = read(PANEL)
selection = method_body(affinity, "private void OnFamilySelectionChangedWithAffinity")
legacy = method_body(panel, "private void OnFamilySelectionChanged", "private void OnFamilySearchChanged")

for forbidden, message in [
    ("EventManager.RegisterClassHandler", "Family inspector fence must remain instance-scoped"),
    ("OnFamilyListSelectionChangedClass", "obsolete ListBox class callback must be removed"),
    ("FindOwningWorkspacePanel", "obsolete visual-tree owner lookup must be removed"),
]:
    if forbidden in affinity:
        errors.append(message)

if "OnFamilySelectionChangedWithAffinity();" not in legacy:
    errors.append("FamilyList XAML handler must delegate directly to the affinity-safe path")
if "_viewModel.SetActiveFamily(" in legacy:
    errors.append("FamilyList XAML handler must not call void SetActiveFamily directly")
if "_viewModel.ShowFamilyProperties();" in legacy:
    errors.append("FamilyList XAML handler must not render properties outside the affinity fence")

for required, message in [
    ("if (_loadingContext) return;", "affinity-safe handler must preserve reconciliation reentrancy suppression"),
    ("var selectedFamily = FamilyList.SelectedItem as ProjectFamily;", "affinity-safe handler must capture selected Family explicitly"),
    ("TryActivateFamilyForWorkspaceAction(selectedFamily", "selected Family must pass the canonical document/project/generation activation fence"),
    ("RefreshProject();", "rejected stale Family selection must reconcile Workspace from the current document/project"),
    ("_viewModel.ShowFamilyProperties();", "property inspector must render after successful activation"),
]:
    if required not in selection:
        errors.append(message)

activation = selection.find("TryActivateFamilyForWorkspaceAction(selectedFamily")
show = selection.find("_viewModel.ShowFamilyProperties();")
if activation >= 0 and show >= 0 and activation > show:
    errors.append("Family properties are rendered before selected-Family affinity/activation succeeds")

reject = selection.find("!TryActivateFamilyForWorkspaceAction(selectedFamily")
refresh = selection.find("RefreshProject();", reject if reject >= 0 else 0)
if reject < 0:
    errors.append("affinity-safe handler must branch on failed canonical Family activation")
elif refresh < 0:
    errors.append("failed canonical Family activation must refresh/reconcile Workspace")
else:
    between = selection[reject:refresh]
    if "ShowFamilyProperties" in between:
        errors.append("rejected stale Family path must not repopulate old property rows")

null_check = selection.find("if (selectedFamily == null)")
first_refresh = selection.find("RefreshProject();", null_check if null_check >= 0 else 0)
if null_check < 0 or first_refresh < 0:
    errors.append("cleared Family selection must reconcile Workspace instead of retaining stale state")

print("QS3D Workspace Family inspector affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: FamilyList selection delegates through the current-document/project Family affinity fence and stale selections reconcile before property rows render.")
