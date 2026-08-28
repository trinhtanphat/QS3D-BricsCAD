#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Updates/UpdateCenterWindow.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

ctor = re.search(
    r"internal UpdateCenterWindow\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*internal void Apply",
    text,
    re.S,
)
if not ctor:
    errors.append("UpdateCenterWindow constructor was not found")
else:
    body = ctor.group("body")
    add = "UpdateCoordinator.Instance.StateChanged += OnStateChanged;"
    apply = "Apply(UpdateCoordinator.Instance.LastResult);"
    if add not in body:
        errors.append("constructor must subscribe UpdateCoordinator.StateChanged")
    if apply not in body:
        errors.append("constructor must apply the last coordinator result")
    if "try" not in body or "catch" not in body:
        errors.append("post-subscription constructor initialization must be guarded transactionally")
    if "DetachCoordinator();" not in body:
        errors.append("constructor failure path must roll back the coordinator subscription")
    add_pos = body.find(add)
    attached_pos = body.find("_coordinatorAttached = true;")
    apply_pos = body.find(apply)
    detach_pos = body.find("DetachCoordinator();")
    if min(add_pos, attached_pos, apply_pos) < 0:
        pass
    elif not (add_pos < attached_pos < apply_pos):
        errors.append("coordinator ownership must be published after subscription and before post-subscription Apply")
    if detach_pos >= 0 and apply_pos >= 0 and detach_pos < apply_pos:
        errors.append("constructor rollback detach must belong to the failure path after guarded Apply")
    if not re.search(r"catch(?:\s*\([^)]*\))?\s*\{[^}]*DetachCoordinator\(\);[^}]*throw;", body, re.S):
        errors.append("constructor catch must detach coordinator and rethrow the original construction failure")

method = re.search(
    r"internal void DetachCoordinator\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private async",
    text,
    re.S,
)
if not method:
    errors.append("DetachCoordinator method was not found")
else:
    body = method.group("body")
    if "if (!_coordinatorAttached) return;" not in body:
        errors.append("normal coordinator detach must remain idempotent")
    remove_pos = body.find("UpdateCoordinator.Instance.StateChanged -= OnStateChanged;")
    clear_pos = body.find("_coordinatorAttached = false;")
    if remove_pos < 0 or clear_pos < 0:
        errors.append("DetachCoordinator must remove StateChanged and clear ownership")

if errors:
    print("Update Center constructor subscription rollback preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("PASS Update Center constructor rolls back coordinator attachment on failed initialization")
