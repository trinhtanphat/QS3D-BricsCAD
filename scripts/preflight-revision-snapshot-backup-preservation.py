#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Revisions" / "RevisionSnapshotStore.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RevisionSnapshotBackupPreservationSmoke.cs"
REGISTRATION = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RevisionSnapshotBackupPreservationSmokeRegistration.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
registration = REGISTRATION.read_text(encoding="utf-8")

required_source = [
    "if (ShouldPreserveValidatedBackup(full, backup))",
    "AtomicFileCommit.ReplaceWithoutBackup(temp, full);",
    "Load(full);",
    "Load(backup);",
    "AtomicFileCommit.ReplaceWithBackup(temp, full, backup);",
    "private bool ShouldPreserveValidatedBackup(string primaryPath, string backupPath)",
    "if (!File.Exists(backupPath)) return false;",
    "Load(primaryPath);",
    "catch (Exception primaryError) when (IsRecoverableDataFailure(primaryError))",
    "Load(backupPath);",
    "catch (Exception backupError) when (IsRecoverableDataFailure(backupError))",
]
for marker in required_source:
    if marker not in source:
        raise SystemExit(f"missing revision backup preservation source contract: {marker}")

preserve = source.index("if (ShouldPreserveValidatedBackup(full, backup))")
replace_without = source.index("AtomicFileCommit.ReplaceWithoutBackup(temp, full);", preserve)
validate_primary = source.index("Load(full);", replace_without)
validate_backup = source.index("Load(backup);", validate_primary)
normal_replace = source.index("AtomicFileCommit.ReplaceWithBackup(temp, full, backup);", preserve)
if not preserve < replace_without < validate_primary < validate_backup < normal_replace:
    raise SystemExit("revision backup publication/validation order is not fail-closed")

helper = source.index("private bool ShouldPreserveValidatedBackup")
primary_load = source.index("Load(primaryPath);", helper)
primary_recovery = source.index("catch (Exception primaryError) when (IsRecoverableDataFailure(primaryError))", primary_load)
backup_load = source.index("Load(backupPath);", primary_recovery)
backup_recovery = source.index("catch (Exception backupError) when (IsRecoverableDataFailure(backupError))", backup_load)
if not helper < primary_load < primary_recovery < backup_load < backup_recovery:
    raise SystemExit("revision backup validation probe order is invalid")

required_smoke = [
    "CorruptPrimarySavePreservesValidatedBackup();",
    "ValidPrimarySaveStillRotatesBackup();",
    'File.WriteAllText(path, "<corrupt");',
    'Equal("A", store.LoadWithBackupFallback(path).Id);',
    'store.Save(Snapshot("C", 3), path);',
    'Equal("C", store.Load(path).Id);',
    'Equal("A", store.Load(backup).Id);',
    'File.WriteAllText(path, "<corrupt-again");',
    'Equal("B", store.Load(backup).Id);',
]
for marker in required_smoke:
    if marker not in smoke:
        raise SystemExit(f"missing revision backup preservation smoke contract: {marker}")

if "RevisionSnapshotBackupPreservationSmoke.Run();" not in registration:
    raise SystemExit("revision backup preservation smoke is not registered")

print("revision snapshot backup preservation preflight: PASS")
