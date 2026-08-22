# Work claim — Preview Review snapshot row-key collision safety

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-preview-review-snapshot-row-key-20260812-1050`
- Registered: `2026-08-12T10:50:00+07:00`
- Priority: P1 review identity / false-duplicate safety

## Confirmed defect

Preview Review comparison already uses a length-prefixed `(ElementId, Field)` identity after the completed composite-row-key lane, but `PreviewReviewSnapshotService.ValidateSnapshot(...)` and `PreviewReviewSnapshotStore.Load(...)` still build duplicate-detection keys as `elementId + "\u001f" + field`. Canonical review ids/fields do not forbid that separator, so distinct row pairs such as `("A", "B\u001fC")` and `("A\u001fB", "C")` collide and are incorrectly rejected as duplicate rows during in-memory verification.

## Reserved scope

- `src/QS3D.Core/Review/PreviewReviewSnapshot.cs`
- focused Preview Review smoke coverage only
- this claim file

## Intended contract

Use collision-free length-prefixed row identity consistently for snapshot invariant validation and load-side duplicate detection. Preserve case-insensitive element/field identity, snapshot ordering/fingerprints, portability policy, XML shape validation, comparison behavior and all existing artifact semantics.

## Validation boundary

Focused source-safe regression + exact readback only. No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed without execution.
