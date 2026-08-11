# Work claim — vertical placement finite-height integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-vertical-placement-finite-height-20260811-2324`
- Registered: `2026-08-11T23:24:00+07:00`
- Baseline main SHA: `b15b3a367fb543ccb2cb39b0d0ef5ad4281a0853`
- Priority: evidence-driven Core invariant hardening during owner-requested `continue all`

## Reserved scope

Harden `ElementVerticalPlacement` / `ElementVerticalPlacementService.ResolveEffectiveHeight` so every returned effective height is finite and strictly positive, including legacy/no-Level paths and extreme finite elevation spans.

## Expected surfaces

- `src/QS3D.Core/Domain/ElementVerticalPlacementService.cs`
- `tests/QS3D.Core.SmokeTests/VerticalPlacementFiniteHeightSmoke.cs`
- `tests/QS3D.Core.SmokeTests/VerticalPlacementFiniteHeightSmokeRegistration.cs`
- this claim file for close-out

## Concrete defects

1. `ResolveEffectiveHeight()` currently returns `legacyHeightM` directly when no Level metadata is configured, bypassing the same finite/positive validation used by `Resolve()`. `NaN`, infinity, zero or negative height can therefore escape through one public path but not the other.
2. `ElementVerticalPlacement` validates both endpoint elevations as finite and ordered, but `TopElevationM - BottomElevationM` can still overflow to infinity for extreme finite endpoints. The object can therefore expose a non-finite `HeightM` despite accepting only finite endpoint inputs.

## Explicit exclusions

- No native BricsCAD Level/Grid materialization or V25 runtime changes.
- No floor/level identity schema changes.
- No hosted-opening relation policy changes beyond preserving the existing finite-height invariant.
- No UI, updater/licensing, interchange, rebar, Actions, release, or LOCAL_PASS work.

## Validation plan

- Legacy/no-Level `ResolveEffectiveHeight` rejects NaN, infinity, zero and negative heights.
- A normal positive legacy height is returned unchanged.
- `ElementVerticalPlacement` rejects an extreme finite endpoint span whose computed height overflows.
- A normal finite positive placement still exposes the expected height.
- Re-fetch/compare `main`, publish atomically via temporary branch/PR, and re-read remote `main` after integration.

## Completion condition

Every public effective-height path preserves finite positive height, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact integration SHA and validation actually performed.
