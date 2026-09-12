from pathlib import Path
import re
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

# The publication exception remains primary (historical #5579 contract), but a
# failure restoring a staged canonical destination must never disappear.
if re.search(r"catch\s*\([^)]*\)\s*(?:when\s*\([^)]*\)\s*)?\{\s*\}", move_with_recovery, re.S):
    print(
        "ERROR: AtomicFileCommit.MoveWithRecovery still contains an empty catch "
        "that can discard critical rollback evidence."
    )
    sys.exit(1)

required_move_tokens = (
    "publicationFailure",
    "RecordRollbackFailure(publicationFailure",
    "Directory.Exists(destinationPath)",
    "!File.Exists(backupPath)",
    "RestorePreviousBackup(previousBackupSafety, backupPath, publicationFailure);",
)
missing = [token for token in required_move_tokens if token not in move_with_recovery]
if missing:
    print(
        "ERROR: AtomicFileCommit.MoveWithRecovery is missing rollback evidence/fence tokens: "
        + ", ".join(missing)
    )
    sys.exit(1)

if "throw publicationFailure;" in move_with_recovery:
    print("ERROR: AtomicFileCommit.MoveWithRecovery must not reset the original publication exception stack.")
    sys.exit(1)

publication_catch = re.search(
    r"catch\s*\(Exception\s+ex\)\s*\{\s*publicationFailure\s*=\s*ex;\s*throw;\s*\}",
    move_with_recovery,
    re.S,
)
if publication_catch is None:
    print("ERROR: AtomicFileCommit.MoveWithRecovery must rethrow the captured publication failure with bare throw;.")
    sys.exit(1)

recreate_start = text.find("private static void PublishMissingDestinationWithoutStaleBackup")
recreate_end = text.find("private static void MoveWithRecovery", recreate_start)
if recreate_start < 0 or recreate_end < 0:
    print("ERROR: AtomicFileCommit missing-destination recreation boundary not found.")
    sys.exit(1)
recreate = text[recreate_start:recreate_end]

# Missing-primary recreation stages an older backup too. If publication fails,
# failure to restore that staged backup is rollback evidence on the same primary
# exception and must not be silently lost in finally.
for token in (
    "Exception? publicationFailure",
    "publicationFailure = ex;",
    "RestorePreviousBackup(staleBackupSafety, backupPath, publicationFailure);",
):
    if token not in recreate:
        print(
            "ERROR: AtomicFileCommit missing-destination recreation can still lose "
            "previous-backup rollback evidence: " + token
        )
        sys.exit(1)

if "throw publicationFailure;" in recreate:
    print("ERROR: AtomicFileCommit missing-destination recreation must preserve the original failure stack with bare throw;.")
    sys.exit(1)

recreate_catch = re.search(
    r"catch\s*\(Exception\s+ex\)\s*\{\s*publicationFailure\s*=\s*ex;\s*throw;\s*\}",
    recreate,
    re.S,
)
if recreate_catch is None:
    print("ERROR: AtomicFileCommit missing-destination recreation must capture and bare-rethrow its publication failure.")
    sys.exit(1)

# Once a newly installed primary is rejected because a backup appeared, rollback
# ownership must transfer before attempting to delete that primary. Avoid parsing
# the nested try/catch with a non-greedy brace regex: that truncates at the inner
# catch and can report a false failure even when source ordering is correct.
rollback_branch = "if (File.Exists(backupPath) || Directory.Exists(backupPath))"
rollback_branch_start = recreate.find(rollback_branch)
if rollback_branch_start < 0:
    print("ERROR: AtomicFileCommit missing-destination rollback branch was not found.")
    sys.exit(1)
rollback_owner_transfer = recreate.find("installed = false;", rollback_branch_start)
rollback_delete = recreate.find("File.Delete(destinationPath);", rollback_branch_start)
rollback_inner_catch = recreate.find("catch (Exception ex) when", rollback_branch_start)
if (
    rollback_owner_transfer < 0
    or rollback_delete < 0
    or rollback_inner_catch < 0
    or not (rollback_owner_transfer < rollback_delete < rollback_inner_catch)
):
    print(
        "ERROR: AtomicFileCommit missing-destination rollback must mark the install uncommitted "
        "before deleting the rejected primary so delete failure cannot discard the staged old backup."
    )
    sys.exit(1)

restore_start = text.find("private static void RestorePreviousBackup", end)
restore_end = text.find("private static void Validate", restore_start)
if restore_start < 0 or restore_end < 0:
    print("ERROR: AtomicFileCommit.RestorePreviousBackup boundary not found.")
    sys.exit(1)
restore_previous_backup = text[restore_start:restore_end]
for token in (
    "Exception? publicationFailure",
    "catch (Exception ex)",
    "if (publicationFailure != null)",
    "RecordRollbackFailure(publicationFailure, ex);",
):
    if token not in restore_previous_backup:
        print(
            "ERROR: AtomicFileCommit.RestorePreviousBackup can still discard previous-backup "
            "recovery evidence while a publication failure is active: " + token
        )
        sys.exit(1)

if "private const string RollbackFailureDataKey = \"QS3D.AtomicFileCommit.RollbackFailure\";" not in text:
    print("ERROR: AtomicFileCommit is missing the stable rollback evidence Data key.")
    sys.exit(1)

helper_start = text.find("private static void RecordRollbackFailure")
if helper_start < 0:
    print("ERROR: AtomicFileCommit.RecordRollbackFailure helper not found.")
    sys.exit(1)
helper_end = text.find("private static", helper_start + len("private static void RecordRollbackFailure"))
helper = text[helper_start:] if helper_end < 0 else text[helper_start:helper_end]
for token in ("publicationFailure.Data", "rollbackFailure", "AggregateException"):
    if token not in helper:
        print("ERROR: AtomicFileCommit.RecordRollbackFailure does not preserve rollback evidence: " + token)
        sys.exit(1)

print("AtomicFileCommit rollback-failure preflight passed.")
