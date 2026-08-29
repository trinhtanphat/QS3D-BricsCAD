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
    raise SystemExit("FAIL coordination isolate launch rollback: Isolate method not found")
body = match.group("body")

required = [
    "RequireTargets(ids);",
    "if (HasIsolation)",
    "RestoreIsolation();",
    "Application.GetSystemVariable(\"OBJECTISOLATIONMODE\")",
    "Application.SetSystemVariable(\"OBJECTISOLATIONMODE\", 0);",
    "_document.Editor.SetImpliedSelection(ids.ToArray());",
    "_document.SendStringToExecute(\"_.ISOLATEOBJECTS \", true, false, false);",
]
for token in required:
    if token not in body:
        raise SystemExit(f"FAIL coordination isolate launch rollback: missing established behavior: {token}")

# Isolation cleanup ownership includes both command ownership and a pending
# OBJECTISOLATIONMODE compensation. Drain it before capturing attempt-local
# launch state so stale restore ownership cannot be overwritten by a new launch.
ownership_gate = body.find("if (HasIsolation)")
restore = body.find("RestoreIsolation();", ownership_gate)
mode_capture = body.find('Application.GetSystemVariable("OBJECTISOLATIONMODE")')
if not (0 <= ownership_gate < restore < mode_capture):
    raise SystemExit("FAIL coordination isolate launch rollback: prior isolation ownership must be drained before launch state capture")

if "try" not in body or "catch" not in body:
    raise SystemExit("FAIL coordination isolate launch rollback: launch mutation is not protected by a synchronous rollback boundary")

if not re.search(r"var\s+modeBefore\s*=\s*Bricscad\.ApplicationServices\.Application\.GetSystemVariable\(\"OBJECTISOLATIONMODE\"\)", body):
    raise SystemExit("FAIL coordination isolate launch rollback: prior isolation mode must remain attempt-local before launch succeeds")

catch = re.search(r"catch\s*\{(?P<catch>.*?)\n\s*\}", body, re.S)
if not catch:
    raise SystemExit("FAIL coordination isolate launch rollback: rollback catch block missing")
catch_body = catch.group("catch")
if "TryRestoreObjectIsolationModeBestEffort(modeBefore)" not in catch_body:
    raise SystemExit("FAIL coordination isolate launch rollback: synchronous failure does not restore attempt-local OBJECTISOLATIONMODE")
if "throw;" not in catch_body:
    raise SystemExit("FAIL coordination isolate launch rollback: original launch failure must be rethrown after compensation")

send_index = body.find('_document.SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false);')
mode_publish = body.find("_objectIsolationModeBefore = modeBefore;")
active_publish = body.find("_isolationActive = true;")
if send_index < 0 or mode_publish < 0 or active_publish < 0:
    raise SystemExit("FAIL coordination isolate launch rollback: successful launch ownership publication is incomplete")
if mode_publish < send_index or active_publish < send_index:
    raise SystemExit("FAIL coordination isolate launch rollback: persistent isolation ownership is published before native command queueing succeeds")

if not re.search(r"private bool TryRestoreObjectIsolationModeBestEffort\(object\? modeBefore\)", text):
    raise SystemExit("FAIL coordination isolate launch rollback: compensation helper must accept attempt-local mode without publishing session ownership")

print("PASS coordination review isolate synchronous launch rollback atomicity")
sys.exit(0)
