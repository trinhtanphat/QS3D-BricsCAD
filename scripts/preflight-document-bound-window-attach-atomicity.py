#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
SOURCE = UI / "DocumentBoundWindowLifetime.cs"
NATIVE_SOURCE = UI / "DocumentBoundNativeLifecycleCoordinator.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DocumentBoundWindowLifetime.cs")
if not NATIVE_SOURCE.is_file():
    errors.append("missing DocumentBoundNativeLifecycleCoordinator.cs")

if not errors:
    text = SOURCE.read_text(encoding="utf-8")
    native = NATIVE_SOURCE.read_text(encoding="utf-8")
    attach_start = text.find("public void Attach(Document document)")
    bind_start = text.find("private static IntPtr GetNativeDatabaseIdentity", attach_start + 1)
    attach = text[attach_start:bind_start] if attach_start >= 0 and bind_start > attach_start else ""

    required_attach = (
        "if (!MatchesNativeDatabase(document))",
        "if (_attached) return;",
        "try",
        "ModelessHostQuiescenceCoordinator.EnsureInitialized();",
        "BindProjectAffinityIfPresent();",
        "_nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(",
        "OnBeginDocumentClose,",
        "OnDocumentCloseAborted,",
        "OnDocumentToBeDestroyed);",
        "ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;",
        "_window.Activated += OnWindowActivated;",
        "_window.PreviewMouseDown += OnPreviewMouseDown;",
        "_window.PreviewKeyDown += OnPreviewKeyDown;",
        "_window.Closed += OnWindowClosed;",
        "_attached = true;",
        "catch",
        "_attached = true;",
        "Detach();",
        "_projectAffinityBound = false;",
        "_projectId = string.Empty;",
        "throw;",
    )
    cursor = 0
    for token in required_attach:
        pos = attach.find(token, cursor)
        if pos < 0:
            errors.append("modeless Attach missing ordered H3 failure-rollback contract: " + token)
            break
        cursor = pos + len(token)

    if "catch\n                {\n                    throw;" in attach:
        errors.append("modeless Attach must clean partial managed/native ownership before rethrow")

    for forbidden in (
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
        "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    ):
        if forbidden in attach:
            errors.append("modeless Attach must not directly own native lifecycle reactors: " + forbidden)

    detach_start = text.find("private void Detach()")
    detach = text[detach_start:] if detach_start >= 0 else ""
    for token in (
        "if (!_attached) return;",
        "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;",
        "DetachDocumentLifecycleHandlersIfSafe();",
        "ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted;",
        "_window.Activated -= OnWindowActivated;",
        "_window.PreviewMouseDown -= OnPreviewMouseDown;",
        "_window.PreviewKeyDown -= OnPreviewKeyDown;",
        "_window.Closed -= OnWindowClosed;",
        "_attached = false;",
    ):
        if token not in detach:
            errors.append("modeless Detach lost best-effort H3 cleanup contract: " + token)

    helper_start = text.find("private void DetachNativeLifecycleSubscription()")
    helper_end = text.find("private void OnWindowClosed", helper_start + 1)
    helper = text[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
    for token in (
        "Interlocked.Exchange(ref _nativeLifecycleSubscription, null)",
        "if (subscription == null) return;",
        "subscription.Dispose();",
    ):
        if token not in helper:
            errors.append("managed native-lifecycle detach helper missing token: " + token)

    safe_start = text.find("private void DetachDocumentLifecycleHandlersIfSafe()")
    safe_end = text.find("private void DetachDocumentLifecycleHandlersAfterAbort()", safe_start + 1)
    safe = text[safe_start:safe_end] if safe_start >= 0 and safe_end > safe_start else ""
    for token in (
        "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;",
        "if (Volatile.Read(ref _documentCloseStarted) != 0) return;",
        "DetachNativeLifecycleSubscription();",
    ):
        if token not in safe:
            errors.append("safe lifecycle detach boundary missing token: " + token)

    for token in (
        "Registrations.GetValue(window, key => new Registration(key, document))",
        'throw new InvalidOperationException("A modeless QS3D window cannot be rebound to a different BricsCAD document.")',
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "database.UnmanagedObject == _nativeDatabaseIdentity",
        "The shared coordinator has already matched this registration",
        "CloseForProjectChange();",
        "_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher))",
        "private void TryCloseWindowOnDispatcher()",
    ):
        if token not in text:
            errors.append("modeless lifetime atomicity change lost existing safety contract: " + token)

    for legacy in (
        "ReferenceEquals(e.Document, _document)",
        "ReferenceEquals(document, _document)",
    ):
        if legacy in text:
            errors.append("modeless lifetime atomicity must not depend on managed Document wrapper identity: " + legacy)

    for token in (
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
        "lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
        "new WeakReference<Callbacks>(callbacks)",
        "TrySnapshotDestroyByLifecycleDocument",
        "TrySnapshotDestroyByNativeIdentity",
    ):
        if token not in native:
            errors.append("shared H3 native coordinator missing atomic ownership token: " + token)

    if attach.count("_attached = true;") != 2:
        errors.append("Attach must mark successful ownership once and temporarily enable Detach exactly once in rollback")

print("QS3D document-bound window attach atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: document-bound modeless attachment rolls partial H3 managed/native subscriptions back through Detach, keeps native reactor ownership centralized and weak, remains retryable, and preserves source-DWG/project fail-closed identity behavior.")
