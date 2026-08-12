# Work claim — Curtain rectangle area overflow

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-rect-area-overflow`
- Registered: `2026-08-12T09:53:00+07:00`
- Completed: `2026-08-12T09:56:00+07:00`
- Baseline main SHA: `fdc054eb677438e1bc88b2ed46d15c8da446e7ae`
- Claim commit: `fdb8394f9cd60767e1c1027070c0ab5990ff5ff3`
- Source fix commit: `f490a146cfc0e1889da993edc03f0cd1acd18d19`
- Regression commit: `26e769a351e1d6f1ac6ec5f12074537f5a6d0240`
- Registration commit: `b5e0cf6dd752ab2c4cbf93adfdd2b446062c259e`
- Priority: P2 — public Core geometry metrics must not return non-finite values from finite dimensions.

## Confirmed defect

`CurtainWallRect.AreaM2` returned `WidthM * HeightM` directly. `CurtainWallRect` is a public geometry value object and its constructor intentionally remains permissive because callers such as `CurtainFrameOpeningPlanner` perform context-specific rectangle validation. Two individually finite positive dimensions could therefore overflow their product and expose `Infinity` as a public area metric while adjacent Core curtain planners/fingerprints already fail closed on derived-area overflow.

## Implemented surfaces

- `src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs` (`CurtainWallRect.AreaM2` only)
- `tests/QS3D.Core.SmokeTests/CurtainWallRectAreaOverflowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallRectAreaOverflowRegistration.cs`
- this claim file

## Implemented contract

- Kept the public `CurtainWallRect` constructor unchanged, preserving existing context-specific frame validation timing.
- `AreaM2` computes the same product for normal dimensions.
- If the derived product is `NaN` or `Infinity`, `AreaM2` now throws `OverflowException` instead of returning a non-finite metric.
- No sign/domain validation was added to the getter; existing planners remain responsible for contextual rectangle validity.

## Excluded scope honored

- No Curtain layout/grid/count changes.
- No frame/opening subtraction behavior or `CurtainOpeningRect` changes.
- No native V25/V26 materialization/UI changes.
- No fingerprint changes.
- No GitHub Actions dispatch or runtime qualification claim.

## Validation actually performed

- Claim commit was published before substantive writes and verified as an ancestor of the then-current `main` (`ahead 4 / behind 0`, merge-base exactly the claim).
- The four concurrent commits after claim publication were inspected and did not touch the reserved Curtain geometry/test paths.
- Re-fetched `CurtainWallDetailPlanner.cs` immediately before the update and confirmed the reviewed blob remained `dd63a1d1e32a9cb8e2cf213068b18fc3bb2c2838`.
- Source was written with exact blob-SHA guard; GitHub reported source commit `f490a146cfc0e1889da993edc03f0cd1acd18d19`.
- Reviewed the exact source commit diff: only `CurtainWallRect.AreaM2` changed (`10` additions / `1` deletion); constructor and planner code remained unchanged.
- Re-read final focused smoke and module-initializer registration from `main`. Smoke verifies a normal `2 x 3` rectangle remains `6`, while finite dimensions `1e308 x 2` fail closed with `OverflowException` on `AreaM2` access.
- No local .NET compile/test execution is claimed in this connector-only lane.
- No BricsCAD V25/V26 runtime qualification is claimed.
- No GitHub Actions were dispatched and no force-push was used.

## Completion condition

Completed. `CurtainWallRect.AreaM2` no longer exposes non-finite derived area values, focused regression source is on `main`, and exact implementation/test SHAs are recorded above.
