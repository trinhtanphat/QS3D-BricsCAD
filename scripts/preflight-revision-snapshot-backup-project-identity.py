from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/RevisionSnapshotStore.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RevisionSnapshotBackupProjectIdentitySmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/revision-snapshot-backup-project-identity.md"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8") if RUNBOOK.exists() else ""

required_source = [
    "ValidateReplacementProjectIdentity(snapshot, full, backup)",
    "string.Equals(existing.ProjectId, candidate.ProjectId, StringComparison.Ordinal)",
    "project identity does not match",
    "ShouldPreserveValidatedBackup(full, backup)",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"revision snapshot backup identity guard missing source token: {token}")

for token in [
    "ForeignValidPrimaryCannotBecomeBackup",
    "ForeignValidatedBackupCannotBePreserved",
    "SameProjectValidatedBackupRemainsUsable",
    "ModuleInitializer",
]:
    if token not in smoke:
        raise SystemExit(f"revision snapshot backup identity smoke missing token: {token}")

for token in ["foreign primary", "foreign validated backup", "same-project", "NOT_APPLICABLE"]:
    if token not in runbook:
        raise SystemExit(f"revision snapshot backup identity runbook missing token: {token}")

print("PASS revision snapshot backup project identity")
