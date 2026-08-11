# Work claim — Bulk empty-property presence semantics

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-bulk-empty-property-presence`
- Registered: `2026-08-12T01:05:00+07:00`
- Last Updated: `2026-08-12T01:05:00+07:00`
- Baseline main SHA: `4091bf5df09ef07ff5f609104ac9f580234e3265`
- Priority: deterministic Core property-presence mismatch found during owner-requested continue-all audit
- Task Key: `CORE-BULK-EMPTY-PROPERTY-PRESENCE`

## Confirmed defect

`BulkEditService.SetProperty(...)` currently reads a property with `TryGetValue(...)` but compares `(before ?? string.Empty)` to the requested value without considering whether the key existed. For an absent property and requested empty string, the comparison succeeds and the bulk API returns a no-op without creating the property.

Canonical `ProjectElement.SetProperty(...)` only returns a no-op when the key **already exists** and its value matches. Therefore absent -> explicit empty is a real semantic map mutation in the canonical element API and persistence model, but the supported bulk API drops it.

## Reserved scope

Make `BulkEditService.SetProperty(...)` distinguish property absence from an existing empty value. Preserve all existing behavior for:

- existing equal-value no-op;
- non-empty property updates;
- editable-key policy/canonicalization;
- ownership, target bounds, atomicity and rollback;
- dirty-flag/geometry policy for real property mutations.

## Expected surfaces

- `src/QS3D.Core/Services/BulkEditService.cs`
- one focused isolated Core smoke under `tests/QS3D.Core.SmokeTests/`
- module-initializer registration for that smoke
- this claim file

## Coordination / exclusions

- Do not modify the just-completed Bulk Family relation-dirty path or its smoke.
- Do not modify `ProjectElement.SetProperty(...)`; it is the canonical reference behavior for this lane.
- No property-removal API, UI behavior, persistence format, quantity engine, BricsCAD adapter/runtime or Family semantics changes.
- No GitHub Actions/build/release dispatch and no licensed BricsCAD runtime PASS claim.

## Validation plan

- Absent property + empty requested value creates the property and reports the element changed.
- Existing empty property + empty requested value remains a complete no-op.
- Existing non-empty -> empty remains a real mutation.
- Real absent->empty mutation preserves the existing property/quantity dirty policy and project freshness behavior.
- Re-fetch current BulkEditService after claim publication, review exact PR diff against moving `main`, and read back merge evidence. Do not claim smoke execution unless actually run.

## Completion condition

Current `main` preserves explicit empty-property presence through `BulkEditService.SetProperty(...)` consistently with `ProjectElement.SetProperty(...)`, with focused deterministic regression source and exact merge evidence.
