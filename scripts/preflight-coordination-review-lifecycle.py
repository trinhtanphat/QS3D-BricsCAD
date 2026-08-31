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


require("[Flags]" in text and "private enum Attachment" in text,
        "review controller must track individually acquired handler ownership")
for token in (
    "private Attachment _attachments;",
    "private bool _attached;",
    "private bool _disposeInProgress;",
    "private bool _sessionDisposed;",
    "WindowClosing",
):
    require(token in text, "missing lifecycle ownership token: " + token)

attach = re.search(
    r"public void Attach\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private static Button AddButton",
    text,
    re.S,
)
require(attach is not None, "Controller.Attach was not found")
if attach is not None:
    body = attach.group("body")
    require("try" in body and "catch" in body,
            "Attach must be transactional")
    subscriptions = (
        ("_highlight.Click += OnHighlight;", "_attachments |= Attachment.Highlight;"),
        ("_clearHighlight.Click += OnClearHighlight;", "_attachments |= Attachment.ClearHighlight;"),
        ("_isolate.Click += OnIsolate;", "_attachments |= Attachment.Isolate;"),
        ("_restoreIsolation.Click += OnRestoreIsolation;", "_attachments |= Attachment.RestoreIsolation;"),
        ("_section.Click += OnSection;", "_attachments |= Attachment.Section;"),
        ("_restoreView.Click += OnRestoreView;", "_attachments |= Attachment.RestoreView;"),
        ("_grid.SelectionChanged += OnSelectionChanged;", "_attachments |= Attachment.GridSelection;"),
        ("_window.Closing += OnWindowClosing;", "_attachments |= Attachment.WindowClosing;"),
        ("_window.Closed += OnWindowClosed;", "_attachments |= Attachment.WindowClosed;"),
        ("Application.DocumentManager.DocumentActivated += OnDocumentActivated;", "_attachments |= Attachment.DocumentActivated;"),
        ("Application.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;", "_attachments |= Attachment.DocumentToBeDestroyed;"),
    )
    cursor = -1
    for add, own in subscriptions:
        add_pos = body.find(add, cursor + 1)
        own_pos = body.find(own, add_pos + 1 if add_pos >= 0 else 0)
        require(add_pos >= 0 and own_pos > add_pos,
                "Attach must publish ownership immediately after successful add: " + add)
        if own_pos >= 0:
            cursor = own_pos

    update_pos = body.find("UpdateActionState();")
    publish_pos = body.find("_attached = true;")
    require(cursor >= 0 and update_pos > cursor and publish_pos > update_pos,
            "Attach must publish active state only after all subscriptions and post-attach initialization")

    catch_pos = body.find("catch")
    detach_pos = body.find("DetachHandlersBestEffort();", catch_pos)
    session_pos = body.find("DisposeSessionBestEffort();", catch_pos)
    throw_pos = body.find("throw;", catch_pos)
    require(catch_pos >= 0 and catch_pos < detach_pos < session_pos < throw_pos,
            "Attach rollback must attempt handler detach and session cleanup before rethrow")

require(
    "if (!_attached || _disposeInProgress || _disposed) return;" in text,
    "partially attached or tearing-down callbacks must be inert",
)

dispose = re.search(
    r"public void Dispose\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void DetachHandlersBestEffort",
    text,
    re.S,
)
require(dispose is not None, "Controller.Dispose was not found")
if dispose is not None:
    body = dispose.group("body")
    require("if (_disposed || _disposeInProgress) return;" in body,
            "Dispose must be idempotent and reentrancy-safe")
    require("_disposeInProgress = true;" in body and "_attached = false;" in body,
            "Dispose must make callbacks inert before cleanup")
    detach_pos = body.find("DetachHandlersBestEffort();")
    session_pos = body.find("DisposeSessionBestEffort();")
    complete_pos = body.find("_disposed = _attachments == Attachment.None && _sessionDisposed;")
    require(0 <= detach_pos < session_pos < complete_pos,
            "Dispose must attempt all handler cleanup then session cleanup before terminal publication")
    require("finally" in body and "_disposeInProgress = false;" in body,
            "Dispose must always release the reentrancy guard")

detach = re.search(
    r"private void DetachHandlersBestEffort\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void TryDetach",
    text,
    re.S,
)
require(detach is not None, "fail-soft handler detach helper was not found")
if detach is not None:
    body = detach.group("body")
    destroyed = body.find("Attachment.DocumentToBeDestroyed")
    activated = body.find("Attachment.DocumentActivated")
    closed = body.find("Attachment.WindowClosed")
    closing = body.find("Attachment.WindowClosing")
    grid = body.find("Attachment.GridSelection")
    highlight = body.find("Attachment.Highlight")
    require(min(destroyed, activated, closed, closing, grid, highlight) >= 0,
            "DetachHandlersBestEffort must cover external, cancellable-window, terminal-window and local handler ownership")
    require(destroyed < activated < closed < closing < grid < highlight,
            "Detach must break BricsCAD publisher roots before window and local WPF handler cleanup")

try_detach = re.search(
    r"private void TryDetach\(Attachment attachment, Action detach\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void DisposeSessionBestEffort",
    text,
    re.S,
)
require(try_detach is not None, "per-handler fail-soft detach helper was not found")
if try_detach is not None:
    body = try_detach.group("body")
    detach_call = body.find("detach();")
    clear = body.find("_attachments &= ~attachment;")
    catch = body.find("catch")
    require(0 <= detach_call < clear < catch,
            "handler ownership may clear only after successful unsubscribe")
    require("Preserve ownership so a later Dispose call can retry" in body,
            "failed unsubscribe must remain retryable")

session = re.search(
    r"private void DisposeSessionBestEffort\(\)\s*\{(?P<body>.*?)\n\s*\}\n\s*\}\n\n\s*private sealed class TransientReviewSession",
    text,
    re.S,
)
require(session is not None, "retryable transient review session cleanup helper was not found")
if session is not None:
    body = session.group("body")
    require("if (_sessionDisposed) return;" in body,
            "session cleanup must be idempotent")
    require(body.find("_session.Dispose();") < body.find("_sessionDisposed = true;"),
            "session ownership may clear only after successful disposal")

closing = re.search(
    r"private void OnWindowClosing\(object sender, CancelEventArgs e\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void OnWindowClosed",
    text,
    re.S,
)
require(closing is not None,
        "cancellable close admission must preserve a retry path before terminal Closed")
if closing is not None:
    body = closing.group("body")
    cleanup = body.find("_session.TryResetTransientStateBestEffort()")
    debt = body.find("_session.HasTransientState")
    cancel = body.find("e.Cancel = true;")
    barrier = body.find("_cleanupBarrier = true;")
    require(0 <= cleanup < debt < cancel < barrier,
            "failed window-close cleanup must retain controller/UI retry ownership")
    require("e.Cancel = false;" not in body,
            "review close handler must not override cancellation from another subscriber")

# Preserve the pre-existing safety boundary: native CAD effects are reached only after
# current-document/project identity and persisted issue/relink/full-pair validation.
for token in (
    "var resolved = ResolveReviewTargets();",
    "effect(resolved);",
    "EvaluateRelink(project, issue.IssueId)",
    "CadHandleService.Resolve(_document, handles)",
):
    require(token in text, "coordination review fail-closed validation contract regressed: " + token)

if errors:
    print("Coordination Manager review lifecycle preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS Coordination Manager review attachment is transactional and teardown/close cleanup is retryable/fail-soft")
