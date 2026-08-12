# Work claim — Preview Review composite row key integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:50:00+07:00`
- Baseline main SHA: `fc26083a68df9adbabd2659f785b94e98f821e4a`
- Priority: evidence-driven remote-safe Preview Review comparison integrity

## Reason

`PreviewReviewSnapshotComparisonService` indexes rows with a synthetic string key formed as `ElementId + U+001F + Field`. Two distinct row identities can therefore collapse to the same key when the delimiter appears in different positions, for example (`A`, `B\u001fC`) and (`A\u001fB`, `C`). Across two individually verified snapshots this can misclassify a removed row plus an added row as one changed row.

## Intended scope

Replace the delimiter-packed comparison key with a collision-free composite representation while preserving existing case-insensitive element/field identity semantics, deterministic ordering, snapshot verification, summary comparison, and all persistence/XML behavior.

## Changed surfaces

- `src/QS3D.Core/Review/PreviewReviewQueryAndComparison.cs`
- focused smoke under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Validation boundary

Remote/static validation only in this hosted session. Do not dispatch/rerun GitHub Actions and do not claim BricsCAD V25/V26 or local .NET runtime PASS without an actual supported runtime execution.
