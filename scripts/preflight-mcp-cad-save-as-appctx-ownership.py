#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadDirectModelRuntime.cs"
text = SOURCE.read_text(encoding="utf-8")


def fail(message: str) -> None:
    print("FAIL: " + message, file=sys.stderr)
    raise SystemExit(1)

save_start = text.find("private static string SaveAs(string body)")
save_end = text.find("private static bool TryParseDirectLayoutCommand", save_start)
if save_start < 0 or save_end < 0:
    fail("cad_save_as boundary not found")
save = text[save_start:save_end]

if "McpDiagnosticHub.InvokeInCadContext" in save:
    fail("cad_save_as still uses response-bounded diagnostic CAD-context dispatch for Database.SaveAs")
if "InvokeSaveAsMutationInCadContext" not in save:
    fail("cad_save_as is not owned by a mutation-aware CAD-context dispatcher")

for token in (
    "SaveAsCadContextWorkItem",
    "SaveAsCadContextQueued",
    "SaveAsCadContextRunning",
    "SaveAsCadContextCancelledBeforeStart",
    "SaveAsCadContextTerminal",
    "ExecuteInApplicationContext(ExecuteSaveAsMutationCadContext",
    "Interlocked.CompareExchange(ref item.State, SaveAsCadContextCancelledBeforeStart, SaveAsCadContextQueued)",
    "Interlocked.CompareExchange(ref item.State, SaveAsCadContextRunning, SaveAsCadContextQueued)",
    "Interlocked.Exchange(ref item.State, SaveAsCadContextTerminal)",
    "item.Done.Wait()",
):
    if token not in text:
        fail("cad_save_as mutation ownership contract missing: " + token)

print("PASS cad_save_as application-context mutation ownership is fail-closed")
