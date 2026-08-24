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
    "Document-bound modeless lifetime must serialize live Document access against teardown.",
)
require(
    "private readonly Document _document;" not in source,
    "WPF modeless affinity must not retain the managed Document wrapper used by the old crash path.",
)
require(
    "private readonly Document _lifecycleDocument;" in source,
    "A lifecycle-only wrapper is required solely for per-document close event ownership.",
)
require(
    "private int _hostQuitStarted;" in source,
    "Application quit must be tracked independently of document wrapper/count state.",
)

attach = method_block(source, "public void Attach(Document document)")
for marker in (
    "BindProjectAffinityIfPresent();",
    "_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted += OnDocumentCloseAborted;",
    "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
    "BcadApplication.BeginQuit += OnApplicationBeginQuit;",
    "BcadApplication.QuitAborted += OnApplicationQuitAborted;",
    "_window.Activated += OnWindowActivated;",
):
    require(marker in attach, f"Attach is missing disposal-safe lifecycle marker: {marker}")
require(
    attach.index("BindProjectAffinityIfPresent();")
    < attach.index("_lifecycleDocument.BeginDocumentClose += OnBeginDocumentClose;")
    < attach.index("BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;")
    < attach.index("BcadApplication.BeginQuit += OnApplicationBeginQuit;")
    < attach.index("_window.Activated += OnWindowActivated;"),
    "Document/host close barriers must be installed before WPF activation can be observed.",
)

resolve = method_block(source, "private bool TryResolveLiveDocument(out Document document)")
for marker in (
    "foreach (Document candidate in BcadApplication.DocumentManager)",
    "candidate.IsDisposed",
    "MatchesNativeDatabase(candidate)",
    "document = candidate;",
):
    require(marker in resolve, f"Live managed-wrapper resolution is missing: {marker}")
require(
    "_lifecycleDocument" not in resolve,
    "Live document resolution must not dereference the retained lifecycle wrapper.",
)

ensure = method_block(source, "private bool EnsureProjectAffinity()")
require(
    "lock (_documentAccessGate)" in ensure,
    "Project-affinity reads must run under the document access gate.",
)
for marker in (
    "Volatile.Read(ref _invalidated) != 0",
    "TryResolveLiveDocument(out var document)",
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
):
    require(marker in ensure, f"Disposal-safe affinity path is missing: {marker}")
require(
    ensure.index("lock (_documentAccessGate)")
    < ensure.index("Volatile.Read(ref _invalidated) != 0")
    < ensure.index("TryResolveLiveDocument(out var document)")
    < ensure.index("ProjectContextCoordinator.TryGetReadOnly(document, out var project)"),
    "Invalidation and live-wrapper resolution must precede all project reads under the gate.",
)
require(
    "ProjectContextCoordinator.TryGetReadOnly(_lifecycleDocument" not in source,
    "The lifecycle-only wrapper must never enter project/path affinity code.",
)

begin_close = method_block(source, "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)")
for marker in (
    "var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;",
    "var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();",
    "lock (_documentAccessGate)",
    "Volatile.Write(ref _documentCloseStarted, 1)",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "if (abandonForHostShutdown) return;",
    "DetachDocumentManagerHandler();",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in begin_close, f"BeginDocumentClose barrier is missing: {marker}")
require(
    begin_close.index("var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;")
    < begin_close.index("var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();")
    < begin_close.index("lock (_documentAccessGate)")
    < begin_close.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < begin_close.index("if (abandonForHostShutdown) return;")
    < begin_close.index("DetachDocumentManagerHandler();")
    < begin_close.index("TryCloseWindow(deferForFinalDocument);"),
    "BeginDocumentClose must classify host quit, fail closed, preserve native lifecycle subscriptions during application shutdown, then clean up and close only for ordinary document close.",
)
require(
    "_lifecycleDocument." not in begin_close,
    "BeginDocumentClose must not dereference the retained lifecycle wrapper once close starts.",
)

teardown = method_block(source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
for marker in (
    "if (!MatchesNativeDatabase(e.Document)) return;",
    "var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;",
    "var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();",
    "lock (_documentAccessGate)",
    "Volatile.Write(ref _documentCloseStarted, 1)",
    "Interlocked.Exchange(ref _invalidated, 1) != 0",
    "if (abandonForHostShutdown) return;",
    "DetachDocumentManagerHandler();",
    "TryCloseWindow(deferForFinalDocument);",
):
    require(marker in teardown, f"DocumentToBeDestroyed barrier is missing: {marker}")
require(
    teardown.index("if (!MatchesNativeDatabase(e.Document)) return;")
    < teardown.index("var abandonForHostShutdown = Volatile.Read(ref _hostQuitStarted) != 0;")
    < teardown.index("var deferForFinalDocument = !abandonForHostShutdown && !HasAnotherLiveDocument();")
    < teardown.index("lock (_documentAccessGate)")
    < teardown.index("Interlocked.Exchange(ref _invalidated, 1) != 0")
    < teardown.index("if (abandonForHostShutdown) return;")
    < teardown.index("DetachDocumentManagerHandler();")
    < teardown.index("TryCloseWindow(deferForFinalDocument);"),
    "DocumentToBeDestroyed must validate native identity, classify host quit, fail closed, preserve native lifecycle subscriptions during shutdown, then use ordinary close cleanup only outside host quit.",
)
require(
    "_lifecycleDocument." not in teardown,
    "Native destruction fallback must never touch the retained lifecycle wrapper.",
)

safe_detach = method_block(source, "private void DetachDocumentLifecycleHandlersIfSafe()")
require(
    "Volatile.Read(ref _documentCloseStarted) != 0" in safe_detach,
    "Window.Closed after native close starts must skip lifecycle-wrapper event removal.",
)
for marker in (
    "_lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;",
    "_lifecycleDocument.CloseAborted -= OnDocumentCloseAborted;",
):
    require(marker in safe_detach, f"Normal live-document detach is missing: {marker}")

abort = method_block(source, "private void OnDocumentCloseAborted(object? sender, EventArgs e)")
require(
    "DetachDocumentLifecycleHandlersAfterAbort();" in abort,
    "An aborted close must release lifecycle subscriptions while the document is live again.",
)

detach = method_block(source, "private void Detach()")
require(
    "DetachDocumentLifecycleHandlersIfSafe();" in detach
    and "DetachDocumentManagerHandler();" in detach,
    "Full detach must release safe lifecycle/global subscriptions without post-disposal dereference.",
)
require(
    "BcadApplication.BeginQuit -= OnApplicationBeginQuit;" in detach
    and "BcadApplication.QuitAborted -= OnApplicationQuitAborted;" in detach,
    "Full detach must release host lifecycle subscriptions on ordinary window closure.",
)

print("[OK] Modeless affinity resolves a live wrapper by native database identity, invalidates before teardown, and keeps BricsCAD native/WPF teardown quiescent during application quit.")
