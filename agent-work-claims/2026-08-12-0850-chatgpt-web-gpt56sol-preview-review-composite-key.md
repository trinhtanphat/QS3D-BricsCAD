# Work claim — Preview Review composite row key integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:50:00+07:00`
- Completed: `2026-08-12T08:55:00+07:00`
- Baseline main SHA: `fc26083a68df9adbabd2659f785b94e98f821e4a`
- Priority: evidence-driven remote-safe Preview Review comparison integrity

## Reason

`PreviewReviewSnapshotComparisonService` indexed rows with a synthetic string key formed as `ElementId + U+001F + Field`. Two distinct row identities could therefore collapse to the same key when the delimiter appeared in different positions, for example (`A`, `B\u001fC`) and (`A\u001fB`, `C`). Across two individually verified snapshots this misclassified a removed row plus an added row as one changed row.

## Changed scope

The comparison index now uses a length-prefixed element/field representation, so distinct component pairs cannot collapse through delimiter placement. Existing `StringComparer.OrdinalIgnoreCase` identity semantics, final row ordering, snapshot verification, summary comparison, public result shape and persistence/XML behavior remain unchanged.

## Changed surfaces

- `src/QS3D.Core/Review/PreviewReviewQueryAndComparison.cs`
- `tests/QS3D.Core.SmokeTests/PreviewReviewCompositeRowKeySmoke.cs`
- this claim file

## Completion

- Claim commit: `a53bb86dd12adc37d672938d479f665ff29542a8`.
- Implementation commit: `09d44d9d24acfd8bfaaca7173245568940d5b7de` — replace delimiter-packed `RowKey` with a collision-free length-prefixed representation.
- Regression commit: `8b05e1108bbcbd809bb3459d2a614aa80ec77e54` — construct individually verified collision fixtures through test-local reflection, prove they compare as one Removed plus one Added row, and preserve case-insensitive element/field identity as Unchanged.
- Validation actually performed:
  - fetched the implementation commit diff and confirmed it changes only the `RowKey` helper;
  - re-fetched current comparison source and confirmed the length-prefixed key is present;
  - re-fetched the dedicated smoke source and checked verified-snapshot collision plus case-insensitive parity coverage;
  - reflection in the smoke follows an existing repository test precedent for invariant fixtures that cannot be produced through current public constructors;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD V25/V26 runtime PASS is claimed.

## Coordination

The latest Preview Review claims before this lane covered XML/document node shape and were already completed. No overlapping comparison claim appeared before this scope was reserved.

## Completion condition

Satisfied: current `main` no longer aliases distinct cross-snapshot element/field identities through `U+001F` delimiter placement, focused regression coverage is present, and this claim is released as `COMPLETED`.
