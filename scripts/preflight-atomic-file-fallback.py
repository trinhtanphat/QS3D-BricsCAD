#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Persistence/AtomicFileCommit.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/AtomicFileCommitFallbackSmoke.cs"
errors = []

for path in (SOURCE, SMOKE):
    if not path.is_file():
        errors.append("missing atomic fallback contract file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "MoveWithRecovery(tempPath, destination, backup, keepBackup: true);",
        "MoveWithRecovery(tempPath, destination, safetyBackup, keepBackup: false);",
        "File.Move(destinationPath, backupPath);",
        "File.Move(tempPath, destinationPath);",
        "if (!File.Exists(destinationPath) && File.Exists(backupPath))",
        "File.Move(backupPath, destinationPath);",
    ):
        if token not in text:
            errors.append("AtomicFileCommit.cs missing recovery token: " + token)
    if "File.Copy(tempPath, destinationPath, true)" in text:
        errors.append("Atomic fallback must not overwrite-copy temp into the live destination.")
    if "finally\n            {\n                TryDelete(safetyBackup);" in text:
        errors.append("ReplaceWithoutBackup must preserve the safety backup when commit/recovery fails.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "CommitsAndKeepsBackup(method);",
        "CommitsAndRemovesSafetyBackup(method);",
        "RestoresDestinationWhenInstallFails(method);",
        'GetMethod("MoveWithRecovery"',
    ):
        if token not in text:
            errors.append("AtomicFileCommitFallbackSmoke.cs missing regression token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: atomic file fallback uses move-based replacement, restores the previous destination on install failure, and does not delete recovery state on failed commit.")
