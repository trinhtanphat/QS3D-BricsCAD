#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
PALETTE = ADAPTER / "PaletteCoordinator.cs"
SELECTION = ADAPTER / "UI" / "WorkspacePanel.SelectionInspection.cs"
LEGACY = ADAPTER / "UI" / "WorkspacePanel.xaml.cs"
VIEW_MODEL = ADAPTER / "UI" / "ViewModels" / "WorkspaceViewModel.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")

palette = read(PALETTE)
selection = read(SELECTION)
legacy = read(LEGACY)
view_model = read(VIEW_MODEL)
inbox = read(INBOX)

for token in (
    "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
    "_workspacePanel?.SetInspectionReadOnly(snapshots, project)",
):
    if token not in palette:
        errors.append("Palette selection sync missing read-only lifecycle token: " + token)

set_start = palette.find("public static void SetInspection(")
set_end = palette.find("public static void SetStatus", set_start)
if set_start < 0 or set_end <= set_start:
    errors.append("cannot isolate PaletteCoordinator.SetInspection")
else:
    set_region = palette[set_start:set_end]
    if "ProjectContextCoordinator.GetOrCreate" in set_region:
        errors.append("Palette selection sync must not create/cache project state")
    if ".SetInspection(snapshots)" in set_region:
        errors.append("Palette selection sync must not call the legacy creating semantic-sync path")

for token in (
    "internal void SetInspectionReadOnly(",
    "if (project == null || _inspection.Count == 0)",
    "SemanticReferenceHandles.GetSelectionAliases(element)",
    "project.FindFamily(singleElement.FamilyId)",
    "_viewModel.SetInspectedElementReadOnly(singleElement)",
    "_inspection = snapshots ?? Array.Empty<EntitySnapshot>()",
    "ApplyFamilyFilter()",
):
    if token not in selection:
        errors.append("Workspace read-only selection partial missing token: " + token)
if "ProjectContextCoordinator.GetOrCreate" in selection or "ExistingProjectMutationContext" in selection:
    errors.append("Workspace read-only selection partial must not bind/create mutable project state")

for token in (
    "public void SetInspectedElementReadOnly(ProjectElement? element) => SetSelectedElementCore(element, false);",
    "if (activateFamily) ProjectFamilyActivationService.SetActive(_project, family.Id);",
):
    if token not in view_model:
        errors.append("Workspace read-only inspector activation policy missing token: " + token)

# Keep the old public method source for compatibility, but PaletteCoordinator must no longer route
# implicit selection events through it because it historically calls GetOrCreate.
if "public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)" not in legacy:
    errors.append("legacy WorkspacePanel.SetInspection compatibility method unexpectedly disappeared")
if "SyncFamilyFromSelection();" not in legacy:
    errors.append("legacy WorkspacePanel.SetInspection contract changed unexpectedly")

for token in (
    "LOCAL-011 — staged native rollback and post-commit UI isolation",
    "unavailable-project palette",
    "stale callbacks cannot mutate the prior project",
):
    if token not in inbox:
        errors.append("LOCAL-011 workspace-unavailable runtime handoff missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: implicit Workspace CAD-selection sync remains read-only/non-creating while preserving raw inspection and the LOCAL-011 unavailable-project runtime scenario.")
