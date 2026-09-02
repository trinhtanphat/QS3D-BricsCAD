#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
LOCK = ROOT / "src/QS3D.Core/Persistence/ProjectFileLock.cs"
SAFETY = ROOT / "src/QS3D.Core/Persistence/PersistencePathSafety.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing project lock generation-binding file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


lock_source = read(LOCK)
safety_source = read(SAFETY)

# Regression-first contract: the pre-open pathname check is still useful for
# rejecting obvious redirects, but the post-open acceptance decision must be tied
# to the exact exclusive FileStream generation. Re-statting only the pathname
# after open can observe a replacement file and falsely bless a stale held lock.
if "RequireExclusiveOpenStillBound(" not in safety_source:
    errors.append("PersistencePathSafety must expose an exact-open generation binding check")
if "PersistencePathSafety.RequireExclusiveOpenStillBound(stream, lockPath, \"project-lock\")" not in lock_source:
    errors.append("ProjectFileLock must bind the accepted pathname to the exact opened exclusive stream")

open_index = lock_source.find("new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)")
bound_index = lock_source.find("PersistencePathSafety.RequireExclusiveOpenStillBound(stream, lockPath, \"project-lock\")")
truncate_index = lock_source.find("stream.SetLength(0)")
if open_index < 0:
    errors.append("ProjectFileLock exclusive open contract missing")
elif bound_index < 0 or bound_index < open_index:
    errors.append("ProjectFileLock generation binding must run after exclusive open")
elif truncate_index >= 0 and bound_index > truncate_index:
    errors.append("ProjectFileLock generation binding must run before truncating lock payload")

# A pathname-only post-open check is insufficient evidence and must not be used as
# the acceptance decision immediately after the stream is opened.
post_open = lock_source[open_index:] if open_index >= 0 else ""
first_truncate = post_open.find("stream.SetLength(0)")
if first_truncate >= 0:
    post_open = post_open[:first_truncate]
if "PersistencePathSafety.RequireNonRedirected(lockPath, \"project-lock\")" in post_open:
    errors.append("ProjectFileLock still relies on pathname-only validation after exclusive open")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: project lock acceptance is bound to the exact exclusive filesystem generation before mutation.")
