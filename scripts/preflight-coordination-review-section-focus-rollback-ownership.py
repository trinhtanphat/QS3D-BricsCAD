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
    raise SystemExit("FAIL section focus ownership: ApplySectionFocus method not found")
body = match.group("body")

for token in [
    "var viewBeforeSection = ViewSnapshot.Capture(view);",
    "_document.Editor.SetCurrentView(view);",
    "TryRestoreSectionViewBestEffort(viewBeforeSection)",
    "_viewBeforeSection = viewBeforeSection;",
    "throw;",
]:
    if token not in body:
        raise SystemExit("FAIL section focus ownership: missing " + token)

apply_at = body.find("_document.Editor.SetCurrentView(view);")
first_publish_at = body.find("_viewBeforeSection = viewBeforeSection;")
if apply_at < 0 or first_publish_at < apply_at:
    raise SystemExit("FAIL section focus ownership: ownership published before native apply attempt")

if not re.search(
    r"catch\s*\{\s*if\s*\(\s*!TryRestoreSectionViewBestEffort\(viewBeforeSection\)\s*\)\s*"
    r"_viewBeforeSection\s*=\s*viewBeforeSection\s*;\s*throw\s*;\s*\}",
    body,
    re.S,
):
    raise SystemExit("FAIL section focus ownership: failed compensation must retain snapshot then rethrow")

helper_start = text.find("private bool TryRestoreSectionViewBestEffort(ViewSnapshot snapshot)")
helper_end = text.find("private Extents3d ReadBounds", helper_start)
if helper_start < 0 or helper_end < 0:
    raise SystemExit("FAIL section focus ownership: bool compensation helper not found")
helper_body = text[helper_start:helper_end]
for token in ["snapshot.Apply(view);", "_document.Editor.SetCurrentView(view);", "return true;", "return false;"]:
    if token not in helper_body:
        raise SystemExit("FAIL section focus ownership: compensation helper missing " + token)

restore_start = text.find("public void RestoreSectionView()")
restore_end = text.find("private bool TryRestoreSectionViewBestEffort", restore_start)
if restore_start < 0 or restore_end < 0:
    raise SystemExit("FAIL section focus ownership: RestoreSectionView method not found")
restore_body = text[restore_start:restore_end]
restore_apply_at = restore_body.find("_document.Editor.SetCurrentView(view);")
restore_clear_at = restore_body.rfind("_viewBeforeSection = null;")
if restore_apply_at < 0 or restore_clear_at < restore_apply_at:
    raise SystemExit("FAIL section focus ownership: ownership must clear after successful native restore")

abandon_start = text.find("public void AbandonDestroyedDocumentState()")
abandon_end = text.find("private void RestoreImpliedSelectionBestEffort", abandon_start)
if abandon_start < 0 or abandon_end < 0 or "_viewBeforeSection = null;" not in text[abandon_start:abandon_end]:
    raise SystemExit("FAIL section focus ownership: destroyed-document abandon boundary missing")

if "private void RestoreSectionViewBestEffort(ViewSnapshot snapshot)" in text:
    raise SystemExit("FAIL section focus ownership: fire-and-forget rollback helper remains")

print("PASS coordination review section focus failed-compensation retry ownership")
sys.exit(0)
