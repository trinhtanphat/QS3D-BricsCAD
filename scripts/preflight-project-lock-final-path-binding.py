#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SAFETY = ROOT / "src" / "QS3D.Core" / "Persistence" / "PersistencePathSafety.cs"
LOCK = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectFileLock.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


safety = SAFETY.read_text(encoding="utf-8")
lock = LOCK.read_text(encoding="utf-8")

required_tokens = (
    "GetFinalPathNameByHandleW",
    "FileFlagOpenReparsePoint",
    "RequireCanonicalFinalPath",
    "NormalizeFinalPath",
    "FileAttributes.ReparsePoint",
)
for token in required_tokens:
    if token not in safety:
        fail(f"PersistencePathSafety must retain {token} for canonical lock-path generation binding")

method_start = safety.index("public static void RequireExclusiveOpenStillBound")
method_end = safety.index("private static IOException CreateIdentityIOException", method_start)
method = safety[method_start:method_end]

held_final = method.find("RequireCanonicalFinalPath(canonical, openedStream.SafeFileHandle")
path_open = method.find("CreateFileW(")
path_reparse = method.find("FileAttributes.ReparsePoint", path_open)
path_final = method.find("RequireCanonicalFinalPath(canonical, pathHandle", path_open)
identity_compare = method.find("heldInformation.VolumeSerialNumber")
post_redirect = method.rfind("RequireNonRedirected(canonical, role)")

if min(held_final, path_open, path_reparse, path_final, identity_compare, post_redirect) < 0:
    fail("exact-generation validation must retain held/path final-name, reparse, redirect, and identity checks")
if not (held_final < path_open < path_reparse < path_final < post_redirect < identity_compare):
    fail("project-lock validation must bind canonical final paths and reparse state before accepting generation identity")

create_call = method[path_open:path_final]
if "NormalAttributes | FileFlagOpenReparsePoint" not in create_call:
    fail("pathname verification handle must open the final component without traversing a reparse point")

if 'PersistencePathSafety.RequireExclusiveOpenStillBound(stream, lockPath, "project-lock")' not in lock:
    fail("ProjectFileLock must retain exact held-generation binding before lock payload truncation")
bind_pos = lock.find('PersistencePathSafety.RequireExclusiveOpenStillBound(stream, lockPath, "project-lock")')
truncate_pos = lock.find("stream.SetLength(0)")
if bind_pos < 0 or truncate_pos < 0 or bind_pos > truncate_pos:
    fail("ProjectFileLock must validate the held canonical generation before truncating the lock payload")

print("PASS: project lock acquisition binds held and verification handles to the canonical non-redirected final path before accepting generation identity")
