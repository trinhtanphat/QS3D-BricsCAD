#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")


def method_body(signature: str, next_signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        raise SystemExit("missing method: " + signature)
    end = text.find(next_signature, start + len(signature))
    if end < 0:
        raise SystemExit("missing following method boundary: " + next_signature)
    return text[start:end]


body = method_body("public void RestoreSectionView()", "private bool TryRestoreSectionViewBestEffort")
for token in (
    "if (_viewBeforeSection == null) return;",
    "if (!IsOwnerNativeGenerationCurrent)",
    "AbandonStaleGenerationState();",
    "RequireOwnerGeneration(\"Section view restore\")",
    "var snapshot = _viewBeforeSection;",
    "snapshot.Apply(view);",
    "_document.Editor.SetCurrentView(view);",
    "_viewBeforeSection = null;",
):
    if token not in body:
        raise SystemExit("FAIL coordination section restore ownership: missing " + token)
apply_index = body.find("_document.Editor.SetCurrentView(view);")
clear_index = body.rfind("_viewBeforeSection = null;")
if apply_index < 0 or clear_index < apply_index:
    raise SystemExit("FAIL coordination section restore ownership: retry ownership released before native restore success")

helper = method_body("private bool TryRestoreSectionViewBestEffort(ViewSnapshot snapshot)", "private Extents3d ReadBounds")
for token in (
    "if (!IsOwnerNativeGenerationCurrent)",
    "AbandonStaleGenerationState();",
    "if (!IsOwnerGenerationActive) return false;",
    "snapshot.Apply(view);",
    "_document.Editor.SetCurrentView(view);",
    "return true;",
    "return false;",
):
    if token not in helper:
        raise SystemExit("FAIL coordination section restore ownership: rollback helper missing " + token)

if "public void ResetTransientStateBestEffort() => ResetTransientStateBestEffort(false);" not in text:
    raise SystemExit("FAIL coordination section restore ownership: public best-effort reset must delegate without dropping retry debt")
if "public Exception? TryResetTransientStateBestEffort() => ResetTransientStateBestEffort(false);" not in text:
    raise SystemExit("FAIL coordination section restore ownership: result-bearing reset entrypoint missing")

core = method_body(
    "private Exception? ResetTransientStateBestEffort(bool throwOnSectionRestoreFailure)",
    "public void AbandonDestroyedDocumentState()",
)
for token in (
    "if (!IsOwnerNativeGenerationCurrent)",
    "AbandonStaleGenerationState();",
    "Exception? cleanupFailure = null;",
    "try { ClearHighlight(); } catch (Exception ex) { cleanupFailure = ex; }",
    "try { RestoreIsolation(); } catch (Exception ex) { cleanupFailure = cleanupFailure ?? ex; }",
    "try { RestoreSectionView(); }",
    "cleanupFailure = cleanupFailure ?? ex;",
    "if (HasTransientState && cleanupFailure == null)",
    "if (throwOnSectionRestoreFailure && cleanupFailure != null)",
    "throw cleanupFailure;",
    "return cleanupFailure;",
):
    if token not in core:
        raise SystemExit("FAIL coordination section restore ownership: reset must retain/surface cleanup debt: " + token)
if "_viewBeforeSection = null" in core:
    raise SystemExit("FAIL coordination section restore ownership: reset core must not erase live section retry ownership")

dispose = method_body("public void Dispose()", "private sealed class ViewSnapshot")
for token in ("ResetTransientStateBestEffort(true);", "_disposed = true;"):
    if token not in dispose:
        raise SystemExit("FAIL coordination section restore ownership: Dispose missing retry-sensitive contract: " + token)
cleanup_index = dispose.find("ResetTransientStateBestEffort(true);")
disposed_index = dispose.find("_disposed = true;")
if cleanup_index < 0 or disposed_index < cleanup_index:
    raise SystemExit("FAIL coordination section restore ownership: session cannot publish disposed before cleanup succeeds")

print("PASS coordination review section restore ownership remains generation-bound and retry-safe until native success")
sys.exit(0)
