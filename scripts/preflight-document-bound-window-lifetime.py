#!/usr/bin/env python3
"""Guard modeless QS3D windows against stale project/document interaction."""

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
    "using System.Threading;" in source and "private int _invalidated;" in source,
    "Document-bound windows must use an atomic invalidation state shared across host/UI threads.",
)
require(
    "private int _hostQuitStarted;" in source,
    "Document-bound windows must track host quit independently of document-count heuristics.",
)
require(
    "private readonly IntPtr _nativeDatabaseIdentity;" in source
    and "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);" in source,
    "Document-bound windows must capture the stable native database identity at bind time.",
)
require(
    "ReferenceEquals(e.Document, _document)" not in source
    and "ReferenceEquals(document, _document)" not in source,
    "Managed Document wrapper identity must not own modeless lifetime or rebind validation.",
)

native_identity = method_block(source, "private static IntPtr GetNativeDatabaseIdentity(Document document)")
require(
    "var identity = database.UnmanagedObject;" in native_identity
    and "identity == IntPtr.Zero" in native_identity,
    "Native document identity must come from a live non-zero database unmanaged pointer.",
)

native_match = method_block(source, "private bool MatchesNativeDatabase(Document document)")
require(
    "database.UnmanagedObject != IntPtr.Zero" in native_match
    and "database.UnmanagedObject == _nativeDatabaseIdentity" in native_match,
    "Managed wrapper drift must match the captured native database while a different database is rejected.",
)

attach = method_block(source, "public void Attach(Document document)")
require(
    "if (!MatchesNativeDatabase(document))" in attach,
    "A modeless window rebind must validate native database identity rather than managed wrapper identity.",
)
for marker in (
    "BcadApplication.BeginQuit += OnApplicationBeginQuit;",
    "BcadApplication.QuitAborted += OnApplicationQuitAborted;",
):
    require(marker in attach, f"Host lifecycle subscription is missing: {marker}")

ensure = method_block(source, "private bool EnsureProjectAffinity()")
require(
    "Volatile.Read(ref _invalidated) != 0" in ensure,
    "Project-affinity checks must observe all later invalidation across host/UI threads.",
)

project_change = method_block(source, "private void CloseForProjectChange()")
for marker in (
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "DetachDocumentManagerHandler();",
    "TryCloseWindow();",
):
    require(marker in project_change, f"Project-change invalidation must retain lifecycle marker: {marker}")
require(
    "Detach();" not in project_change,
    "Project-change close must not detach window input guards before the window actually closes.",
)
require(
    project_change.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < project_change.index("TryCloseWindow();"),
    "The stale-window fail-closed state must transition atomically before attempting Window.Close().",
)

teardown = method_block(source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
for marker in (
    "if (!MatchesNativeDatabase(e.Document)) return;",
    "var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;",
    "var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "DetachDocumentManagerHandler();",
    "if (abandonForHostShutdown) return;",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in teardown, f"Document teardown must retain lifecycle marker: {marker}")
require(
    "Detach();" not in teardown,
    "Document teardown must keep window-local input guards attached when explicit close is suppressed or fails.",
)
require(
    teardown.index("if (!MatchesNativeDatabase(e.Document)) return;")
    < teardown.index("var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;")
    < teardown.index("var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();")
    < teardown.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < teardown.index("DetachDocumentManagerHandler();")
    < teardown.index("if (abandonForHostShutdown) return;")
    < teardown.index("TryCloseWindow(deferForFinalDocument);"),
    "Document teardown must validate native identity, classify host quit, atomically invalidate, release the global subscription, then suppress WPF close during application quit or use ordinary document-close dispatch.",
)

request_close = method_block(source, "private void TryCloseWindow(bool deferOnDispatcher = false)")
require(
    "_window.Dispatcher.CheckAccess()" in request_close,
    "Close dispatch must classify dispatcher affinity before choosing synchronous or queued close.",
)
require(
    "if (deferOnDispatcher)" in request_close,
    "Final/only-document teardown and quit-abort recovery must retain an explicit dispatcher deferral branch.",
)
require(
    request_close.count("_window.Dispatcher.BeginInvoke(new Action(TryCloseWindowOnDispatcher));") == 2,
    "Deferred UI-thread close and every cross-thread close must queue through the guarded callback.",
)
require(
    "TryCloseWindowOnDispatcher();" in request_close,
    "Normal dispatcher-thread close must retain the proven synchronous callback path.",
)
require(
    request_close.index("if (deferOnDispatcher)") < request_close.index("TryCloseWindowOnDispatcher();"),
    "Deferred close must be considered before the normal synchronous close path.",
)
require(
    "_window.Close();" not in request_close,
    "The dispatcher scheduling wrapper must not contain an unguarded direct Window.Close().",
)

dispatched_close = method_block(source, "private void TryCloseWindowOnDispatcher()")
require(
    "try" in dispatched_close and "_window.Close();" in dispatched_close and "catch" in dispatched_close,
    "The dispatcher callback must swallow Window.Close failures while leaving fail-closed guards active.",
)

begin_quit = method_block(source, "private void OnApplicationBeginQuit(object? sender, EventArgs e)")
require(
    "Volatile.Write(ref _hostQuitStarted, 1);" in begin_quit,
    "BeginQuit must establish host ownership before document teardown can initiate WPF destruction.",
)
quit_aborted = method_block(source, "private void OnApplicationQuitAborted(object? sender, EventArgs e)")
require(
    "Volatile.Write(ref _hostQuitStarted, 0);" in quit_aborted
    and "TryCloseWindow(deferOnDispatcher: true);" in quit_aborted,
    "QuitAborted must clear host ownership and safely close an already-invalidated stale window later on the dispatcher.",
)

mouse = method_block(source, "private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)")
keyboard = method_block(source, "private void OnPreviewKeyDown(object sender, KeyEventArgs e)")
for name, snippet in (("mouse", mouse), ("keyboard", keyboard)):
    require(
        "if (!EnsureProjectAffinity()) e.Handled = true;" in snippet,
        f"Invalidated modeless {name} input must remain handled/fail-closed.",
    )

closed = method_block(source, "private void OnWindowClosed(object? sender, EventArgs e)")
require(
    "Detach();" in closed,
    "Successful/actual Window.Closed must remain the authoritative full-detach path.",
)

detach = method_block(source, "private void Detach()")
for marker in (
    "BcadApplication.BeginQuit -= OnApplicationBeginQuit;",
    "BcadApplication.QuitAborted -= OnApplicationQuitAborted;",
):
    require(marker in detach, f"Ordinary detach must release host lifecycle subscription: {marker}")

print("[OK] Document-bound modeless windows use native database identity, remain atomically fail-closed, and let BricsCAD own WPF teardown during application quit.")
