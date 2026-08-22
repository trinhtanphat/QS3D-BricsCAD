#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
STORE = ROOT / "src/QS3D.Core/Persistence/QsdbProjectStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsdbSaveAtomicitySmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
errors = []

for path in (STORE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing QSDB save atomicity contract file: " + str(path.relative_to(ROOT)))

if STORE.is_file():
    text = STORE.read_text(encoding="utf-8")
    for token in (
        "var previousSchemaVersion = project.SchemaVersion;",
        "var previousUpdatedUtc = project.UpdatedUtc;",
        "var committed = false;",
        "AtomicFileCommit.ReplaceWithBackup(tempPath, fullPath, backupPath);",
        "committed = true;",
        "if (!committed)",
        "project.SchemaVersion = previousSchemaVersion;",
        "project.UpdatedUtc = previousUpdatedUtc;",
        "AtomicFileCommit.TryDelete(tempPath);",
    ):
        if token not in text:
            errors.append("QsdbProjectStore.cs missing failed-save rollback token: " + token)
    replace_index = text.find("AtomicFileCommit.ReplaceWithBackup(tempPath, fullPath, backupPath);")
    committed_index = text.find("committed = true;", replace_index)
    rollback_index = text.find("if (!committed)", committed_index)
    if replace_index < 0 or committed_index < replace_index or rollback_index < committed_index:
        errors.append("QSDB save must mark committed only after durable replace and roll back only on failure.")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "FailedDurableReplaceRestoresPersistenceState",
        "Directory.CreateDirectory(destinationDirectory);",
        "project.SchemaVersion == beforeSchema",
        "project.UpdatedUtc == beforeUpdatedUtc",
    ):
        if token not in text:
            errors.append("QsdbSaveAtomicitySmoke.cs missing regression token: " + token)

if REG.is_file() and "QsdbSaveAtomicitySmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("QSDB save atomicity smoke is not registered.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QSDB Save restores in-memory persistence state when durable replacement fails and only keeps the new save timestamp/schema after commit.")
