#!/usr/bin/env python3
"""Guard V25 modeless windows with an early, non-reentrant BricsCAD host-quit barrier."""

# Lane-Key: issue-3621 — keep this H.2 regression on the canonical source carrier.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"


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


source = SOURCE.read_text(encoding="utf-8")

require(
    "private int _hostQuitStarted;" in source,
    "Each modeless registration must track BricsCAD host-quit state independently of document-count heuristics.",
)

attach = method_block(source, "public void Attach(Document document)")
for marker in (
    "BcadApplication.QuitWillStart += OnApplicationQuitWillStart;",
    "BcadApplication.QuitAborted += OnApplicationQuitAborted;",
):
    require(marker in attach, f"Attach must subscribe the host lifecycle barrier: {marker}")
require(
    "BcadApplication.BeginQuit +=" not in attach,
    "The final-host barrier must be armed at QuitWillStart, not the later BeginQuit callback.",
)

quit_will_start = method_block(source, "private void OnApplicationQuitWillStart(object? sender, EventArgs e)")
require(
    "Volatile.Write(ref _hostQuitStarted, 1);" in quit_will_start,
    "QuitWillStart must atomically mark host shutdown before document destruction can trigger WPF teardown.",
)
for forbidden in (
    "_window.Close()",
    "TryCloseWindow(",
    "DetachDocumentManagerHandler(",
    "DetachDocumentLifecycleHandlers",
):
    require(
        forbidden not in quit_will_start,
        f"QuitWillStart is a state-only barrier and must not perform reentrant WPF/native teardown: {forbidden}",
    )

ensure_affinity = method_block(source, "private bool EnsureProjectAffinity()")
require(
    "if (Volatile.Read(ref _hostQuitStarted) != 0) return false;" in ensure_affinity,
    "Modeless input/activation must stop before resolving BricsCAD documents once host quit has started.",
)
require(
    ensure_affinity.index("if (Volatile.Read(ref _hostQuitStarted) != 0) return false;")
    < ensure_affinity.index("lock (_documentAccessGate)"),
    "The host-quit input barrier must run before any DocumentManager/project access.",
)

quit_aborted = method_block(source, "private void OnApplicationQuitAborted(object? sender, EventArgs e)")
require(
    "Volatile.Write(ref _hostQuitStarted, 0);" in quit_aborted,
    "QuitAborted must clear the host-shutdown marker.",
)
require(
    "TryRecoverAfterQuitAbort();" in quit_aborted,
    "If quit aborts after this registration failed closed, native subscription cleanup and WPF close must be deferred outside the native quit callback.",
)

recover = method_block(source, "private void TryRecoverAfterQuitAbort()")
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
    "Quit-abort recovery must release per-document handlers before shared detach and stale-window close, even when the dispatcher runs before CloseAborted.",
)

for signature in (
    "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)",
    "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)",
):
    teardown = method_block(source, signature)
    for marker in (
        "var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;",
        "if (abandonForHostShutdown) return;",
        "DetachDocumentManagerHandler();",
        "TryCloseWindow(deferForFinalDocument);",
    ):
        require(marker in teardown, f"{signature} is missing host-shutdown barrier: {marker}")
    require(
        teardown.index("if (abandonForHostShutdown) return;")
        < teardown.index("DetachDocumentManagerHandler();")
        < teardown.index("TryCloseWindow(deferForFinalDocument);"),
        f"{signature} must leave native DocumentManager subscriptions untouched during host quit before any normal close cleanup/dispatch.",
    )

close_aborted = method_block(source, "private void OnDocumentCloseAborted(object? sender, EventArgs e)")
require(
    "if (Volatile.Read(ref _hostQuitStarted) != 0) return;" in close_aborted,
    "Document CloseAborted must not remove BricsCAD lifecycle handlers while host quit is still active.",
)

# Host-owned final teardown must never be translated into a dispatcher Window.Close request.
require(
    "TryCloseWindow(abandonForHostShutdown" not in source,
    "Host-shutdown state must suppress explicit WPF close, not merely change its dispatch timing.",
)

# A close request may have been queued before QuitWillStart and execute after QuitWillStart. The final
# dispatcher callback is therefore the last authoritative barrier before WPF detaches its HWND.
close_on_dispatcher = method_block(source, "private void TryCloseWindowOnDispatcher()")
for marker in (
    "if (Volatile.Read(ref _hostQuitStarted) != 0) return;",
    "_window.Close();",
):
    require(marker in close_on_dispatcher, f"Dispatcher close owner is missing host-quit race guard: {marker}")
require(
    close_on_dispatcher.index("if (Volatile.Read(ref _hostQuitStarted) != 0) return;")
    < close_on_dispatcher.index("_window.Close();"),
    "An already-queued dispatcher close must re-check host quit before initiating WPF Window.Close.",
)

window_closed = method_block(source, "private void OnWindowClosed(object? sender, EventArgs e)")
require(
    "if (Volatile.Read(ref _hostQuitStarted) != 0) return;" in window_closed,
    "Host-owned WPF Closed must not translate into BricsCAD lifecycle unsubscription during native teardown.",
)
require(
    "Detach();" in window_closed,
    "Ordinary WPF Closed must still detach the registration.",
)
require(
    window_closed.index("if (Volatile.Read(ref _hostQuitStarted) != 0) return;")
    < window_closed.index("Detach();"),
    "Window Closed must cross the host-quit barrier before normal native subscription cleanup.",
)

detach = method_block(source, "private void Detach()")
require(
    "if (Volatile.Read(ref _hostQuitStarted) != 0) return;" in detach,
    "The shared detach owner must fail closed instead of mutating BricsCAD subscriptions during host quit.",
)
for marker in (
    "BcadApplication.QuitWillStart -= OnApplicationQuitWillStart;",
    "BcadApplication.QuitAborted -= OnApplicationQuitAborted;",
):
    require(marker in detach, f"Normal detach must release the host lifecycle subscription: {marker}")
require(
    detach.index("if (Volatile.Read(ref _hostQuitStarted) != 0) return;")
    < detach.index("DetachDocumentManagerHandler();")
    < detach.index("BcadApplication.QuitWillStart -= OnApplicationQuitWillStart;"),
    "No BricsCAD lifecycle handler may be removed by shared detach after QuitWillStart has started.",
)

print("[OK] BricsCAD QuitWillStart arms the final native/WPF barrier before document teardown: QS3D blocks modeless document access, Window.Close, and native lifecycle unsubscription until normal exit or dispatcher-deferred QuitAborted recovery.")
