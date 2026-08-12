# Work claim — Preview Review category canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-preview-review-category-canonicality`
- Registered: `2026-08-12T09:41:00+07:00`
- Last Updated: `2026-08-12T09:41:00+07:00`
- Baseline main SHA: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`
- Priority: evidence-driven Core review facet/filter canonicality found during owner-requested `continue all`
- Task Key: `REVIEW-PREVIEW-CATEGORY-CANONICALITY`

## Confirmed defect

Preview Review writers only create entry categories in two forms: Quantity Rule reviews use canonical `ElementCategory.ToString()` names and Regeneration reviews use the exact empty string. The reader/validator contract is weaker: `PreviewReviewSnapshotStore.Load(...)` reads the persisted `category` attribute with raw `Value(...)`, while `PreviewReviewSnapshotService.ValidateSnapshot(...)` does not validate category canonicality.

`PreviewReviewQueryService` uses `entry.Category` directly for category filtering, searching and category facet grouping, and snapshot comparison treats Category as semantic row content. A fingerprint-valid v1 artifact can therefore carry padded or whitespace-only category text that the current writer can never emit, causing noncanonical facet/filter/comparison semantics.

## Reserved scope

Mirror the already-established optional-field contract for entry Category: exact empty remains valid for Regeneration rows; every non-empty category must be nonblank and contain no leading/trailing whitespace. Reject persisted noncanonical values rather than trimming/repairing them. Preserve the exact canonical category string and do not narrow categories to a fixed enum set at the artifact validator.

## Expected surfaces

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs`
- this claim file

## Explicit exclusions / coordination

- The completed Preview Review field-canonicality lane (`#705`, `2d4e0ae59d282aec953b5f3ff7cfe7f79c719a55`) remains intact; this lane only adds Category parity.
- No Query/Comparison behavior changes, XML shape, portability, row-key, snapshot version/fingerprint, Revisions or UI/native changes.
- Do not require non-empty categories or a specific enum on persisted review rows; exact empty remains the regeneration representation and canonical non-empty text remains extensible.
- No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime qualification.

## Validation plan

- Existing canonical Quantity Rule category round-trip remains unchanged.
- Existing Regeneration exact-empty category remains valid.
- Persisted padded category is rejected at the category canonicality boundary before fingerprint fallback.
- Persisted whitespace-only category is rejected rather than becoming a whitespace facet.
- `PreviewReviewSnapshotService.Verify(...)` and XML load enforce the same optional-category invariant.
- Re-fetch current source/test blobs and review exact PR diff before merge.

## Completion condition

Current `main` accepts only exact-empty or canonical non-empty Preview Review categories, focused persisted regression source is merged, and this claim is closed `COMPLETED` with exact evidence.