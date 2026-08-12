# Work claim — Curtain rectangle area overflow

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-rect-area-overflow`
- Registered: `2026-08-12T09:53:00+07:00`
- Baseline main SHA: `fdc054eb677438e1bc88b2ed46d15c8da446e7ae`
- Priority: P2 — public Core geometry metrics must not return non-finite values from finite dimensions.

## Confirmed defect

`CurtainWallRect.AreaM2` currently returns `WidthM * HeightM` directly. `CurtainWallRect` is a public geometry value object and its constructor intentionally remains permissive because callers such as `CurtainFrameOpeningPlanner` perform context-specific rectangle validation. However, two individually finite positive dimensions can overflow their product, causing the public area metric itself to return `Infinity` while adjacent Core curtain fingerprint/detail planners already fail closed on derived-area overflow.

## Reserved scope

- `src/QS3D.Core/Geometry/CurtainWallDetailPlanner.cs` (`CurtainWallRect.AreaM2` only; no planner-layout behavior changes)
- `tests/QS3D.Core.SmokeTests/CurtainWallRectAreaOverflowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/CurtainWallRectAreaOverflowRegistration.cs`
- this claim file

## Intended contract

- Keep the public `CurtainWallRect` constructor and existing frame validation timing unchanged.
- `AreaM2` returns the same product for normal finite dimensions.
- If the derived area is `NaN`/`Infinity`, `AreaM2` fails closed with `OverflowException` instead of returning a non-finite metric.
- Do not introduce sign/domain validation into the getter; context-specific invalid width/height handling remains owned by existing planners.

## Excluded scope

- No Curtain layout/grid/count changes.
- No frame/opening subtraction behavior or `CurtainOpeningRect` changes.
- No native V25/V26 materialization/UI changes.
- No fingerprint changes.
- No GitHub Actions dispatch or runtime qualification claim.

## Validation plan

- Verify claim ancestry and re-fetch exact `CurtainWallDetailPlanner.cs` blob before write.
- Add focused module-initializer smoke proving `new CurtainWallRect(1e308, 0, 2, 2).AreaM2` fails closed while normal `2 x 3` area remains `6`.
- Re-read current source/tests and exact pushed diff.
- Close claim with exact commit SHAs and ancestry verification.
- No local .NET/BricsCAD runtime PASS will be claimed unless actually executed.
