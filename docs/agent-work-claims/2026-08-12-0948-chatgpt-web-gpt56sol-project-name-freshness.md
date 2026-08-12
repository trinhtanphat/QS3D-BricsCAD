# Work claim — ProjectState Name persistence freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-name-freshness-20260812-0948`
- Registered: `2026-08-12T09:48:00+07:00`
- Baseline main SHA: `0773c70848f5bf5bdd48123e6031dd21d1c03454`
- Priority: P1 — ensure public Project Name mutation participates in ChangeVersion/persistence dirty tracking.

## Confirmed defect

`ProjectState.Name` is a public mutable project field and QSDB persists it, but its setter currently only validates/trims the new value and assigns `_name`. `ProjectPersistenceStamp.RequiresSave(...)` relies on `ProjectState.ChangeVersion` (plus recovery state), so a real direct project rename can leave `ChangeVersion`/`UpdatedUtc` unchanged and the persistence stamp can report that no save is required even though serialized project content changed.

## Reserved surfaces

- `src/QS3D.Core/Domain/ProjectState.cs` — Name setter only
- `tests/QS3D.Core.SmokeTests/ProjectNameFreshnessSmoke.cs` — new focused regression
- this claim file

## Intended fix

- Normalize/validate the requested name first.
- Preserve canonical-equivalent same-name assignment as a true no-op.
- On a real name change, assign the normalized name then call the existing `Touch()` primitive exactly once.
- Preserve constructor behavior by continuing to initialize `_name` directly.
- Preserve snapshot/load behavior: `ProjectStateSnapshot.CopyInto(...)` ultimately restores source `UpdatedUtc`/`ChangeVersion` via `RestorePersistenceState`, while detached-copy construction uses the same canonical name.
- Add focused smoke proving real rename advances one revision and makes an existing `ProjectPersistenceStamp` require save; same canonical name and invalid blank input do not mutate freshness/state.

## Coordination

The earlier completed Project Name invariant lane remains authoritative for nonblank/canonical validation. Active QSDB changeVersion canonicality owns `QsdbProjectStore.cs`, not this file. Snapshot identity work owns snapshot semantics only; this lane does not modify snapshot code.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff review. No GitHub Actions dispatch; no licensed BricsCAD V25/V26 runtime PASS claimed.
