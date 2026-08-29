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
restore_pickfirst = "RestoreImpliedSelectionBestEffort(impliedSelectionBefore);"
restore_mode_call = "TryRestoreObjectIsolationModeBestEffort(modeBefore)"
for token in [restore_pickfirst, restore_mode_call, "throw;"]:
    if token not in catch_body:
        raise SystemExit(f"FAIL coordination isolate PICKFIRST rollback: catch missing {token}")

if catch_body.find(restore_pickfirst) > catch_body.find(restore_mode_call):
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: PICKFIRST compensation must run before mode compensation")
if catch_body.find(restore_mode_call) > catch_body.find("throw;"):
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: mode compensation must run before original failure rethrow")

if body.count(restore_pickfirst) != 1:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: PICKFIRST restore must occur only in the synchronous failure path")

helper = re.search(
    r"private void RestoreImpliedSelectionBestEffort\(ObjectId\[\] impliedSelectionBefore\)\s*\{(?P<body>.*?)\n\s*\}",
    text,
    re.S,
)
if not helper:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: compensation helper missing")
helper_body = helper.group("body")
if "_destroyed" not in helper_body:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: destroyed-document compensation must fail closed")
if "_document.Editor.SetImpliedSelection(impliedSelectionBefore);" not in helper_body:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: helper does not restore exact prior implied selection")
if "try" not in helper_body or "catch" not in helper_body:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: compensation must be best-effort and preserve the original launch failure")

send_index = body.find(send)
mode_publish = body.find("_objectIsolationModeBefore = modeBefore;")
active_publish = body.find("_isolationActive = true;")
if send_index < 0 or mode_publish < send_index or active_publish < send_index:
    raise SystemExit("FAIL coordination isolate PICKFIRST rollback: persistent isolation ownership must publish only after native queue acceptance")

print("PASS coordination review isolate PICKFIRST synchronous rollback atomicity")
sys.exit(0)
