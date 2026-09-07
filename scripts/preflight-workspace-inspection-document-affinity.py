#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
AFFINITY_SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DocumentAffinity.cs"
WORKSPACE_SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
PALETTE_SOURCE = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
SELECTION_SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs"
errors = []


def read_source(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


affinity = read_source(AFFINITY_SOURCE)
workspace = read_source(WORKSPACE_SOURCE)
palette = read_source(PALETTE_SOURCE)
selection = read_source(SELECTION_SOURCE)

# Preserve the merged #5945 lifecycle contract: modeless presentation is invalidated
# synchronously at native document activation/destruction and subscriptions are symmetric.
for needle in [
    "private static readonly bool DocumentAffinityRegistrationReady",
    "private bool _workspaceDocumentAffinityAttached;",
    "Application.DocumentManager.DocumentActivated += OnWorkspaceDocumentActivated;",
    "Application.DocumentManager.DocumentActivated -= OnWorkspaceDocumentActivated;",
    "Application.DocumentManager.DocumentToBeDestroyed += OnWorkspaceDocumentToBeDestroyed;",
    "Application.DocumentManager.DocumentToBeDestroyed -= OnWorkspaceDocumentToBeDestroyed;",
    "private void InvalidateWorkspaceDocumentState",
    "ClearProject(",
]:
    if needle not in affinity:
        errors.append("Workspace lifecycle affinity contract missing token: " + needle)

if "Dispatcher.BeginInvoke" in affinity or ".BeginInvoke(" in affinity:
    errors.append("Workspace document invalidation must stay synchronous")

for forbidden in [
    "CadHandleService.",
    "SetImpliedSelection",
    "SendStringToExecute",
    "ExistingProjectMutationContext",
    "ProjectContextCoordinator.GetOrCreate",
    "DocumentLock",
]:
    if forbidden in affinity:
        errors.append("Workspace lifecycle invalidation must remain presentation-only: " + forbidden)

# #6037 payload contract: the exact source Document must be carried from the selection
# authority through PaletteCoordinator into Workspace inspection state. A consumer must
# never infer ownership from whichever MDI document happens to be active later.
selection_required = [
    "PaletteCoordinator.SetInspection(document, snapshots);",
]
for needle in selection_required:
    if needle not in selection:
        errors.append("Selection producer must publish exact source Document with snapshots: " + needle)

palette_required = [
    "public static void SetInspection(Document sourceDocument, IReadOnlyList<EntitySnapshot> snapshots)",
    "ReferenceEquals(sourceDocument, Application.DocumentManager.MdiActiveDocument)",
    "ProjectContextCoordinator.TryGetReadOnly(sourceDocument, out var currentProject)",
    "_workspacePanel?.SetInspectionReadOnly(sourceDocument, snapshots, project);",
]
for needle in palette_required:
    if needle not in palette:
        errors.append("Palette inspection handoff missing exact-document token: " + needle)

if "var document = Application.DocumentManager.MdiActiveDocument;" in palette:
    set_start = palette.find("public static void SetInspection(")
    status_start = palette.find("public static void SetStatus", set_start)
    set_body = palette[set_start:status_start if status_start >= 0 else len(palette)] if set_start >= 0 else ""
    if "var document = Application.DocumentManager.MdiActiveDocument;" in set_body:
        errors.append("Palette SetInspection must not infer the payload owner from current MDI state")

workspace_required = [
    "private Document? _inspectionSourceDocument;",
    "public void SetInspectionReadOnly(Document sourceDocument, IReadOnlyList<EntitySnapshot> snapshots, ProjectState? project)",
    "_inspectionSourceDocument = sourceDocument;",
    "_inspectionSourceDocument = null;",
    "var sourceDocument = _inspectionSourceDocument;",
    "var activeDocument = Application.DocumentManager.MdiActiveDocument;",
    "!ReferenceEquals(sourceDocument, activeDocument)",
]
for needle in workspace_required:
    if needle not in workspace:
        errors.append("Workspace inspection payload affinity missing token: " + needle)

sync_start = workspace.find("private void SyncFamilyFromSelection")
sync_end = workspace.find("private ", sync_start + len("private void SyncFamilyFromSelection")) if sync_start >= 0 else -1
sync_body = workspace[sync_start:sync_end if sync_end >= 0 else len(workspace)] if sync_start >= 0 else ""
if not sync_body:
    errors.append("missing SyncFamilyFromSelection")
else:
    identity = sync_body.find("!ReferenceEquals(sourceDocument, activeDocument)")
    database = sync_body.find(".Database")
    transaction = sync_body.find("StartTransaction")
    handle_lookup = sync_body.find("GetObjectId")
    if identity < 0:
        errors.append("SyncFamilyFromSelection must fail closed on source/active Document mismatch")
    for name, index in [("Database", database), ("StartTransaction", transaction), ("GetObjectId", handle_lookup)]:
        if index >= 0 and identity >= 0 and index < identity:
            errors.append("SyncFamilyFromSelection touches %s before exact document-affinity fence" % name)
    if "var document = Application.DocumentManager.MdiActiveDocument;" in sync_body:
        errors.append("SyncFamilyFromSelection must not treat current MDI document as the inspection source")

# Clearing/invalidation must release the native Document reference so a detached modeless
# panel cannot retain or resurrect stale payload ownership.
clear_start = workspace.find("public void ClearInspection")
clear_end = workspace.find("public ", clear_start + len("public void ClearInspection")) if clear_start >= 0 else -1
clear_body = workspace[clear_start:clear_end if clear_end >= 0 else len(workspace)] if clear_start >= 0 else ""
if clear_body and "_inspectionSourceDocument = null;" not in clear_body:
    errors.append("ClearInspection must release inspection source Document identity")

# This fix must not add process-wide/native event subscriptions to the payload path.
for text, label in [(workspace, "Workspace"), (palette, "PaletteCoordinator"), (selection, "SelectionSyncCoordinator")]:
    if "EventManager.RegisterClassHandler" in text:
        errors.append(label + " inspection payload path must not add process-wide WPF class handlers")

print("QS3D Workspace inspection document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Workspace preserves #5945 lifecycle invalidation and binds every inspection payload to the exact source Document before project/database/handle consumption.")
