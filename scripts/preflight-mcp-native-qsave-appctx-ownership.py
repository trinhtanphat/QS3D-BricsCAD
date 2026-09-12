#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpNativeCurrentDocumentSave.cs"
text = SOURCE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print("FAIL: " + message, file=sys.stderr)
    raise SystemExit(1)


for token in (
    "NativeCadContextWorkItem",
    "CadContextQueued",
    "CadContextRunning",
    "CadContextCancelledBeforeStart",
    "CadContextTerminal",
    "InvokeMutationInCadContext",
    "ExecuteInApplicationContext(ExecuteMutationCadContext",
    "Interlocked.CompareExchange(ref item.State, CadContextCancelledBeforeStart, CadContextQueued)",
    "Interlocked.Exchange(ref item.State, CadContextTerminal)",
    "item.Done.Wait()",
):
    if token not in text:
        fail("native QSAVE mutation-aware CAD-context ownership contract missing: " + token)

save_start = text.find("internal static SaveResult SaveCurrentDocument(Document?")
save_end = text.find("private static void EnsureRetainedCleanupResolved", save_start)
if save_start < 0 or save_end < 0:
    fail("SaveCurrentDocument boundary not found")
save = text[save_start:save_end]
if "McpDiagnosticHub.InvokeInCadContext" in save:
    fail("side-effecting QSAVE setup still uses response-bounded diagnostic CAD-context dispatch")
if "InvokeMutationInCadContext(operation.QueueInCadContext)" not in save:
    fail("QSAVE setup is not owned by the mutation-aware CAD-context dispatcher")

detach_start = text.find("internal bool DetachBestEffort()")
detach_end = text.find("private void AttachHandlers", detach_start)
if detach_start < 0 or detach_end < 0:
    fail("DetachBestEffort boundary not found")
detach = text[detach_start:detach_end]
if "McpDiagnosticHub.InvokeInCadContext" in detach:
    fail("native handler detach still uses diagnostic CAD-context dispatch")
if "InvokeMutationInCadContext" not in detach:
    fail("native handler detach is not owned by the mutation-aware CAD-context dispatcher")

running_wait = text.find("item.Done.Wait()")
cancel = text.find("CadContextCancelledBeforeStart")
if cancel < 0 or running_wait < 0 or cancel > running_wait:
    fail("started mutation work must retain caller ownership until callback reaches terminal completion")

print("PASS native QSAVE application-context timeout ownership is fail-closed for side effects")
