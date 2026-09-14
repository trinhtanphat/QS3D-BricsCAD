#!/usr/bin/env python3
from pathlib import Path
import re
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

body = method_body("public void Isolate(IReadOnlyList<ObjectId> ids)", "public void RestoreIsolation()")

capture = "var impliedSelectionBefore = CadSelectionGuard.ReadImpliedSelection(_document);"
set_mode = 'Application.SetSystemVariable("OBJECTISOLATIONMODE", 0);'
set_pickfirst = "_document.Editor.SetImpliedSelection(ids.ToArray());"
send = '_document.SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false);'
for token in [capture, set_mode, set_pickfirst, send]:
    if token not in body:
        raise SystemExit(f"FAIL coordination isolate PICKFIRST rollback: missing required behavior: {token}")

capture_index = body.find(capture)
if capture_index > body.find(set_mode) or capture_index > body.find(set_pickfirst):
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: prior PICKFIRST must be captured before launch mutation")

catch_index = body.find("catch")
if catch_index < 0:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: synchronous launch catch missing")
catch_body = body[catch_index:]
restore_pickfirst = "TryRestoreImpliedSelectionBestEffort(impliedSelectionBefore)"
restore_mode_call = "TryRestoreObjectIsolationModeBestEffort(modeBefore)"
for token in [restore_pickfirst, "_impliedSelectionBeforeIsolation = impliedSelectionBefore;", restore_mode_call, "throw;"]:
    if token not in catch_body:
        raise SystemExit(f"FAIL coordination isolate PICKFIRST rollback: catch missing {token}")
if catch_body.find(restore_pickfirst) > catch_body.find(restore_mode_call):
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: PICKFIRST compensation must run before mode compensation")
if catch_body.find(restore_mode_call) > catch_body.find("throw;"):
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: mode compensation must run before original failure rethrow")

helper_body = method_body(
    "private bool TryRestoreImpliedSelectionBestEffort(ObjectId[] impliedSelectionBefore)",
    "private void RestorePendingImpliedSelectionBestEffort()",
)
for token in ("_destroyed", "IsOwnerNativeGenerationCurrent", "IsOwnerGenerationActive", "_document.Editor.SetImpliedSelection(impliedSelectionBefore);", "return true;", "return false;"):
    if token not in helper_body:
        raise SystemExit("FAIL coordination isolate PICKFIRST rollback: compensation helper missing " + token)

pending = method_body(
    "private void RestorePendingImpliedSelectionBestEffort()",
    "private void RestoreObjectIsolationModeBestEffort()",
)
for token in ("_impliedSelectionBeforeIsolation", "TryRestoreImpliedSelectionBestEffort", "_impliedSelectionBeforeIsolation = null;"):
    if token not in pending:
        raise SystemExit("FAIL coordination isolate PICKFIRST rollback: pending compensation ownership missing " + token)

send_index = body.find(send)
mode_publish = body.rfind("_objectIsolationModeBefore = modeBefore;")
active_publish = body.find("_isolationActive = true;", send_index)
if send_index < 0 or mode_publish < send_index or active_publish < send_index:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: persistent isolation ownership must publish only after native queue acceptance")

print("PASS coordination review isolate PICKFIRST synchronous rollback retains retry debt within exact generation")
sys.exit(0)
