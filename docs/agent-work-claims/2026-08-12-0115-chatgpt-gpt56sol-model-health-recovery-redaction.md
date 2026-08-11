# Work claim — Model Health recovery metadata redaction

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-model-health-recovery-redaction`
- Registered: `2026-08-12T01:15:00+07:00`
- Last Updated: `2026-08-12T01:15:00+07:00`
- Baseline main SHA: `c406188c5aeefea6e3612defee6c649f22590ca9`
- Priority: P1 — read-only health diagnostics must not reflect persisted/imported recovery detail verbatim.
- Task Key: `CORE-MODEL-HEALTH-RECOVERY-REDACTION`

## Confirmed defect

`ModelHealthService.Inspect(project)` currently reads `QS3D.LoadWarning` from `ProjectState.Metadata` when `QS3D.ReadOnlyRecoveryRequired=true` and appends that value verbatim to the `PROJECT_LOAD_FAILED` issue message. Recovery metadata can contain exception detail or local filesystem paths, so a persisted/imported value can be reflected directly through a user-facing diagnostic surface.

The health contract only needs to tell the user that the project is protected because the primary `.qsdb` failed to load and that they should inspect/recover before saving. It does not require raw recovery detail to be echoed.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthRecoveryRedactionSmoke.cs`
- this claim file

## Intended contract

- Preserve issue code `PROJECT_LOAD_FAILED` and `HealthSeverity.Error`.
- Replace verbatim `QS3D.LoadWarning` reflection with a stable generic/actionable message.
- Do not mutate or delete recovery metadata; inspection remains read-only.
- Preserve `PROJECT_RECOVERED_BACKUP` behavior.
- Add focused auto-registered Core smoke coverage proving a sentinel/path stored in `QS3D.LoadWarning` is absent from emitted health text and the protection diagnostic remains visible.
- Do not touch BricsCAD adapters/runtime, ProjectSession recovery mechanics, backup selection, or unrelated health services.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Coordination

Recent runtime-health lanes for semantic tags, Grid annotations, Curtain Panels, dependency relations, generated solids, and safe generated ownership are completed or reserve different files. The completed safe-ownership claim explicitly reserves `SafeGeneratedHandleOwnershipHealthService.cs`, not `ModelHealthService.cs`. No recent claim/commit search surfaced a recovery-redaction lane or `PROJECT_LOAD_FAILED` ownership.

## Validation plan

- Build a valid minimal `ProjectState` with `QS3D.ReadOnlyRecoveryRequired=true` and a `QS3D.LoadWarning` sentinel containing a path-like value.
- `Inspect()` yields exactly the expected project-load Error diagnostic.
- The issue message does not contain the sentinel, path fragment, or raw warning.
- `ProjectState.ChangeVersion` and metadata remain unchanged across inspection.
- Backup-recovered warning behavior remains unchanged.
- Re-fetch current source after this claim reaches `main`, patch from a fresh post-claim baseline, inspect exact PR changed-file set, and read back merged `main` source.

## Completion condition

Raw recovery warning detail is no longer exposed by `ModelHealthService`, focused deterministic regression source is on `main`, exact merge evidence is recorded, and this claim is closed/released.