#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")

restore_start = text.find("public void RestoreSectionView()")
restore_end = text.find("private bool TryRestoreSectionViewBestEffort", restore_start)
if restore_start < 0 or restore_end < 0:
    raise SystemExit("FAIL coordination section restore ownership: RestoreSectionView method not found")
body = text[restore_start:restore_end]

required = [
    "if (_viewBeforeSection == null) return;",
    "var snapshot = _viewBeforeSection;",
    "snapshot.Apply(view);",
    "_document.Editor.SetCurrentView(view);",
]
for token in required:
    if token not in body:
        raise SystemExit(f"FAIL coordination section restore ownership: missing established restore behavior: {token}")

apply_index = body.find("_document.Editor.SetCurrentView(view);")
clear_index = body.rfind("_viewBeforeSection = null;")
if clear_index < 0:
    raise SystemExit("FAIL coordination section restore ownership: successful restore must release persistent section ownership")
if clear_index < apply_index:
    raise SystemExit("FAIL coordination section restore ownership: section ownership is released before native view restore succeeds")

if not re.search(r"if\s*\(_destroyed\)\s*\{(?P<destroyed>.*?)\}", body, re.S):
    raise SystemExit("FAIL coordination section restore ownership: destroyed-document abandon path must be explicit")
destroyed = re.search(r"if\s*\(_destroyed\)\s*\{(?P<destroyed>.*?)\}", body, re.S).group("destroyed")
if "_viewBeforeSection = null;" not in destroyed or "return;" not in destroyed:
    raise SystemExit("FAIL coordination section restore ownership: destroyed document must abandon retained snapshot without native access")

helper_start = text.find("private bool TryRestoreSectionViewBestEffort(ViewSnapshot snapshot)")
helper_end = text.find("private Extents3d ReadBounds", helper_start)
if helper_start < 0 or helper_end < 0:
    raise SystemExit("FAIL coordination section restore ownership: result-bearing section rollback helper is missing")
helper_body = text[helper_start:helper_end]
for token in (
    "snapshot.Apply(view);",
    "_document.Editor.SetCurrentView(view);",
    "return true;",
    "return false;",
):
    if token not in helper_body:
        raise SystemExit(f"FAIL coordination section restore ownership: rollback helper missing {token}")

reset_public = re.search(
    r"public void ResetTransientStateBestEffort\(\)\s*\{(?P<body>.*?)\n\s*\}",
    text,
    re.S,
)
if not reset_public:
    raise SystemExit("FAIL coordination section restore ownership: public best-effort reset method not found")
if "_viewBeforeSection = null" in reset_public.group("body"):
    raise SystemExit("FAIL coordination section restore ownership: best-effort reset must not erase retry ownership after section restore failure")
if "ResetTransientStateBestEffort(false);" not in reset_public.group("body"):
    raise SystemExit("FAIL coordination section restore ownership: public reset must delegate to non-throwing section cleanup mode")

reset_core = re.search(
    r"private Exception\? ResetTransientStateBestEffort\(bool throwOnSectionRestoreFailure\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*public void AbandonDestroyedDocumentState",
    text,
    re.S,
)
if not reset_core:
    raise SystemExit("FAIL coordination section restore ownership: retry-aware result-bearing reset core overload is missing")
core_body = reset_core.group("body")
for token in (
    "Exception? cleanupFailure = null;",
    "try { ClearHighlight(); } catch (Exception ex) { cleanupFailure = ex; }",
    "try { RestoreIsolation(); } catch (Exception ex) { cleanupFailure = cleanupFailure ?? ex; }",
    "try { RestoreSectionView(); }",
    "cleanupFailure = cleanupFailure ?? ex;",
    "if (throwOnSectionRestoreFailure && cleanupFailure != null)",
    "throw cleanupFailure;",
    "return cleanupFailure;",
):
    if token not in core_body:
        raise SystemExit(f"FAIL coordination section restore ownership: retry-aware reset must retain and surface first cleanup failure while attempting all cleanup: {token}")
if "_viewBeforeSection = null" in core_body:
    raise SystemExit("FAIL coordination section restore ownership: reset core must never erase section retry ownership on live restore failure")

dispose = re.search(
    r"public void Dispose\(\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private sealed class ViewSnapshot",
    text,
    re.S,
)
if not dispose:
    raise SystemExit("FAIL coordination section restore ownership: session Dispose method not found")
dispose_body = dispose.group("body")
cleanup_index = dispose_body.find("ResetTransientStateBestEffort(true);")
disposed_index = dispose_body.find("_disposed = true;")
if cleanup_index < 0:
    raise SystemExit("FAIL coordination section restore ownership: Dispose must use throwing cleanup mode so controller can retry")
if disposed_index < 0 or disposed_index < cleanup_index:
    raise SystemExit("FAIL coordination section restore ownership: session cannot publish disposed state before retry-sensitive cleanup succeeds")

print("PASS coordination review section restore ownership remains retry-safe until native success")
sys.exit(0)
