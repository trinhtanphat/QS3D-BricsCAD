#!/usr/bin/env python3
from pathlib import Path
import re

SOURCE = Path("src/QS3D.BricsCAD.V25/UI/CoordinationManagerReviewUi.cs")
text = SOURCE.read_text(encoding="utf-8")

start = text.find("public void Isolate(IReadOnlyList<ObjectId> ids)")
end = text.find("public void RestoreIsolation()", start)
if start < 0 or end < 0:
    raise SystemExit("FAIL isolate rollback ownership: Isolate method not found")
body = text[start:end]

required = (
    'var modeBefore = Bricscad.ApplicationServices.Application.GetSystemVariable("OBJECTISOLATIONMODE");',
    'Application.SetSystemVariable("OBJECTISOLATIONMODE", 0);',
    'SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false);',
    "if (!TryRestoreImpliedSelectionBestEffort(impliedSelectionBefore) && !_generationAbandoned)",
    "_impliedSelectionBeforeIsolation = impliedSelectionBefore;",
    "if (!TryRestoreObjectIsolationModeBestEffort(modeBefore) && !_generationAbandoned)",
    "_objectIsolationModeBefore = modeBefore;",
    "throw;",
    "_isolationActive = true;",
)
for token in required:
    if token not in body:
        raise SystemExit("FAIL isolate rollback ownership: missing " + token)

catch_at = body.find("catch")
selection_compensate = body.find("TryRestoreImpliedSelectionBestEffort(impliedSelectionBefore)", catch_at)
selection_transfer = body.find("_impliedSelectionBeforeIsolation = impliedSelectionBefore;", selection_compensate)
mode_compensate = body.find("TryRestoreObjectIsolationModeBestEffort(modeBefore)", selection_transfer)
mode_transfer = body.find("_objectIsolationModeBefore = modeBefore;", mode_compensate)
throw_at = body.find("throw;", mode_transfer)
if not (0 <= catch_at < selection_compensate < selection_transfer < mode_compensate < mode_transfer < throw_at):
    raise SystemExit("FAIL isolate rollback ownership: failed compensation must retain PICKFIRST and mode debt before bare rethrow")

session_start = text.find("private sealed class TransientReviewSession : IDisposable")
session = text[session_start:]
if "public bool HasIsolation => _isolationActive || _objectIsolationModeBefore != null || _impliedSelectionBeforeIsolation != null;" not in session:
    raise SystemExit("FAIL isolate rollback ownership: all isolation cleanup debt must remain observable")

restore_start = text.find("public void RestoreIsolation()", session_start)
restore_end = text.find("public void ApplySectionFocus", restore_start)
restore = text[restore_start:restore_end]
for token in ("RestorePendingImpliedSelectionBestEffort();", "RestoreObjectIsolationModeBestEffort();"):
    if token not in restore:
        raise SystemExit("FAIL isolate rollback ownership: retained cleanup debt must be retryable: " + token)

for fence in (
    'RequireOwnerGeneration("Isolation / PICKFIRST capture")',
    'RequireOwnerGeneration("Isolation / mode capture")',
    'RequireOwnerGeneration("Isolation / command dispatch")',
    'RequireOwnerGeneration("Isolation / publication")',
):
    if fence not in body:
        raise SystemExit("FAIL isolate rollback ownership: missing native-generation fence: " + fence)

print("PASS coordination review failed-isolate rollback retains PICKFIRST/mode debt within exact native generation")
