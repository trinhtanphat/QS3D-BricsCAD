#!/usr/bin/env python3
"""Guard modeless window close dispatch against BricsCAD document-teardown reentrancy."""

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
close = method_block(source, "private void TryCloseWindow()")

require(
    "_window.Dispatcher.CheckAccess()" in close,
    "Modeless close dispatch must keep the dispatcher-affinity guard used by the lifetime contract.",
)
require(
    close.count("_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));") == 2,
    "Both UI-thread and cross-thread close paths must queue the WPF close asynchronously.",
)
require(
    "TryCloseWindowOnDispatcher();" not in close,
    "Document teardown must never call Window.Close synchronously through TryCloseWindowOnDispatcher.",
)
require(
    "_window.Close();" not in close,
    "Document teardown must not close the WPF window directly on the BricsCAD event stack.",
)
require(
    close.index("_window.Dispatcher.CheckAccess()")
    < close.index("_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));"),
    "Dispatcher access must be classified before the queued close path is chosen.",
)

close_on_dispatcher = method_block(source, "private void TryCloseWindowOnDispatcher()")
require(
    "_window.Close();" in close_on_dispatcher,
    "The queued dispatcher callback must remain the single WPF close owner.",
)

for signature in (
    "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)",
    "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)",
):
    teardown = method_block(source, signature)
    require(
        "Interlocked.Exchange(ref _invalidated, 1) != 0" in teardown,
        f"{signature} must invalidate before scheduling WPF close.",
    )
    require(
        "TryCloseWindow();" in teardown,
        f"{signature} must route close through the deferred dispatcher path.",
    )
    require(
        teardown.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
        < teardown.index("TryCloseWindow();"),
        f"{signature} must fail closed before any deferred WPF teardown is queued.",
    )

print("[OK] Document-bound modeless close invalidates synchronously but always defers WPF Close until the BricsCAD document-teardown callback unwinds.")
