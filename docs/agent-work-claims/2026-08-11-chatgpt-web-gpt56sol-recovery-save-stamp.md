# Agent work claim — recovery save stamp

Status: ACTIVE

Agent: ChatGPT Web / GPT-5.6 Sol
Date: 2026-08-11 (UTC+7)
Baseline main SHA observed before reservation: `4862b9d1bccef98d99858be1fb6ffb2cae71a302`

## Scope

Fix the CAD-independent persistence stamp contract so a project marked as recovered-from-backup is required to save once, but does not remain permanently pending after `ProjectPersistenceStamp.MarkSaved()` succeeds.

Expected implementation surfaces:

- `src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs`
- `tests/QS3D.Core.SmokeTests/ProjectPersistenceLifecycleSmoke.cs`
- this claim file for completion status

## Concrete defect

`RequiresSave()` treats metadata `QS3D.RecoveredFromBackup=true` as a mandatory save condition. `MarkSaved()` currently updates only `_savedChangeVersion`, leaving the recovery marker unchanged, so the same stamp can still report `RequiresSave(project) == true` immediately after the project has explicitly been marked saved. This violates the stamp's own lifecycle contract and can keep a recovered project perpetually pending.

## Exclusions

- No BricsCAD V25/native runtime, UI, updater, quantity/rule, installer, or release changes.
- No change to backup selection or file replacement logic.
- No GitHub Actions dispatch.

## Validation plan

- Extend the deterministic persistence lifecycle smoke to prove a recovered project is pending before `MarkSaved()` and clean afterward.
- Preserve ordinary semantic-change tracking and cross-project guard behavior.
- Re-fetch current `main` and both target files before every write; never force-push.
