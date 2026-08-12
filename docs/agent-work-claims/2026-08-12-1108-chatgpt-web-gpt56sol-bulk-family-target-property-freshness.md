# Work claim — Bulk Family target property freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:08:00+07:00`
- Baseline main SHA: `451cb3eda9d851ccb3d45a371617d560c34a0924`
- Priority: P1 — bulk Family assignment must not apply a stale target-property snapshot after caller-controlled target enumeration mutates the still-canonical Family instance.
- Task Key: `CORE-BULK-FAMILY-TARGET-PROPERTY-FRESHNESS`

## Confirmed defect

`BulkEditService.AssignFamily(...)` resolves the target `ProjectFamily` and snapshots its default properties before materializing caller-provided `elementIds`. Structural freshness now verifies that the same Family object remains canonical after enumeration, but `ProjectFamily.Properties` is mutable. A lazy target enumerable can therefore mutate properties on that same canonical Family without calling `project.Touch()`. The reference-identity and `ChangeVersion` guards both pass, while the assignment continues with the stale pre-enumeration property snapshot and can write defaults that no longer match the canonical Family.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs` — target Family property snapshot ordering/freshness in `AssignFamily(...)` only
- one focused Core smoke proving same-instance target-property mutation during lazy ID enumeration uses the current canonical target defaults rather than stale data
- this claim file for close-out

## Intended contract

- preserve global Family identity, structural ownership, target bounds and `ChangeVersion` freshness guards;
- snapshot target Family properties only after caller target enumeration and structural ownership revalidation, so the snapshot represents the current canonical Family;
- preserve all-or-nothing category checks, previous-Family inheritance, no-op behavior, dirty flags and transactional mutation semantics;
- do not edit ProjectFamilyService, semantic-selection, persistence/schema, LOCAL fixtures, or any currently claimed lane.

## Validation boundary

Source and focused regression will be read back from `main`. No force-push, GitHub Actions dispatch, executable full-smoke/build PASS or licensed BricsCAD V25/V26 runtime qualification will be claimed unless actually executed.