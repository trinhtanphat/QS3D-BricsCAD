# Work claim — vertical placement finite-height integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-vertical-placement-finite-height-20260811-2324`
- Registered: `2026-08-11T23:24:00+07:00`
- Baseline main SHA: `b15b3a367fb543ccb2cb39b0d0ef5ad4281a0853`
- Integrated main SHA: `45cbf973c7f75bf9c6bb9e377df06968f2b25dac`
- PR: `#529`
- Priority: evidence-driven Core invariant hardening during owner-requested `continue all`

## Completed scope

Hardened `ElementVerticalPlacement` / `ElementVerticalPlacementService.ResolveEffectiveHeight` so returned effective heights preserve a finite, strictly-positive contract on the legacy/no-Level path and finite endpoint spans cannot expose an infinite computed `HeightM`.

## Changes

- Legacy/no-Level `ResolveEffectiveHeight` now routes through the existing `Positive(...)` validation instead of returning an unchecked height.
- `ElementVerticalPlacement` materializes its height in the constructor and rejects overflow/non-finite spans before the object becomes observable.
- Added dedicated focused smoke coverage plus `ModuleInitializer` registration; no shared smoke-runner edits.

## Validation actually performed

- Reviewed the source diff on PR #529.
- Regression covers NaN, positive infinity, zero and negative legacy heights, a normal positive legacy height, an extreme finite endpoint span that overflows during subtraction, and a normal finite placement height.
- Re-read `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs` and `tests/QS3D.Core.SmokeTests/VerticalPlacementFiniteHeightSmoke.cs` from remote `main` after merge.
- Compared concurrent main changes before refresh/merge; no overlap with the reserved vertical-placement scope was found.
- No GitHub Actions were dispatched.
- No local .NET compile, licensed BricsCAD V25 runtime, native Level/Grid behavior, or LOCAL_PASS is claimed from this environment.

## Integration

PR #529 was refreshed on current `main` without force-push and squash-merged as `45cbf973c7f75bf9c6bb9e377df06968f2b25dac`.
