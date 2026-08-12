# Work claim — Room/Wall property finite metric integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:38:00+07:00`
- Baseline main SHA: `e8de8effb41facd89927a329ecf4281349445282`
- Priority: evidence-driven remote-safe Core domain integrity during owner-requested `continue all`

## Confirmed defect

`RoomPropertySet` and `WallPropertySet` expose public metric `double` auto-properties that currently accept `double.NaN` and infinities. This allows malformed non-finite room/wall measurements to be retained at public Core domain boundaries. The sibling `OpeningPropertySet` already enforces the same finite-only invariant under the completed opening-property finite-metrics lane.

## Reserved scope

Require every room/wall metric assignment to be finite while preserving all existing finite values, including zero and negative values. This lane does not introduce dimensional positivity, minimum-size, level, placement, geometry, or engineering policy.

## Expected surfaces

- `src/QS3D.Core/Domain/RoomPropertySet.cs`
- `src/QS3D.Core/Domain/WallPropertySet.cs`
- `tests/QS3D.Core.SmokeTests/RoomWallPropertySetFiniteMetricsSmoke.cs`
- `tests/QS3D.Core.SmokeTests/RoomWallPropertySetFiniteMetricsRegistration.cs`
- this claim file

## Coordination / exclusions

- No `ProjectElement.cs`, family/floor/zone, room lifecycle, finish generation, wall quantity, native geometry, V25/V26 adapter, or persistence changes.
- No positivity/minimum-size engineering semantics.
- Recent active claims for family create, grid renumber, license XML, sidecar revision, dependency identity, and other hot lanes do not reserve these two DTO files.
- No GitHub Actions dispatch and no BricsCAD/.NET runtime PASS claim from this remote lane.

## Validation plan

- Preserve current defaults and representative ordinary finite values.
- Preserve finite negative and zero values rather than invent business semantics.
- Reject NaN, +Infinity and -Infinity for every metric property.
- Verify failed assignments leave the prior finite value unchanged.
- Use a dedicated module initializer to avoid shared smoke registration contention.
- Re-fetch target blobs immediately before product writes and review exact pushed diffs/ancestry.

## Completion condition

Both public property sets cannot retain non-finite metric values through their setters, focused Core smoke coverage is integrated on current `main`, concurrent work is preserved, and this claim is closed with exact commit SHAs/evidence.
