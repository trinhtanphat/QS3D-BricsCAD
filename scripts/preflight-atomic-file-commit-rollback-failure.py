from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "AtomicFileCommit.cs"

text = SOURCE.read_text(encoding="utf-8")
start = text.find("private static void MoveWithRecovery")
end = text.find("private static void RestorePreviousBackup", start)
if start < 0 or end < 0:
    print("ERROR: AtomicFileCommit.MoveWithRecovery boundary not found.")
    sys.exit(1)

move_with_recovery = text[start:end]

# Once the canonical destination has been staged aside, a failure restoring it is
# a stronger persistence-safety failure than the original publication failure.
# Never regress to an empty filtered catch that discards that evidence.
empty_rollback_catch = (
    "catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException) { }"
)
if empty_rollback_catch in move_with_recovery:
    print(
        "ERROR: AtomicFileCommit.MoveWithRecovery still suppresses a critical "
        "destination rollback failure after publication fails."
    )
    sys.exit(1)

required_evidence = (
    "rollback",
    "IOException",
)
missing = [token for token in required_evidence if token not in move_with_recovery]
if missing:
    print(
        "ERROR: AtomicFileCommit.MoveWithRecovery is missing explicit rollback "
        "failure evidence: " + ", ".join(missing)
    )
    sys.exit(1)

print("AtomicFileCommit rollback-failure preflight passed.")
