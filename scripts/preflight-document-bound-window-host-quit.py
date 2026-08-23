#!/usr/bin/env python3
"""Guard V25 modeless windows against explicit WPF close during BricsCAD host shutdown."""

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
    "BcadApplication.BeginQuit += OnApplicationBeginQuit;",
    "BcadApplication.QuitAborted += OnApplicationQuitAborted;",
):
    require(marker in attach, f"Attach must subscribe the host lifecycle barrier: {marker}")

begin_quit = method_block(source, "private void OnApplicationBeginQuit(object? sender, EventArgs e)")
require(
    "Volatile.Write(ref _hostQuitStarted, 1);" in begin_quit,
    "BeginQuit must atomically mark host shutdown before document destruction can trigger WPF teardown.",
)

quit_aborted = method_block(source, "private void OnApplicationQuitAborted(object? sender, EventArgs e)")
require(
    "Volatile.Write(ref _hostQuitStarted, 0);" in quit_aborted,
    "QuitAborted must clear the host-shutdown marker.",
)
require(
    "TryCloseWindow(deferOnDispatcher: true);" in quit_aborted,
    "If quit aborts after this registration already failed closed, its window must close later on the dispatcher rather than remain stale.",
)

for signature in (
    "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)",
    "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)",
):
    teardown = method_block(source, signature)
    for marker in (
        "var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;",
        "if (abandonForHostShutdown) return;",
        "TryCloseWindow(deferForFinalDocument);",
    ):
        require(marker in teardown, f"{signature} is missing host-shutdown barrier: {marker}")
    require(
        teardown.index("if (abandonForHostShutdown) return;")
        < teardown.index("TryCloseWindow(deferForFinalDocument);"),
        f"{signature} must abandon explicit WPF close during host quit before any normal/final-document close dispatch.",
    )

# Host-owned final teardown must never be translated into a dispatcher Window.Close request.
require(
    "TryCloseWindow(abandonForHostShutdown" not in source,
    "Host-shutdown state must suppress explicit WPF close, not merely change its dispatch timing.",
)

# A close request may have been queued before BeginQuit and execute after BeginQuit. The final
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

detach = method_block(source, "private void Detach()")
for marker in (
    "BcadApplication.BeginQuit -= OnApplicationBeginQuit;",
    "BcadApplication.QuitAborted -= OnApplicationQuitAborted;",
):
    require(marker in detach, f"Normal detach must release the host lifecycle subscription: {marker}")

print("[OK] BricsCAD BeginQuit owns final host teardown, including already-queued dispatcher closes: QS3D invalidates document state but never initiates WPF Window.Close after host quit starts.")
