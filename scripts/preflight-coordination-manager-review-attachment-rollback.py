#!/usr/bin/env python3
from pathlib import Path
import sys

# issue-4398 + issue-4699: deterministic source guard for transactional Coordination
# Manager modeless-window publication and instance-safe ownership.
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
    ("var previous = _published;", "capture prior published manager ownership"),
    ("candidate = new CoordinationManagerWindow", "construct unpublished candidate"),
    ("CoordinationManagerReviewUi.Attach(candidate", "review attach against unpublished candidate"),
    ("var published = new PublishedManager(publishedWindow, document);", "atomic window/document ownership candidate"),
    ("Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);", "host show before static publication"),
    ("_published = published;", "publish only successful modeless manager"),
    ("candidate = null;", "transfer local ownership after publication"),
    ("try { candidate.Close(); } catch { }", "failed-initialization candidate cleanup"),
    ("if (ReferenceEquals(_published, published)) _published = null;", "instance-safe Closed cleanup"),
]

for needle, label in required:
    if needle not in source:
        errors.append(f"missing {label}: {needle}")

construct_at = source.find("candidate = new CoordinationManagerWindow")
attach_at = source.find("CoordinationManagerReviewUi.Attach(candidate")
ownership_at = source.find("var published = new PublishedManager(publishedWindow, document);")
show_at = source.find("Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);")
publish_at = source.find("_published = published;")
transfer_at = source.find("candidate = null;", publish_at if publish_at >= 0 else 0)
if min(construct_at, attach_at, ownership_at, show_at, publish_at, transfer_at) < 0 or not (
    construct_at < attach_at < ownership_at < show_at < publish_at < transfer_at
):
    errors.append("candidate lifecycle must be construct -> review attach -> ownership object -> host show -> static publish -> local ownership transfer")

legacy = [
    "_window = new CoordinationManagerWindow",
    "_window = null;",
    "try { previous.Close(); } catch { }",
]
for token in legacy:
    if token in source:
        errors.append("legacy close-and-replace/static pre-clear pattern must not return: " + token)

if errors:
    print("ERROR: Coordination Manager review attachment rollback source guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS: Coordination Manager publishes only fully attached/shown document-bound owners and rolls failed candidates back without orphaning a live prior manager.")
print("NOTE: this is deterministic source evidence only; licensed BricsCAD modeless behavior is not claimed.")
