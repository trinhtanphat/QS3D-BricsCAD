# Work claim — Grid naming bounded enumeration

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:04:00+07:00`
- Baseline main SHA observed: `a269ba2e35530daa7b7c03dc472227948b3c626c`
- Priority: P1 — deterministic Core resource-bound correctness.

## Confirmed defect

`GridNamingService.Renumber()` declares `MaxGridBatch = 2000`, but currently executes `orderedGridElementIds.Select(...).ToList()` before checking `ids.Count > MaxGridBatch`.

The declared batch cap therefore limits only accepted cardinality, not enumeration or allocation. A huge, expensive, adversarial, or non-terminating lazy enumerable can be consumed without bound before the method reaches the 2,000-item guard.

This defect is independent of the currently active Grid intersection identity/spatial bounded-enumeration claims: this lane reserves only semantic Grid naming input materialization and its isolated regression/static gate.

## Reserved scope

- `src/QS3D.Core/Domain/GridNamingService.cs`
- `tests/QS3D.Core.SmokeTests/GridNamingBoundedEnumerationSmoke.cs` (new)
- `tests/QS3D.Core.SmokeTests/GridNamingBoundedEnumerationSmokeRegistration.cs` (new)
- `scripts/preflight-grid-naming-bounded-enumeration.py` (new)
- `docs/plans/2026-08-12-grid-naming-bounded-enumeration.md` (new)
- this claim file for close-out

## Detailed implementation plan

### Phase 1 — revalidate moving-main boundaries

- Re-fetch exact current `main` and `GridNamingService.cs` after this claim lands.
- Re-check recent Grid claims/commits; stop if another ACTIVE/BLOCKED claim has reserved `GridNamingService.cs`.
- Preserve existing Grid label formatting, duplicate-id validation, target/category validation, reserved-label collision checks, no-op behavior, dirty/timestamp semantics, and renumber ordering.

### Phase 2 — make `MaxGridBatch` bound enumeration

- Replace full LINQ materialization with one-pass bounded materialization.
- Consume at most the first `MaxGridBatch + 1` source items: accepted inputs of up to 2,000 are normalized exactly as today; the 2,001st yielded item triggers the existing oversize error immediately.
- Do not request a 2,002nd item after the cap is known to be exceeded.
- Preserve indexed `Required(...)` validation for accepted items and the current public error message.

### Phase 3 — deterministic adversarial regression

- Add a lazy source that can keep yielding indefinitely but throws if `Renumber()` requests item 2,002.
- Assert the method throws the existing 2,000-item `InvalidOperationException` after exactly 2,001 yielded items, before project element resolution or semantic mutation.
- Assert `ProjectState.ChangeVersion` remains unchanged.
- Module-register the new smoke without editing the shared smoke registration hotspot.

### Phase 4 — focused static gate

- Add an auto-discovered preflight requiring bounded one-pass enumeration before project resolution.
- Reject reintroduction of the legacy `.Select(...).ToList()` materialization path in `Renumber()`.
- Require the oversize regression and isolated module registration.

### Phase 5 — moving-main integration

- Implement on an isolated branch from the post-claim `main`.
- Compare moving `main` before PR and before merge; if `GridNamingService.cs` changed concurrently, do not overwrite the winner and re-read/reconcile only if scopes are non-overlapping.
- Open a focused PR and squash-merge using expected head SHA; never force-update `main`.
- Close this claim on `main` with exact PR/merge evidence and validation limitations.

## Explicit exclusions

- No Grid intersection identity, spatial ordering, annotation rendering, native CAD geometry, command lifecycle, UI, updater, release, or persistence-format changes.
- No change to numeric/alphabetic label formatting, prefix/suffix semantics, duplicate detection, label collision rules, or 2,000-item public capacity.
- No GitHub Actions dispatch.
- No BricsCAD V25 runtime PASS claim.

## Validation level

Source/static review plus committed CAD-independent Core smoke regression and focused preflight. The current web/container environment cannot be relied upon for a repository checkout, so no executable smoke/preflight PASS will be claimed unless an actual run succeeds.

## Completion condition

`GridNamingService.Renumber()` enforces the existing 2,000-item capacity while enumerating, never requests item 2,002 for oversize lazy input, regression/preflight coverage is merged on current `main`, and this claim is marked `COMPLETED` with exact evidence.