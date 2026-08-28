#!/usr/bin/env python3
"""Guard cleanup of WPF windows closed while BricsCAD host quiescence is active."""

# Lane-Key: issue-4279
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

require("private int _windowClosedDuringQuiescence;" in source,
        "Registration must remember a WPF Closed event whose cleanup was deferred by host quiescence.")

closed = method_block(source, "private void OnWindowClosed(object? sender, EventArgs e)")
for marker in (
    "if (ModelessHostQuiescenceCoordinator.IsQuiescing)",
    "Volatile.Write(ref _windowClosedDuringQuiescence, 1);",
    "return;",
    "Detach();",
):
    require(marker in closed, f"Closed-path deferred cleanup marker missing: {marker}")
require(
    closed.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing)")
    < closed.index("Volatile.Write(ref _windowClosedDuringQuiescence, 1);")
    < closed.index("return;")
    < closed.index("Detach();"),
    "Closed during host quiescence must record deferred cleanup in the same guarded branch before returning; ordinary close must still detach.",
)

host_abort = method_block(source, "private void OnHostQuiescenceAborted(object? sender, EventArgs e)")
for marker in (
    "Volatile.Read(ref _windowClosedDuringQuiescence) != 0",
    "TryRecoverClosedWindowAfterQuitAbort();",
    "TryRecoverAfterQuitAbort();",
):
    require(marker in host_abort, f"QuitAborted must preserve both closed-window and document-close recovery: {marker}")
require(
    host_abort.index("TryRecoverClosedWindowAfterQuitAbort();")
    < host_abort.index("TryRecoverAfterQuitAbort();"),
    "Already-closed registration recovery must be selected before document-close recovery.",
)

recover = method_block(source, "private void TryRecoverClosedWindowAfterQuitAbort()")
for marker in (
    "_window.Dispatcher.BeginInvoke",
    "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;",
    "Volatile.Read(ref _windowClosedDuringQuiescence) == 0",
    "DetachDocumentLifecycleHandlersAfterAbort();",
    "Detach();",
    "if (!_attached)",
    "Interlocked.Exchange(ref _windowClosedDuringQuiescence, 0);",
):
    require(marker in recover, f"Closed-window quit-abort recovery marker missing: {marker}")
require("TryCloseWindowOnDispatcher();" not in recover and "_window.Close();" not in recover,
        "An already-closed WPF window must not be closed a second time during QuitAborted recovery.")
first_barrier = recover.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;")
release_native = recover.index("DetachDocumentLifecycleHandlersAfterAbort();")
second_barrier = recover.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;", first_barrier + 1)
detach = recover.index("Detach();")
consume = recover.index("Interlocked.Exchange(ref _windowClosedDuringQuiescence, 0);")
require(first_barrier < release_native < second_barrier < detach < consume,
        "Deferred recovery must keep its marker armed across a repeated quit and consume it only after managed detach completes.")
require(recover.index("if (!_attached)") < consume,
        "Deferred cleanup marker may clear only after Detach reports the registration is no longer attached.")

attach = method_block(source, "public void Attach(Document document)")
require("Volatile.Write(ref _windowClosedDuringQuiescence, 0);" in attach,
        "Failed partial attach cleanup must reset the deferred-Closed marker before rethrowing.")

print("[OK] V25 modeless windows closed during host quiescence are cleanup-recovered after QuitAborted, remain armed across repeated quit, and are never closed twice.")
