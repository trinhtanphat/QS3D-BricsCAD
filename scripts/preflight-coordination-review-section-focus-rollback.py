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

catch = re.search(r"catch\s*\{(?P<catch>.*?)\n\s*\}", body, re.S)
if not catch:
    raise SystemExit("FAIL coordination section focus rollback: rollback catch block missing")
catch_body = catch.group("catch")
if "RestoreSectionViewBestEffort(viewBeforeSection)" not in catch_body:
    raise SystemExit("FAIL coordination section focus rollback: failed view apply does not restore attempt-local prior view")
if "throw;" not in catch_body:
    raise SystemExit("FAIL coordination section focus rollback: original view-apply failure must be rethrown after compensation")

apply_index = body.find("_document.Editor.SetCurrentView(view);")
publish_index = body.find("_viewBeforeSection = viewBeforeSection;")
if apply_index < 0 or publish_index < 0:
    raise SystemExit("FAIL coordination section focus rollback: successful section ownership publication is incomplete")
if publish_index < apply_index:
    raise SystemExit("FAIL coordination section focus rollback: persistent section ownership is published before native view apply succeeds")

if "_viewBeforeSection = ViewSnapshot.Capture(view);" in body:
    raise SystemExit("FAIL coordination section focus rollback: ApplySectionFocus still publishes snapshot before native apply")

if not re.search(r"private void RestoreSectionViewBestEffort\(ViewSnapshot snapshot\)", text):
    raise SystemExit("FAIL coordination section focus rollback: compensation helper must restore an attempt-local snapshot")

print("PASS coordination review section focus synchronous view-apply rollback atomicity")
sys.exit(0)
