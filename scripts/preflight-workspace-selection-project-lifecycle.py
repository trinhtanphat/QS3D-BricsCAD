#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
PALETTE = ADAPTER / "PaletteCoordinator.cs"
SELECTION = ADAPTER / "UI" / "WorkspacePanel.SelectionInspection.cs"
RESOLVER = ADAPTER / "UI" / "WorkspacePanel.MultiSelectionProperties.cs"
LEGACY = ADAPTER / "UI" / "WorkspacePanel.xaml.cs"
INBOX = ROOT / "docs" / "LOCAL-AGENT-INBOX.md"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def region(text, start_token, end_token, label):
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end <= start:
        errors.append("cannot isolate " + label)
        return ""
    return text[start:end]


palette = read(PALETTE)
selection = read(SELECTION)
resolver = read(RESOLVER)
legacy = read(LEGACY)
inbox = read(INBOX)

for token in (
    "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
    "_workspacePanel?.SetInspectionReadOnly(snapshots, project)",
):
    if token not in palette:
        errors.append("Palette selection sync missing read-only lifecycle token: " + token)

set_region = region(palette, "public static void SetInspection(", "public static void SetStatus", "PaletteCoordinator.SetInspection")
if "ProjectContextCoordinator.GetOrCreate" in set_region:
    errors.append("Palette selection sync must not create/cache project state")
if ".SetInspection(snapshots)" in set_region:
    errors.append("Palette selection sync must not route implicit events through the compatibility path")

for token in (
    "internal void SetInspectionReadOnly(",
    "if (project == null || _inspection.Count == 0)",
    "TryResolveSemanticSelection(project, _inspection, out var selectedElements, out var selectionError)",
    "PresentMultiSelection(project, selectedElements)",
    "project.FindFamily(singleElement.FamilyId)",
    "_viewModel.SetSelectedElement(singleElement)",
):
    if token not in selection:
        errors.append("Workspace read-only selection partial missing token: " + token)
if "SemanticReferenceHandles.GetSelectionAliases(element)" not in resolver:
    errors.append("Workspace semantic selection resolver missing source/generated alias coverage")
read_only_resolver = resolver[:resolver.find("private string ApplyMultiSelectionProperty")]
if "ProjectContextCoordinator.GetOrCreate" in read_only_resolver or "ExistingProjectMutationContext" in read_only_resolver:
    errors.append("Workspace read-only semantic selection resolution must not create/bind mutable project state")
if "ProjectContextCoordinator.GetOrCreate" in selection or "ExistingProjectMutationContext" in selection:
    errors.append("Workspace read-only selection partial must not bind/create mutable project state")

if "public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)" not in legacy:
    errors.append("WorkspacePanel.SetInspection compatibility method unexpectedly disappeared")
if "SyncFamilyFromSelection();" not in legacy:
    errors.append("WorkspacePanel.SetInspection compatibility method must continue semantic sync")

sync_region = region(legacy, "private void SyncFamilyFromSelection()", "private void OnZoneChanged", "WorkspacePanel.SyncFamilyFromSelection")
if "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)" not in sync_region:
    errors.append("Workspace compatibility selection sync must resolve the project read-only")
if "ProjectContextCoordinator.GetOrCreate" in sync_region or "ExistingProjectMutationContext" in sync_region:
    errors.append("Workspace compatibility selection sync must not create/bind mutable project state")

add_region = region(legacy, "private void OnAddClick", "private void OnDeleteClick", "WorkspacePanel.OnAddClick")
for token in (
    "var selected = FamilyList.SelectedItem as ProjectFamily;",
    "selected == null",
    "ProjectContextCoordinator.GetOrCreate(doc)",
    "ExistingProjectMutationContext.Require(doc, \"Nhân bản Family từ Workspace\")",
):
    if token not in add_region:
        errors.append("Workspace Family create/duplicate boundary missing token: " + token)
if add_region.count("ProjectContextCoordinator.GetOrCreate(doc)") != 1:
    errors.append("Workspace Family add must keep exactly one creation-capable resolver for explicit create-new intent")

delete_region = region(legacy, "private void OnDeleteClick", "private void OnCaptureSelectedClick", "WorkspacePanel.OnDeleteClick")
if "ExistingProjectMutationContext.Require(doc, \"Xóa Family từ Workspace\")" not in delete_region:
    errors.append("Workspace Family delete must require an existing canonical project")
if "ProjectContextCoordinator.GetOrCreate" in delete_region:
    errors.append("Workspace Family delete must not create a replacement project")

source_region = region(legacy, "private int SelectInspectionSemanticSourcesForBuild()", "private void ApplyFamilyFilter", "WorkspacePanel.SelectInspectionSemanticSourcesForBuild")
if "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)" not in source_region:
    errors.append("Workspace source-handle restore must resolve semantic state read-only")
if "ProjectContextCoordinator.GetOrCreate" in source_region or "ExistingProjectMutationContext" in source_region:
    errors.append("Workspace source-handle restore must not create/bind mutable project state")

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

print("PASS: Workspace selection/source inspection stays read-only, delete/duplicate require an existing canonical project, and only explicit create-new Family intent may create project state.")
