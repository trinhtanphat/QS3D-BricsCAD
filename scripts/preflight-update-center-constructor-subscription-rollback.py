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
    closed = "Closed += (_, __) => DetachCoordinator();"
    add = "UpdateCoordinator.Instance.StateChanged += OnStateChanged;"
    attached = "_coordinatorAttached = true;"
    apply = "Apply(UpdateCoordinator.Instance.LastResult);"

    for token, message in (
        (closed, "constructor must register normal Closed cleanup"),
        (add, "constructor must subscribe UpdateCoordinator.StateChanged"),
        (attached, "constructor must publish coordinator ownership after subscription"),
        (apply, "constructor must apply the last coordinator result"),
    ):
        if token not in body:
            errors.append(message)

    closed_pos = body.find(closed)
    add_pos = body.find(add)
    attached_pos = body.find(attached)
    apply_pos = body.find(apply)
    if min(closed_pos, add_pos, attached_pos, apply_pos) >= 0 and not (
        closed_pos < add_pos < attached_pos < apply_pos
    ):
        errors.append(
            "constructor must register Closed cleanup before subscribing, publish ownership after subscription, then Apply"
        )

    try_pos = body.find("try")
    catch_match = re.search(
        r"catch(?:\s*\([^)]*\))?\s*\{(?P<catch_body>[^}]*)\}",
        body,
        re.S,
    )
    if try_pos < 0 or catch_match is None:
        errors.append("post-subscription constructor initialization must be guarded transactionally")
    else:
        catch_body = catch_match.group("catch_body")
        if "DetachCoordinator();" not in catch_body or "throw;" not in catch_body:
            errors.append("constructor catch must detach coordinator and rethrow the original construction failure")
        if apply_pos >= 0 and catch_match.start() < apply_pos:
            errors.append("constructor rollback catch must follow the guarded post-subscription Apply")

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
    if remove_pos < 0 or clear_pos < 0 or remove_pos >= clear_pos:
        errors.append("DetachCoordinator must remove StateChanged before clearing ownership")

if errors:
    print("Update Center constructor subscription rollback preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("PASS Update Center constructor registers cleanup before acquisition and rolls back coordinator attachment on failed initialization")
