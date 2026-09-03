#!/usr/bin/env python3
"""Guard host-quiescence race boundaries before native/WPF teardown work."""

# Lane-Key: issue-3621 — H.3 must not dereference BricsCAD wrappers, dispatch modeless
# callbacks, or unsubscribe native reactors after host quiescence starts.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WINDOW_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
NATIVE_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundNativeLifecycleCoordinator.cs"


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


require(NATIVE_SOURCE.exists(), "H.3 shared native lifecycle coordinator source is missing.")
window_source = WINDOW_SOURCE.read_text(encoding="utf-8")
native_source = NATIVE_SOURCE.read_text(encoding="utf-8")
barrier = "if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;"

# The native DocumentToBeDestroyed owner sees the event before individual windows. It must stop
# before reading e.Document because the managed wrapper may already front native teardown state.
native_destroyed = method_block(native_source, "private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
require(barrier in native_destroyed,
        "Shared DocumentToBeDestroyed must fail closed immediately during host quiescence.")
require("document = e.Document;" in native_destroyed,
        "Shared DocumentToBeDestroyed must capture the event document only inside its guarded owner.")
require(native_destroyed.index(barrier) < native_destroyed.index("document = e.Document;"),
        "Shared DocumentToBeDestroyed must test host quiescence before dereferencing the destroying wrapper.")
for marker in ("TrySnapshotDestroyByLifecycleDocument", "TrySnapshotDestroyByNativeIdentity"):
    require(marker in native_destroyed, f"Shared DocumentToBeDestroyed is missing affinity lookup: {marker}")

# Per-document native BeginDocumentClose / CloseAborted handlers must not dispatch callbacks after
# the host quit boundary. That keeps per-window WPF registrations off native teardown stacks.
for signature in (
    "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)",
    "private void OnDocumentCloseAborted(object? sender, EventArgs e)",
):
    block = method_block(native_source, signature)
    require(barrier in block, f"{signature} must own a global host-quiescence barrier.")
    require(block.index(barrier) < block.index("callbacks = SnapshotLiveCallbacks();"),
            f"{signature} must test quiescence before dispatching managed callbacks.")

# Registration itself still re-checks before DocumentManager access; quiescence may arm between
# native coordinator dispatch and the managed callback's first instruction. The coordinator has
# already proved affinity, so the per-window callback must not reopen the event Document.
window_destroyed = method_block(window_source, "private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)")
require(barrier in window_destroyed,
        "Managed DocumentToBeDestroyed callback must fail closed immediately during host quiescence.")
require("e.Document" not in window_destroyed and "MatchesNativeDatabase(" not in window_destroyed,
        "Managed DocumentToBeDestroyed callback must consume coordinator-owned affinity without wrapper access.")
require(window_destroyed.index(barrier) < window_destroyed.index("HasAnotherLiveDocument()"),
        "Managed DocumentToBeDestroyed callback must re-check quiescence before DocumentManager enumeration.")

window_begin = method_block(window_source, "private void OnBeginDocumentClose(object sender, DocumentBeginCloseEventArgs e)")
require(barrier in window_begin,
        "Managed BeginDocumentClose callback must fail closed immediately during host quiescence.")
require(window_begin.index(barrier) < window_begin.index("HasAnotherLiveDocument()"),
        "Managed BeginDocumentClose callback must re-check quiescence before DocumentManager enumeration.")

# Project-affinity close can race with host quit between its caller check and helper body.
project_change = method_block(window_source, "private void CloseForProjectChange()")
require(barrier in project_change,
        "Project-affinity close must re-check host quiescence at its own boundary.")
require(project_change.index(barrier) < project_change.index("lock (_documentAccessGate)"),
        "Project-affinity close must re-check quiescence before invalidation/native cleanup.")
require(project_change.index(barrier) < project_change.index("DetachDocumentLifecycleHandlersIfSafe();"),
        "Project-affinity close must not release the managed native subscription after quiescence starts.")

# Window-side lifecycle detach helpers own their own barriers instead of trusting stale caller checks.
for signature in (
    "private void DetachDocumentLifecycleHandlersIfSafe()",
    "private void DetachDocumentLifecycleHandlersAfterAbort()",
):
    block = method_block(window_source, signature)
    require(barrier in block, f"{signature} must own a host-quiescence barrier.")
    require(block.index(barrier) < block.index("DetachNativeLifecycleSubscription();"),
            f"{signature} must re-check quiescence before releasing the managed native subscription.")

# The shared Subscription.Dispose path can be called from ordinary WPF cleanup. The coordinator
# itself must refuse native unsubscription if host quiescence races with that call.
unregister = method_block(native_source, "private static void Unregister(Entry entry, Callbacks callbacks)")
require("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;" in unregister,
        "Shared native unregister must own a host-quiescence barrier.")
require(
    unregister.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;")
    < unregister.index("entry.DetachNativeHandlersIfSafe();"),
    "Shared native unregister must re-check quiescence before native event unsubscription.",
)

native_detach = method_block(native_source, "public void DetachNativeHandlersIfSafe()")
require("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;" in native_detach,
        "Per-document native detach must own a second host-quiescence barrier.")
require(
    native_detach.index("if (ModelessHostQuiescenceCoordinator.IsQuiescing) return;")
    < native_detach.index("lifecycleDocument.BeginDocumentClose -= OnBeginDocumentClose;"),
    "Per-document native detach must re-check quiescence immediately before native unsubscription.",
)

# QuitAborted recovery is dispatcher-deferred. A second host quit may begin before it executes.
recover = method_block(window_source, "private void TryRecoverAfterQuitAbort()")
require(barrier in recover,
        "Deferred QuitAborted recovery must re-check host quiescence before cleanup.")
require(recover.index(barrier) < recover.index("DetachDocumentLifecycleHandlersAfterAbort();"),
        "Deferred QuitAborted recovery must re-check quiescence before releasing lifecycle ownership.")

print("[OK] V25 modeless shutdown re-checks global host quiescence before native wrapper access, managed callback dispatch, DocumentManager enumeration, native unsubscription, and deferred abort recovery.")
