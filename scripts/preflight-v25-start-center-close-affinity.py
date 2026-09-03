#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
errors = []


def require(text, token, label):
    if token not in text:
        errors.append(label + ": missing " + token)


def forbid(text, token, label):
    if token in text:
        errors.append(label + ": forbidden " + token)


if not SOURCE.is_file():
    errors.append("missing Start Center source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

# A close/destruction notification is an explicit suppression decision, not a false value that can
# be swallowed by an earlier OR-coalesced activation. A later activation must still be able to
# supersede that suppression, while re-entrant refresh scheduling must preserve the pending intent.
for token, label in (
    ("private enum ActiveDrawingRecordIntent", "refresh intent must be explicit/tri-state"),
    ("Preserve", "re-entrant scheduling must preserve pending intent"),
    ("Record", "activation must request recording"),
    ("Suppress", "document destruction must suppress recording"),
    ("QueueHomeRefresh(ActiveDrawingRecordIntent.Record)", "normal activation must record"),
    ("QueueHomeRefresh(ActiveDrawingRecordIntent.Suppress)", "destroy transition must suppress"),
    ("QueueHomeRefresh(ActiveDrawingRecordIntent.Preserve)", "re-entrant refresh must preserve intent"),
    ("_queuedActiveDrawingRecordIntent = intent;", "latest explicit lifecycle intent must win"),
    ("var recordActiveDrawing = _queuedActiveDrawingRecordIntent == ActiveDrawingRecordIntent.Record;", "drain must derive record decision from queued intent"),
    ("_queuedActiveDrawingRecordIntent = ActiveDrawingRecordIntent.Preserve;", "drain/close must clear queued intent"),
):
    require(source, token, label)

for token, label in (
    ("_queuedRecordActiveDrawing |= recordActiveDrawing;", "OR coalescing lets a closing document remain recordable"),
    ("private bool _queuedRecordActiveDrawing;", "boolean queue state cannot represent preserve vs suppress"),
):
    forbid(source, token, label)

# Keep lifecycle cleanup and display-only refresh invariants pinned while repairing coalescing.
for token, label in (
    ("DocumentActivated += OnHostDocumentActivated", "host activation subscription must remain"),
    ("DocumentToBeDestroyed += OnHostDocumentToBeDestroyed", "host destroy subscription must remain"),
    ("DocumentActivated -= OnHostDocumentActivated", "host activation unsubscribe must remain"),
    ("DocumentToBeDestroyed -= OnHostDocumentToBeDestroyed", "host destroy unsubscribe must remain"),
    ("Dispatcher.BeginInvoke(DispatcherPriority.Background", "host lifecycle refresh must remain deferred"),
    ("if (_hostRefreshInProgress)", "refresh reentrancy guard must remain"),
    ("ProjectContextCoordinator.TryGetReadOnly(document", "Start Center refresh must remain read-only"),
):
    require(source, token, label)

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS: Start Center close-affinity refresh intent is precedence-safe and lifecycle-clean.")
