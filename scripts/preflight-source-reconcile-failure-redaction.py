#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = (
    'catch (Exception)\n            {\n                ReportOperationFailure(document, "QS3DSYNCSOURCE lỗi: không thể reconcile source CAD đã chọn.");',
    'var uiSyncFailed = false;',
    'try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }',
    'try { document.Editor.Regen(); } catch { uiSyncFailed = true; }',
    'try { PaletteCoordinator.SetStatus(status); } catch { uiSyncFailed = true; }',
    'try { document.Editor.WriteMessage("\\nQS3D " + status); } catch { uiSyncFailed = true; }',
    'Sync Source UI sync warning: reconcile đã hoàn tất; một phần UI không thể đồng bộ.',
    'private static void ReportOperationFailure',
    'try { PaletteCoordinator.SetStatus(message); } catch { }',
    'private static void TryWriteMessage',
    'try { document.Editor.WriteMessage(message); } catch { }',
)
for token in required:
    if token not in text:
        errors.append("missing stable/fail-isolated contract: " + token)

for forbidden in ("ex.Message", "exception.Message", "Exception.Message"):
    if forbidden in text:
        errors.append("raw exception detail remains in Source Reconcile user-visible surface: " + forbidden)

sync = text.find("public void SyncSource()")
reconcile = text.find("result = SourceReconcileService.ReconcileSelection(document);", sync)
catch = text.find("catch (Exception)", reconcile)
finalize_call = text.find("FinalizeUi(document, result);", catch)
finalize = text.find("private static void FinalizeUi", finalize_call)
report = text.find("private static void ReportOperationFailure", finalize)
if min(sync, reconcile, catch, finalize_call, finalize, report) < 0 or not sync < reconcile < catch < finalize_call < finalize < report:
    errors.append("reconcile must complete/return before best-effort post-reconcile UI synchronization")
else:
    body = text[finalize:report]
    if "SourceReconcileService.ReconcileSelection" in body:
        errors.append("FinalizeUi must remain UI-only after reconcile completion")
    ordered = [
        body.find("PaletteCoordinator.RefreshProject()"),
        body.find("document.Editor.Regen()"),
        body.find("PaletteCoordinator.SetStatus(status)"),
        body.find('document.Editor.WriteMessage("\\nQS3D " + status)'),
        body.find("if (uiSyncFailed)"),
    ]
    if min(ordered) < 0 or ordered != sorted(ordered):
        errors.append("post-reconcile UI sync must preserve refresh -> regen -> status -> editor -> stable warning order")
    if body.count("catch") < 4:
        errors.append("Palette refresh, Regen, status and Editor write must fail independently")

if errors:
    print("Source Reconcile failure redaction guard FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("Source Reconcile failure redaction guard PASS")
