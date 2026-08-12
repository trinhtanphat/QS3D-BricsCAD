# Work claim — Preview Review snapshot row-key collision safety

- Status: `ABORTED (SUPERSEDED)`
- Agent: `chatgpt-web-gpt56sol-preview-review-snapshot-row-key-20260812-1050`
- Registered: `2026-08-12T10:50:00+07:00`
- Superseded: `2026-08-12T10:58:00+07:00`
- Priority: P1 review identity / false-duplicate safety

## Original evidence

Preview Review comparison already used a length-prefixed `(ElementId, Field)` identity after the completed composite-row-key lane, while `PreviewReviewSnapshotService.ValidateSnapshot(...)` and `PreviewReviewSnapshotStore.Load(...)` still built duplicate-detection keys as `elementId + "\u001f" + field`. At claim time, canonical review ids/fields did not forbid that separator.

## Superseding change

A concurrent, already-claimed Preview Review XML-text lane changed the persisted-text invariant before this lane could safely edit the shared source file:

- this claim registration: `5711c367edc1754c993988470f904fd2bd902074`
- concurrent XML-text source: `337b0b3dc6c5c1dcb3e0f913ad4436a01bf03331`
- concurrent XML-text regression: `c3b433262e77a06d8ed1b8f2bf78cc6fec4b3a51`
- concurrent XML-text close: `9576dea47c55073d23e3cc8cb57de61fb9240f33`

`PreviewReviewSnapshotService.ComputeFingerprint(...)` now verifies every persisted text part with `XmlConvert.VerifyXmlChars`. U+001F is not XML-valid and is therefore rejected before a verified/persistable Preview Review snapshot can reach the old delimiter-based duplicate detector. The originally claimed product defect is no longer reachable under the current artifact contract, so no source change is warranted.

## Collision handling

An attempted source update was rejected with GitHub `409` because `PreviewReviewSnapshot.cs` had changed concurrently. No force update or source overwrite was performed. Re-read confirmed the concurrent change belonged to the XML-text lane above.

## Follow-up

The older `PreviewReviewCompositeRowKeySmoke.cs` still constructs U+001F-bearing snapshots and expects them to verify successfully. That test is now stale under the new XML-text contract and will be reconciled under a separate test-only claim if still unowned.

## Validation boundary

No GitHub Actions/full build or BricsCAD V25/V26 runtime PASS claimed.
