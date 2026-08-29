#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")

match = re.search(
    r"public void ApplySectionFocus\(IReadOnlyList<ObjectId> ids\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*public void RestoreSectionView",
    text,
    re.S,
)
if not match:
    raise SystemExit("FAIL coordination section focus rollback ownership: ApplySectionFocus method not found")
body = match.group("body")

for token, label in [
    ("var viewBeforeSection = ViewSnapshot.Capture(view);", "attempt-local prior-view snapshot"),
    ("_document.Editor.SetCurrentView(view);", "native section view apply"),
    ("TryRestoreSectionViewBestEffort(viewBeforeSection)", "observable rollback result"),
    ("_viewBeforeSection = viewBeforeSection;", "persistent retry ownership publication"),
    ("throw;", "original native apply failure rethrow"),
]:
    if token not in body:
        raise SystemExit(f"FAIL coordination section focus rollback ownership: missing {label}: {token}")

apply_at = body.find("_document.Editor.SetCurrentView(view);")
catch_at = body.find("catch", apply_at)
if apply_at < 0 or catch_at < 0:
    raise SystemExit("FAIL coordination section focus rollback ownership: native apply must have a compensation catch")

catch_tail = body[catch_at:]
conditional = re.search(
    r"catch\s*\{\s*if\s*\(\s*!TryRestoreSectionViewBestEffort\(viewBeforeSection\)\s*\)\s*"
    r"_viewBeforeSection\s*=\s*viewBeforeSection\s*;\s*throw\s*;\s*\}",
    catch_tail,
    re.S,
)
if not conditional:
    raise SystemExit(
        "FAIL coordination section focus rollback ownership: failed native apply must retain the attempt-local snapshot only when rollback is unconfirmed, then rethrow the original failure"
    )

# The first persistent publication may be the failed-compensation transfer, but it must
# never occur before the initial native SetCurrentView attempt.
first_publish_at = body.find("_viewBeforeSection = viewBeforeSection;")
if first_publish_at < apply_at:
    raise SystemExit("FAIL coordination section focus rollback ownership: persistent snapshot ownership is published before native apply is attempted")

helper = re.search(
    r"private bool TryRestoreSectionViewBestEffort\(ViewSnapshot snapshot\)\s*\{(?P<body>.*?)\n\s*\}",
    text,
    re.S,
)
if not helper:
    raise SystemExit("FAIL coordination section focus rollback ownership: bool compensation helper not found")
helper_body = helper.group("body")
for token, label in [
    ("snapshot.Apply(view);", "prior snapshot application"),
    ("_document.Editor.SetCurrentView(view);", "native prior-view restoration"),
    ("return true;", "confirmed rollback result"),
    ("return false;", "unconfirmed rollback result"),
]:
    if token not in helper_body:
        raise SystemExit(f"FAIL coordination section focus rollback ownership: helper missing {label}: {token}")

restore = re.search(
    r"public void RestoreSectionView\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private bool TryRestoreSectionViewBestEffort",
    text,
    re.S,
)
if not restore:
    raise SystemExit("FAIL coordination section focus rollback ownership: RestoreSectionView method not found")
restore_body = restore.group("body")
restore_apply_at = restore_body.find("_document.Editor.SetCurrentView(view);")
restore_clear_at = restore_body.rfind("_viewBeforeSection = null;")
if restore_apply_at < 0 or restore_clear_at < restore_apply_at:
    raise SystemExit("FAIL coordination section focus rollback ownership: retry ownership must clear only after native prior-view restoration succeeds")

abandon = re.search(
    r"public void AbandonDestroyedDocumentState\(\)\s*\{(?P<body>.*?)\n\s*\}",
    text,
    re.S,
)
if not abandon or "_viewBeforeSection = null;" not in abandon.group("body"):
    raise SystemExit("FAIL coordination section focus rollback ownership: destroyed-document path must remain the explicit section-view ownership abandon boundary")

if "private void RestoreSectionViewBestEffort(ViewSnapshot snapshot)" in text:
    raise SystemExit("FAIL coordination section focus rollback ownership: fire-and-forget rollback helper cannot preserve failed compensation ownership")

print("PASS coordination review section focus failed-compensation retry ownership")
sys.exit(0)
