# Work claim — WallPier property geometry freshness

- Status: `COMPLETED`
- Agent: `gpt-5.6-sol-chatgpt`
- Registered: `2026-08-12T10:40:00+07:00`
- Completed source-side: `2026-08-12T10:47:00+07:00`
- Baseline main SHA: `bc9ac1c59a8909a3a32bc643a0fb7e3fb10cbcf7`
- Claim commit: `589dfcab1b7e4bb247958845a5d3e805770c77d8`
- Final source branch commit: `e1784363f83a3be721db50e4b1c3e6b1ddf21a69`
- Final smoke branch commit: `0a0fd351db9e4b0957a16f8bde465666bce68a3e`
- Reconcile commit: `e865ae8533bf8cd78c4c8ca71b33691cc77d1c18`
- Merge commit: `2cb0798802fa791651ead554673b498231c8e684`
- PR: `#773`
- Priority: owner-requested continue-all source-safe bug fixing

## Confirmed defect

`WallRegenerator` resolves `WallPierProfileMode` and `WallPierChamferM` from instance/Family semantic properties and uses them to select/parameterize the generated WallPier profile. `ProjectFamilyService.SetProperty()` propagates inherited Family changes through `ProjectElement.SetProperty()`, whose generated-geometry freshness is delegated to `ElementGeometryPolicy`. The policy classified generic dimensions plus GlassWall curtain layout keys, but not these WallPier profile keys. Editing either key could therefore update the semantic model without dirtying WallPier Geometry or marking an existing generated solid stale, allowing the native WallPier profile to remain out of date.

## Implemented

- Added WallPier-specific geometry classification for `WallPierProfileMode` and `WallPierChamferM`.
- `ProjectElement.SetProperty()` now dirties Geometry and marks existing generated solid output stale when either key changes on a WallPier.
- Existing `ProjectFamilyService.SetProperty()` propagation inherits the same freshness behavior without a separate mutation path.
- The same keys remain irrelevant to unrelated generated categories.
- Existing generic geometry, GlassWall curtain-only and generated-output-only material semantics remain unchanged.

## Regression source

`tests/QS3D.Core.SmokeTests/WallPierPropertyGeometryFreshnessSmoke.cs` covers:

- instance `WallPierProfileMode` edit;
- instance `WallPierChamferM` edit;
- inherited Family profile edit propagation;
- unrelated generated-category control.

## Validation performed

- Re-read `WallRegenerator`, `ProjectFamilyService`, `ProjectElement` and `ElementGeometryPolicy` production paths before writing.
- Collision-checked current commits/claims; no competing WallPier property freshness lane was found.
- Reconciled against rapidly moving `main` without force-push and compared current `main` to the PR head; the effective diff remained exactly the policy source plus focused smoke file.
- Post-merge read-back confirmed both source and smoke are present on `main`.
- No GitHub Actions, local build, executable smoke, BricsCAD V25/V26 runtime, packaging or signing gate was run; no PASS is claimed for those gates.
