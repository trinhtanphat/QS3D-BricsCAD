#!/usr/bin/env python3
"""Guard normal document close vs plugin-global host-quiescence modeless teardown."""

# Lane-Key: issue-3621 — keep ordinary close dispatch while H.2 centralizes host quit ownership.
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
barrier = "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;"
abandon = "var abandonForHostShutdown = ModelessHostQuiescenceCoordinator.IsQuiescing;"
defer = "var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();"

require("private int _hostQuitStarted;" not in source,
        "Host quit state must come from the plugin-global quiescence coordinator.")
for marker in (
    "BcadApplication.BeginQuit +=",
    "BcadApplication.BeginQuit -=",
    "BcadApplication.QuitWillStart +=",
    "BcadApplication.QuitWillStart -=",
    "BcadApplication.QuitAborted +=",
    "BcadApplication.QuitAborted -=",
):
    require(marker not in source,
            f"DocumentBoundWindowLifetime must not own native application-quit reactors: {marker}")

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
require("return false;" in other_document,
        "Ambiguous/unsafe document enumeration must fail closed to final-document deferral.")

close = method_block(source, "private void TryCloseWindow(bool deferOnDispatcher = false)")
require("_window.Dispatcher.CheckAccess()" in close,
        "Modeless close dispatch must keep the dispatcher-affinity guard.")
require("if (deferOnDispatcher)" in close,
        "Dispatcher-thread close must retain an explicit final-document deferral branch.")
require(
    close.count("_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));") == 2,
    "Deferred UI-thread close and all cross-thread closes must queue through Dispatcher.BeginInvoke.",
)
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

begin_close = method_block(source, "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)")
for marker in (
    barrier,
    abandon,
    defer,
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "if (abandonForHostShutdown) return;",
    "DetachDocumentManagerHandler();",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in begin_close, f"BeginDocumentClose must retain close-dispatch marker: {marker}")
require(
    begin_close.index(barrier)
    < begin_close.index(abandon)
    < begin_close.index(defer)
    < begin_close.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < begin_close.index("if (abandonForHostShutdown) return;")
    < begin_close.index("DetachDocumentManagerHandler();")
    < begin_close.index("TryCloseWindow(deferForFinalDocument);"),
    "BeginDocumentClose must cross the global host barrier before document enumeration, then preserve ordinary close dispatch only outside host quit.",
)

destroyed = method_block(source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
for marker in (
    barrier,
    "if (!MatchesNativeDatabase(e.Document)) return;",
    abandon,
    defer,
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "if (abandonForHostShutdown) return;",
    "DetachDocumentManagerHandler();",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in destroyed, f"DocumentToBeDestroyed must retain close-dispatch marker: {marker}")
require(
    destroyed.index(barrier)
    < destroyed.index("if (!MatchesNativeDatabase(e.Document)) return;")
    < destroyed.index(abandon)
    < destroyed.index(defer)
    < destroyed.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < destroyed.index("if (abandonForHostShutdown) return;")
    < destroyed.index("DetachDocumentManagerHandler();")
    < destroyed.index("TryCloseWindow(deferForFinalDocument);"),
    "DocumentToBeDestroyed must cross global host quiescence before native-wrapper access and use ordinary close dispatch only outside host quit.",
)

project_change = method_block(source, "private void CloseForProjectChange()")
require("TryCloseWindow();" in project_change,
        "Non-document project-affinity close must retain the normal synchronous dispatcher path.")

print("[OK] Normal document close keeps dispatcher behavior while plugin-global host quiescence suppresses explicit WPF/native teardown during BricsCAD quit.")
