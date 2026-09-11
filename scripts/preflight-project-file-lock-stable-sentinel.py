#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectFileLock.cs"
text = SOURCE.read_text(encoding="utf-8")

errors = []

if "File.Delete(_lockPath)" in text or "File.Delete(lockPath)" in text:
    errors.append("ProjectFileLock release must not unlink the stable .lock sentinel after releasing its handle")

if "_stream.Dispose();" not in text:
    errors.append("ProjectFileLock.Dispose must release the exclusive FileStream handle")

if "FileShare.None" not in text:
    errors.append("ProjectFileLock.Acquire must preserve exclusive handle ownership via FileShare.None")

if errors:
    for error in errors:
        print(f"ERROR: {error}", file=sys.stderr)
    raise SystemExit(1)

print("PASS: ProjectFileLock uses a stable sentinel and handle-based exclusive ownership")
