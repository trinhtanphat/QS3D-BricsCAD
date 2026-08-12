# Work claim — Project persistence stamp scalar drift

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T14:05:50+07:00`
- Completed: `2026-08-12T14:27:00+07:00`
- Baseline main SHA: `6ca953768c76f8d916e8c87c982c06c7dc298245`
- Priority: persisted project-level scalar changes can be false-clean when `ChangeVersion` is unchanged.

## Reserved scope

Make `ProjectPersistenceStamp` detect drift in persisted project-level scalars `DrawingPath`, `DrawingFingerprint`, `ActiveFloorId`, and `ActiveZoneId` even when `ProjectState.ChangeVersion` does not advance.

## Implemented fix

`ProjectPersistenceStamp` now snapshots all four persisted scalar values at construction and on `MarkSaved(...)`, then compares them ordinally in `RequiresSave(...)` alongside the existing `ChangeVersion`, recovery-metadata, and metadata-snapshot checks. `ProjectState` setter and `Touch()` semantics are unchanged.

## Regression evidence

`ProjectPersistenceStampScalarDirtySmoke` proves:

- a freshly captured stamp is clean;
- direct changes to `DrawingPath`, `DrawingFingerprint`, `ActiveZoneId`, and `ActiveFloorId` leave `ChangeVersion` unchanged but make `RequiresSave(...)` true;
- `MarkSaved(...)` refreshes the scalar snapshot and restores the clean state.

The focused smoke is registered through a `ModuleInitializer`, matching the existing Core smoke-test pattern.

## Integration

- claim: `7335c8a2bcaac7b90c23750230ee416d589afbae`
- source fix: `8d185ad6fb926eb464a272a64a3683312ef10306`
- regression smoke: `9b7c2694b9c6c5b4e1d5c7b2436d534843852a6d`
- smoke registration: `a091c0848963f7247afba86cc4200a68255440d4`

## Excluded scope

- `ProjectState` setter/`Touch()` semantics or `ChangeVersion` policy.
- QSDB serialization, schema/canonicality, current-schema preflight, or migration behavior.
- Drawing identity canonicalization/normalization.
- Active Floor/Zone validation, UI/native active-context behavior, or BricsCAD runtime qualification.

## Validation

Current `main` source and focused smoke were read back after integration and contain the expected persisted-scalar snapshot/compare logic and four-scalar regression. No GitHub Actions, full solution build, or licensed BricsCAD V25/V26 runtime was executed in this lane.

## Completion condition

Completed: source and focused regression are pushed to `main`, current remote source/test readback is verified, and this claim is closed with exact implementation/validation SHAs.
