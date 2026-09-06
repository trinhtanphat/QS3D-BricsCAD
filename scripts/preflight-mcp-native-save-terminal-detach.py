#!/usr/bin/env python3
"""Fail closed unless native QSAVE success owns one truthful terminal lifecycle."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpNativeCurrentDocumentSave.cs"
text = SOURCE.read_text(encoding="utf-8")
failures = []

synchronous = (
    "Application.DocumentManager.ExecuteInCommandContextAsync(" in text
    and 'document.Editor.Command("_.QSAVE");' in text
)

if synchronous:
    wait = text.find("Task.WaitAny(")
    observe = text.find("completion.GetAwaiter().GetResult();", wait)
    dbmod = text.find("WaitForCleanDbmod(operation, ensureRunning)", observe)
    if wait < 0:
        failures.append("synchronous native QSAVE must await command-context completion")
    if observe < 0:
        failures.append("synchronous native QSAVE must observe command-context completion exceptions")
    if dbmod < 0 or not (0 <= wait < observe < dbmod):
        failures.append("synchronous QSAVE must wait -> observe completion -> verify DBMOD before success")
    if text.count('document.Editor.Command("_.QSAVE");') != 1:
        failures.append("synchronous native QSAVE must execute exactly one native command attempt")
    for forbidden in (
        "DetachInCadContext",
        "DetachBestEffort",
        "CommandEnded +=",
        "CommandCancelled +=",
        "CommandFailed +=",
        "ManualResetEventSlim",
        "document.SendStringToExecute(",
    ):
        if forbidden in text:
            failures.append("synchronous native QSAVE must not retain event-handler detach ownership: " + forbidden)
else:
    wait = text.find("operation.Done.Wait(CommandCompletionTimeoutMilliseconds)")
    cleanup = text.find("if (!operation.DetachBestEffort())", wait)
    terminal_error = text.find("if (!string.IsNullOrEmpty(operation.TerminalError))", wait)
    dbmod = text.find("WaitForCleanDbmod(operation, ensureRunning)", wait)

    if wait < 0:
        failures.append("native QSAVE worker must await the terminal completion event")
    if cleanup < 0:
        failures.append("native QSAVE worker must fail closed when post-terminal handler detach cannot be proven")
    elif not (wait < cleanup < terminal_error < dbmod):
        failures.append("post-terminal detach proof must occur after terminal wait and before terminal/result DBMOD success checks")

    complete_match = re.search(
        r"private void Complete\(object sender, CommandEventArgs e, string error, string state\)(.*?)\n\s*private bool Matches",
        text,
        re.S,
    )
    if not complete_match:
        failures.append("could not locate NativeSaveOperation.Complete")
    else:
        complete = complete_match.group(1)
        if "DetachInCadContext();" not in complete or "Done.Set();" not in complete:
            failures.append("terminal callback must attempt in-context detach and publish Done exactly through its terminal path")
        if complete.find("DetachInCadContext();") > complete.find("Done.Set();"):
            failures.append("terminal callback must attempt detach before publishing Done")

    if text.count('SendStringToExecute("_.QSAVE\\n"') != 1:
        failures.append("native QSAVE must have exactly one dispatch site; cleanup failure must not replay the command")

if "Do not retry automatically" not in text:
    failures.append("uncertain native-save outcomes must retain explicit no-auto-retry guidance")
if "Database.Save();" in text or "Database.SaveAs(" in text:
    failures.append("native current-document QSAVE helper must not write the active path through Database.Save/SaveAs")

if failures:
    for failure in failures:
        print(f"ERROR: {failure}", file=sys.stderr)
    raise SystemExit(1)

print("PASS native QSAVE terminal lifecycle truthfulness")