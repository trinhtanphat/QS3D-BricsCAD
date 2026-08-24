#!/usr/bin/env python3
"""Guard host-quiescence race boundaries before native/WPF teardown work."""

# Lane-Key: issue-3621 — p06 must not touch BricsCAD/native wrappers after host quiescence starts.
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

# DocumentToBeDestroyed can arrive while native wrappers are already being dismantled. The global
# barrier must therefore run before even reading e.Document.Database through MatchesNativeDatabase.
destroyed = method_block(source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
require(barrier in destroyed, "DocumentToBeDestroyed must fail closed immediately during host quiescence.")
require(
    destroyed.index(barrier) < destroyed.index("MatchesNativeDatabase(e.Document)"),
    "DocumentToBeDestroyed must test host quiescence before dereferencing the destroying Document wrapper.",
)

# BeginDocumentClose must not enumerate DocumentManager after the application quit boundary.
begin_close = method_block(source, "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)")
require(barrier in begin_close, "BeginDocumentClose must fail closed immediately during host quiescence.")
require(
    begin_close.index(barrier) < begin_close.index("HasAnotherLiveDocument()"),
    "BeginDocumentClose must test host quiescence before enumerating DocumentManager.",
)

# Every helper that removes a BricsCAD lifecycle handler must protect itself, not rely only on a
# caller-side check that can become stale before native unsubscription executes.
for signature, native_marker in (
    ("private void DetachDocumentManagerHandler()", "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;"),
    ("private void DetachDocumentLifecycleHandlersIfSafe()", "_lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;"),
    ("private void DetachDocumentLifecycleHandlersAfterAbort()", "_lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;"),
):
    block = method_block(source, signature)
    require(barrier in block, f"{signature} must own a host-quiescence barrier.")
    require(
        block.index(barrier) < block.index(native_marker),
        f"{signature} must re-check host quiescence before native event unsubscription.",
    )

# QuitAborted recovery is dispatcher-deferred. A second host quit may begin before that callback
# executes, so the callback needs a fresh barrier before any detach or close operation.
recover = method_block(source, "private void TryRecoverAfterQuitAbort()")
require(
    "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;" in recover,
    "Deferred QuitAborted recovery must re-check host quiescence before cleanup.",
)
require(
    recover.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;")
    < recover.index("DetachDocumentLifecycleHandlersAfterAbort();"),
    "Deferred QuitAborted recovery must re-check quiescence before detaching lifecycle handlers.",
)

print("[OK] V25 modeless shutdown re-checks global host quiescence before destroying-document access, DocumentManager enumeration, native unsubscription, and deferred abort recovery.")
