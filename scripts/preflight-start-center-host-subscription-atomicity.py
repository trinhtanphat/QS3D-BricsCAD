#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
text = SOURCE.read_text(encoding="utf-8")

errors = []

match = re.search(
    r"private void SubscribeToHostLifecycle\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void UnsubscribeFromHostLifecycle",
    text,
    re.S,
)
if not match:
    errors.append("SubscribeToHostLifecycle method was not found")
else:
    body = match.group("body")
    activated_add = "Application.DocumentManager.DocumentActivated += OnHostDocumentActivated;"
    destroy_add = "Application.DocumentManager.DocumentToBeDestroyed += OnHostDocumentToBeDestroyed;"
    activated_remove = "Application.DocumentManager.DocumentActivated -= OnHostDocumentActivated;"
    destroy_remove = "Application.DocumentManager.DocumentToBeDestroyed -= OnHostDocumentToBeDestroyed;"

    for token in (activated_add, destroy_add):
        if token not in body:
            errors.append(f"missing required host subscription: {token}")

    if "try" not in body or "catch" not in body:
        errors.append("host lifecycle subscription must be guarded transactionally")

    if activated_remove not in body:
        errors.append("partial DocumentActivated subscription must be rolled back inside SubscribeToHostLifecycle")

    if destroy_remove not in body:
        errors.append("partial DocumentToBeDestroyed subscription must be rollback-capable inside SubscribeToHostLifecycle")

    success_assignment = body.find("_hostLifecycleSubscribed = true;")
    last_add = max(body.find(activated_add), body.find(destroy_add))
    if success_assignment < 0 or success_assignment < last_add:
        errors.append("_hostLifecycleSubscribed may become true before both host event subscriptions succeed")

    if "_hostLifecycleSubscribed = false;" not in body:
        errors.append("failed/rolled-back subscription must leave ownership state false")

    if not re.search(r"catch(?:\s*\([^)]*\))?\s*\{", body):
        errors.append("subscription failure path must explicitly catch host add failures")

unsub = re.search(
    r"private void UnsubscribeFromHostLifecycle\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private void OnHostDocumentActivated",
    text,
    re.S,
)
if not unsub:
    errors.append("UnsubscribeFromHostLifecycle method was not found")
else:
    body = unsub.group("body")
    if body.count("try") < 2 or body.count("catch") < 2:
        errors.append("host lifecycle unsubscribe must continue attempting both native detach operations independently")
    if "_hostLifecycleSubscribed = false;" not in body:
        errors.append("unsubscribe must clear host lifecycle ownership state")
    if re.search(r"if\s*\(\s*!_hostLifecycleSubscribed\s*\)\s*return\s*;", body):
        errors.append("unsubscribe must retry both native detach operations even when a failed rollback left ownership unpublished")

if errors:
    print("Start Center host subscription atomicity preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("PASS Start Center host lifecycle subscription is transactional and teardown remains fail-soft")
