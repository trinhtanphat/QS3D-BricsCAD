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
    "NativeDatabaseIdentity",
    "Database.UnmanagedObject",
    "RequireSameNativeDatabaseGeneration",
):
    if token not in text:
        fail("native QSAVE database-generation affinity contract missing: " + token)

queue_start = text.find("internal void QueueInCadContext()")
queue_end = text.find("internal bool DetachBestEffort()", queue_start)
if queue_start < 0 or queue_end < 0:
    fail("QueueInCadContext boundary not found")
queue = text[queue_start:queue_end]
if "NativeDatabaseIdentity" not in queue or "Database.UnmanagedObject" not in queue:
    fail("native database identity is not captured before QSAVE is queued")

verify_start = text.find("private static int WaitForCleanDbmod")
verify_end = text.find("private static void EnsureCommandContextAutomationNotStopped", verify_start)
if verify_start < 0 or verify_end < 0:
    fail("post-QSAVE DBMOD verification boundary not found")
verify = text[verify_start:verify_end]
if "RequireSameNativeDatabaseGeneration" not in verify:
    fail("post-QSAVE success verification does not fence the exact native database generation")

complete_start = text.find("private void Complete(object sender")
complete_end = text.find("private bool Matches", complete_start)
if complete_start < 0 or complete_end < 0:
    fail("terminal event completion boundary not found")
complete = text[complete_start:complete_end]
if "RequireSameNativeDatabaseGeneration" not in complete:
    fail("terminal QSAVE event is accepted without exact native database generation revalidation")

print("PASS native QSAVE completion is fenced to the exact native database generation")
