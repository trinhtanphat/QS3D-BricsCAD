# Work claim — bulk/selection family canonical no-op

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulk-family-canonical-noop-20260812-0008`
- Registered: `2026-08-12T00:08:00+07:00`
- Baseline main SHA: `8f1300b8178fd99b666ad2de15e6210176068a67`
- Priority: evidence-driven Core mutation/reporting correctness during owner-requested `continue all`

## Reserved scope

Extend the already-established canonical Family identity no-op invariant to bulk and semantic-selection family assignment so padded/case-varied references to the target Family neither mutate project state nor report false changes.

## Expected surfaces

- `src/QS3D.Core/Services/BulkEditService.cs`
- `src/QS3D.Core/Selection/SemanticSelectionBulkEditService.cs`
- focused Core smoke regression for bulk/selection Family no-op behavior
- this claim file for close-out

## Concrete defects

1. `BulkEditService.AssignFamily()` compares raw `element.FamilyId` to `family.Id` before it computes the trimmed previous Family ID. An element whose stored mutable FamilyId is `"  TARGET  "` is therefore queued as a real reassignment even though canonical ownership already matches, causing unnecessary property rewrite, dirty flags and `ProjectState.Touch()`.
2. `SemanticSelectionBulkEditService.AssignFamily()` precomputes `changedIds` with the same raw comparison. Even when downstream assignment is or becomes a canonical no-op, the selection result can report a non-zero changed count for semantically identical padded/case-varied Family identity.

## Explicit exclusions

- No Family delete/default propagation/category policy changes.
- No property-edit, quantity, geometry, persistence format, V25/native/UI, Actions/release/LOCAL_PASS work.
- Preserve stored padded/case-varied FamilyId on true no-op, matching the existing `ProjectFamilyService.Assign()` contract.

## Validation plan

- Direct bulk assignment to the canonically identical target returns zero, leaves stored FamilyId untouched, does not dirty the element and does not advance project ChangeVersion.
- Selection bulk assignment to the same canonical target reports zero changed IDs/count and likewise leaves state untouched.
- Genuine reassignment behavior remains unchanged.
- Re-fetch/compare moving `main`, publish through a feature branch/PR without force-push, then re-read remote `main` after integration.

## Completion condition

Bulk and selection Family assignment use the same trimmed/case-insensitive target identity for no-op decisions, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact integration SHA and validation actually performed.
