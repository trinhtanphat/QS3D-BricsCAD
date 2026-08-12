# Work claim — Bulk Family target property freshness

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:08:00+07:00`
- Completed: `2026-08-12T11:10:00+07:00`
- Baseline main SHA: `451cb3eda9d851ccb3d45a371617d560c34a0924`
- Claim commit: `cdd23aa6b1cc207264d758e97168ca9dc88dcd76`
- Source fix: `8d59009817fd4c3755cabcde9f9d96194f2f1252`
- Regression: `f5f5166b56405b8b327f24654711fb28dbeaf74a`
- Priority: P1 — bulk Family assignment must not apply a stale target-property snapshot after caller-controlled target enumeration mutates the still-canonical Family instance.
- Task Key: `CORE-BULK-FAMILY-TARGET-PROPERTY-FRESHNESS`

## Confirmed defect

`BulkEditService.AssignFamily(...)` resolved the target `ProjectFamily` and snapshotted its default properties before materializing caller-provided `elementIds`. Structural freshness verified that the same Family object remained canonical after enumeration, but `ProjectFamily.Properties` is mutable. A lazy target enumerable could therefore mutate properties on that same canonical Family without calling `project.Touch()`. The reference-identity and `ChangeVersion` guards both passed, while the assignment continued with the stale pre-enumeration property snapshot and could write defaults that no longer matched the canonical Family.

## Resolution

`AssignFamily(...)` now materializes the caller target IDs, verifies `ChangeVersion`, and revalidates exact Family/element ownership before it snapshots target Family properties. The subsequent category checks and assignment therefore operate from the current canonical target defaults.

Focused smoke coverage verifies that a same-instance target Family property changed during lazy ID enumeration is assigned using the new value, and a target property removed during enumeration does not leak from a stale snapshot into the element.

## Reserved scope completed

- `src/QS3D.Core/Services/BulkEditService.cs` — target Family property snapshot ordering/freshness in `AssignFamily(...)`
- `tests/QS3D.Core.SmokeTests/BulkFamilyTargetPropertyFreshnessSmoke.cs`
- this claim file

## Validation

Source and focused regression were read back from `main` after the commits. Existing structural ownership, global Family identity, target bounds, category checks, previous-Family inheritance, no-op behavior, dirty flags and transactional mutation flow remain in place.

No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification is claimed for this batch.