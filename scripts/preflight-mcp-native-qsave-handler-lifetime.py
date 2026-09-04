from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpNativeCurrentDocumentSave.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "private bool _commandEndedAttached;",
    "private bool _commandCancelledAttached;",
    "private bool _commandFailedAttached;",
    "_commandEndedAttached = true;",
    "_commandCancelledAttached = true;",
    "_commandFailedAttached = true;",
    "return !_commandEndedAttached && !_commandCancelledAttached && !_commandFailedAttached;",
]
for token in required:
    if token not in text:
        raise SystemExit(f"ERROR: native QSAVE handler lifetime guard missing token: {token}")

for forbidden in [
    "private bool _handlersAttached;",
    "_handlersAttached = false;",
    "try { document.CommandEnded -= OnCommandEnded; } catch { }",
    "try { document.CommandCancelled -= OnCommandCancelled; } catch { }",
    "try { document.CommandFailed -= OnCommandFailed; } catch { }",
]:
    if forbidden in text:
        raise SystemExit(f"ERROR: native QSAVE handler lifetime guard found obsolete fail-open topology: {forbidden}")

attach_start = text.index("private void AttachHandlers(Document document)")
detach_start = text.index("private bool DetachInCadContext()")
attach = text[attach_start:detach_start]
for add, mark in [
    ("document.CommandEnded += OnCommandEnded;", "_commandEndedAttached = true;"),
    ("document.CommandCancelled += OnCommandCancelled;", "_commandCancelledAttached = true;"),
    ("document.CommandFailed += OnCommandFailed;", "_commandFailedAttached = true;"),
]:
    if attach.index(add) > attach.index(mark):
        raise SystemExit(f"ERROR: ownership must publish only after successful subscription: {add}")

if "catch\n                {\n                    if (!DetachInCadContext())" not in attach:
    raise SystemExit("ERROR: partial subscription failure must rollback and fail closed when detach cannot be proven")

print("PASS: MCP native QSAVE handler lifetime is per-subscription, rollback-safe, and fail-closed")
