#!/usr/bin/env python3
"""Guard V25 modeless windows with one plugin-global, non-reentrant host-quiescence owner."""

# Lane-Key: issue-3621 — H.2 must eliminate per-window BricsCAD Application quit callbacks.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
COORDINATOR_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ModelessHostQuiescenceCoordinator.cs"


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


require(
    COORDINATOR_SOURCE.exists(),
    "H.2 requires one plugin-global ModelessHostQuiescenceCoordinator instead of per-window BricsCAD Application quit subscriptions.",
)
window_source = WINDOW_SOURCE.read_text(encoding="utf-8")
coordinator_source = COORDINATOR_SOURCE.read_text(encoding="utf-8")

# The only native Application quit owner is the global coordinator.
for marker in (
    "BcadApplication.QuitWillStart += OnQuitWillStart;",
    "BcadApplication.QuitAborted += OnQuitAborted;",
):
    require(marker in coordinator_source, f"Global coordinator is missing native host lifecycle ownership: {marker}")

for forbidden in (
    "BcadApplication.BeginQuit +=",
    "BcadApplication.QuitWillStart +=",
    "BcadApplication.QuitAborted +=",
    "BcadApplication.BeginQuit -=",
    "BcadApplication.QuitWillStart -=",
    "BcadApplication.QuitAborted -=",
    "_hostQuitStarted",
):
    require(
        forbidden not in window_source,
        f"Per-window Registration must not own native BricsCAD Application quit lifecycle state: {forbidden}",
    )

require(
    "internal static bool IsQuiescing" in coordinator_source,
    "Global coordinator must expose managed host-quiescence state.",
)
require(
    "internal static event EventHandler? QuiescenceAborted;" in coordinator_source,
    "Global coordinator must expose a managed quit-abort recovery event.",
)

quit_will_start = method_block(coordinator_source, "private static void OnQuitWillStart(object? sender, EventArgs e)")
require(
    "Volatile.Write(ref _isQuiescing, 1);" in quit_will_start,
    "QuitWillStart must atomically arm global quiescence before document destruction.",
)
for forbidden in (
    "Window.Close",
    ".Close()",
    "DocumentManager",
    "Detach",
    "QuiescenceAborted",
):
    require(
        forbidden not in quit_will_start,
        f"QuitWillStart must remain state-only and non-reentrant: {forbidden}",
    )

quit_aborted = method_block(coordinator_source, "private static void OnQuitAborted(object? sender, EventArgs e)")
require(
    "Interlocked.Exchange(ref _isQuiescing, 0)" in quit_aborted,
    "QuitAborted must clear global quiescence atomically.",
)
require(
    "QuiescenceAborted?.Invoke(null, EventArgs.Empty);" in quit_aborted,
    "QuitAborted must notify windows through a managed event after clearing global state.",
)
require(
    quit_aborted.index("Interlocked.Exchange(ref _isQuiescing, 0)")
    < quit_aborted.index("QuiescenceAborted?.Invoke(null, EventArgs.Empty);"),
    "QuitAborted must clear global state before managed recovery callbacks run.",
)

attach = method_block(window_source, "public void Attach(Document document)")
for marker in (
    "ModelessHostQuiescenceCoordinator.EnsureInitialized();",
    "ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;",
):
    require(marker in attach, f"Per-window registration is missing managed coordinator wiring: {marker}")

host_abort = method_block(window_source, "private void OnHostQuiescenceAborted(object? sender, EventArgs e)")
require(
    "TryRecoverAfterQuitAbort();" in host_abort,
    "Managed quit-abort callback must enter dispatcher-deferred recovery.",
)

ensure_affinity = method_block(window_source, "private bool EnsureProjectAffinity()")
barrier = "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return false;"
require(barrier in ensure_affinity, "Modeless activation/input must stop before BricsCAD document access during host quiescence.")
require(
    ensure_affinity.index(barrier) < ensure_affinity.index("lock (_documentAccessGate)"),
    "Global host-quiescence guard must run before DocumentManager/project access.",
)

for signature in (
    "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)",
    "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)",
):
    teardown = method_block(window_source, signature)
    for marker in (
        "var abandonForHostShutdown = ModelessHostQuiescenceCoordinator.IsQuiescing;",
        "if (abandonForHostShutdown) return;",
        "DetachDocumentManagerHandler();",
        "TryCloseWindow(deferForFinalDocument);",
    ):
        require(marker in teardown, f"{signature} is missing global host-quiescence barrier: {marker}")
    require(
        teardown.index("if (abandonForHostShutdown) return;")
        < teardown.index("DetachDocumentManagerHandler();")
        < teardown.index("TryCloseWindow(deferForFinalDocument);"),
        f"{signature} must not mutate native subscriptions or request WPF close after global quiescence starts.",
    )

close_aborted = method_block(window_source, "private void OnDocumentCloseAborted(object? sender, EventArgs e)")
require(
    "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;" in close_aborted,
    "Document CloseAborted must not mutate native lifecycle handlers while host quiescence is active.",
)

recover = method_block(window_source, "private void TryRecoverAfterQuitAbort()")
for marker in (
    "_window.Dispatcher.BeginInvoke",
    "DetachDocumentLifecycleHandlersAfterAbort();",
    "Detach();",
    "TryCloseWindowOnDispatcher();",
):
    require(marker in recover, f"Quit-abort recovery is missing deferred cleanup marker: {marker}")
require(
    recover.index("DetachDocumentLifecycleHandlersAfterAbort();")
    < recover.index("Detach();")
    < recover.index("TryCloseWindowOnDispatcher();"),
    "Quit-abort recovery must release document handlers before shared detach and stale-window close.",
)

close_on_dispatcher = method_block(window_source, "private void TryCloseWindowOnDispatcher()")
require(
    "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;" in close_on_dispatcher,
    "Already-queued dispatcher close must re-check global host quiescence before WPF HWND teardown.",
)
require(
    close_on_dispatcher.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;")
    < close_on_dispatcher.index("_window.Close();"),
    "Global quiescence guard must precede Window.Close().",
)

window_closed = method_block(window_source, "private void OnWindowClosed(object? sender, EventArgs e)")
require(
    "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;" in window_closed,
    "Host-owned WPF Closed must not translate into native lifecycle unsubscription.",
)
require(
    window_closed.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;")
    < window_closed.index("Detach();"),
    "Window Closed must cross global quiescence before normal cleanup.",
)

detach = method_block(window_source, "private void Detach()")
require(
    "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;" in detach,
    "Shared detach must fail closed while global host quiescence is active.",
)
require(
    "ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted;" in detach,
    "Normal detach must release the managed recovery subscription.",
)
require(
    detach.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;")
    < detach.index("DetachDocumentManagerHandler();"),
    "Shared detach must not touch native DocumentManager handlers after host quiescence starts.",
)

print("[OK] V25 modeless host teardown uses one plugin-global QuitWillStart/QuitAborted owner; per-window registrations consume managed quiescence state and never own BricsCAD Application quit callbacks.")
