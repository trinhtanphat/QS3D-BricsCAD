#!/usr/bin/env python3
"""Guard normal document close dispatch while shared native ownership suppresses host-quit teardown."""

# Lane-Key: issue-3621 — H.3 keeps ordinary close behavior while native document callbacks
# are centralized behind the global host-quiescence barrier.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
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


require(NATIVE_SOURCE.exists(), "H.3 shared native lifecycle coordinator is missing.")
source = SOURCE.read_text(encoding="utf-8")
native = NATIVE_SOURCE.read_text(encoding="utf-8")
barrier = "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;"
defer = "var deferForFinalDocument = !HasAnotherLiveDocument();"

require("private int _hostQuitStarted;" not in source,
        "Host quit state must come from the plugin-global quiescence coordinator.")
for marker in (
    "BcadApplication.BeginQuit +=", "BcadApplication.BeginQuit -=",
    "BcadApplication.QuitWillStart +=", "BcadApplication.QuitWillStart -=",
    "BcadApplication.QuitAborted +=", "BcadApplication.QuitAborted -=",
    "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
):
    require(marker not in source,
            f"DocumentBoundWindowLifetime must not own native lifecycle reactor: {marker}")

other_document = method_block(source, "private bool HasAnotherLiveDocument()")
require("foreach (Document candidate in BcadApplication.DocumentManager)" in other_document,
        "Final-document classification must enumerate the live BricsCAD document manager.")
require("candidate == null || candidate.IsDisposed" in other_document,
        "Final-document classification must reject disposed managed document wrappers.")
require("identity != IntPtr.Zero && identity != _nativeDatabaseIdentity" in other_document,
        "Another live document must be distinguished by stable native database identity.")
require("return false;" in other_document,
        "Ambiguous/unsafe document enumeration must fail closed to final-document deferral.")

close = method_block(source, "private void TryCloseWindow(bool deferOnDispatcher = false)")
require("_window.Dispatcher.CheckAccess()" in close,
        "Modeless close dispatch must keep the dispatcher-affinity guard.")
require("if (deferOnDispatcher)" in close,
        "Dispatcher-thread close must retain an explicit final-document deferral branch.")
require(close.count("_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));") == 2,
        "Deferred UI-thread close and all cross-thread closes must queue through Dispatcher.BeginInvoke.")
require("TryCloseWindowOnDispatcher();" in close,
        "Normal UI-thread document close must retain the proven synchronous close path.")
require(close.index("if (deferOnDispatcher)") < close.index("TryCloseWindowOnDispatcher();"),
        "Final-document deferral must be evaluated before the synchronous normal-close path.")
require("_window.Close();" not in close,
        "TryCloseWindow must not directly own WPF Window.Close.")

close_on_dispatcher = method_block(source, "private void TryCloseWindowOnDispatcher()")
require(barrier in close_on_dispatcher,
        "Queued/synchronous dispatcher close must re-check host quiescence before Window.Close.")
require("_window.Close();" in close_on_dispatcher,
        "The dispatcher close helper must remain the single explicit WPF Window.Close owner.")
require(close_on_dispatcher.index(barrier) < close_on_dispatcher.index("_window.Close();"),
        "Host quiescence must be checked before explicit WPF close.")

# Shared native BeginDocumentClose callback must stop dispatch before the per-window close path.
native_begin = method_block(native, "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)")
require(barrier in native_begin,
        "Shared BeginDocumentClose owner must stop during host quiescence.")
require(native_begin.index(barrier) < native_begin.index("callbacks = SnapshotLiveCallbacks();"),
        "Shared BeginDocumentClose owner must cross quiescence before managed dispatch.")

begin_close = method_block(source, "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)")
for marker in (
    barrier,
    defer,
    "lock (_documentAccessGate)",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in begin_close, f"BeginDocumentClose must retain close-dispatch marker: {marker}")
require(
    begin_close.index(barrier)
    < begin_close.index(defer)
    < begin_close.index("lock (_documentAccessGate)")
    < begin_close.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < begin_close.index("TryCloseWindow(deferForFinalDocument);"),
    "BeginDocumentClose must cross host quiescence before DocumentManager access, then preserve ordinary close dispatch.",
)
for forbidden in ("abandonForHostShutdown", "DetachNativeLifecycleSubscription", "+=", "-="):
    require(forbidden not in begin_close,
            f"Managed BeginDocumentClose must not own host/native teardown mutation: {forbidden}")

destroyed = method_block(source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
for marker in (
    barrier,
    defer,
    "lock (_documentAccessGate)",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in destroyed, f"DocumentToBeDestroyed must retain close-dispatch marker: {marker}")
require("e.Document" not in destroyed and "MatchesNativeDatabase(" not in destroyed,
        "DocumentToBeDestroyed must consume shared-coordinator affinity without reopening the event Document.")
require(
    destroyed.index(barrier)
    < destroyed.index(defer)
    < destroyed.index("lock (_documentAccessGate)")
    < destroyed.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < destroyed.index("TryCloseWindow(deferForFinalDocument);"),
    "DocumentToBeDestroyed must cross host quiescence before DocumentManager access and preserve ordinary close dispatch.",
)
for forbidden in ("abandonForHostShutdown", "DetachNativeLifecycleSubscription", "+=", "-="):
    require(forbidden not in destroyed,
            f"Managed DocumentToBeDestroyed must not own host/native teardown mutation: {forbidden}")
for marker in ("TrySnapshotDestroyByLifecycleDocument", "TrySnapshotDestroyByNativeIdentity"):
    require(marker in native, f"Shared destroy affinity owner is missing: {marker}")

project_change = method_block(source, "private void CloseForProjectChange()")
require("TryCloseWindow();" in project_change,
        "Non-document project-affinity close must retain the normal synchronous dispatcher path.")

print("[OK] Normal document close keeps dispatcher behavior while shared native ownership and global quiescence suppress explicit WPF/native teardown during BricsCAD quit.")
