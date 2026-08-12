# Work claim — Project persistence stamp scalar drift

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T14:05:50+07:00`
- Baseline main SHA: `6ca953768c76f8d916e8c87c982c06c7dc298245`
- Priority: persisted project-level scalar changes can be false-clean when `ChangeVersion` is unchanged.

## Reserved scope

Make `ProjectPersistenceStamp` detect drift in persisted project-level scalars `DrawingPath`, `DrawingFingerprint`, `ActiveFloorId`, and `ActiveZoneId` even when `ProjectState.ChangeVersion` does not advance.

## Expected surfaces

- `src/QS3D.Core/Persistence/ProjectPersistenceStamp.cs`
- Focused Core smoke/regression coverage for `ProjectPersistenceStamp.RequiresSave(...)`.

## Excluded scope

- `ProjectState` setter/`Touch()` semantics or `ChangeVersion` policy.
- QSDB serialization, schema/canonicality, current-schema preflight, or migration behavior.
- Drawing identity canonicalization/normalization.
- Active Floor/Zone validation, UI/native active-context behavior, or BricsCAD runtime qualification.

## Validation plan

- Prove a freshly saved stamp remains clean when unchanged.
- Prove changing each persisted scalar without `Touch()` leaves `ChangeVersion` unchanged but makes `RequiresSave(...)` true.
- Preserve metadata recovery and same-project identity behavior.

## Coordination

This lane is distinct from current QSDB ChangeVersion fixture work and prior drawing-identity roundtrip/canonicality lanes; it changes only save-dirty detection in `ProjectPersistenceStamp` plus focused Core regression coverage.

## Completion condition

Source and focused regression are pushed to `main`, read back on current remote state, and this claim is marked `COMPLETED` with exact implementation/validation SHAs.
