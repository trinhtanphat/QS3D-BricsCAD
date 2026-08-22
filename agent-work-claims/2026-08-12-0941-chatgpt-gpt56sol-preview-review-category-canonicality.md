# Work claim — Preview Review category canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-preview-review-category-canonicality`
- Registered: `2026-08-12T09:41:00+07:00`
- Last Updated: `2026-08-12T09:49:00+07:00`
- Baseline main SHA: `2d59c7e11f156387b452e86077a23a6f0f8a8db0`
- Claim commit: `55371e725ae2b81212985c77e49b5ac73585d47b`
- Implementation PR: `#716`
- Main implementation commit: `3d115ec5fd52d48b2a8e6d9fd7a238417fbd9681`
- Priority: evidence-driven Core review facet/filter canonicality found during owner-requested `continue all`
- Task Key: `REVIEW-PREVIEW-CATEGORY-CANONICALITY`

## Confirmed defect

Preview Review writers only create entry categories in two forms: Quantity Rule reviews use canonical `ElementCategory.ToString()` names and Regeneration reviews use the exact empty string. Before this fix, `PreviewReviewSnapshotStore.Load(...)` read the persisted `category` attribute with raw `Value(...)`, while `PreviewReviewSnapshotService.ValidateSnapshot(...)` did not validate category canonicality.

`PreviewReviewQueryService` uses `entry.Category` directly for category filtering, searching and category facet grouping, and snapshot comparison treats Category as semantic row content. A fingerprint-valid v1 artifact could therefore carry padded or whitespace-only category text that the current writer can never emit, causing noncanonical facet/filter/comparison semantics.

## Implemented scope

Added optional-category parity with the completed field-canonicality contract:

- exact empty string remains valid for Regeneration rows;
- every non-empty category must be nonblank;
- every non-empty category must contain no leading/trailing whitespace;
- persisted noncanonical data is rejected rather than trimmed/repaired;
- canonical non-empty text remains extensible and is not restricted to a fixed enum at the artifact layer.

`ValidateSnapshot(...)` now enforces this category invariant before the existing field/portability/row-key checks. `PreviewReviewSnapshotStore.Load(...)` applies the same raw category boundary and throws `InvalidDataException` before generic fingerprint validation.

## Regression source

Extended `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs` with persisted XML tamper coverage for:

- padded category `" Beam "`;
- whitespace-only category `"   "`;
- both must fail specifically with the category canonicality boundary rather than merely failing later because the fingerprint changed.

Existing canonical Quantity Rule round-trip, exact-empty Regeneration category behavior, and Preview Review field-canonicality coverage remain intact.

## Surfaces changed

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs`
- this claim file

## Coordination / exclusions preserved

- Completed Preview Review field canonicality from PR `#705` / `2d4e0ae59d282aec953b5f3ff7cfe7f79c719a55` remains intact.
- No Query/Comparison implementation, XML shape, portability, row-key, snapshot version/fingerprint, Revisions or UI/native surface changed.
- No fixed `ElementCategory` enum validation was added at the artifact layer.
- No GitHub Actions/build/release workflow was dispatched and no licensed BricsCAD V25/V26 runtime PASS is claimed.

## Validation evidence

- Claim was committed to `main` before source work at `55371e725ae2b81212985c77e49b5ac73585d47b`.
- Post-claim source/test read-back confirmed the inherited field fix but no Category guard; source/test blobs were `1ee4e50d0fba203a2b36d33b313d3081d8e6799c` and `10e06d7f6287d776d0297bfb652a76000c8db0d9`.
- Branch compare showed exactly two changed files, source `+7/-0` and smoke `+29/-0`.
- PR `#716` exact unified diff was reviewed before merge and contained only the optional-category helper, one `ValidateSnapshot(...)` guard, one XML-load guard and the focused persisted-category regression.
- Server-side squash merge with exact reviewed head `ed34f0666d061a09f74c0493ce90bb5fb622cba2` produced `3d115ec5fd52d48b2a8e6d9fd7a238417fbd9681`.
- Post-merge read-back on `main` shows source blob `88498c55ff45b91e96c9a6ffecfd969896b3a1e4` and smoke blob `7c728cd51e2b83bdedc848294c40a1784d4d2465` with the intended guards/regression.
- Local executable smoke/build was not run or claimed in this connector-only environment.

## Completion

`COMPLETED`: current `main` accepts only exact-empty or canonical non-empty Preview Review categories, rejects padded/whitespace category semantics before fingerprint fallback, and carries focused regression source with exact integration evidence.