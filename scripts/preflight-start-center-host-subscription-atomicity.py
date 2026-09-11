#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
text = SOURCE.read_text(encoding="utf-8")

errors = []

match = re.search(
    r"private void SubscribeToHostLifecycle\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void RetryHostLifecycleDetach",
    text,
    re.S,
)
if not match:
    errors.append("SubscribeToHostLifecycle method was not found")
else:
    body = match.group("body")
    activated_add = "Application.DocumentManager.DocumentActivated += OnHostDocumentActivated;"
    destroy_add = "Application.DocumentManager.DocumentToBeDestroyed += OnHostDocumentToBeDestroyed;"
    for token in (activated_add, destroy_add):
        if token not in body:
            errors.append(f"missing required host subscription: {token}")
    if "try" not in body or "catch" not in body:
        errors.append("host lifecycle subscription must be guarded transactionally")
    activated_owner = body.find("_documentActivatedMayBeSubscribed = true;")
    activated_add_pos = body.find(activated_add)
    destroy_owner = body.find("_documentDestroyMayBeSubscribed = true;")
    destroy_add_pos = body.find(destroy_add)
    if activated_owner < 0 or activated_add_pos < 0 or activated_owner > activated_add_pos:
        errors.append("DocumentActivated ownership must publish before fallible native add")
    if destroy_owner < 0 or destroy_add_pos < 0 or destroy_owner > destroy_add_pos:
        errors.append("DocumentToBeDestroyed ownership must publish before fallible native add")
    success_assignment = body.find("_hostLifecycleActive = true;")
    last_add = max(activated_add_pos, destroy_add_pos)
    if success_assignment < 0 or success_assignment < last_add:
        errors.append("active callback authority may become true before both host subscriptions succeed")
    catch_pos = body.find("catch")
    if catch_pos < 0 or body.find("_hostLifecycleActive = false;", catch_pos) < 0 or body.find("RetryHostLifecycleDetach();", catch_pos) < 0:
        errors.append("partial host subscription failure must revoke authority and retry exact detach")

unsub = re.search(
    r"private void RetryHostLifecycleDetach\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void OnHostDocumentActivated",
    text,
    re.S,
)
if not unsub:
    errors.append("RetryHostLifecycleDetach method was not found")
else:
    body = unsub.group("body")
    if body.count("try") < 3 or body.count("catch") < 2:
        errors.append("host lifecycle retry detach must isolate both native remove failures and restore reentrancy state")
    for remove, clear in (("Application.DocumentManager.DocumentToBeDestroyed -= OnHostDocumentToBeDestroyed;", "_documentDestroyMayBeSubscribed = false;"), ("Application.DocumentManager.DocumentActivated -= OnHostDocumentActivated;", "_documentActivatedMayBeSubscribed = false;")):
        rp=body.find(remove); cp=body.find(clear, rp + len(remove)) if rp >= 0 else -1
        if rp < 0 or cp < 0:
            errors.append(f"native ownership must clear only after exact detach succeeds: {remove}")
    if "_hostLifecycleDetachInProgress = true;" not in body or "_hostLifecycleDetachInProgress = false;" not in body:
        errors.append("retry detach must fence and release detach reentrancy")

if errors:
    print("Start Center host subscription atomicity preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("PASS Start Center host lifecycle subscription is transactional, ownership-retaining, and retry-detachable")
