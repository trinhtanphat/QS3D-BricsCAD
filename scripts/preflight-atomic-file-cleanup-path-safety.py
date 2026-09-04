#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "AtomicFileCommit.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


def method_block(text: str, signature: str, next_signature: str) -> str:
    start = text.index(signature)
    end = text.index(next_signature, start)
    return text[start:end]


def require_safe_before_delete(block: str, delete_expression: str, role: str) -> None:
    delete_pos = block.find(delete_expression)
    if delete_pos < 0:
        fail(f"{role} must retain its rollback delete")
    safe_pos = block.rfind("RequireSafe(", 0, delete_pos)
    if safe_pos < 0:
        fail(f"{role} must revalidate path safety before rollback deletion")
    between = block[safe_pos:delete_pos]
    for observation in ("File.Exists(", "Directory.Exists(", "File.Move(", "File.Replace("):
        if observation in between:
            fail(f"{role} must not perform another filesystem observation/mutation between its final safety fence and rollback delete")


def require_safe_pair_after_observation(
    block: str,
    mutation_expression: str,
    first_safe: str,
    second_safe: str,
    role: str,
) -> None:
    mutation_pos = block.find(mutation_expression)
    if mutation_pos < 0:
        fail(f"{role} must retain its recovery mutation")
    last_exists = block.rfind("File.Exists(", 0, mutation_pos)
    if last_exists < 0:
        fail(f"{role} must retain its recovery existence observation")
    fenced_tail = block[last_exists:mutation_pos]
    if first_safe not in fenced_tail or second_safe not in fenced_tail:
        fail(f"{role} must revalidate both source and destination after the final existence observation")


text = SOURCE.read_text(encoding="utf-8")
try_delete = method_block(
    text,
    "public static void TryDelete(string? path)",
    "private static void PublishMissingDestinationWithoutStaleBackup",
)

if "RequireSafe(" not in try_delete:
    fail("AtomicFileCommit.TryDelete must apply persistence path safety before cleanup deletion")

exists_pos = try_delete.find("File.Exists(")
delete_pos = try_delete.find("File.Delete(")
first_safe = try_delete.find("RequireSafe(")
if exists_pos < 0 or delete_pos < 0:
    fail("AtomicFileCommit.TryDelete must retain bounded existence/delete cleanup semantics")
if not (first_safe < exists_pos < delete_pos):
    fail("cleanup path safety must be established before observing or deleting the cleanup member")

second_safe = try_delete.find("RequireSafe(", first_safe + 1)
if second_safe < 0 or not (exists_pos < second_safe < delete_pos):
    fail("cleanup path safety must be revalidated immediately before destructive deletion")

for exception in (
    "ArgumentException",
    "NotSupportedException",
    "InvalidDataException",
    "IOException",
    "UnauthorizedAccessException",
):
    if f"catch ({exception})" not in try_delete:
        fail(f"best-effort cleanup must refuse {exception} without masking the primary operation")

publish_new = method_block(
    text,
    "public static void PublishNew(string tempPath, string destinationPath, string backupPath)",
    "public static void TryDelete(string? path)",
)
require_safe_before_delete(publish_new, "File.Delete(destination)", "PublishNew backup-race rollback")

recreate = method_block(
    text,
    "private static void PublishMissingDestinationWithoutStaleBackup",
    "private static void MoveWithRecovery",
)
require_safe_before_delete(recreate, "File.Delete(destinationPath)", "missing-primary backup-race rollback")

move_recovery = method_block(
    text,
    "private static void MoveWithRecovery",
    "private static void RestorePreviousBackup",
)
require_safe_pair_after_observation(
    move_recovery,
    "File.Move(backupPath, destinationPath)",
    'RequireSafe(backupPath, "backup")',
    'RequireSafe(destinationPath, "destination")',
    "fallback destination restore",
)

restore_backup = method_block(
    text,
    "private static void RestorePreviousBackup",
    "private static void Validate",
)
require_safe_pair_after_observation(
    restore_backup,
    "File.Move(previousBackupPath, backupPath)",
    'RequireSafe(previousBackupPath, "previous-backup safety")',
    'RequireSafe(backupPath, "backup")',
    "previous-backup restore",
)

print("PASS: atomic cleanup, rollback delete, and recovery moves revalidate non-redirected safety")
