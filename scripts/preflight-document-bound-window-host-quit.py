#!/usr/bin/env python3
"""Guard V25 modeless windows with bounded application/document native lifecycle ownership."""

# Lane-Key: issue-3621 — H.3 keeps one plugin-global host quit owner and centralizes
# BricsCAD document/native reactor subscriptions outside per-window WPF registrations.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
HOST_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ModelessHostQuiescenceCoordinator.cs"
NATIVE_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundNativeLifecycleCoordinator.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    require(start >= 0, f"{signature} is missing.")
    brace = source.find("{", start)
    require(brace >= 0, f"{signature} body is missing.")
    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise AssertionError(f"{signature} body is unterminated.")


for path, message in (
    (HOST_SOURCE, "H.3 requires one plugin-global ModelessHostQuiescenceCoordinator."),
    (NATIVE_SOURCE, "H.3 requires one shared DocumentBoundNativeLifecycleCoordinator."),
):
    require(path.exists(), message)

window_source = WINDOW_SOURCE.read_text(encoding="utf-8")
host_source = HOST_SOURCE.read_text(encoding="utf-8")
native_source = NATIVE_SOURCE.read_text(encoding="utf-8")
barrier = "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;"

for marker in (
    "BcadApplication.QuitWillStart += OnQuitWillStart;",
    "BcadApplication.QuitAborted += OnQuitAborted;",
):
    require(marker in host_source, f"Global host coordinator is missing native lifecycle ownership: {marker}")

for forbidden in (
    "BcadApplication.BeginQuit +=",
    "BcadApplication.QuitWillStart +=",
    "BcadApplication.QuitAborted +=",
    "BcadApplication.BeginQuit -=",
    "BcadApplication.QuitWillStart -=",
    "BcadApplication.QuitAborted -=",
    "_hostQuitStarted",
):
    require(forbidden not in window_source,
            f"Per-window Registration must not own native BricsCAD Application quit state: {forbidden}")

for forbidden in (
    "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
    "_lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted -= OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
):
    require(forbidden not in window_source,
            f"Per-window Registration must not own BricsCAD document/native reactor callback: {forbidden}")

for marker in (
    "lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
    "TrySnapshotDestroyByLifecycleDocument",
    "TrySnapshotDestroyByNativeIdentity",
):
    require(marker in native_source, f"Shared native coordinator is missing ownership marker: {marker}")

require("internal static bool IsQuiescing" in host_source,
        "Global coordinator must expose managed host-quiescence state.")
require("internal static event EventHandler? QuiescenceAborted;" in host_source,
        "Global coordinator must expose a managed quit-abort recovery event.")

quit_will_start = method_block(host_source, "private static void OnQuitWillStart(object? sender, EventArgs e)")
require("Volatile.Write(ref _isQuiescing, 1);" in quit_will_start,
        "QuitWillStart must atomically arm global quiescence before document destruction.")
for forbidden in ("Window.Close", ".Close()", "DocumentManager", "Detach", "QuiescenceAborted"):
    require(forbidden not in quit_will_start,
            f"QuitWillStart must remain state-only and non-reentrant: {forbidden}")

quit_aborted = method_block(host_source, "private static void OnQuitAborted(object? sender, EventArgs e)")
require("Interlocked.Exchange(ref _isQuiescing, 0)" in quit_aborted,
        "QuitAborted must clear global quiescence atomically.")
require("QuiescenceAborted?.Invoke(null, EventArgs.Empty);" in quit_aborted,
        "QuitAborted must notify windows through a managed event after clearing global state.")
require(
    quit_aborted.index("Interlocked.Exchange(ref _isQuiescing, 0)")
    < quit_aborted.index("QuiescenceAborted?.Invoke(null, EventArgs.Empty);"),
    "QuitAborted must clear global state before managed recovery callbacks run.",
)

attach = method_block(window_source, "public void Attach(Document document)")
for marker in (
    "ModelessHostQuiescenceCoordinator.EnsureInitialized();",
    "DocumentBoundNativeLifecycleCoordinator.Register(",
    "ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;",
):
    require(marker in attach, f"Per-window registration is missing managed coordinator wiring: {marker}")

host_abort = method_block(window_source, "private void OnHostQuiescenceAborted(object? sender, EventArgs e)")
require("TryRecoverAfterQuitAbort();" in host_abort,
        "Managed quit-abort callback must retain document-close dispatcher-deferred recovery.")

ensure_affinity = method_block(window_source, "private bool EnsureProjectAffinity()")
fail_closed = "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;"
require(fail_closed in ensure_affinity,
        "Modeless activation/input must stop before BricsCAD document access during host quiescence.")
