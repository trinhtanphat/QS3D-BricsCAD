#!/usr/bin/env python3
"""Guard document-bound modeless activation against native document disposal races."""

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
    "private readonly object _documentAccessGate = new object();" in source,
    "Document-bound modeless lifetime must serialize retained Document access against teardown.",
)

attach = method_block(source, "public void Attach(Document document)")
for marker in (
    "BindProjectAffinityIfPresent();",
    "_document.CloseWillStart += OnDocumentCloseWillStart;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
    "_window.Activated += OnWindowActivated;",
):
    require(marker in attach, f"Attach is missing disposal-safe lifecycle marker: {marker}")
require(
    attach.index("BindProjectAffinityIfPresent();")
    < attach.index("_document.CloseWillStart += OnDocumentCloseWillStart;")
    < attach.index("BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;")
    < attach.index("_window.Activated += OnWindowActivated;"),
    "The early document-close barrier must be installed before WPF activation can be observed.",
)

ensure = method_block(source, "private bool EnsureProjectAffinity()")
require(
    "lock (_documentAccessGate)" in ensure,
    "Project-affinity reads must run under the document access gate.",
)
require(
    "Volatile.Read(ref _invalidated) != 0" in ensure,
    "Project-affinity reads must reject an already invalidated window before native access.",
)
require(
    "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" in ensure,
    "The guarded affinity path must continue using the canonical project coordinator.",
)
require(
    ensure.index("lock (_documentAccessGate)")
    < ensure.index("Volatile.Read(ref _invalidated) != 0")
    < ensure.index("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)"),
    "Invalidation must be observed under the gate before any retained Document project access.",
)

close_start = method_block(source, "private void OnDocumentCloseWillStart(object? sender, EventArgs e)")
for marker in (
    "lock (_documentAccessGate)",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "DetachDocumentCloseHandler();",
    "DetachDocumentManagerHandler();",
    "TryCloseWindow();",
):
    require(marker in close_start, f"CloseWillStart disposal barrier is missing: {marker}")
require(
    close_start.index("lock (_documentAccessGate)")
    < close_start.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < close_start.index("DetachDocumentCloseHandler();")
    < close_start.index("TryCloseWindow();"),
    "CloseWillStart must atomically invalidate under the access gate before handler release/close.",
)

teardown = method_block(
    source,
    "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)",
)
require(
    "if (!MatchesNativeDatabase(e.Document)) return;" in teardown,
    "DocumentToBeDestroyed must remain the managed-wrapper-drift native identity fallback.",
)
require(
    "lock (_documentAccessGate)" in teardown
    and "Interlocked.Exchange(ref _invalidated, 1) != 0" in teardown,
    "DocumentToBeDestroyed must share the same atomic native-access barrier.",
)

close_detach = method_block(source, "private void DetachDocumentCloseHandler()")
require(
    "_document.CloseWillStart -= OnDocumentCloseWillStart;" in close_detach,
    "The per-document early-close subscription must be removed best-effort.",
)

detach = method_block(source, "private void Detach()")
require(
    "DetachDocumentCloseHandler();" in detach and "DetachDocumentManagerHandler();" in detach,
    "Full detach must release both early per-document and native-identity fallback subscriptions.",
)

bind = method_block(source, "private void BindProjectAffinityIfPresent()")
require(
    source.count("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)") == 2
    and "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" in bind,
    "Retained Document project reads must remain limited to initial binding and the synchronized affinity path.",
)

print("[OK] Document-bound modeless activation is serialized behind CloseWillStart/DocumentToBeDestroyed invalidation before retained Document project access.")
