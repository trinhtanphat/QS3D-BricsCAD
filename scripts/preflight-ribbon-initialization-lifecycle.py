#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing Ribbon initialization lifecycle source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")


def require(token: str, label: str) -> None:
    if token not in source:
        errors.append(label + " missing required token: " + token)


for token in (
    "private enum HostSubscription : byte",
    "DocumentCreated = 1",
    "DocumentActivated = 2",
    "All = DocumentCreated | DocumentActivated",
    "private static HostSubscription _hostSubscriptions;",
    "private static bool _cleanupPending;",
    "private static bool _stopping;",
    "TryEnsureHostSubscriptions();",
    "acquiredThisAttempt |= HostSubscription.DocumentCreated;",
    "acquiredThisAttempt |= HostSubscription.DocumentActivated;",
    "RollbackHostSubscriptions(documents, acquiredThisAttempt);",
    "_hostSubscriptions &= ~subscription;",
    "if (!TryEnsureHostSubscriptions())",
    "if (!_initialized || _hostSubscriptions != HostSubscription.All)",
    "if (!StopTimedRetry()) cleanupComplete = false;",
    "_cleanupPending = !cleanupComplete",
    "|| _hostSubscriptions != HostSubscription.None",
    "|| _retryTimer != null;",
):
    require(token, "Ribbon lifecycle")

start_begin = source.find("public static void Start()")
stop_begin = source.find("public static void Stop()", start_begin)
ensure_begin = source.find("private static bool TryEnsureHostSubscriptions()", stop_begin)
rollback_begin = source.find("private static void RollbackHostSubscriptions", ensure_begin)
detach_begin = source.find("private static bool TryDetachHostSubscription", rollback_begin)
cleanup_begin = source.find("private static bool TryCleanup", detach_begin)
retry_tick_begin = source.find("private static void OnRetryTick", cleanup_begin)
initialize_begin = source.find("private static bool TryInitializeAll()", retry_tick_begin)

if min(start_begin, stop_begin, ensure_begin, rollback_begin, detach_begin, cleanup_begin, retry_tick_begin, initialize_begin) < 0:
    errors.append("Ribbon lifecycle methods could not be located in required order")
else:
    start_body = source[start_begin:stop_begin]
    stop_body = source[stop_begin:ensure_begin]
    ensure_body = source[ensure_begin:rollback_begin]
    detach_body = source[detach_begin:cleanup_begin]
    retry_body = source[retry_tick_begin:initialize_begin]

    if "TryInitializeAll()" in start_body:
        errors.append("Ribbon Start must remain passive and must not synchronously initialize the Ribbon")
    if "TryEnsureHostSubscriptions();" not in start_body or "StartTimedRetry();" not in start_body:
        errors.append("Ribbon Start must attempt host ownership and schedule bounded retry")

    created_add = ensure_body.find("documents.DocumentCreated += OnDocumentAvailable;")
    created_bit = ensure_body.find("_hostSubscriptions |= HostSubscription.DocumentCreated;")
    activated_add = ensure_body.find("documents.DocumentActivated += OnDocumentAvailable;")
    activated_bit = ensure_body.find("_hostSubscriptions |= HostSubscription.DocumentActivated;")
    rollback = ensure_body.find("RollbackHostSubscriptions(documents, acquiredThisAttempt);")
    if min(created_add, created_bit, activated_add, activated_bit, rollback) < 0:
        errors.append("Host subscription acquisition/rollback markers are incomplete")
    elif not (created_add < created_bit < activated_add < activated_bit < rollback):
        errors.append("Host ownership bits must publish only after each successful add and rollback must follow acquisition failures")

    detach_call = detach_body.find("detach();")
    detach_clear = detach_body.find("_hostSubscriptions &= ~subscription;")
    if min(detach_call, detach_clear) < 0 or not detach_call < detach_clear:
        errors.append("Host subscription ownership must clear only after successful detach")

    retry_ensure = retry_body.find("if (!TryEnsureHostSubscriptions())")
    retry_init = retry_body.find("if (TryInitializeAll())")
    if min(retry_ensure, retry_init) < 0 or not retry_ensure < retry_init:
        errors.append("Idle retry must repair host subscriptions before Ribbon initialization")

    if "if (_stopping) return;" not in stop_body:
        errors.append("Ribbon Stop must be reentrancy-safe")
    if "try" not in stop_body or "finally" not in stop_body:
        errors.append("Ribbon Stop must publish cleanup-pending state from a finally boundary")

    cleanup_targets = (
        "BltBimWorkspaceActivationCoordinator.Stop",
        "HomeTabActivationCoordinator.Stop",
        "Blt3dShellChromeCoordinator.Reset",
        "BltHomeRibbonAugmenter.Reset",
        "BltDrawRibbonAugmenter.Reset",
        "BltToolRibbonAugmenter.Reset",
        "BltToolRibbonCommandBinder.Reset",
        "BltToolRibbonIconPolisher.Reset",
        "BltRecognitionRibbonAugmenter.Reset",
        "BltRecognitionIconPolisher.Reset",
        "BltViewRibbonAugmenter.Reset",
        "BltViewActionOverrideAugmenter.Reset",
        "BltBimRibbonMirrorAugmenter.Reset",
        "BltModelingRibbonVisualRefiner.Reset",
        "BltModelingRibbonFunctionRefiner.Reset",
        "BltModelingRibbonAugmenter.Reset",
        "QuantityReferenceRibbonAugmenter.Reset",
        "BltTopbarTabContract.Reset",
        "RibbonBootstrapIconAugmenter.Reset",
        "Qs3dRibbonTabGroupCoordinator.Reset",
    )
    for target in cleanup_targets:
        method_group = "TryCleanup(" + target + ")"
        block_lambda = "TryCleanup(() => { " + target + "(); })"
        if method_group not in stop_body and block_lambda not in stop_body:
            errors.append("Ribbon Stop must isolate downstream teardown: " + target)

    if stop_body.count("TryCleanup(") < len(cleanup_targets):
        errors.append("Ribbon Stop does not attempt every downstream teardown through fail-soft isolation")

# The older retry contract must remain source-compatible: document availability still
# participates in the passive ApplicationIdle retry path and the timer remains bounded.
for token in (
    "MaxTimedAttempts = 60",
    "TimeSpan.FromMilliseconds(500)",
    "new DispatcherTimer(DispatcherPriority.ApplicationIdle)",
    "if (_timedAttempts >= MaxTimedAttempts)",
    "documents.DocumentCreated += OnDocumentAvailable",
    "documents.DocumentActivated += OnDocumentAvailable",
    "documents.DocumentCreated -= OnDocumentAvailable",
    "documents.DocumentActivated -= OnDocumentAvailable",
):
    require(token, "Existing Ribbon retry contract")

print("QS3D Ribbon initialization lifecycle preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Ribbon host subscriptions are ownership-tracked and repaired before initialization, while Stop retries failed ownership cleanup and isolates every downstream teardown action.")