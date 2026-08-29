#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


require("using System.ComponentModel;" in text,
        "review controller must use cancellable Window.Closing semantics")
require("WindowClosing" in text,
        "review controller must track Window.Closing handler ownership separately")
require("_window.Closing += OnWindowClosing;" in text,
        "Attach must subscribe the cancellable close admission before Closed")
require("_attachments |= Attachment.WindowClosing;" in text,
        "Attach must publish Window.Closing ownership immediately after subscription")
require("TryDetach(Attachment.WindowClosing, () => _window.Closing -= OnWindowClosing);" in text,
        "teardown must release Window.Closing ownership retry-safely")

handler = re.search(
    r"private void OnWindowClosing\(object sender, CancelEventArgs e\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void OnWindowClosed",
    text,
    re.S,
)
require(handler is not None, "cancellable OnWindowClosing handler was not found")
if handler is not None:
    body = handler.group("body")
    live_guard = "if (!_attached || _disposeInProgress || _disposed) return;"
    pre_cancel_guard = "if (e.Cancel) return;"
    require(live_guard in body,
            "close admission must be inert outside an attached live controller")
    live_pos = body.find(live_guard)
    pre_cancel_pos = body.find(pre_cancel_guard)
    cleanup_pos = body.find("var cleanupFailure = _session.TryResetTransientStateBestEffort();")
    debt_pos = body.find("_session.HasTransientState")
    cancel_pos = body.find("e.Cancel = true;")
    barrier_pos = body.find("_cleanupBarrier = true;")
    state_pos = body.find("UpdateActionState();")
    require(pre_cancel_pos > live_pos,
            "incoming Closing cancellation must be respected after the controller liveness guard")
    require(cleanup_pos > pre_cancel_pos and debt_pos > cleanup_pos,
            "pre-cancelled close must return before any transient cleanup or ownership inspection")
    require(cancel_pos > debt_pos and barrier_pos > debt_pos,
            "failed close cleanup must cancel close and preserve an explicit cleanup barrier")
    require(state_pos > barrier_pos,
            "cancelled close must refresh cleanup controls after retaining ownership")
    require("e.Cancel = false;" not in body,
            "close admission must not override another Closing subscriber's cancellation")

attach = re.search(
    r"public void Attach\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static Button AddButton",
    text,
    re.S,
)
if attach is not None:
    body = attach.group("body")
    closing = body.find("_window.Closing += OnWindowClosing;")
    closed = body.find("_window.Closed += OnWindowClosed;")
    require(0 <= closing < closed,
            "cancellable Closing admission must be acquired before terminal Closed handling")

destroyed = re.search(
    r"private void OnDocumentToBeDestroyed\(object sender, DocumentCollectionEventArgs e\)\s*\{(?P<body>.*?)\n\s*\}",
    text,
    re.S,
)
require(destroyed is not None, "destroyed-document handler was not found")
if destroyed is not None:
    body = destroyed.group("body")
    abandon = body.find("_session.AbandonDestroyedDocumentState();")
    close = body.find("_window.Close();")
    require(0 <= abandon < close,
            "destroyed-document path must abandon native ownership before closing the window")

if errors:
    print("Coordination review window-close cleanup ownership preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS Coordination review window close retains retry ownership and respects prior cancellation")
