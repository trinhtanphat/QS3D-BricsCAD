#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "AtomicFileCommit.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.index("public static void TryDelete(string? path)")
end = text.index("private static void PublishMissingDestinationWithoutStaleBackup", start)
block = text[start:end]

if "RequireSafe(" not in block:
    fail("AtomicFileCommit.TryDelete must apply persistence path safety before cleanup deletion")

exists_pos = block.find("File.Exists(")
delete_pos = block.find("File.Delete(")
first_safe = block.find("RequireSafe(")
if exists_pos < 0 or delete_pos < 0:
    fail("AtomicFileCommit.TryDelete must retain bounded existence/delete cleanup semantics")
if not (first_safe < exists_pos < delete_pos):
    fail("cleanup path safety must be established before observing or deleting the cleanup member")

second_safe = block.find("RequireSafe(", first_safe + 1)
if second_safe < 0 or not (exists_pos < second_safe < delete_pos):
    fail("cleanup path safety must be revalidated immediately before destructive deletion")

# TryDelete is deliberately best-effort cleanup. Introducing RequireSafe must not
# turn malformed/unrepresentable cleanup paths into a new exception surface that
# can mask the primary persistence failure from a finally block.
for exception in (
    "ArgumentException",
    "NotSupportedException",
    "InvalidDataException",
    "IOException",
    "UnauthorizedAccessException",
):
    if f"catch ({exception})" not in block:
        fail(f"best-effort cleanup must refuse {exception} without masking the primary operation")

print("PASS: atomic temp cleanup refuses redirected/invalid paths and rechecks before deletion")
