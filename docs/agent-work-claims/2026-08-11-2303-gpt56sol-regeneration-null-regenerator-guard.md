# Work claim — regeneration null regenerator guard

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-regeneration-null-regenerator-guard-20260811-2303`
- Registered: `2026-08-11T23:03:00+07:00`
- Completed: `2026-08-11T23:11:30+07:00`
- Baseline main SHA: `8d2d27c2cdc37811a1cc3fd41444446bf933f648`
- Priority: evidence-driven Core constructor invariant hardening during owner-requested `continue all`

## Completed scope

`RegenerationEngine` now rejects null entries in its regenerator collection at construction time instead of accepting an invalid engine that would fail later with `NullReferenceException` during regeneration.

## Changed surfaces

- `src/QS3D.Core/Services/RegenerationEngine.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationConstructorIntegritySmoke.cs`
- `tests/QS3D.Core.SmokeTests/RegenerationConstructorIntegritySmokeRegistration.cs`
- this claim file for coordination/close-out

## Result

The constructor now:

- rejects a null graph as before;
- rejects a null regenerator enumerable as before;
- materializes the enumerable once;
- rejects any null entry with `ArgumentException` before the engine can be used;
- preserves a valid empty regenerator list for measured-solid / quantity-rule-only handling paths.

No regenerator selection ordering, regeneration pass behavior, dirty semantics, or quantity-rule behavior was changed.

## Regression coverage

Focused smoke verifies:

- null graph rejection;
- null regenerator enumerable rejection;
- null entry rejection;
- empty regenerator collection remains constructible.

Registration uses a dedicated `ModuleInitializer` file, avoiding shared smoke-runner edits.

## Integration

- Claim commit: `ee0abefc308b0c635d38d5aab32013d2b585e3aa`
- Atomic implementation commit: `3306f6cc9564de03d5aa4ab09dde147af7272a89`
- Temporary branch refresh commits: `dc76b83d21785ed58ae008defa29dfd69f7a8c2d`, `0c7b3c38b10885ecc61706b5146c699caa619db6`
- PR: `#520` — `fix(regeneration): reject null regenerator entries`
- `main` integration merge: `d1cb481f4521b6fa73b3e9167dceef3864442797`
- Later `main` observed during post-merge verification: `7a0a564d43e99d93f1dce431c2d6d3b4abf19f83`

The integration merge is an ancestor of the later observed `main`; intervening commits did not modify this lane's files.

## Validation actually performed

- Re-read the constructor from remote `main`; the null-entry guard is present before `_regenerators` assignment.
- Re-read the focused smoke from remote `main`; null graph, null enumerable, null entry, and empty collection cases are present.
- Verified PR #520 contained exactly three changed files and GitHub reported it mergeable after refreshing current `main` into the temporary branch.
- Verified `d1cb481f4521b6fa73b3e9167dceef3864442797` is an ancestor of later observed `main` `7a0a564d43e99d93f1dce431c2d6d3b4abf19f83`; intervening changes were outside this lane.
- GitHub Actions were not dispatched by this lane.
- Local compilation/Core smoke execution and BricsCAD V25 runtime execution were not available in this remote connector environment, so no unexecuted build/runtime PASS is claimed.

## Exclusions retained

No regeneration selection-order/pass-count changes, ProjectElement dirty semantics, quantity-rule changes, BricsCAD V25/native/runtime, UI, updater/licensing, persistence, Actions, release, or LOCAL_PASS claims were made.
