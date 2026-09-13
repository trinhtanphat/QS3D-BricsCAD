#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
TARGET = UI / "DocumentBoundWindowLifetime.cs"
NATIVE = UI / "DocumentBoundNativeLifecycleCoordinator.cs"


def fail(message):
    print("ERROR:", message)
    return 1


def main():
    if not TARGET.exists():
        return fail("DocumentBoundWindowLifetime.cs is missing")
    if not NATIVE.exists():
        return fail("DocumentBoundNativeLifecycleCoordinator.cs is missing")

    text = TARGET.read_text(encoding="utf-8")
    native = NATIVE.read_text(encoding="utf-8")

    required = [
        "ConditionalWeakTable<Window, Registration>",
        "Registrations.GetValue(window, key => new Registration(key, document))",
        "registration.Attach(document);",
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "database.UnmanagedObject == _nativeDatabaseIdentity",
        "if (!MatchesNativeDatabase(document))",
        "if (_attached)",
        "if (ReferenceEquals(document, _lifecycleDocument)) return;",
        "MatchesBoundDocumentAffinity(document)",
        "DocumentBoundNativeLifecycleCoordinator.Rebind(",
        "_lifecycleDocument = document;",
        "private IDisposable? _nativeLifecycleSubscription;",
        "_nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(",
        "DetachNativeLifecycleSubscription();",
        "_window.Closed += OnWindowClosed;",
        "_window.Closed -= OnWindowClosed;",
    ]
    missing = [needle for needle in required if needle not in text]
    if missing:
        return fail("modeless lifetime idempotence/wrapper-rebind invariant is incomplete: " + ", ".join(missing))

    attach_start = text.find("public void Attach(Document document)")
    attach_end = text.find("private static IntPtr GetNativeDatabaseIdentity", attach_start)
    attach = text[attach_start:attach_end]
    if attach.find("DocumentBoundNativeLifecycleCoordinator.Rebind(") > attach.find("_lifecycleDocument = document;"):
        return fail("repeated Attach must publish the replacement wrapper only after native rebind succeeds")

    for legacy in (
        "ReferenceEquals(e.Document, _document)",
        "ReferenceEquals(document, _document)",
    ):
        if legacy in text:
            return fail("modeless lifetime idempotence must not depend on legacy managed Document wrapper identity: " + legacy)

    for forbidden in (
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
        "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
        "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    ):
        if forbidden in text:
            return fail("per-window lifetime must not directly own native lifecycle reactors: " + forbidden)

    native_required = [
        "private static readonly Dictionary<IntPtr, Entry> Entries",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
        "lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
        "new WeakReference<Callbacks>(callbacks)",
        "return new Subscription(entry, callbacks);",
        "entry.EnsureLifecycleDocument(lifecycleDocument, replacementAffinity);",
        "if (!entry.DetachNativeHandlersIfSafe()) return;",
    ]
    native_missing = [needle for needle in native_required if needle not in native]
    if native_missing:
        return fail("shared native lifecycle ownership invariant is incomplete: " + ", ".join(native_missing))

    if native.count("BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;") != 1:
        return fail("shared coordinator must own exactly one global DocumentToBeDestroyed subscription site")

    if "new Registration(window, document).Attach();" in text:
        return fail("Attach still creates an untracked Registration on every call")

    print("PASS: each modeless Window owns one managed subscription token; same-wrapper Attach is idempotent, proven replacement wrappers rebind before publication, and native document reactors remain centralized by native database identity with weak per-window callbacks.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
