from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/McpNativeCurrentDocumentSave.cs"
text = SOURCE.read_text(encoding="utf-8")

match = re.search(
    r"private void Complete\(object sender, CommandEventArgs e, string error, string state\)\s*\{(?P<body>.*?)\n\s*\}\n\n\s*private bool Matches",
    text,
    re.S,
)
if not match:
    raise SystemExit("FAIL: NativeSaveOperation.Complete body not found")
body = match.group("body")

required = [
    (r"Interlocked\.CompareExchange\(ref _terminalSet, 1, 0\)", "exactly-once terminal winner"),
    (r"TerminalError\s*=\s*error\s*;", "terminal result publication"),
    (r"finally\s*\{\s*Done\.Set\(\)\s*;\s*\}", "Done.Set terminal publication in finally"),
]
for pattern, label in required:
    if not re.search(pattern, body, re.S):
        raise SystemExit(f"FAIL: missing {label}")

if re.search(r"_audit\?\.Invoke\([^;]+\);\s*Done\.Set\(\)", body, re.S):
    raise SystemExit("FAIL: audit callback still directly precedes Done.Set and can suppress terminal publication")

# The event callback is a native host boundary. Diagnostics must be fail-soft so an
# optional audit sink cannot escape the callback or replace the QSAVE terminal result.
audit_guard = re.search(
    r"try\s*\{\s*_audit\?\.Invoke\(\"native QSAVE \" \+ state\);\s*\}\s*catch\s*\{\s*\}",
    body,
    re.S,
)
if not audit_guard:
    raise SystemExit("FAIL: native terminal audit callback is not fail-soft")

# Preserve #5610 ownership semantics: terminal processing still attempts handler
# detach before signalling, while final cleanup remains responsible for proving detach.
if "DetachInCadContext();" not in body:
    raise SystemExit("FAIL: terminal callback no longer attempts native handler detach")

print("PASS: native QSAVE terminal publication is audit-safe and completion signalling is finally-bound")
