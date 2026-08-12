# Work claim — Preview Review field canonicality

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-preview-review-field-canonicality`
- Registered: `2026-08-12T09:31:00+07:00`
- Last Updated: `2026-08-12T09:31:00+07:00`
- Baseline main SHA: `dd5213b42b5d0edf6d15d2eacb334379be97803e`
- Priority: evidence-driven Core artifact reader/validator asymmetry found during owner-requested `continue all`
- Task Key: `REVIEW-PREVIEW-FIELD-CANONICALITY`

## Confirmed defect

`PreviewReviewSnapshotService.Create(...)` only emits review entry fields that are either the exact empty string (the intentional regeneration placeholder when every source field is omitted) or a non-empty canonical field validated with `CanonicalRequired(...)`. The load/verification boundary is weaker: `PreviewReviewSnapshotStore.Load(...)` reads the persisted `field` attribute with raw `Value(...)`, and `PreviewReviewSnapshotService.ValidateSnapshot(...)` checks portability but does not enforce the writer's optional-field canonicality contract.

`IsPortableReviewField(...)` intentionally returns true for empty/whitespace values and for ordinary non-handle fields, so a v1 artifact can carry whitespace-only or padded non-empty fields such as `"   "` or `" Quantity:Cost "` and reach the snapshot/fingerprint path even though the current writer can never create those values. This makes persisted row identity and comparison/facet semantics non-canonical.

## Reserved scope

Enforce one shared optional-field contract at both `ValidateSnapshot(...)` and XML load: exact empty string remains valid for the existing regeneration placeholder; every non-empty field must be nonblank and contain no leading/trailing whitespace. Preserve the exact canonical field string. Do not reinterpret whitespace as empty or trim persisted data.

## Expected surfaces

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs` for focused persisted-field regression coverage
- this claim file

## Explicit exclusions / coordination

- No Preview Review XML shape, document-node shape, portability policy, composite row-key, query/facet or snapshot comparison changes.
- No `src/QS3D.Core/Revisions/SemanticChangeReview.cs`; the separate active semantic-change-review revision-id claim owns that surface.
- No change to exact-empty regeneration placeholder semantics, snapshot format/version, fingerprint algorithm, summary counts, field portability rules or atomic publication.
- No UI/CAD/native runtime or release/workflow surface.
- No GitHub Actions/build/release dispatch and no licensed BricsCAD V25/V26 runtime qualification.

## Validation plan

- Current writer-generated canonical non-empty field still round-trips unchanged.
- Exact-empty regeneration placeholder field remains valid.
- Persisted padded non-empty field is rejected at the load/canonicality boundary before generic fingerprint failure.
- Persisted whitespace-only field is rejected instead of being treated as the intentional exact-empty placeholder.
- `PreviewReviewSnapshotService.Verify(...)` applies the same optional-field canonicality invariant as load.
- Re-fetch moving `main`, exact source/test blobs and open claims before implementation and integration; inspect exact PR diff before merge.

## Completion condition

Current `main` accepts only exact-empty or canonical non-empty Preview Review fields, focused regression source is merged, and this claim is closed `COMPLETED` with exact integration/read-back evidence.