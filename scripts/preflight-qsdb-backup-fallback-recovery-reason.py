#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "QsdbProjectStore.cs"
text = SOURCE.read_text(encoding="utf-8")

method_start = text.index("public ProjectLoadResult LoadWithBackupFallback")
method_end = text.index("private static XDocument Serialize", method_start)
method = text[method_start:method_end]

required = [
    'private const string BackupRecoveryReason = "Primary QSDB was invalid; loaded validated backup.";',
    "new ProjectLoadResult(project, backupPath, true, BackupRecoveryReason)",
    'throw new InvalidDataException("Both the QSDB project and its backup are invalid.", new AggregateException(primary, backup));',
]
for snippet in required:
    if snippet not in text:
        raise SystemExit(f"missing QSDB backup-fallback recovery contract: {snippet}")

for forbidden in ("primary.Message", "backup.Message"):
    if forbidden in method:
        raise SystemExit(f"raw recoverable exception detail crosses ProjectLoadResult boundary: {forbidden}")

print("PASS QSDB backup-fallback recovery reason is stable and exception-redacted")
