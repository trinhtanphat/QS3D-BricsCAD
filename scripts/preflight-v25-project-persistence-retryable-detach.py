#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs"
errors = []
source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
if not source:
    errors.append("missing DocumentLifecycleCoordinator.cs")

required = [
    "private sealed class ProjectPersistenceSubscription",
    "private static readonly Dictionary<object, ProjectPersistenceSubscription> ProjectPersistenceSubscriptions",
    "MayHaveSaveComplete",
    "MayHaveBeginClose",
    "DetachRequested",
    "DetachInProgress",
    "RequestProjectPersistenceDetach(",
    "TryDetachProjectPersistenceSubscription(",
    "RetryPendingProjectPersistenceDetaches()",
]
for needle in required:
    if needle not in source:
        errors.append("project-persistence durable ownership contract missing: " + needle)

attach_start = source.find("private static void AttachProjectPersistence")
detach_start = source.find("private static void DetachProjectPersistence", attach_start)
save_start = source.find("private static void OnDrawingSaveComplete", detach_start)
close_start = source.find("private static void OnBeginDocumentClose", save_start)
if min(attach_start, detach_start, save_start, close_start) < 0:
    errors.append("project-persistence lifecycle method layout unavailable")
else:
    attach = source[attach_start:detach_start]
    detach = source[detach_start:save_start]
    save = source[save_start:close_start]
    record = attach.find("ProjectPersistenceSubscriptions[")
    save_may = attach.find("MayHaveSaveComplete = true")
    save_add = attach.find("document.Database.SaveComplete +=")
    close_may = attach.find("MayHaveBeginClose = true")
    close_add = attach.find("document.BeginDocumentClose +=")
    if min(record, save_may, save_add, close_may, close_add) < 0 or not (record < save_may < save_add < close_may < close_add):
        errors.append("Attach must publish exact durable may-be-subscribed ownership before each fallible native add")

    if "SaveCompleteHandlers.Remove(document)" in detach or "BeginCloseHandlers.Remove(document)" in detach:
        errors.append("Detach must not forget native ownership unconditionally after swallowed remove failure")
    if "RequestProjectPersistenceDetach(" not in detach:
        errors.append("Detach must route through retryable project-persistence detach ownership")

    if "DetachRequested" not in save or "TryDetachProjectPersistenceSubscription" not in save:
        errors.append("retained SaveComplete callback must retry detach and fail closed before sidecar work")

stop_start = source.find("public static void Stop()")
created_start = source.find("private static void OnDocumentCreated", stop_start)
if stop_start < 0 or created_start < 0:
    errors.append("Stop lifecycle section unavailable")
else:
    stop = source[stop_start:created_start]
    if "RetryPendingProjectPersistenceDetaches();" not in stop:
        errors.append("Stop must retry retained project-persistence native subscriptions")
    if "ProjectPersistenceSubscriptions.Clear()" in stop:
        errors.append("Stop must not clear subscriptions whose native remove may still be pending")

for forbidden in ["SendStringToExecute", "StartTransaction", "DocumentLock", "ExecuteDirect("]:
    lifecycle = source[attach_start:close_start] if attach_start >= 0 and close_start >= 0 else ""
    if forbidden in lifecycle:
        errors.append("project-persistence subscription lifecycle must remain CAD-mutation-free: " + forbidden)

print("QS3D V25 project-persistence retryable native detach preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: project persistence retains retry-safe exact native subscription ownership across partial add/remove failure.")
