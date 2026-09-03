#!/usr/bin/env python3
"""Guard modeless QS3D windows against stale project/document interaction and native reactor rooting."""

# Lane-Key: issue-3621 — H.3 centralizes native document lifecycle ownership while
# preserving stable native identity, fail-closed input guards, and host quiescence.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
HOST_COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ModelessHostQuiescenceCoordinator.cs"
NATIVE_COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundNativeLifecycleCoordinator.cs"


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


require(HOST_COORDINATOR.exists(),
        "Document-bound lifetime requires the plugin-global modeless host quiescence coordinator.")
require(NATIVE_COORDINATOR.exists(),
        "Document-bound lifetime requires the shared native document lifecycle coordinator.")
source = SOURCE.read_text(encoding="utf-8")
host = HOST_COORDINATOR.read_text(encoding="utf-8")
native = NATIVE_COORDINATOR.read_text(encoding="utf-8")
barrier = "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;"

require("using System.Threading;" in source and "private int _invalidated;" in source,
        "Document-bound windows must use an atomic invalidation state shared across host/UI threads.")
require("private int _hostQuitStarted;" not in source,
        "Document-bound windows must consume plugin-global host quiescence rather than per-window quit state.")
require(
    "private readonly IntPtr _nativeDatabaseIdentity;" in source
    and "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);" in source,
    "Document-bound windows must capture the stable native database identity at bind time.",
)
require(
    "private IDisposable? _nativeLifecycleSubscription;" in source,
    "Each window must own one managed subscription token rather than BricsCAD reactor delegates.",
)
require(
    "ReferenceEquals(e.Document, _document)" not in source
    and "ReferenceEquals(document, _document)" not in source,
    "Managed Document wrapper identity must not own modeless lifetime or rebind validation.",
)

for marker in (
    "BcadApplication.BeginQuit +=", "BcadApplication.BeginQuit -=",
    "BcadApplication.QuitWillStart +=", "BcadApplication.QuitWillStart -=",
    "BcadApplication.QuitAborted +=", "BcadApplication.QuitAborted -=",
    "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
    "_lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted -= OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
):
    require(marker not in source, f"Per-window lifetime must not own native lifecycle callback: {marker}")

require("internal static bool IsQuiescing => Volatile.Read(ref _isQuiescing) != 0;" in host,
        "Global host quiescence must expose an atomic read barrier.")
require("private static readonly Dictionary<IntPtr, Entry> Entries" in native,
        "Shared document lifecycle ownership must be keyed by stable native database identity.")
require("List<WeakReference<Callbacks>>" in native,
        "Native lifecycle entries must not strongly root per-window WPF callback bundles.")
for marker in ("TrySnapshotDestroyByLifecycleDocument", "TrySnapshotDestroyByNativeIdentity"):
    require(marker in native, f"Shared destroy affinity owner is missing: {marker}")

native_identity = method_block(source, "private static IntPtr GetNativeDatabaseIdentity(Document document)")
require(
    "var identity = database.UnmanagedObject;" in native_identity and "identity == IntPtr.Zero" in native_identity,
    "Native document identity must come from a live non-zero database unmanaged pointer.",
)

native_match = method_block(source, "private bool MatchesNativeDatabase(Document document)")
require(
    "database.UnmanagedObject != IntPtr.Zero" in native_match
    and "database.UnmanagedObject == _nativeDatabaseIdentity" in native_match,
    "Managed wrapper drift must match the captured native database while a different database is rejected.",
)

attach = method_block(source, "public void Attach(Document document)")
require("if (!MatchesNativeDatabase(document))" in attach,
        "A modeless window rebind must validate native database identity rather than managed wrapper identity.")
for marker in (
    "ModelessHostQuiescenceCoordinator.EnsureInitialized();",
    "_nativeLifecycleSubscription = DocumentBoundNativeLifecycleCoordinator.Register(",
    "OnBeginDocumentClose,",
    "OnDocumentCloseAborted,",
    "OnDocumentToBeDestroyed);",
    "ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;",
):
    require(marker in attach, f"Managed lifecycle wiring is missing: {marker}")

ensure = method_block(source, "private bool EnsureProjectAffinity()")
require("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;" in ensure,
        "Project-affinity checks must fail closed before touching BricsCAD documents once host quit starts.")
require(
    ensure.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;")
    < ensure.index("lock (_documentAccessGate)"),
    "The global host-quiescence barrier must run before DocumentManager/project affinity access.",
)
require("Volatile.Read(ref _invalidated) != 0" in ensure,
        "Project-affinity checks must observe all later invalidation across host/UI threads.")

project_change = method_block(source, "private void CloseForProjectChange()")
for marker in (
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "DetachDocumentLifecycleHandlersIfSafe();",
    "TryCloseWindow();",
):
    require(marker in project_change, f"Project-change invalidation must retain lifecycle marker: {marker}")
require("Detach();" not in project_change,
        "Project-change close must not detach window input guards before the window actually closes.")
require(project_change.index("Interlocked.Exchange(ref _invalidated, 1) != 0") < project_change.index("TryCloseWindow();"),
        "The stale-window fail-closed state must transition atomically before attempting Window.Close().")

