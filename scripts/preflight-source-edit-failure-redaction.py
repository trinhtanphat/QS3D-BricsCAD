#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SourceEditCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = (
    'catch (Exception)\n            {\n                ReportFailure(document, "QS3DEDITSOURCE lỗi: không thể hoàn tất edit/reconcile source CAD đã chọn.");',
    'ApplyTransform(document, selection, transform.Value.Forward);',
    'SourceReconcileService.ReconcileSelection(document);',
    'ApplyTransform(document, selection, transform.Value.Inverse);',
    'reconcile failed; the authoritative CAD transform was reversed',
    'var uiSyncFailed = false;',
    'try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }',
    'try { document.Editor.Regen(); } catch { uiSyncFailed = true; }',
    'try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }',
    'try { document.Editor.WriteMessage("\\nQS3D " + status); } catch { uiSyncFailed = true; }',
    'Edit Source UI sync warning: edit + reconcile đã hoàn tất; một phần UI không thể đồng bộ.',
    'private static void TryWriteMessage',
)
for token in required:
    if token not in text:
        errors.append("missing stable/fail-isolated Source Edit contract: " + token)

for forbidden in ("ex.Message", "uiError.Message", "exception.Message", "Exception.Message"):
    if forbidden in text:
        errors.append("raw exception detail remains in Source Edit user-visible surface: " + forbidden)

edit = text.find("public void EditSource()")
fresh = text.find("RequireFreshSelection(document, selection);", edit)
forward = text.find("ApplyTransform(document, selection, transform.Value.Forward);", fresh)
reconcile = text.find("SourceReconcileService.ReconcileSelection(document);", forward)
reverse = text.find("ApplyTransform(document, selection, transform.Value.Inverse);", reconcile)
finalize_call = text.find("FinalizeSuccess(document, operation, reconcile);", reconcile)
finalize = text.find("private static void FinalizeSuccess", finalize_call)
report = text.find("private static void ReportFailure", finalize)
if min(edit, fresh, forward, reconcile, reverse, finalize_call, finalize, report) < 0:
    errors.append("Source Edit lifecycle boundaries are incomplete")
else:
    if not edit < fresh < forward < reconcile < reverse < finalize_call < finalize < report:
        errors.append("Source Edit must preserve freshness -> forward transform -> reconcile -> rollback-on-failure -> post-success UI ordering")
    body = text[finalize:report]
    ordered = [
        body.find("PaletteCoordinator.RefreshProject()"),
        body.find("document.Editor.Regen()"),
        body.find("PaletteCoordinator.SetStatus(status)"),
        body.find('document.Editor.WriteMessage("\\nQS3D " + status)'),
        body.find("if (uiSyncFailed)"),
    ]
    if min(ordered) < 0 or ordered != sorted(ordered):
        errors.append("post-edit UI sync must preserve refresh -> regen -> status -> editor -> stable warning order")
    if body.count("catch") < 4:
        errors.append("post-edit Palette refresh, Regen, status and Editor output must fail independently")
    if "SourceReconcileService.ReconcileSelection" in body or "ApplyTransform(" in body:
        errors.append("FinalizeSuccess must remain UI-only after edit/reconcile completion")

if errors:
    print("Source Edit failure redaction guard FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Source Edit failure redaction guard PASS")
