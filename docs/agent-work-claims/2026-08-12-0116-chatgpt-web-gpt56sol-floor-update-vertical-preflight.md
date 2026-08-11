# Work claim — Floor elevation update vertical-reference preflight

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-floor-update-vertical-preflight`
- Registered: `2026-08-12T01:16:00+07:00`
- Baseline main SHA: `c406188c5aeefea6e3612defee6c649f22590ca9`
- Priority: deterministic validate-before-mutate integrity defect found during owner-requested continue-all audit

## Confirmed defect

`ProjectFloorService.Update(...)` validates the new Floor elevation itself as finite, resolves all semantic elements referencing that Floor, then touches/mutates the project. It does not preflight the **prospective vertical placement** of elements whose `BottomLevelId` / `TopLevelId` references the Floor being moved.

A finite Floor elevation update can therefore:

- overflow with an existing finite Bottom/Top offset, producing a non-finite effective level elevation; or
- move an effective Bottom Level to/above its Top Level (or Top to/below Bottom).

Both states are rejected later by the vertical placement resolver / assignment contract, but the Floor update has already been persisted and marked dirty. This is a validate-before-mutate inconsistency.

## Reserved scope

When `elevationChanged` is true, preflight only elements whose Bottom or Top Level relation references the Floor being updated:

- substitute the candidate Floor elevation for that referenced endpoint;
- resolve the counterpart Level from the existing project when present;
- apply existing finite offset parsing and finite-add closure;
- if both endpoints are present, preserve the existing `top > bottom` invariant;
- throw before `project.Touch()`, Floor mutation or element dirty propagation on failure.

Elements referencing the Floor only through legacy `FloorId` and not through Bottom/Top Level relations are not subject to this new vertical-pair preflight.

## Expected surfaces

- `src/QS3D.Core/Domain/ProjectFloorService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFloorUpdateVerticalPreflightSmoke.cs`
- module-initializer registration in the new smoke file
- this claim file

## Excluded scope

- No changes to Floor/Zone canonical identity, tolerance/no-op policy, dependency propagation, `ElementVerticalPlacementService`, assignment APIs, persistence schema or V25/V26 UI/native workflows.
- No repair of pre-existing unrelated invalid Level references.
- No new engineering bounds on finite elevations/offsets.
- No GitHub Actions dispatch.

## Validation plan

- Updating a Floor used as Bottom Level rejects prospective finite-add overflow before mutation.
- Updating a Floor used as Top Level rejects prospective finite-add overflow before mutation.
- Moving Bottom effective elevation to/above existing Top is rejected before mutation.
- Moving Top effective elevation to/below existing Bottom is rejected before mutation.
- A Floor referenced only via legacy `FloorId` remains updateable under the existing contract.
- A valid vertical-reference Floor elevation update still mutates the Floor, touches project, and marks referenced semantic elements dirty as before.
- Failure preserves Floor name/elevation, project ChangeVersion/UpdatedUtc, and element UpdatedUtc/Dirty.
- Inspect exact implementation diff and read back final source/test from moving `main` before close-out.

## Coordination

The immediately preceding Floor assignment overflow lane is completed (`152d0779148f340f7fc777273a07e1b5c090ce32`). Existing Floor tolerance/canonical-reference lanes are also complete. Recent commit search found no active claim for prospective vertical-reference validation during `ProjectFloorService.Update`.

## Completion condition

Current `main` refuses Floor elevation updates that would make referenced Bottom/Top placement non-finite or inverted, without broadening unrelated FloorId behavior, focused deterministic regression coverage is present, and this claim is closed `COMPLETED`.
