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

teardown = method_block(
    source,
    "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)",
)
for marker in (
    "if (!MatchesNativeDatabase(e.Document)) return;",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "DetachDocumentManagerHandler();",
    "TryCloseWindow();",
):
    require(marker in teardown, f"Document teardown must retain lifecycle marker: {marker}")
require(
    "Detach();" not in teardown,
    "Document teardown must keep window-local input guards attached when close fails.",
)
require(
    teardown.index("if (!MatchesNativeDatabase(e.Document)) return;")
    < teardown.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < teardown.index("DetachDocumentManagerHandler();")
    < teardown.index("TryCloseWindow();"),
    "Document teardown must validate native identity, atomically invalidate, release the global subscription, then best-effort close.",
)

request_close = method_block(source, "private void TryCloseWindow()")
require(
    "new Action(TryCloseWindowOnDispatcher)" in request_close,
    "Cross-thread close must dispatch through the guarded close callback.",
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

print("[OK] Document-bound modeless windows use native database identity and remain atomically fail-closed across wrapper drift and close failures.")
