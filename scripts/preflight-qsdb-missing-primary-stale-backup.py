#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Persistence/AtomicFileCommit.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/QsdbSaveAtomicitySmoke.cs").read_text(encoding="utf-8")

required_source = [
    "PublishMissingDestinationWithoutStaleBackup(temp, destination, backup)",
    "private static void PublishMissingDestinationWithoutStaleBackup",
    "File.Move(backupPath, staleBackupSafety)",
    "File.Move(tempPath, destinationPath)",
    "RestorePreviousBackup(staleBackupSafety, backupPath)",
    "A normal replacement backup represents the immediately previous",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"FAIL: missing stale-backup generation guard token: {token}")

missing_branch = source.split("if (!File.Exists(destination))", 1)[1].split("try", 1)[0]
if "PublishMissingDestinationWithoutStaleBackup" not in missing_branch:
    raise SystemExit("FAIL: missing-primary replacement bypasses stale-backup retirement helper")
if "File.Move(temp, destination);" in missing_branch:
    raise SystemExit("FAIL: missing-primary replacement regressed to direct publish before backup-generation admission")

required_smoke = [
    "MissingPrimaryReplacementRetiresStaleBackup();",
    "private static void MissingPrimaryReplacementRetiresStaleBackup()",
    "Require(!File.Exists(path + \".bak\")",
    "Corrupt recreated primary resurrected a stale backup generation through fallback.",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"FAIL: missing deterministic stale-backup regression token: {token}")

print("PASS qsdb missing-primary stale-backup generation guard")
