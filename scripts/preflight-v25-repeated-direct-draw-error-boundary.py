#!/usr/bin/env python3
"""Guard repeated Direct Draw UI failure boundaries and partial-success semantics."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DirectDrawRepeatedCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []

for forbidden in (
    'Report(document, label + " lỗi: " + ex.Message);',
    'label + " dừng sau " + accepted + " segment đã commit: " + deferredSegmentError.Message',
):
    if forbidden in text:
        failures.append("Repeated Direct Draw still exposes internal error detail: " + forbidden)

for required in (
    'Report(document, label + ": không thể hoàn tất thao tác. Vui lòng thử lại.");',
    'label + " dừng sau " + accepted + " segment đã commit; các segment đã commit vẫn được giữ. Vui lòng thử segment tiếp theo bằng lệnh mới."',
):
    if required not in text:
        failures.append("Repeated Direct Draw is missing stable outcome-specific UI text: " + required)

# Keep the partial-success and rollback boundaries distinct. A segment error is deferred until after
# the accepted-set checkpoint/transition scope, while a command-level Undo registration failure still
# invokes the whole-command rollback path.
for required in (
    'termination = "SEGMENT_ERROR";',
    "if (accepted > 0)",
    "if (checkpointed == 0)",
    "UpdateExternalTransitionCheckpoint(",
    "throw RollbackWholeCommand(",
    "if (deferredSegmentError != null)",
    'if (ex is RepeatedWholeCommandRollbackException) throw;',
):
    if required not in text:
        failures.append("Repeated Direct Draw outcome/rollback invariant changed unexpectedly: " + required)

if failures:
    for failure in failures:
        print("ERROR: " + failure, file=sys.stderr)
    raise SystemExit(1)

print("V25 repeated Direct Draw UI outcome boundary preflight passed")
