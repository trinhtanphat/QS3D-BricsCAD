# Work claim — Backup fallback public failure-message redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-backup-failure-message-redaction-20260812-1421`
- Registered: `2026-08-12T14:21:00+07:00`
- Baseline main SHA: `9f8398883e0408dc6f1c6a6500c5a94eb80f624f`
- Priority: P1 persistence privacy / recovery contract
- Task Key: `CORE-QSDB-BACKUP-FAILURE-MESSAGE-REDACTION`

## Confirmed defect

`QsdbProjectStore.LoadWithBackupFallback()` returns the raw primary exception message through public `ProjectLoadResult.PrimaryFailureMessage` when backup recovery succeeds. `FileNotFoundException` is intentionally classified as a recoverable primary failure, and its message can contain the absolute project path. This leaks filesystem identity through a public recovery result even though other recovery/health/file-lock surfaces have explicit path-redaction contracts.

Recent commit/claim checks found no active lane owning `ProjectLoadResult.PrimaryFailureMessage` or backup-fallback public-message redaction. Existing backup-fallback work covers recovery preservation/healing, not this public result field.

## Reserved scope

- `src/QS3D.Core/Persistence/QsdbProjectStore.cs`
- focused Core smoke for backup fallback public-message redaction
- this claim file

## Intended contract

- preserve successful primary-load behavior;
- preserve fallback to a valid `.bak` for recoverable primary failures;
- preserve `RecoveredFromBackup`, `SourcePath`, and failure diagnostics without exposing absolute filesystem paths through `PrimaryFailureMessage`;
- keep original exceptions available only inside thrown failure chains when both primary and backup fail;
- do not change backup preservation/healing or save semantics.

## Validation boundary

Focused auto-registered Core smoke + exact source/diff/readback + ancestry only. No GitHub Actions, full executable smoke, local .NET build, or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.