# Work claim — ProjectState UpdatedUtc UTC invariant

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-updatedutc-invariant-20260812-0958`
- Registered: `2026-08-12T09:58:00+07:00`
- Baseline main SHA observed: `bcc3d13fca83ee747cec362945883bc6686b3a08`
- Priority: P1 deterministic persistence-state integrity
- Task Key: `CORE-PROJECT-UPDATEDUTC-INVARIANT`

## Confirmed defect

`ProjectState` initializes and advances `UpdatedUtc` with `DateTime.UtcNow`, and `RestorePersistenceState(...)` explicitly rejects any timestamp whose `DateTimeKind` is not `Utc`. QSDB persistence also has an established deterministic-UTC save gate (`f497819ad7de4178e42f25c070c38ac77b850412`). Despite those contracts, `UpdatedUtc` is currently a public auto-property setter, so any caller can assign `DateTimeKind.Local` or `Unspecified` and leave the live project in a state that its own persistence boundary refuses to save.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — `UpdatedUtc` storage/setter invariant only
- `tests/QS3D.Core.SmokeTests/ProjectStateUpdatedUtcInvariantSmoke.cs` — focused auto-registered regression
- this claim file

## Intended contract

- `ProjectState.UpdatedUtc` must always have `DateTimeKind.Utc`.
- Default construction, `Touch()`, successful Name mutation and snapshot/persistence restore continue using UTC values.
- Direct UTC assignment remains allowed and preserves the exact value.
- Direct Local/Unspecified assignment fails before mutation and leaves the previous timestamp unchanged.
- No ChangeVersion increment is added to direct timestamp assignment; this lane enforces the existing timestamp representation invariant only.

## Excluded scope

No changes to Project Name freshness/overflow semantics, ChangeVersion policy, ProjectElement timestamps, QSDB parser/schema, snapshots, CAD/UI/runtime, Actions/build/release.

## Validation plan

Add ModuleInitializer Core smoke coverage for default UTC, exact UTC assignment, Local/Unspecified rejection with atomic timestamp preservation, and `Touch()` continuing to emit UTC while incrementing ChangeVersion once. Re-fetch moving `main`, compare exact overlap, merge with expected head SHA, and close this claim with immutable evidence.

No GitHub Actions/full build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim from this lane.
