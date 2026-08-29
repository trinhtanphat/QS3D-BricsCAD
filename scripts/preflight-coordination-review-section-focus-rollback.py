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
    raise SystemExit("FAIL coordination section focus rollback: ApplySectionFocus method not found")
body = match.group("body")

required = [
    "RequireTargets(ids);",
    "RestoreSectionView();",
    "var bounds = ReadBounds(ids);",
    "ViewSnapshot.Capture(view)",
    "_document.Editor.SetCurrentView(view);",
]
for token in required:
    if token not in body:
        raise SystemExit(f"FAIL coordination section focus rollback: missing established behavior: {token}")

if not re.search(r"var\s+viewBeforeSection\s*=\s*ViewSnapshot\.Capture\(view\)", body):
    raise SystemExit("FAIL coordination section focus rollback: prior view snapshot must remain attempt-local before native view apply succeeds")

if "try" not in body or "catch" not in body:
    raise SystemExit("FAIL coordination section focus rollback: native view apply lacks synchronous compensation boundary")

catch_at = body.find("catch", body.find("_document.Editor.SetCurrentView(view);"))
if catch_at < 0:
    raise SystemExit("FAIL coordination section focus rollback: rollback catch block missing")
catch_body = body[catch_at:]
if "TryRestoreSectionViewBestEffort(viewBeforeSection)" not in catch_body:
    raise SystemExit("FAIL coordination section focus rollback: failed view apply does not attempt restoration from the prior-view snapshot")
if not re.search(
    r"if\s*\(\s*!TryRestoreSectionViewBestEffort\(viewBeforeSection\)\s*\)\s*"
    r"_viewBeforeSection\s*=\s*viewBeforeSection\s*;",
    catch_body,
    re.S,
):
    raise SystemExit("FAIL coordination section focus rollback: unconfirmed rollback must transfer the prior snapshot into retry ownership")
if "throw;" not in catch_body:
    raise SystemExit("FAIL coordination section focus rollback: original view-apply failure must be rethrown after compensation")

apply_index = body.find("_document.Editor.SetCurrentView(view);")
publish_index = body.find("_viewBeforeSection = viewBeforeSection;")
if apply_index < 0 or publish_index < 0:
    raise SystemExit("FAIL coordination section focus rollback: section ownership publication is incomplete")
if publish_index < apply_index:
    raise SystemExit("FAIL coordination section focus rollback: persistent section ownership is published before native view apply is attempted")

if "_viewBeforeSection = ViewSnapshot.Capture(view);" in body:
    raise SystemExit("FAIL coordination section focus rollback: ApplySectionFocus still publishes snapshot before native apply")

helper = re.search(
    r"private bool TryRestoreSectionViewBestEffort\(ViewSnapshot snapshot\)\s*\{(?P<body>.*?)\n\s*\}",
    text,
    re.S,
)
if not helper:
    raise SystemExit("FAIL coordination section focus rollback: compensation helper must report whether the attempt-local snapshot was restored")
helper_body = helper.group("body")
for token in ["snapshot.Apply(view);", "_document.Editor.SetCurrentView(view);", "return true;", "return false;"]:
    if token not in helper_body:
        raise SystemExit(f"FAIL coordination section focus rollback: compensation helper missing {token}")

print("PASS coordination review section focus synchronous view-apply rollback atomicity and retry ownership")
sys.exit(0)
