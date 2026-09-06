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
class_handler = method_body(affinity, "private static void OnFamilyListSelectionChangedClass", "private static WorkspacePanel? FindOwningWorkspacePanel")
selection = method_body(affinity, "private void OnFamilySelectionChangedWithAffinity")
legacy = method_body(panel, "private void OnFamilySelectionChanged", "private void OnFamilySearchChanged")

for required, message in [
    ("EventManager.RegisterClassHandler(", "Workspace must register a class-level SelectionChanged fence"),
    ("typeof(ListBox)", "class handler must be registered on ListBox so it runs before the source ListBox instance/XAML handler"),
    ("Selector.SelectionChangedEvent", "class handler must target Selector.SelectionChangedEvent"),
    ("FindOwningWorkspacePanel(familyList)", "global ListBox class handler must resolve and restrict itself to an owning WorkspacePanel"),
]:
    if required not in affinity:
        errors.append(message)

if "sender is ListBox familyList" not in class_handler:
    errors.append("class handler must require a ListBox routed-event source")
if "string.Equals(familyList.Name, \"FamilyList\", StringComparison.Ordinal)" not in class_handler:
    errors.append("global ListBox class handler must reject unrelated ListBox controls by name")
if "ReferenceEquals(familyList, panel.FamilyList)" not in class_handler:
    errors.append("class handler must prove the source is the exact FamilyList owned by the resolved WorkspacePanel")
if "e.Handled = true;" not in class_handler:
    errors.append("FamilyList event must be marked handled before its stale instance/XAML handler can run")
if "panel.OnFamilySelectionChangedWithAffinity();" not in class_handler:
    errors.append("class handler must delegate to the affinity-safe Family selection path")

for required, message in [
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

# The old XAML handler remains as compatibility fallback. This assertion intentionally
# fails if that shape changes so the source-level class-handler pre-emption is reviewed.
if "_viewModel.SetActiveFamily(FamilyList.SelectedItem as ProjectFamily);" not in legacy:
    errors.append("legacy Family selection handler shape changed; reassess whether class-level pre-emption is still required")

print("QS3D Workspace Family inspector affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: source ListBox class handler pre-empts the legacy FamilyList handler; stale project-generation Families are rejected and reconciled before property rows render.")
