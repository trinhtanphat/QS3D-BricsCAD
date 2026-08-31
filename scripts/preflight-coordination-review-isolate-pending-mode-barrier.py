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
    raise SystemExit("FAIL coordination isolate pending mode barrier: Isolate method not found")
body = match.group("body")

session_start = text.find("private sealed class TransientReviewSession : IDisposable")
if session_start < 0:
    raise SystemExit("FAIL coordination isolate pending mode barrier: session not found")
session = text[session_start:]
if "public bool HasIsolation => _isolationActive || _objectIsolationModeBefore != null;" not in session:
    raise SystemExit("FAIL coordination isolate pending mode barrier: HasIsolation must include pending mode restore ownership")

if "if (_isolationActive) RestoreIsolation();" in body:
    raise SystemExit("FAIL coordination isolate pending mode barrier: new isolate may not gate cleanup on command ownership alone")

first_gate = body.find("if (HasIsolation)")
restore = body.find("RestoreIsolation();", first_gate)
second_gate = body.find("if (HasIsolation)", first_gate + len("if (HasIsolation)"))
reject = body.find("throw new InvalidOperationException(", second_gate)

mutation_tokens = [
    "CadSelectionGuard.ReadImpliedSelection(_document)",
    'GetSystemVariable("OBJECTISOLATIONMODE")',
    'SetSystemVariable("OBJECTISOLATIONMODE", 0)',
    "SetImpliedSelection(ids.ToArray())",
    'SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false)',
]
mutation_indices = []
for token in mutation_tokens:
    index = body.find(token)
    if index < 0:
        raise SystemExit(f"FAIL coordination isolate pending mode barrier: established isolate behavior missing: {token}")
    mutation_indices.append(index)
first_mutation = min(mutation_indices)

if not (0 <= first_gate < restore < second_gate < reject < first_mutation):
    raise SystemExit("FAIL coordination isolate pending mode barrier: prior isolation ownership must be drained and rechecked before any host-state observation/mutation")

queue = body.find('_document.SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false);')
publish_mode = body.find("_objectIsolationModeBefore = modeBefore;")
publish_active = body.find("_isolationActive = true;")
if queue < 0 or publish_mode < queue or publish_active < queue:
    raise SystemExit("FAIL coordination isolate pending mode barrier: new isolation ownership must publish only after queue acceptance")

print("PASS coordination review isolate pending mode restore barrier")
sys.exit(0)
