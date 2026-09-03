#!/usr/bin/env python3
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterWindow.cs"


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise AssertionError(f"{label} missing token: {token}")


def method_body(text: str, signature: str, next_signature: str, label: str) -> str:
    start = text.find(signature)
    if start < 0:
        raise AssertionError(f"{label} missing signature: {signature}")
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise AssertionError(f"{label} missing following signature: {next_signature}")
    return text[start:end]


def main() -> None:
    if not SOURCE.exists():
        raise AssertionError("Missing Start Center window source")
    text = SOURCE.read_text(encoding="utf-8")

    # Window ownership: subscribe only while the modeless window is alive.
    require(text, "Loaded += OnWindowLoaded;", "window load lifecycle")
    require(text, "Activated += OnWindowActivated;", "window activation lifecycle")
    require(text, "Closed += OnWindowClosed;", "window close lifecycle")
    require(text, "SubscribeToHostLifecycle();", "window host subscription")
    require(text, "UnsubscribeFromHostLifecycle();", "window host unsubscription")
    require(text, "private bool _hostLifecycleSubscribed;", "idempotent subscription state")
    require(text, "if (_hostLifecycleSubscribed || _windowClosed)", "idempotent subscription guard")

    # Unsubscription deliberately attempts both removes even when the published ownership flag is false.
    # A transactional add rollback can itself fail during host teardown; close must retry that cleanup.
    unsubscribe = method_body(text, "private void UnsubscribeFromHostLifecycle()", "private void OnHostDocumentActivated", "host unsubscription")
    if re.search(r"if\s*\(\s*!_hostLifecycleSubscribed\s*\)\s*return\s*;", unsubscribe):
        raise AssertionError("Start Center close must retry native detach after a partially failed subscription rollback")
    if unsubscribe.count("try") < 2 or unsubscribe.count("catch") < 2:
        raise AssertionError("Start Center host unsubscription must attempt both native detach operations independently")
    require(unsubscribe, "_hostLifecycleSubscribed = false;", "unsubscription ownership reset")

    # Host transitions are symmetrical and do not directly refresh from the event stack.
    require(text, "Application.DocumentManager.DocumentActivated += OnHostDocumentActivated;", "document activation subscription")
    require(text, "Application.DocumentManager.DocumentActivated -= OnHostDocumentActivated;", "document activation unsubscription")
    require(text, "Application.DocumentManager.DocumentToBeDestroyed += OnHostDocumentToBeDestroyed;", "document destruction subscription")
    require(text, "Application.DocumentManager.DocumentToBeDestroyed -= OnHostDocumentToBeDestroyed;", "document destruction unsubscription")
    activated = method_body(text, "private void OnHostDocumentActivated", "private void OnHostDocumentToBeDestroyed", "activation handler")
    destroying = method_body(text, "private void OnHostDocumentToBeDestroyed", "private void QueueHomeRefresh", "destruction handler")
    require(activated, "QueueHomeRefresh(ActiveDrawingRecordIntent.Record);", "activation deferred record refresh")
    require(destroying, "var destroyingDocument = e.Document;", "destruction event-local document")
    require(destroying, "var activeDocument = Application.DocumentManager.MdiActiveDocument;", "destruction active-document comparison")
    require(destroying, "ReferenceEquals(destroyingDocument, activeDocument)", "destruction document affinity")
    require(destroying, "? ActiveDrawingRecordIntent.Suppress", "active destruction suppression")
    require(destroying, ": ActiveDrawingRecordIntent.Preserve;", "background destruction preservation")
    require(destroying, "QueueHomeRefresh(destroyIntent);", "destruction deferred refresh")
    if "RefreshHomeShell(" in activated or "RefreshHomeShell(" in destroying:
        raise AssertionError("BricsCAD document events must defer rather than refresh inline")

    # Dispatcher coalescing/reentrancy keeps rapid MDI transitions bounded while preserving lifecycle intent.
    for token, label in (
        ("private enum ActiveDrawingRecordIntent", "record-intent enum"),
        ("Preserve,", "record-intent preserve state"),
        ("Record,", "record-intent record state"),
        ("Suppress", "record-intent suppress state"),
        ("private bool _hostRefreshQueued;", "coalescing state"),
        ("private bool _hostRefreshInProgress;", "reentrancy state"),
        ("private ActiveDrawingRecordIntent _queuedActiveDrawingRecordIntent = ActiveDrawingRecordIntent.Preserve;", "record intent state"),
        ("private void QueueHomeRefresh(ActiveDrawingRecordIntent intent)", "typed refresh queue"),
        ("if (intent != ActiveDrawingRecordIntent.Preserve)", "preserve intent merge guard"),
        ("_queuedActiveDrawingRecordIntent = intent;", "latest explicit lifecycle intent publication"),
        ("Dispatcher.HasShutdownStarted", "dispatcher shutdown guard"),
        ("Dispatcher.HasShutdownFinished", "dispatcher shutdown guard"),
        ("Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(DrainQueuedHomeRefresh));", "deferred dispatcher refresh"),
        ("if (_hostRefreshQueued)", "refresh coalescing guard"),
        ("if (_hostRefreshInProgress)", "refresh reentrancy guard"),
        ("QueueHomeRefresh(ActiveDrawingRecordIntent.Preserve);", "reentrant refresh preservation"),
        ("var recordActiveDrawing = _queuedActiveDrawingRecordIntent == ActiveDrawingRecordIntent.Record;", "drained record decision"),
        ("_queuedActiveDrawingRecordIntent = ActiveDrawingRecordIntent.Preserve;", "drained intent reset"),
        ("RefreshHomeShell(recordActiveDrawing);", "single drained refresh"),
    ):
        require(text, token, label)

    # Explicitly reject the superseded OR-only boolean coalescing contract that can record a closing drawing.
    for legacy in (
        "private bool _queuedRecordActiveDrawing;",
        "private void QueueHomeRefresh(bool recordActiveDrawing)",
        "_queuedRecordActiveDrawing |= recordActiveDrawing;",
        "QueueHomeRefresh(recordActiveDrawing: true);",
        "QueueHomeRefresh(recordActiveDrawing: false);",
    ):
        if legacy in text:
            raise AssertionError(f"Start Center lifecycle must not regress to legacy boolean queue contract: {legacy}")

    # Refresh remains click/run-time bound to the active host document, never a cached native object.
    refresh = method_body(text, "private void RefreshHomeShell(bool recordActiveDrawing)", "private void RefreshRecentProjects()", "home refresh")
    require(refresh, "Application.DocumentManager.MdiActiveDocument", "active-document resolution")
    require(text, "QueueHomeRefresh(ActiveDrawingRecordIntent.Record);", "post-action record refresh")

    native_field_patterns = (
        r"^\s*private\s+(?:readonly\s+|static\s+)*(?:Bricscad\.ApplicationServices\.)?Document\??\s+_",
        r"^\s*private\s+(?:readonly\s+|static\s+)*(?:Teigha\.DatabaseServices\.)?ObjectId\??\s+_",
        r"^\s*private\s+(?:readonly\s+|static\s+)*(?:Teigha\.DatabaseServices\.)?DBObject\??\s+_",
    )
    for pattern in native_field_patterns:
        if re.search(pattern, text, re.MULTILINE):
            raise AssertionError("Start Center window must not retain native Document/ObjectId/DBObject fields")
    if re.search(r"\b_\w+\s*=\s*e\.Document\b", text):
        raise AssertionError("Start Center window must not retain document-event arguments")

    print("PASS Start Center window host lifecycle synchronization guard")


if __name__ == "__main__":
    main()
