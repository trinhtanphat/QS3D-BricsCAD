#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.DocumentAffinity.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing Workspace affinity source: " + str(SOURCE.relative_to(ROOT)))
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

required = [
    "public partial class WorkspacePanel",
    "private static readonly bool DocumentAffinityRegistrationReady",
    "private bool _workspaceDocumentAffinityAttached;",
    "FrameworkElement.LoadedEvent",
    "FrameworkElement.UnloadedEvent",
    "Application.DocumentManager.DocumentActivated += OnWorkspaceDocumentActivated;",
    "Application.DocumentManager.DocumentActivated -= OnWorkspaceDocumentActivated;",
    "Application.DocumentManager.DocumentToBeDestroyed += OnWorkspaceDocumentToBeDestroyed;",
    "Application.DocumentManager.DocumentToBeDestroyed -= OnWorkspaceDocumentToBeDestroyed;",
    "private void InvalidateWorkspaceDocumentState",
    "ClearProject(",
]
for needle in required:
    if needle not in text:
        errors.append("Workspace affinity source missing token: " + needle)

if "Dispatcher.BeginInvoke" in text or ".BeginInvoke(" in text:
    errors.append("Workspace document invalidation must be synchronous; Dispatcher queuing re-opens the activation-to-idle stale-state window")

for forbidden in [
    "CadHandleService.",
    "SetImpliedSelection",
    "SendStringToExecute",
    "ExistingProjectMutationContext",
    "ProjectContextCoordinator.GetOrCreate",
    "DocumentLock",
]:
    if forbidden in text:
        errors.append("Workspace affinity invalidation must remain presentation-only: " + forbidden)


def method_body(signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]

loaded = method_body("private static void OnWorkspaceAffinityLoaded", "private static void OnWorkspaceAffinityUnloaded")
if loaded:
    if "AttachWorkspaceDocumentAffinity" not in loaded:
        errors.append("Workspace Loaded must attach document affinity")
    if "InvalidateWorkspaceDocumentState" not in loaded:
        errors.append("Workspace Loaded must invalidate state that may have gone stale while detached")
    if "RefreshProject" not in loaded:
        errors.append("Workspace Loaded must rehydrate active project after stale-state invalidation")

attach = method_body("private void AttachWorkspaceDocumentAffinity", "private void DetachWorkspaceDocumentAffinity")
if attach:
    if "if (_workspaceDocumentAffinityAttached) return;" not in attach:
        errors.append("Workspace affinity attach must be idempotent")
    if "_workspaceDocumentAffinityAttached = true;" not in attach:
        errors.append("Workspace affinity attach must publish attached state only after both native hooks succeed")

detach = method_body("private void DetachWorkspaceDocumentAffinity", "private void OnWorkspaceDocumentActivated")
if detach:
    if "if (!_workspaceDocumentAffinityAttached) return;" not in detach:
        errors.append("Workspace affinity detach must be idempotent")
    if "_workspaceDocumentAffinityAttached = false;" not in detach:
        errors.append("Workspace affinity detach must clear attached state")

activated = method_body("private void OnWorkspaceDocumentActivated", "private void OnWorkspaceDocumentToBeDestroyed")
if activated and "InvalidateWorkspaceDocumentState" not in activated:
    errors.append("DocumentActivated must synchronously invalidate stale Workspace state")

destroyed = method_body("private void OnWorkspaceDocumentToBeDestroyed", "private void InvalidateWorkspaceDocumentState")
if destroyed:
    if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, e.Document)" not in destroyed:
        errors.append("destroy handling must only invalidate when the active/owner document is going away")
    if "InvalidateWorkspaceDocumentState" not in destroyed:
        errors.append("DocumentToBeDestroyed must invalidate active Workspace state")

invalidate = method_body("private void InvalidateWorkspaceDocumentState", "}")
if invalidate and "ClearProject(" not in invalidate:
    errors.append("Workspace invalidation must clear inspection/family/project presentation before deferred reconcile")

print("QS3D Workspace document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Workspace synchronously invalidates document-bound presentation on activation/destruction, safely covers unload/reload gaps, and leaves CAD/project mutation to active-document reconciliation.")
