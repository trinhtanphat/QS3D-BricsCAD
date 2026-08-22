# Work claim — Preview Review field canonicality

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-preview-review-field-canonicality`
- Registered: `2026-08-12T09:31:00+07:00`
- Last Updated: `2026-08-12T09:38:00+07:00`
- Baseline main SHA: `dd5213b42b5d0edf6d15d2eacb334379be97803e`
- Claim commit: `9eedae6b5ed907d2ec1faaed324187282b8cbd31`
- Implementation PR: `#705`
- Main implementation commit: `2d4e0ae59d282aec953b5f3ff7cfe7f79c719a55`
- Priority: evidence-driven Core artifact reader/validator asymmetry found during owner-requested `continue all`
- Task Key: `REVIEW-PREVIEW-FIELD-CANONICALITY`

## Confirmed defect

`PreviewReviewSnapshotService.Create(...)` only emits review entry fields that are either the exact empty string (the intentional regeneration placeholder when every source field is omitted) or a non-empty canonical field validated with `CanonicalRequired(...)`. Before this fix, the load/verification boundary was weaker: `PreviewReviewSnapshotStore.Load(...)` read the persisted `field` attribute with raw `Value(...)`, and `PreviewReviewSnapshotService.ValidateSnapshot(...)` checked portability but did not enforce the writer's optional-field canonicality contract.

`IsPortableReviewField(...)` intentionally accepts the exact empty placeholder and previously also returned true for whitespace-only values and ordinary padded non-handle fields. A v1 artifact could therefore carry `"   "` or `" Quantity:Cost "` as row identity and proceed to the snapshot/fingerprint path even though the current writer can never create those values.

## Implemented scope

Added shared `PreviewReviewSnapshotService.IsCanonicalOptionalReviewField(...)` semantics:

- exact empty string remains valid;
- every non-empty value must be nonblank;
- every non-empty value must equal its own `Trim()` result;
- persisted data is rejected, not trimmed/repaired.

`ValidateSnapshot(...)` now enforces this invariant before portability and row-key checks. `PreviewReviewSnapshotStore.Load(...)` applies the same predicate at the raw XML field boundary and throws `InvalidDataException` before generic fingerprint validation for a noncanonical persisted field.

## Regression source

Extended `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs` with persisted XML tamper coverage for:

- padded non-empty field `" Quantity:Cost "`;
- whitespace-only field `"   "`;
- both must fail specifically with the canonical field boundary rather than merely failing later because the fingerprint changed.

Existing canonical quantity round-trip and regeneration snapshot coverage remain unchanged, including the writer's existing exact-empty regeneration placeholder behavior.

## Surfaces changed

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`
- `tests/QS3D.Core.SmokeTests/PreviewReviewSnapshotSmoke.cs`
- this claim file

## Coordination / exclusions preserved

- No Preview Review XML shape, document-node shape, portability policy, composite row-key, query/facet or snapshot comparison changes.
- `src/QS3D.Core/Revisions/SemanticChangeReview.cs` was not modified; the separate revision-id integrity lane remained isolated.
- No snapshot format/version, fingerprint algorithm, summary count, portability-rule or atomic-publication changes.
- No UI/CAD/native runtime or release/workflow surface changed.
- No GitHub Actions/build/release workflow was dispatched, and no licensed BricsCAD V25/V26 runtime PASS is claimed.

## Validation evidence

- Claim was visible on `main` before source work at `9eedae6b5ed907d2ec1faaed324187282b8cbd31`.
- Post-claim source/test blobs were re-read as `9bee2cac5017ada059c700475528e46466d3731d` and `e3f2bb9d4caea8e43cf0b22d54fddf9d4ebdf3d5`; both still contained the confirmed gap.
- Branch compare against its post-claim baseline showed exactly two changed files, `+38/-0`.
- PR `#705` exact unified diff was reviewed before merge: production changes were the shared predicate plus one `ValidateSnapshot(...)` guard and one XML-load guard; smoke changes were only the focused canonical-field tamper regression.
- Server-side squash merge with exact reviewed head `12c96e4f8849695cfb16da702b96f37f81f3d7c4` produced `2d4e0ae59d282aec953b5f3ff7cfe7f79c719a55`.
- Post-merge read-back on `main` shows source blob `1ee4e50d0fba203a2b36d33b313d3081d8e6799c` with both guards, and smoke blob `10e06d7f6287d776d0297bfb652a76000c8db0d9` with padded/whitespace tamper coverage.
- Local executable smoke/build was not run or claimed in this connector-only environment.

## Completion

`COMPLETED`: current `main` accepts only exact-empty or canonical non-empty Preview Review fields, rejects padded/whitespace persisted row identity before fingerprint fallback, and carries focused regression source with exact integration evidence.