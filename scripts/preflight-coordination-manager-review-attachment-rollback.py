#!/usr/bin/env python3
from pathlib import Path
import sys

# issue-4398: deterministic source guard for transactional Coordination Manager
# modeless-window publication. Reservation-v2 metadata is machine-readable on the Issue.
ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CoordinationManagerCommands.cs"

errors = []

if not COMMAND.exists():
    errors.append(f"missing required file: {COMMAND.relative_to(ROOT)}")
    source = ""
else:
    source = COMMAND.read_text(encoding="utf-8")

required = [
    ("CoordinationManagerWindow? candidate = null;", "local unpublished candidate ownership"),
    ("var previous = _window;", "capture prior published window"),
    ("_window = null;", "clear prior static ownership before replacement"),
    ("try { previous.Close(); } catch { }", "best-effort stale-window cleanup"),
    ("candidate = new CoordinationManagerWindow", "construct unpublished candidate"),
    ("CoordinationManagerReviewUi.Attach(candidate", "review attach against unpublished candidate"),
    ("Application.ShowModelessWindow(IntPtr.Zero, published, true);", "host show before static publication"),
    ("_window = published;", "publish only successful modeless window"),
    ("candidate = null;", "transfer local ownership after publication"),
    ("try { candidate.Close(); } catch { }", "failed-initialization candidate cleanup"),
    ("if (ReferenceEquals(_window, published)) _window = null;", "instance-safe Closed cleanup"),
]

for needle, label in required:
    if needle not in source:
        errors.append(f"missing {label}: {needle}")

construct_at = source.find("candidate = new CoordinationManagerWindow")
attach_at = source.find("CoordinationManagerReviewUi.Attach(candidate")
show_at = source.find("Application.ShowModelessWindow(IntPtr.Zero, published, true);")
publish_at = source.find("_window = published;")
transfer_at = source.find("candidate = null;", publish_at if publish_at >= 0 else 0)
if min(construct_at, attach_at, show_at, publish_at, transfer_at) < 0 or not (
    construct_at < attach_at < show_at < publish_at < transfer_at
):
    errors.append("candidate lifecycle must be construct -> review attach -> host show -> static publish -> local ownership transfer")

legacy = "_window = new CoordinationManagerWindow"
if legacy in source:
    errors.append("static _window must not receive a newly constructed candidate before initialization succeeds")

old_cleanup = "if (_window != null && _window.IsLoaded) _window.Close();"
if old_cleanup in source:
    errors.append("stale previous-window cleanup must not depend on IsLoaded")

if errors:
    print("ERROR: issue-4398 Coordination Manager review attachment rollback source guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS: issue-4398 Coordination Manager publishes only fully attached/shown windows and rolls failed candidates back without static retention.")
print("NOTE: this is deterministic source evidence only; licensed BricsCAD modeless behavior is not claimed.")
