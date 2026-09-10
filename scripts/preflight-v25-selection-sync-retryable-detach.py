#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

required = [
    "private sealed class NativeSelectionSubscription",
    "private static readonly Dictionary<object, NativeSelectionSubscription> NativeSubscriptions",
    "private bool DetachRequested",
    "private bool DetachInProgress",
    "NativeSubscriptions[attachmentToken] = subscription;",
    "subscription.MayBeSubscribed = true;",
    "RequestDetach(subscription);",
    "private static void RequestDetach(NativeSelectionSubscription subscription)",
    "private static void TryDetachSubscription(NativeSelectionSubscription subscription)",
    "if (!subscription.MayBeSubscribed || subscription.DetachInProgress) return;",
    "subscription.DetachInProgress = true;",
    "subscription.Document.ImpliedSelectionChanged -= subscription.Handler;",
    "subscription.MayBeSubscribed = false;",
    "NativeSubscriptions.Remove(subscription.Token);",
    "subscription.DetachInProgress = false;",
    "private static void RetryPendingDetaches()",
]
for needle in required:
    if needle not in source:
        errors.append("selection native-detach contract missing: " + needle)

attach_start = source.find("public static void Attach(Document? document)")
detach_start = source.find("public static void Detach(Document? document)")
by_name_start = source.find("public static void DetachByName", detach_start if detach_start >= 0 else 0)
rollback_start = source.find("private static void RollbackAttachment", by_name_start if by_name_start >= 0 else 0)
release_start = source.find("private static void ReleaseRefresh", rollback_start if rollback_start >= 0 else 0)
callback_start = source.find("private static void OnImpliedSelectionChanged", release_start if release_start >= 0 else 0)
schedule_start = source.find("private static void ScheduleRefresh", callback_start if callback_start >= 0 else 0)
request_start = source.find("private static void RequestDetach", schedule_start if schedule_start >= 0 else 0)
try_detach_start = source.find("private static void TryDetachSubscription", request_start if request_start >= 0 else 0)
retry_start = source.find("private static void RetryPendingDetaches", try_detach_start if try_detach_start >= 0 else 0)

if min(attach_start, detach_start, by_name_start, rollback_start, release_start, callback_start, schedule_start) < 0:
    errors.append("selection coordinator method layout unavailable for lifecycle guard")
else:
    attach = source[attach_start:detach_start]
    detach = source[detach_start:by_name_start]
    rollback = source[rollback_start:release_start]
    callback = source[callback_start:schedule_start]

    # The active generation must be revoked before the native remove boundary, while the
    # separate native subscription record survives a failed remove and remains retryable.
    revoke_tokens = detach.find("AttachmentTokens.Remove(document);")
    request = detach.find("RequestDetach(subscription);")
    if min(revoke_tokens, request) < 0 or revoke_tokens > request:
        errors.append("Detach must revoke active generation before requesting native detach")
    if "document.ImpliedSelectionChanged -= attachmentHandler" in detach:
        errors.append("Detach must not perform a fire-and-forget native remove after forgetting ownership")

    if "RequestDetach(subscription);" not in rollback:
        errors.append("Attach rollback must preserve retryable native detach ownership")
    if "document.ImpliedSelectionChanged -= attachmentHandler" in rollback:
        errors.append("Attach rollback must not abandon failed native remove ownership")

    callback_retry = callback.find("subscription.DetachRequested")
    callback_detach = callback.find("TryDetachSubscription(subscription)", callback_retry if callback_retry >= 0 else 0)
    callback_current = callback.find("IsCurrentAttachment", callback_detach if callback_detach >= 0 else 0)
    if min(callback_retry, callback_detach, callback_current) < 0 or not (callback_retry < callback_detach < callback_current):
        errors.append("retained callback must retry requested native detach before active-generation lookup")

if min(request_start, try_detach_start, retry_start) < 0:
    errors.append("missing retryable native detach helpers")
else:
    try_detach = source[try_detach_start:retry_start]
    guard = try_detach.find("if (!subscription.MayBeSubscribed || subscription.DetachInProgress) return;")
    fence = try_detach.find("subscription.DetachInProgress = true;")
    remove = try_detach.find("subscription.Document.ImpliedSelectionChanged -= subscription.Handler;")
    clear = try_detach.find("subscription.MayBeSubscribed = false;")
    forget = try_detach.find("NativeSubscriptions.Remove(subscription.Token);")
    caught = try_detach.find("catch")
    final = try_detach.find("finally")
    unfence = try_detach.find("subscription.DetachInProgress = false;", final if final >= 0 else 0)
    if min(guard, fence, remove, clear, forget, caught, final, unfence) < 0 or not (
        guard < fence < remove < clear < forget < caught < final < unfence
    ):
        errors.append("native detach must fence reentrancy and forget ownership only after successful remove")

# This lifecycle layer is modeless selection synchronization only. It must not gain CAD/project
# mutations while fixing native event ownership.
for forbidden in ["StartTransaction", "DocumentLock", "SendStringToExecute", "ExecuteDirect(", "ProjectContextCoordinator"]:
    if forbidden in source:
        errors.append("selection-sync lifecycle must remain mutation-free: " + forbidden)

print("QS3D V25 SelectionSync retryable native detach preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: SelectionSync revokes stale UI generations while retaining retry-safe native subscription ownership.")
