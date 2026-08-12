# Work claim — Measured solid cleanup dirty propagation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-measured-solid-cleanup-dirty`
- Registered: `2026-08-12T13:15:00+07:00`
- Baseline main SHA: `3de60ce39149fee75f01bd6d4751967f6ab5c035`
- Priority: P1 — stale measured quantity cleanup must not mutate semantic quantity state while leaving a clean element false-clean.

## Confirmed defect

`MeasuredSolidQuantityPolicy.Apply()` retracts stale policy-owned quantity outputs by mutating `ProjectElement.Quantities` directly. When one or more outputs are removed, the policy currently calls only `TouchPersistenceState()`. That updates `UpdatedUtc` but does not set `ElementDirtyFlags.Quantity`.

Concrete counterexample: a measured Beam is applied, then marked clean. Its measured volume source is removed and `MeasuredSolidQuantityPolicy.Apply()` retracts `MeasuredSolidVolumeM3`, `GrossVolumeM3`, and `NetVolumeM3`. Quantity state changes, but because cleanup bypasses `SetQuantity()` and only touches persistence time, the element can remain `Dirty == None` even though persisted/calculated quantity data changed.

The existing `ProjectElement.SetQuantity` dirty-propagation lane is already completed; this claim does not modify `ProjectElement` and is limited to the cleanup path in `MeasuredSolidQuantityPolicy`.

## Reserved scope

- `src/QS3D.Core/Services/MeasuredSolidQuantityPolicy.cs`, limited to dirty propagation after stale output removal
- focused Core smoke regression under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Any actual stale measured quantity removal marks `ElementDirtyFlags.Quantity`.
- Preserve current removal ownership rules: remove `MeasuredSolidVolumeM3`, and remove Gross/Net only when still equal to the policy-owned measured value; preserve independent overrides.
- Preserve current surface-area cleanup behavior, finite/non-negative input validation, supported-category behavior, and `Apply()` return semantics.
- Do not change `ProjectElement.SetQuantity`, generated-output staleness, persistence schema, or BricsCAD runtime code.
- Regression must prove a previously clean element becomes Quantity-dirty after cleanup and that a no-removal/no-op path does not invent dirty state.

## Validation boundary

No GitHub Actions or BricsCAD runtime/build PASS will be claimed unless actually observed.
