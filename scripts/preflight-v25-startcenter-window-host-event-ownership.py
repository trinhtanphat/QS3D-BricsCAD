#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")


def require(token, message):
    if token not in text:
        errors.append(message)


def require_order(first, second, message):
    a = text.find(first)
    b = text.find(second, a + len(first)) if a >= 0 else -1
    if a < 0 or b < 0 or a >= b:
        errors.append(message)


for token, message in (
    ("_documentActivatedMayBeSubscribed", "Start Center window must retain DocumentActivated may-be-subscribed ownership"),
    ("_documentDestroyMayBeSubscribed", "Start Center window must retain DocumentToBeDestroyed may-be-subscribed ownership"),
    ("_hostLifecycleActive", "Start Center window must separate active callback authority from durable native ownership"),
    ("_hostLifecycleDetachInProgress", "Start Center window must fence native detach reentrancy"),
    ("RetryHostLifecycleDetach", "Start Center window must expose retryable native host lifecycle detach"),
):
    require(token, message)

require_order(
    "_documentActivatedMayBeSubscribed = true",
    "DocumentActivated += OnHostDocumentActivated",
    "Start Center window must publish DocumentActivated ownership before fallible native +=",
)
require_order(
    "_documentDestroyMayBeSubscribed = true",
    "DocumentToBeDestroyed += OnHostDocumentToBeDestroyed",
    "Start Center window must publish DocumentToBeDestroyed ownership before fallible native +=",
)
require_order(
    "DocumentActivated -= OnHostDocumentActivated",
    "_documentActivatedMayBeSubscribed = false",
    "Start Center window must clear DocumentActivated ownership only after exact native -= succeeds",
)
require_order(
    "DocumentToBeDestroyed -= OnHostDocumentToBeDestroyed",
    "_documentDestroyMayBeSubscribed = false",
    "Start Center window must clear DocumentToBeDestroyed ownership only after exact native -= succeeds",
)

closed = text.find("private void OnWindowClosed")
closed_mark = text.find("_windowClosed = true", closed)
closed_revoke = text.find("_hostLifecycleActive = false", closed_mark)
closed_detach = text.find("RetryHostLifecycleDetach();", closed_revoke)
if closed < 0 or closed_mark < 0 or closed_revoke < 0 or closed_detach < 0:
    errors.append("Closed must revoke active window authority before retrying retained host-event detach")

activated = text.find("private void OnHostDocumentActivated")
activated_stale = text.find("if (_windowClosed || !_hostLifecycleActive)", activated)
activated_retry = text.find("RetryHostLifecycleDetach();", activated_stale)
activated_queue = text.find("QueueHomeRefresh", activated)
if activated < 0 or activated_stale < 0 or activated_retry < 0 or activated_queue < 0 or activated_retry > activated_queue:
    errors.append("retained/inactive DocumentActivated callbacks must retry detach before any UI refresh")

destroyed = text.find("private void OnHostDocumentToBeDestroyed")
destroyed_stale = text.find("if (_windowClosed || !_hostLifecycleActive)", destroyed)
destroyed_retry = text.find("RetryHostLifecycleDetach();", destroyed_stale)
destroyed_args = text.find("e.Document", destroyed)
destroyed_mdi = text.find("Application.DocumentManager.MdiActiveDocument", destroyed)
if (destroyed < 0 or destroyed_stale < 0 or destroyed_retry < 0 or destroyed_args < 0 or destroyed_mdi < 0 or
        destroyed_retry > destroyed_args or destroyed_retry > destroyed_mdi):
    errors.append("retained/inactive DocumentToBeDestroyed callbacks must retry detach before document/UI access")

attach_catch = text.find("catch\n            {", text.find("private void SubscribeToHostLifecycle"))
attach_revoke = text.find("_hostLifecycleActive = false", attach_catch)
attach_detach = text.find("RetryHostLifecycleDetach();", attach_revoke)
if attach_catch < 0 or attach_revoke < 0 or attach_detach < 0:
    errors.append("partial native attach failure must revoke callback authority before detach compensation")

if "_hostLifecycleSubscribed" in text:
    errors.append("Start Center window must not collapse independent native ownership into a forgetful fully-subscribed boolean")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: V25 Start Center window retains independent native document-event ownership across partial add/remove failures, revokes stale callback authority, retries retained detaches, and clears ownership only after exact removal.")
