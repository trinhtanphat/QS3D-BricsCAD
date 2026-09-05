#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs").read_text(encoding="utf-8")
smoke = (ROOT / "tests/QS3D.Core.SmokeTests/QsdbSaveAtomicitySmoke.cs").read_text(encoding="utf-8")

method = source.split("public void SavePreservingValidatedBackup", 1)[1].split("private void SaveCore", 1)[0]
required_source = [
    "var validatedBackup = Load(backupPath);",
    "validatedBackup.ProjectId",
    "project.ProjectId",
    "StringComparison.Ordinal",
    "Validated QSDB backup project identity does not match",
]
for token in required_source:
    if token not in method:
        raise SystemExit(f"FAIL: recovery-safe publication lacks project-identity admission token: {token}")

identity_check = method.find("validatedBackup.ProjectId")
publish = method.find("SaveCore(project, fullPath, SaveMode.ReplacePrimaryOnly")
if identity_check < 0 or publish < 0 or identity_check > publish:
    raise SystemExit("FAIL: foreign-backup identity admission must happen before recovery-safe primary publication")
if "Load(backupPath);\n            SaveCore(project" in method:
    raise SystemExit("FAIL: recovery-safe publication regressed to parse-only backup validation")

required_smoke = [
    "RecoverySaveRejectsForeignProjectBackup();",
    "private static void RecoverySaveRejectsForeignProjectBackup()",
    "foreign-backup",
    "candidate persistence state changed",
    "foreign validated backup was accepted",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"FAIL: missing deterministic foreign-backup regression token: {token}")

print("PASS qsdb recovery backup project identity guard")
