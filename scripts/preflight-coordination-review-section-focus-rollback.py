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

for token in [
    "RequireTargets(ids);",
    "RestoreSectionView();",
    "var bounds = ReadBounds(ids);",
    "ViewSnapshot.Capture(view)",
    "_document.Editor.SetCurrentView(view);",
]:
    if token not in body:
        raise SystemExit("FAIL coordination section focus rollback: missing established behavior: " + token)

if not re.search(r"var\s+viewBeforeSection\s*=\s*ViewSnapshot\.Capture\(view\)", body):
    raise SystemExit("FAIL coordination section focus rollback: prior view snapshot must remain attempt-local before native view apply")

apply_index = body.find("_document.Editor.SetCurrentView(view);")
catch_index = body.find("catch", apply_index)
publish_index = body.find("_viewBeforeSection = viewBeforeSection;")
if apply_index < 0 or catch_index < 0 or publish_index < apply_index:
    raise SystemExit("FAIL coordination section focus rollback: native apply/rollback ownership ordering is invalid")

catch_body = body[catch_index:]
if not re.search(
    r"if\s*\(\s*!TryRestoreSectionViewBestEffort\(viewBeforeSection\)\s*\)\s*"
    r"_viewBeforeSection\s*=\s*viewBeforeSection\s*;",
    catch_body,
    re.S,
):
    raise SystemExit("FAIL coordination section focus rollback: unconfirmed rollback must transfer prior snapshot into retry ownership")
if "throw;" not in catch_body:
    raise SystemExit("FAIL coordination section focus rollback: original native apply failure must remain primary")

if "_viewBeforeSection = ViewSnapshot.Capture(view);" in body:
    raise SystemExit("FAIL coordination section focus rollback: snapshot cannot be published before native apply")

helper_start = text.find("private bool TryRestoreSectionViewBestEffort(ViewSnapshot snapshot)")
helper_end = text.find("private Extents3d ReadBounds", helper_start)
if helper_start < 0 or helper_end < 0:
    raise SystemExit("FAIL coordination section focus rollback: result-bearing compensation helper not found")
helper_body = text[helper_start:helper_end]
for token in ["snapshot.Apply(view);", "_document.Editor.SetCurrentView(view);", "return true;", "return false;"]:
    if token not in helper_body:
        raise SystemExit("FAIL coordination section focus rollback: compensation helper missing " + token)

print("PASS coordination review section focus synchronous view-apply rollback atomicity and retry ownership")
sys.exit(0)