teardown = method_block(source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
for marker in (
    barrier,
    "var deferForFinalDocument = !HasAnotherLiveDocument();",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in teardown, f"Document teardown must retain lifecycle marker: {marker}")
require("e.Document" not in teardown and "MatchesNativeDatabase(" not in teardown,
        "Document teardown must consume coordinator-owned affinity without reopening the event Document.")
require("Detach();" not in teardown and "DetachNativeLifecycleSubscription" not in teardown,
        "Document teardown must keep input guards attached and leave native ownership to the shared coordinator.")
require(
    teardown.index(barrier)
    < teardown.index("var deferForFinalDocument = !HasAnotherLiveDocument();")
    < teardown.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < teardown.index("TryCloseWindow(deferForFinalDocument);"),
    "Document teardown must cross quiescence, classify final-document deferral, invalidate atomically, then request ordinary close.",
)

request_close = method_block(source, "private void TryCloseWindow(bool deferOnDispatcher = false)")
require("_window.Dispatcher.CheckAccess()" in request_close,
        "Close dispatch must classify dispatcher affinity before choosing synchronous or queued close.")
require("if (deferOnDispatcher)" in request_close,
        "Final/only-document teardown must retain an explicit dispatcher deferral branch.")
require(
    request_close.count("_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));") == 2,
    "Deferred UI-thread close and every cross-thread close must queue through the guarded callback.",
)
require("TryCloseWindowOnDispatcher();" in request_close,
        "Normal dispatcher-thread close must retain the proven synchronous callback path.")
require(request_close.index("if (deferOnDispatcher)") < request_close.index("TryCloseWindowOnDispatcher();"),
        "Deferred close must be considered before the normal synchronous close path.")
require("_window.Close();" not in request_close,
        "The dispatcher scheduling wrapper must not contain an unguarded direct Window.Close().")

dispatched_close = method_block(source, "private void TryCloseWindowOnDispatcher()")
require(barrier in dispatched_close,
        "Dispatcher close callback must re-check global host quiescence.")
require("try" in dispatched_close and "_window.Close();" in dispatched_close and "catch" in dispatched_close,
        "The dispatcher callback must swallow Window.Close failures while leaving fail-closed guards active.")
require(dispatched_close.index(barrier) < dispatched_close.index("_window.Close();"),
        "Global host quiescence must suppress Window.Close before final host teardown.")

host_abort = method_block(source, "private void OnHostQuiescenceAborted(object? sender, EventArgs e)")
require("TryRecoverAfterQuitAbort();" in host_abort,
        "Managed QuitAborted notification must defer stale-registration recovery outside the native callback.")
require("BcadApplication." not in host_abort,
        "Per-window host-abort handling must not access native application lifecycle APIs.")

recover = method_block(source, "private void TryRecoverAfterQuitAbort()")
for marker in (
    "_window.Dispatcher.BeginInvoke",
    barrier,
    "DetachDocumentLifecycleHandlersAfterAbort();",
    "Detach();",
    "TryCloseWindowOnDispatcher();",
):
    require(marker in recover, f"Quit-abort recovery must retain deferred cleanup marker: {marker}")
require(
    recover.index(barrier)
    < recover.index("DetachDocumentLifecycleHandlersAfterAbort();")
    < recover.index("Detach();")
    < recover.index("TryCloseWindowOnDispatcher();"),
    "Deferred recovery must re-check quiescence, release managed native subscription, detach, then close.",
)

detach_subscription = method_block(source, "private void DetachNativeLifecycleSubscription()")
for marker in (
    "Interlocked.Exchange(ref _nativeLifecycleSubscription, null)",
    "subscription.Dispose();",
):
    require(marker in detach_subscription, f"Managed native subscription release is missing: {marker}")
for forbidden in ("BeginDocumentClose -=", "CloseAborted -=", "DocumentToBeDestroyed -="):
    require(forbidden not in detach_subscription,
            f"Per-window managed release must not unsubscribe BricsCAD native callbacks directly: {forbidden}")

mouse = method_block(source, "private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)")
keyboard = method_block(source, "private void OnPreviewKeyDown(object sender, KeyEventArgs e)")
for name, snippet in (("mouse", mouse), ("keyboard", keyboard)):
    require("if (!EnsureProjectAffinity()) e.Handled = true;" in snippet,
            f"Invalidated modeless {name} input must remain handled/fail-closed.")

closed = method_block(source, "private void OnWindowClosed(object? sender, EventArgs e)")
closed_barrier = "if (ModelessHostQuiescenceCoordinator.IsQuiescing)"
closed_marker = "Volatile.Write(ref _windowClosedDuringQuiescence, 1);"
for marker in (closed_barrier, closed_marker, "return;", "Detach();"):
    require(marker in closed, f"Window.Closed must retain quiescent deferred-cleanup marker: {marker}")
require(
    closed.index(closed_barrier)
    < closed.index(closed_marker)
    < closed.index("return;")
    < closed.index("Detach();"),
    "Window.Closed must record deferred cleanup and return during host quit before ordinary full detach.",
)

detach = method_block(source, "private void Detach()")
require(barrier in detach,
        "Shared detach must not mutate lifecycle ownership during host quit.")
require("ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted;" in detach,
        "Ordinary detach must release the managed host-abort notification.")
require(
    detach.index(barrier)
    < detach.index("DetachDocumentLifecycleHandlersIfSafe();")
    < detach.index("ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted;"),
    "Host quit must block managed native-subscription release before shared detach reaches managed cleanup.",
)

print("[OK] Document-bound modeless windows use stable native identity, remain atomically fail-closed, and consume shared native lifecycle subscriptions that do not root WPF windows from BricsCAD reactors.")
