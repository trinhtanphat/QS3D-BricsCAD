# Work claim — Room/Wall property finite metric integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:38:00+07:00`
- Baseline main SHA: `e8de8effb41facd89927a329ecf4281349445282`
- Priority: evidence-driven remote-safe Core domain integrity during owner-requested `continue all`

## Confirmed defect

`RoomPropertySet` and `WallPropertySet` exposed public metric `double` auto-properties that accepted `double.NaN` and infinities. This allowed malformed non-finite room/wall measurements to be retained at public Core domain boundaries. The sibling `OpeningPropertySet` already enforced the same finite-only invariant under the completed opening-property finite-metrics lane.

## Delivered contract

Every room/wall metric assignment now requires a finite value while preserving all existing finite values, including zero and negative values. No dimensional positivity, minimum-size, level, placement, geometry, or engineering policy was added.

## Published commits

- Claim reservation: `6ca1966f67c55594f779620704ecf59badca7220`
- Room source fix: `a3a6311bacbeea8e645cf7db9f4336ba0e56828d`
- Wall source fix: `d898b7ba2e20e979a105b2781145e1eba45bb67c`
- Focused smoke: `4d1a7b53d90db490c70fd02c1ab11c8ca8fc47b9`
- Smoke registration: `5b366b0ee39af8fbfed1a05cbd91a10093d7f86d`

## Surfaces

- `src/QS3D.Core/Domain/RoomPropertySet.cs`
- `src/QS3D.Core/Domain/WallPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/RoomWallPropertySetFiniteMetricsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/RoomWallPropertySetFiniteMetricsRegistration.cs`
- this claim file

## Validation / coordination evidence

- Re-read both product source files from current `main` after publication; the expected finite-only setters are present.
- Reviewed exact pushed source/test diffs.
- Focused smoke preserves defaults and representative finite zero/negative values; for every metric property it exercises NaN, +Infinity and -Infinity rejection and verifies the prior finite value remains unchanged.
- Dedicated `ModuleInitializer` registration avoids shared smoke-registry contention.
- No `ProjectElement.cs`, family/floor/zone, room lifecycle, finish generation, wall quantity, native geometry, V25/V26 adapter, or persistence changes were made.
- One initial claim create received HTTP 409 because concurrent `main` advanced; the operation was re-fetched/retried without force-push or overwrite.
- No GitHub Actions were dispatched.
- No .NET/BricsCAD runtime PASS is claimed because the exact executable/native qualification environment was not run in this remote lane.

## Completion

The scoped non-finite retention defect is fixed and regression-guarded on `main`. This claim is closed; broader LOCAL_ONLY/product/engineering closure gates remain governed by the repository completion documents.
