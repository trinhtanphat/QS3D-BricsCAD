#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CoordinationManagerReviewUi.cs"
text = SOURCE.read_text(encoding="utf-8")

match = re.search(
    r"public void Isolate\(IReadOnlyList<ObjectId> ids\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*public void RestoreIsolation",
    text,
    re.S,
)
if not match:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: Isolate method not found")
body = match.group("body")

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

catch = re.search(r"catch\s*\{(?P<catch>.*?)\n\s*\}", body, re.S)
if not catch:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: synchronous launch catch missing")
catch_body = catch.group("catch")
restore_pickfirst = "TryRestoreImpliedSelectionBestEffort(impliedSelectionBefore)"
restore_mode_call = "TryRestoreObjectIsolationModeBestEffort(modeBefore)"
for token in [restore_pickfirst, "_impliedSelectionBeforeIsolation = impliedSelectionBefore;", restore_mode_call, "throw;"]:
    if token not in catch_body:
        raise SystemExit(f"FAIL coordination isolate PICKFIRST rollback: catch missing {token}")
if catch_body.find(restore_pickfirst) > catch_body.find(restore_mode_call):
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: PICKFIRST compensation must run before mode compensation")
if catch_body.find(restore_mode_call) > catch_body.find("throw;"):
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: mode compensation must run before original failure rethrow")

helper = re.search(
    r"private bool TryRestoreImpliedSelectionBestEffort\(ObjectId\[\] impliedSelectionBefore\)\s*\{(?P<body>.*?)\n\s*\}",
    text,
    re.S,
)
if not helper:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: result-bearing compensation helper missing")
helper_body = helper.group("body")
for token in ("_destroyed", "IsOwnerNativeGenerationCurrent", "IsOwnerGenerationActive", "_document.Editor.SetImpliedSelection(impliedSelectionBefore);", "return true;", "return false;"):
    if token not in helper_body:
        raise SystemExit("FAIL coordination isolate PICKFIRST rollback: compensation helper missing " + token)

pending_start = text.find("private void RestorePendingImpliedSelectionBestEffort()")
pending_end = text.find("private void RestoreObjectIsolationModeBestEffort()", pending_start)
pending = text[pending_start:pending_end]
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
