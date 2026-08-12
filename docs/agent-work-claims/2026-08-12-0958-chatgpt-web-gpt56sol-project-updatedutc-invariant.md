# Work claim — ProjectState UpdatedUtc UTC invariant

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-updatedutc-invariant-20260812-0958`
- Registered: `2026-08-12T09:58:00+07:00`
- Completed: `2026-08-12T10:03:00+07:00`
- Baseline main SHA observed: `bcc3d13fca83ee747cec362945883bc6686b3a08`
- Claim commit: `f80afc84ceb319d9fd1a8d0d87315c7926403046`
- Pull Request: `#733`
- Reviewed head: `2977c4f59ccbd62c5ca8afba0a139ee69b2815d2`
- Merge SHA: `3c99695ffacc78c012602735df2a5ec5f3908acf`
- Priority: P1 deterministic persistence-state integrity
- Task Key: `CORE-PROJECT-UPDATEDUTC-INVARIANT`

## Confirmed defect

`ProjectState` initializes and advances `UpdatedUtc` with UTC, `RestorePersistenceState(...)` rejects non-UTC timestamps, and QSDB save has an established deterministic-UTC gate, but public `UpdatedUtc` assignment previously accepted Local/Unspecified values and could leave the live project in a state its own persistence boundary refused to save.

## Completed implementation

- `ProjectState.UpdatedUtc` now uses a private backing field and rejects any non-UTC assignment before mutation.
- Exact valid UTC values remain assignable without changing `ChangeVersion`.
- `Touch()` and successful Name mutation continue to assign UTC timestamps.
- `RestorePersistenceState(...)` uses the same UTC validation while preserving its existing change-version validation and restore semantics.
- Project Name freshness/overflow behavior, ProjectElement timestamps and QSDB code were not changed.

## Regression evidence

`tests/QS3D.Core.SmokeTests/ProjectStateUpdatedUtcInvariantSmoke.cs` covers default UTC state, exact UTC assignment without revision mutation, atomic Local/Unspecified rejection, and `Touch()` preserving UTC while advancing one revision.

Moving-main comparison from the claim commit showed no overlap with `ProjectState.cs` or the smoke, and the source blob on moving `main` was re-read unchanged immediately before the head-locked squash merge.

## Validation boundary

No GitHub Actions/full build/release dispatch occurred. No local/full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed.
