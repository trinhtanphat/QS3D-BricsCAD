#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
VIEW_MODEL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ViewModels" / "WorkspaceViewModel.cs"
SELECTION = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.SelectionInspection.cs"
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


view_model = read(VIEW_MODEL)
selection = read(SELECTION)
inbox = read(INBOX)

for token in (
    "private bool TryGetCurrentProjectForMutation(string operation, out ProjectState project)",
    "ExistingProjectMutationContext.TryGet(document, out var current)",
    "ReferenceEquals(current, _project)",
    "Workspace đang giữ project stale sau reload/thay thế",
    "không tạo project thay thế",
):
    if token not in view_model:
        errors.append("Workspace canonical mutation guard missing: " + token)

load = region(view_model, "public void Load(ProjectState project)", "public int ActiveZoneIndex()", "WorkspaceViewModel.Load")
for forbidden in (
    "SynchronizeActiveCatalogs",
    "ProjectZoneService.SetActive",
    "ProjectFloorService.SetActive",
    "ProjectFamilyActivationService.SetActive",
    ".Touch()",
):
    if forbidden in load:
        errors.append("Workspace Load must remain read-only; found: " + forbidden)

selected = region(view_model, "public void SetSelectedElement(ProjectElement? element)", "private void LoadCurrentProperties()", "WorkspaceViewModel.SetSelectedElement")
for forbidden in (
    "ProjectFamilyActivationService.SetActive",
    "ProjectZoneService.SetActive",
    "ProjectFloorService.SetActive",
    ".Touch()",
    "SetProperty(",
):
    if forbidden in selected:
        errors.append("Workspace read-only semantic selection still mutates project state: " + forbidden)

mutation_regions = (
    ("public void SetActiveZone(string? name)", "public void SetActiveFloor(string? name)", "SetActiveZone"),
    ("public void SetActiveFloor(string? name)", "public void SetActiveFamily(ProjectFamily? family)", "SetActiveFloor"),
    ("public void SetActiveFamily(ProjectFamily? family)", "public void ShowFamilyProperties()", "SetActiveFamily"),
    ("private string ApplyFamilyName(ProjectFamily family, string value)", "private string ApplyFamilyProperty", "ApplyFamilyName"),
    ("private string ApplyFamilyProperty(ProjectFamily family, string key, string unit, string value)", "private string ApplyInstanceProperty", "ApplyFamilyProperty"),
    ("private string ApplyInstanceProperty(ProjectElement element, ProjectFamily family, string key, string unit, PropertyRowViewModel row, string value)", "private bool TryGetCurrentProjectForMutation", "ApplyInstanceProperty"),
)
for start_token, end_token, label in mutation_regions:
    body = region(view_model, start_token, end_token, label)
    if "TryGetCurrentProjectForMutation(" not in body:
        errors.append(label + " must verify current canonical project before mutation")

if "SynchronizeActiveCatalogs(" in view_model:
    errors.append("obsolete mutating Workspace load-normalization helper must not remain callable")

for token in (
    "internal void SetInspectionReadOnly(",
    "_viewModel.SetSelectedElement(singleElement)",
):
    if token not in selection:
        errors.append("Workspace read-only selection bridge missing: " + token)
if "ProjectContextCoordinator.GetOrCreate" in selection or "ExistingProjectMutationContext" in selection:
    errors.append("read-only selection bridge must not create or bind a mutable project")

for token in (
    "LOCAL-011 — staged native rollback and post-commit UI isolation",
    "unavailable-project palette",
    "stale callbacks cannot mutate the prior project",
):
    if token not in inbox:
        errors.append("LOCAL-011 already-defined Workspace runtime proof missing: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Workspace load/selection are project-read-only and all modeless authoring writes verify the current canonical existing project before mutation.")
