# Work claim — Measured solid cleanup dirty propagation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-measured-solid-cleanup-dirty`
- Registered: `2026-08-12T13:15:00+07:00`
- Completed: `2026-08-12T13:20:00+07:00`
- Baseline main SHA: `3de60ce39149fee75f01bd6d4751967f6ab5c035`
- Claim merge SHA: `efdde24dcfbdada11cddab3fba9ebed4d59a87e9`
- Implementation SHA: `66dbf414721d774ec2b19a809278c401e8683ad0`
- Implementation PR: `#915`
- Priority: P1 — stale measured quantity cleanup must not mutate semantic quantity state while leaving a clean element false-clean.

## Confirmed defect

`MeasuredSolidQuantityPolicy.Apply()` retracted stale policy-owned quantity outputs by mutating `ProjectElement.Quantities` directly. When one or more outputs were removed, the policy called only `TouchPersistenceState()`. That updated `UpdatedUtc` but did not set `ElementDirtyFlags.Quantity`.

Concrete counterexample: a measured Beam is applied, then marked clean. Its measured volume source is removed and `MeasuredSolidQuantityPolicy.Apply()` retracts `MeasuredSolidVolumeM3`, `GrossVolumeM3`, and `NetVolumeM3`. Quantity state changes, but the old timestamp-only cleanup could leave `Dirty == None`.

## Implemented contract

- Any actual stale measured quantity removal now calls `MarkDirty(ElementDirtyFlags.Quantity)`.
- Existing removal ownership rules are preserved: `MeasuredSolidVolumeM3` is policy-owned; Gross/Net are removed only while still equal to the measured value; independent overrides remain untouched.
- Surface-area cleanup, finite/non-negative input validation, supported-category behavior, and `Apply()` return semantics remain unchanged.
- `ProjectElement.SetQuantity`, generated-output staleness, persistence schema, and BricsCAD runtime code were not modified.
- Existing `MeasuredSolidQuantityPolicySmoke` now proves a previously clean element becomes exactly Quantity-dirty after cleanup and a no-removal path remains clean.

## Validation

- Claim-only PR `#914` squash-merged as `efdde24dcfbdada11cddab3fba9ebed4d59a87e9` before source changes.
- Implementation PR `#915` changed exactly two files and squash-merged as `66dbf414721d774ec2b19a809278c401e8683ad0`.
- Commit readback confirms the dirty-propagation source change and both focused regression cases are present.
- GitHub combined status returned no status checks (`statuses=[]`). No GitHub Actions or BricsCAD runtime/build PASS is claimed.

## Reserved scope

- `src/QS3D.Core/Services/MeasuredSolidQuantityPolicy.cs`, limited to dirty propagation after stale output removal
- `tests/QS3D.Core.SmokeTests/MeasuredSolidQuantityPolicySmoke.cs`
- this claim file
