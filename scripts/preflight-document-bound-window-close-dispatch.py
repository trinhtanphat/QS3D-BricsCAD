#!/usr/bin/env python3
"""Guard normal document close vs host-owned application-quit modeless teardown."""

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

other_document = method_block(source, "private bool HasAnotherLiveDocument()")
require(
    "foreach (Document candidate in BcadApplication.DocumentManager)" in other_document,
    "Final-document classification must enumerate the live BricsCAD document manager.",
)
require(
    "candidate == null || candidate.IsDisposed" in other_document,
    "Final-document classification must reject disposed managed document wrappers.",
)
require(
    "identity != IntPtr.Zero && identity != _nativeDatabaseIdentity" in other_document,
    "Another live document must be distinguished by stable native database identity.",
)
require(
    "return false;" in other_document,
    "Ambiguous/unsafe document enumeration must fail closed to final-document deferral.",
)

close = method_block(source, "private void TryCloseWindow(bool deferOnDispatcher = false)")
require(
    "_window.Dispatcher.CheckAccess()" in close,
    "Modeless close dispatch must keep the dispatcher-affinity guard.",
)
require(
    "if (deferOnDispatcher)" in close,
    "Dispatcher-thread close must retain an explicit final-document deferral branch.",
)
require(
    close.count("_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));") == 2,
    "Deferred UI-thread close and all cross-thread closes must queue through Dispatcher.BeginInvoke.",
)
require(
    "TryCloseWindowOnDispatcher();" in close,
    "Normal UI-thread document close must retain the proven synchronous close path.",
)
require(
    close.index("if (deferOnDispatcher)") < close.index("TryCloseWindowOnDispatcher();"),
    "Final-document deferral must be evaluated before the synchronous normal-close path.",
)
require(
    "_window.Close();" not in close,
    "TryCloseWindow must not directly own WPF Window.Close.",
)

close_on_dispatcher = method_block(source, "private void TryCloseWindowOnDispatcher()")
require(
    "_window.Close();" in close_on_dispatcher,
    "The dispatcher close helper must remain the single explicit WPF Window.Close owner.",
)

for signature in (
    "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)",
    "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)",
):
    teardown = method_block(source, signature)
    for marker in (
        "var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;",
        "var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();",
        "Interlocked.Exchange(ref _invalidated, 1) != 0",
        "DetachDocumentManagerHandler();",
        "if (abandonForHostShutdown) return;",
        "TryCloseWindow(deferForFinalDocument);",
    ):
        require(marker in teardown, f"{signature} must retain close-dispatch marker: {marker}")
    require(
        teardown.index("var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;")
        < teardown.index("var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();")
        < teardown.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
        < teardown.index("DetachDocumentManagerHandler();")
        < teardown.index("if (abandonForHostShutdown) return;")
        < teardown.index("TryCloseWindow(deferForFinalDocument);"),
        f"{signature} must classify host quit first, fail closed, release the global handler, then suppress explicit WPF close or use the ordinary document-close dispatcher.",
    )

project_change = method_block(source, "private void CloseForProjectChange()")
require(
    "TryCloseWindow();" in project_change,
    "Non-document project-affinity close must retain the normal synchronous dispatcher path.",
)

print("[OK] Normal document close keeps the proven dispatcher behavior, while BricsCAD application quit owns final modeless HWND/WPF teardown without an explicit QS3D Window.Close.")
