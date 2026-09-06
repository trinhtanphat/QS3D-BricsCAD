#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.DocumentAffinity.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RightPanel affinity source: " + str(SOURCE.relative_to(ROOT)))
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

required = [
    "public partial class RightPanel",
    "private bool _documentAffinityAttached;",
    "FrameworkElement.LoadedEvent",
    "FrameworkElement.UnloadedEvent",
    "Application.DocumentManager.DocumentActivated += OnRightPanelDocumentActivated;",
    "Application.DocumentManager.DocumentActivated -= OnRightPanelDocumentActivated;",
    "Application.DocumentManager.DocumentToBeDestroyed += OnRightPanelDocumentToBeDestroyed;",
    "Application.DocumentManager.DocumentToBeDestroyed -= OnRightPanelDocumentToBeDestroyed;",
    "private void InvalidateRightPanelDocumentState",
    "_refreshingDrawings = true;",
    "_refreshingLayers = true;",
    "_viewModel.Drawings.Clear();",
    "_viewModel.Layers.Clear();",
    "DrawingList?.UnselectAll();",
    "LayerList?.UnselectAll();",
]
for needle in required:
    if needle not in text:
        errors.append("RightPanel affinity source missing token: " + needle)

if "Dispatcher.BeginInvoke" in text or ".BeginInvoke(" in text:
    errors.append("document-affinity invalidation must be synchronous; Dispatcher queuing re-opens the activation-to-idle stale-row window")

# This partial is a presentation invalidation fence only. It must never mutate CAD,
# send commands or resolve stale row semantics against the new active document.
for forbidden in [
    "SetImpliedSelection",
    "XrefService.",
    "LayerVisibilityService.",
    "SendStringToExecute",
    "CadHandleService.",
    "DocumentLock",
]:
    if forbidden in text:
        errors.append("RightPanel affinity invalidation must not mutate CAD: " + forbidden)


def method_body(signature, next_signature):
    start = text.find(signature)
    if start < 0:
        errors.append("missing method: " + signature)
        return ""
    end = text.find(next_signature, start + len(signature))
    return text[start:end if end >= 0 else len(text)]

attach = method_body("private void AttachRightPanelDocumentAffinity", "private void DetachRightPanelDocumentAffinity")
if attach:
    if "if (_documentAffinityAttached) return;" not in attach:
        errors.append("RightPanel affinity attach must be idempotent")
    if "_documentAffinityAttached = true;" not in attach:
        errors.append("RightPanel affinity attach must publish attached state")

detach = method_body("private void DetachRightPanelDocumentAffinity", "private void OnRightPanelDocumentActivated")
if detach:
    if "if (!_documentAffinityAttached) return;" not in detach:
        errors.append("RightPanel affinity detach must be idempotent")
    if "_documentAffinityAttached = false;" not in detach:
        errors.append("RightPanel affinity detach must clear attached state")

activated = method_body("private void OnRightPanelDocumentActivated", "private void OnRightPanelDocumentToBeDestroyed")
if activated and "InvalidateRightPanelDocumentState" not in activated:
    errors.append("DocumentActivated must synchronously invalidate stale RightPanel rows")

destroyed = method_body("private void OnRightPanelDocumentToBeDestroyed", "private void InvalidateRightPanelDocumentState")
if destroyed:
    if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, e.Document)" not in destroyed:
        errors.append("destroy handling must invalidate when the owner/active document is going away")
    if "InvalidateRightPanelDocumentState" not in destroyed:
        errors.append("DocumentToBeDestroyed must invalidate owner rows")

invalidate = method_body("private void InvalidateRightPanelDocumentState", "}")
if invalidate:
    for needle in [
        "_refreshingDrawings = true;",
        "_refreshingLayers = true;",
        "_viewModel.Drawings.Clear();",
        "_viewModel.Layers.Clear();",
        "DrawingList?.UnselectAll();",
        "LayerList?.UnselectAll();",
    ]:
        if needle not in invalidate:
            errors.append("invalidation missing callback-safe clear token: " + needle)

print("QS3D RightPanel document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: RightPanel synchronously invalidates stale CAD rows on MDI activation/destruction, suppresses row callbacks during clear, and keeps this owner fence presentation-only with symmetric lifecycle hooks.")
