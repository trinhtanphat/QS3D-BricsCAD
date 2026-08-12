# Work claim — Model Health recovery metadata redaction

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-model-health-recovery-redaction`
- Registered: `2026-08-12T01:15:00+07:00`
- Completed: `2026-08-12T01:26:00+07:00`
- Last Updated: `2026-08-12T01:26:00+07:00`
- Baseline main SHA: `c406188c5aeefea6e3612defee6c649f22590ca9`
- Priority: P1 — read-only health diagnostics must not reflect persisted/imported recovery detail verbatim.
- Task Key: `CORE-MODEL-HEALTH-RECOVERY-REDACTION`

## Confirmed defect

`ModelHealthService.Inspect(project)` read `QS3D.LoadWarning` from `ProjectState.Metadata` when `QS3D.ReadOnlyRecoveryRequired=true` and appended that value verbatim to the `PROJECT_LOAD_FAILED` issue message. Recovery metadata can contain exception detail or local filesystem paths, so a persisted/imported value could be reflected directly through a user-facing diagnostic surface.

The health contract only needs to tell the user that the project is protected because the primary `.qsdb` failed to load and that they should inspect/recover before saving. It does not require raw recovery detail to be echoed.

## Reserved scope

- `src/QS3D.Core/Diagnostics/ModelHealthService.cs`
- `tests/QS3D.Core.SmokeTests/ModelHealthRecoveryRedactionSmoke.cs`
- this claim file

## Implemented contract

- Preserved issue code `PROJECT_LOAD_FAILED` and `HealthSeverity.Error`.
- Removed verbatim `QS3D.LoadWarning` reflection from Model Health and replaced it with a stable generic/actionable protection message.
- Recovery metadata is neither deleted nor mutated; inspection remains read-only.
- Preserved `PROJECT_RECOVERED_BACKUP` behavior and Warning severity.
- Added auto-registered `ModelHealthRecoveryRedactionSmoke` proving a sentinel/path stored in `QS3D.LoadWarning` is absent from emitted health text, raw metadata is unchanged, `ProjectState.ChangeVersion` is unchanged, and backup-recovered warning severity remains Warning.
- BricsCAD adapters/runtime, ProjectSession recovery mechanics, backup selection, and unrelated health services were not changed.

## Coordination

Recent runtime-health lanes for semantic tags, Grid annotations, Curtain Panels, dependency relations, generated solids, and safe generated ownership were completed or reserved different files. The completed safe-ownership claim reserved `SafeGeneratedHandleOwnershipHealthService.cs`, not `ModelHealthService.cs`. Rechecks before implementation and before PR creation did not surface a concurrent recovery-redaction or `PROJECT_LOAD_FAILED` lane, and current `main` still had the original `ModelHealthService.cs` blob immediately before the implementation PR.

## Validation / evidence

- Registration PR #601 was squash-merged to `main` as `139bfdec84ff34ab16470d57ab3a0b3d10b4f682` before implementation.
- Post-registration `docs/AGENT-WORK-REGISTRATION.md` was re-read from `main` and recent concurrent claims were rechecked.
- Implementation branch source commit: `74a3ccc2381b60f571a2bf96b8eac799047deb43`.
- Focused smoke source commit: `b64e9c46e007f26978a8d290b5e93853c1f3cdef` (followed by branch head `3cc0f622844347c75bf7b6b03936a5ef771e41b7`; content unchanged in the no-op normalization write).
- PR #603 changed only `ModelHealthService.cs` and `ModelHealthRecoveryRedactionSmoke.cs`; exact PR patch was reviewed before merge.
- Commit-status query for PR head returned no CI statuses. No GitHub Actions/build/release workflow was dispatched and no executable Core smoke/build PASS is claimed from this remote session.
- PR #603 was squash-merged to `main` as `ecc23d3d5b7b3743f0873fb2e56df7fbbd82e2f0`.
- Post-merge `main` readback at descendant `3f0d5946f45ef3c8d9cdd4848b967516f145494e` confirmed `PROJECT_LOAD_FAILED` no longer reads/appends `QS3D.LoadWarning`, and `tests/QS3D.Core.SmokeTests/ModelHealthRecoveryRedactionSmoke.cs` is present.
- No BricsCAD V25 runtime qualification is required for this pure-Core diagnostic text/redaction lane, and no BricsCAD runtime PASS is claimed.

## Result

Raw recovery warning detail is no longer exposed by `ModelHealthService`. Focused deterministic regression source is on `main`; the source-side lane is complete and released for other agents.