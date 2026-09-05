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

start = source.find("private void OnHostDocumentToBeDestroyed")
end = source.find("private void QueueHomeRefresh", start + 1)
destroy_handler = source[start:end] if start >= 0 and end > start else ""
if not destroy_handler:
    errors.append("destroy handler block not found")

# Lifecycle intent is ordered and document-affine. A close/destruction notification for the active
# document suppresses recording so a closing DWG cannot enter Recent Projects. A background document
# close must only preserve the queued intent; otherwise it can erase a pending Record for the still-
# active document. If the host cannot safely expose active identity during teardown, fail closed by
# publishing Suppress and returning; retrying can cross document generations.
for token, label in (
    ("private enum ActiveDrawingRecordIntent", "refresh intent must be explicit/tri-state"),
    ("Preserve", "re-entrant scheduling must preserve pending intent"),
    ("Record", "activation must request recording"),
    ("Suppress", "active-document destruction must suppress recording"),
    ("QueueHomeRefresh(ActiveDrawingRecordIntent.Record)", "normal activation must record"),
    ("var destroyingDocument = e.Document;", "destroy handler must inspect the document being destroyed"),
    ("Application.DocumentManager.MdiActiveDocument", "destroy handler must resolve current active document at event time"),
    ("ReferenceEquals(destroyingDocument, activeDocument)", "destroy suppression must be document-affine"),
    ("? ActiveDrawingRecordIntent.Suppress", "active-document destroy must suppress recording"),
    (": ActiveDrawingRecordIntent.Preserve", "background-document destroy must preserve pending record intent"),
    ("QueueHomeRefresh(destroyIntent);", "destroy handler must enqueue the document-affine intent"),
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

if destroy_handler:
    lookup = destroy_handler.find("Application.DocumentManager.MdiActiveDocument")
    try_pos = destroy_handler.find("try")
    catch_pos = destroy_handler.find("catch", lookup + 1 if lookup >= 0 else 0)
    suppress_pos = destroy_handler.find("QueueHomeRefresh(ActiveDrawingRecordIntent.Suppress);", catch_pos + 1 if catch_pos >= 0 else 0)
    return_pos = destroy_handler.find("return;", catch_pos + 1 if catch_pos >= 0 else 0)
    if lookup < 0 or try_pos < 0 or try_pos > lookup or catch_pos < 0:
        errors.append("destroy active-document lookup must remain inside one fail-soft try/catch boundary")
    if suppress_pos < 0 or return_pos < 0 or suppress_pos > return_pos:
        errors.append("destroy lookup failure must suppress queued recording before returning")
    if destroy_handler.count("MdiActiveDocument") != 1:
        errors.append("destroy handler must observe active-document identity exactly once")

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

print("PASS: Start Center close-affinity refresh intent is precedence-safe, document-affine, teardown-fail-soft, and lifecycle-clean.")
