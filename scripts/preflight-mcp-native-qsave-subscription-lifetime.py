from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpNativeCurrentDocumentSave.cs"
text = SOURCE.read_text(encoding="utf-8")
failures = []

def require(condition: bool, message: str) -> None:
    if not condition:
        failures.append(message)

start = text.find("private void AttachHandlers(Document document)")
end = text.find("private bool DetachInCadContext()", start)
attach = text[start:end] if start >= 0 and end > start else ""
require(bool(attach), "could not isolate NativeSaveOperation.AttachHandlers")

specs = [
    ("_commandEndedAttached", "document.CommandEnded += OnCommandEnded"),
    ("_commandCancelledAttached", "document.CommandCancelled += OnCommandCancelled"),
    ("_commandFailedAttached", "document.CommandFailed += OnCommandFailed"),
]
for flag, add_token in specs:
    publish = attach.find(flag + " = true;")
    native_add = attach.find(add_token)
    require(publish >= 0 and native_add >= 0 and publish < native_add,
            flag + " must publish may-be-subscribed ownership before fallible native add")

for flag, remove_token in [
    ("_commandEndedAttached", "document.CommandEnded -= OnCommandEnded"),
    ("_commandCancelledAttached", "document.CommandCancelled -= OnCommandCancelled"),
    ("_commandFailedAttached", "document.CommandFailed -= OnCommandFailed"),
]:
    detach_start = text.find("private bool DetachInCadContext()")
    detach_end = text.find("private void OnCommandEnded", detach_start)
    detach = text[detach_start:detach_end]
    remove_pos = detach.find(remove_token)
    clear_pos = detach.find(flag + " = false;")
    require(remove_pos >= 0 and clear_pos > remove_pos,
            flag + " may clear only after matching native remove succeeds")

require("Do not retry automatically" in text,
        "native QSAVE uncertain-state errors must continue to prohibit blind retry")
if failures:
    for failure in failures:
        print("FAIL:", failure)
    sys.exit(1)
print("PASS: native QSAVE retains terminal-handler ownership across partial add/remove faults")