require(ensure_affinity.index(fail_closed) < ensure_affinity.index("lock (_documentAccessGate)"),
        "Global host-quiescence guard must run before DocumentManager/project access.")

native_destroyed = method_block(native_source, "private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
require(barrier in native_destroyed,
        "Shared DocumentToBeDestroyed callback must fail closed during host quiescence.")
require("document = e.Document;" in native_destroyed,
        "Shared DocumentToBeDestroyed callback must read the event document only in its guarded owner.")
require(native_destroyed.index(barrier) < native_destroyed.index("document = e.Document;"),
        "Shared DocumentToBeDestroyed callback must cross quiescence before dereferencing e.Document.")

native_begin = method_block(native_source, "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)")
require(barrier in native_begin,
        "Shared BeginDocumentClose callback must fail closed during host quiescence.")
require(native_begin.index(barrier) < native_begin.index("callbacks = SnapshotLiveCallbacks();"),
        "Shared BeginDocumentClose callback must cross quiescence before dispatching to windows.")

native_abort = method_block(native_source, "private void OnDocumentCloseAborted(object? sender, EventArgs e)")
require(barrier in native_abort,
        "Shared CloseAborted callback must fail closed during host quiescence.")
require(native_abort.index(barrier) < native_abort.index("callbacks = SnapshotLiveCallbacks();"),
        "Shared CloseAborted callback must cross quiescence before dispatching to windows.")

for signature in (
    "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)",
    "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)",
):
    teardown = method_block(window_source, signature)
    require(barrier in teardown, f"{signature} must defensively re-check host quiescence.")
    require("TryCloseWindow(deferForFinalDocument);" in teardown,
            f"{signature} must retain normal document-bound close behavior.")
    for forbidden in ("+=", "-=", "DetachNativeLifecycleSubscription()"):
        require(forbidden not in teardown,
                f"{signature} must not mutate native lifecycle ownership: {forbidden}")

window_destroyed = method_block(window_source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
require("e.Document" not in window_destroyed and "MatchesNativeDatabase(" not in window_destroyed,
        "Managed destroy callback must not repeat affinity matching after the coordinator has dispatched it.")

recover = method_block(window_source, "private void TryRecoverAfterQuitAbort()")
for marker in (
    "_window.Dispatcher.BeginInvoke",
    barrier,
    "DetachDocumentLifecycleHandlersAfterAbort();",
    "Detach();",
    "TryCloseWindowOnDispatcher();",
):
    require(marker in recover, f"Quit-abort recovery is missing deferred cleanup marker: {marker}")
require(
    recover.index(barrier)
    < recover.index("DetachDocumentLifecycleHandlersAfterAbort();")
    < recover.index("Detach();")
    < recover.index("TryCloseWindowOnDispatcher();"),
    "Quit-abort recovery must re-check quiescence, release managed native subscription, detach, then close.",
)

close_on_dispatcher = method_block(window_source, "private void TryCloseWindowOnDispatcher()")
require(barrier in close_on_dispatcher,
        "Already-queued dispatcher close must re-check global host quiescence before WPF HWND teardown.")
require(close_on_dispatcher.index(barrier) < close_on_dispatcher.index("_window.Close();"),
        "Global quiescence guard must precede Window.Close().")

window_closed = method_block(window_source, "private void OnWindowClosed(object? sender, EventArgs e)")
closed_guard = "if (ModelessHostQuiescenceCoordinator.IsQuiescing)"
require(closed_guard in window_closed,
        "Host-owned WPF Closed must cross the global quiescence barrier before normal cleanup.")
require("Volatile.Write(ref _windowClosedDuringQuiescence, 1);" in window_closed,
        "Closed during host quiescence must remember that normal cleanup was deferred until QuitAborted.")
require(
    window_closed.index(closed_guard)
    < window_closed.index("Volatile.Write(ref _windowClosedDuringQuiescence, 1);")
    < window_closed.index("return;")
    < window_closed.index("Detach();"),
    "Window Closed must record deferred cleanup and return while quiescing; ordinary close must still detach.",
)

detach = method_block(window_source, "private void Detach()")
require(barrier in detach,
        "Shared detach must fail closed while global host quiescence is active.")
require("ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted;" in detach,
        "Normal detach must release the managed recovery subscription.")
require(detach.index(barrier) < detach.index("DetachDocumentLifecycleHandlersIfSafe();"),
        "Shared detach must not release the managed native subscription after host quiescence starts.")

print("[OK] V25 modeless host teardown uses one global host-quiescence owner and one shared document/native lifecycle coordinator; windows closed during quiescence defer cleanup safely to QuitAborted.")
